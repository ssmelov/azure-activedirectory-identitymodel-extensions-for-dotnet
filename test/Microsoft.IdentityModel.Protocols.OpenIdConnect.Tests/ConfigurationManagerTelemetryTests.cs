// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//using System;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect.Configuration;
using Microsoft.IdentityModel.TestUtils;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Xunit;

namespace Microsoft.IdentityModel.Protocols.OpenIdConnect.Tests
{
    public class ConfigurationManagerTelemetryTests
    {
        readonly List<Metric> ExportedItems;
        readonly MeterProvider MeterProvider;

        public ConfigurationManagerTelemetryTests()
        {
            ExportedItems = new List<Metric>();

            MeterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault())
                .AddMeter(IdentityModelTelemetry.MeterName)
                .AddInMemoryExporter(ExportedItems, (options) =>
                {
                    options.PeriodicExportingMetricReaderOptions = new PeriodicExportingMetricReaderOptions
                    {
                        ExportIntervalMilliseconds = 10,
                    };
                })
                .Build();
        }

        [Theory, MemberData(nameof(GetConfiguration_ExpectedTagList_TheoryData), DisableDiscoveryEnumeration = true)]
        public async Task GetConfigurationAsync_ExpectedTagList(ConfigurationManagerTelemetryTheoryData<OpenIdConnectConfiguration> theoryData)
        {
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                theoryData.MetadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                theoryData.DocumentRetriever,
                theoryData.ConfigurationValidator);
            try
            {

                await configurationManager.GetConfigurationAsync();

                if (theoryData.SecondRequest)
                {
                    await configurationManager.GetConfigurationAsync();
                }
            }
            catch (Exception)
            {
                // Ignore exceptions
            }
            MeterProvider.ForceFlush();
            MeterProvider.Shutdown();
            MeterProvider.Dispose();

            VerifyConfigurationManagerCounter(ExportedItems, theoryData.ExpectedTagList);
            VerifyHistogramReporting(ExportedItems, theoryData.ExpectedTagList);
        }

        public static TheoryData<ConfigurationManagerTelemetryTheoryData<OpenIdConnectConfiguration>> GetConfiguration_ExpectedTagList_TheoryData()
        {
            return new TheoryData<ConfigurationManagerTelemetryTheoryData<OpenIdConnectConfiguration>>
            {
                new ConfigurationManagerTelemetryTheoryData<OpenIdConnectConfiguration>("Success-retrieve from endpoint")
                {
                    MetadataAddress = OpenIdConfigData.AccountsGoogle,
                    DocumentRetriever = new HttpDocumentRetriever(),
                    ConfigurationValidator = new OpenIdConnectConfigurationValidator(),
                    ExpectedTagList = new Dictionary<string, object>
                    {
                        { IdentityModelTelemetryUtil.MetadataAddressTag, OpenIdConfigData.AccountsGoogle },
                        { IdentityModelTelemetryUtil.RefreshReasonTag, IdentityModelTelemetryUtil.Requested },
                        { IdentityModelTelemetryUtil.OperationStatusTag, IdentityModelTelemetryUtil.Success },
                        { IdentityModelTelemetryUtil.ExceptionTypeTag, string.Empty }
                    }
                },
                new ConfigurationManagerTelemetryTheoryData<OpenIdConnectConfiguration>("Success-retrieve from cache")
                {
                    MetadataAddress = OpenIdConfigData.AADCommonUrl,
                    DocumentRetriever = new HttpDocumentRetriever(),
                    ConfigurationValidator = new OpenIdConnectConfigurationValidator(),
                    SecondRequest = true,
                    ExpectedTagList = new Dictionary<string, object>
                    {
                        { IdentityModelTelemetryUtil.MetadataAddressTag, OpenIdConfigData.AADCommonUrl },
                        { IdentityModelTelemetryUtil.RefreshReasonTag, IdentityModelTelemetryUtil.LKG },
                        { IdentityModelTelemetryUtil.OperationStatusTag, IdentityModelTelemetryUtil.Success },
                        { IdentityModelTelemetryUtil.ExceptionTypeTag, string.Empty }
                    }
                },
                new ConfigurationManagerTelemetryTheoryData<OpenIdConnectConfiguration>("Failure-invalid metadata address")
                {
                    MetadataAddress = OpenIdConfigData.HttpsBadUri,
                    DocumentRetriever = new HttpDocumentRetriever(),
                    ConfigurationValidator = new OpenIdConnectConfigurationValidator(),
                    ExpectedTagList = new Dictionary<string, object>
                    {
                        { IdentityModelTelemetryUtil.MetadataAddressTag, OpenIdConfigData.HttpsBadUri },
                        { IdentityModelTelemetryUtil.RefreshReasonTag, IdentityModelTelemetryUtil.Requested },
                        { IdentityModelTelemetryUtil.OperationStatusTag, IdentityModelTelemetryUtil.Failure },
                        { IdentityModelTelemetryUtil.ExceptionTypeTag, IdentityModelTelemetryUtil.ConfigurationRetrievalFailed }
                    }
                },
                new ConfigurationManagerTelemetryTheoryData<OpenIdConnectConfiguration>("Failure-invalid config")
                {
                    MetadataAddress = OpenIdConfigData.JsonFile,
                    DocumentRetriever = new FileDocumentRetriever(),
                    // The config being loaded has two keys; require three to force invalidity
                    ConfigurationValidator = new OpenIdConnectConfigurationValidator() { MinimumNumberOfKeys = 3 },
                    ExpectedTagList = new Dictionary<string, object>
                    {
                        { IdentityModelTelemetryUtil.MetadataAddressTag, OpenIdConfigData.JsonFile },
                        { IdentityModelTelemetryUtil.RefreshReasonTag, IdentityModelTelemetryUtil.Requested },
                        { IdentityModelTelemetryUtil.OperationStatusTag, IdentityModelTelemetryUtil.Failure },
                        { IdentityModelTelemetryUtil.ExceptionTypeTag, IdentityModelTelemetryUtil.ConfigurationInvalid }
                    }
                },
            };
        }

        private void VerifyConfigurationManagerCounter(List<Metric> exportedMetrics, Dictionary<string, object> expectedTagList)
        {
            var expectedTagsFound = false;
            foreach (Metric metric in exportedMetrics)
            {
                if (!metric.Name.Equals(IdentityModelTelemetry.WilsonConfigurationManagerCounterName))
                    continue;

                foreach (MetricPoint metricPoint in metric.GetMetricPoints())
                {
                    if (MatchTagValues(metricPoint, expectedTagList))
                        expectedTagsFound = true;
                }
            }
            Assert.True(expectedTagsFound);
        }

        private void VerifyHistogramReporting(List<Metric> exportedMetrics, Dictionary<string, object> expectedTagList)
        {
            var histogramMetricFound = false;
            foreach (Metric metric in exportedMetrics)
            {
                if (!metric.Name.Equals(IdentityModelTelemetry.TotalDurationHistogramName))
                    continue;

                Assert.Equal(MetricType.Histogram, metric.MetricType);
                foreach (var metricPoint in metric.GetMetricPoints())
                {
                    if (MatchTagValues(metricPoint, expectedTagList))
                        histogramMetricFound = true;
                }
            }
            Assert.True(histogramMetricFound);
        }

        private bool MatchTagValues(MetricPoint metricPoint, Dictionary<string, object> expectedTagList)
        {
            foreach (var expectedTag in expectedTagList.Keys)
            {
                Dictionary<string, object> tags = [];
                foreach (var tag in metricPoint.Tags)
                {
                    tags[tag.Key] = tag.Value?.ToString() ?? "null";
                }

                if (!tags.ContainsKey(expectedTag))
                    return false;

                if (tags[expectedTag] != expectedTagList[expectedTag])
                    return false;
            }

            return true;
        }
    }

    public class ConfigurationManagerTelemetryTheoryData<T> : TheoryDataBase where T : class
    {
        public ConfigurationManagerTelemetryTheoryData(string testId) : base(testId) { }

        public string MetadataAddress { get; set; }

        public IDocumentRetriever DocumentRetriever { get; set; }

        public IConfigurationValidator<T> ConfigurationValidator { get; set; }

        public bool SecondRequest { get; set; } = false;

        public Dictionary<string, object> ExpectedTagList { get; set; }
    }
}
