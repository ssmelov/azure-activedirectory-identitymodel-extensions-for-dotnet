// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.IdentityModel.Logging
{
    internal class IdentityModelTelemetry
    {
        /// <summary>
        /// Meter name for MicrosoftIdentityModel.
        /// </summary>
        public const string MeterName = "MicrosoftIdentityModel_Meter";
        public const string ServiceName = "MicrosoftIdentityModel";

        /// <summary>
        /// The meter responsible for creating instruments.
        /// </summary>
        public static readonly Meter WilsonMeter = new(MeterName, "1.0.0");

        internal const string TotalDurationHistogramName = "IdentityModelConfigurationRequestTotalDurationInMS";

        /// <summary>
        /// Counter to capture requests to configuration manager.
        /// </summary>
        internal const string WilsonConfigurationManagerCounterName = "WilsonConfigurationManagerCounter";
        internal const string WilsonConfigurationManagerCounterDescription = "Counter capturing configuration manager operations.";
        internal static readonly Counter<long> ConfigurationManagerCounter = WilsonMeter.CreateCounter<long>(WilsonConfigurationManagerCounterName, description: WilsonConfigurationManagerCounterDescription);

        /// <summary>
        /// Histogram to capture total duration of configuration manager operations in milliseconds.
        /// </summary>
        internal static readonly Histogram<long> TotalDurationHistogram = WilsonMeter.CreateHistogram<long>(
            TotalDurationHistogramName,
            unit: "ms",
            description: "Performance of getting configuration calls total latency");

        internal static void RecordTotalDurationHistogram(long requestDurationInMs, in TagList tagList)
        {
            TotalDurationHistogram.Record(requestDurationInMs, tagList);
        }

        internal static void IncrementConfigurationManagerCounter(in TagList tagList)
        {
            ConfigurationManagerCounter.Add(1, tagList);
        }
    }
}
