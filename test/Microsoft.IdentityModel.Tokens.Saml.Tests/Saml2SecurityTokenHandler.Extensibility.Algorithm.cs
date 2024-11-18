﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.TestUtils;

using Xunit;

#nullable enable
namespace Microsoft.IdentityModel.Tokens.Saml2.Extensibility.Tests
{
    public partial class Saml2SecurityTokenHandlerValidateTokenAsyncTests
    {
        [Theory, MemberData(nameof(Algorithm_ExtensibilityTestCases), DisableDiscoveryEnumeration = true)]
        public async Task ValidateTokenAsync_AlgorithmValidator_Extensibility(AlgorithmExtensibilityTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.{nameof(ValidateTokenAsync_AlgorithmValidator_Extensibility)}", theoryData);
            context.IgnoreType = false;
            for (int i = 0; i < theoryData.ExtraStackFrames; i++)
                theoryData.ValidationError!.AddStackFrame(new StackFrame(false));

            Saml2SecurityToken saml2Token = (Saml2SecurityToken)theoryData.Saml2SecurityTokenHandler.CreateToken(new SecurityTokenDescriptor()
            {
                Issuer = Default.Issuer,
                Subject = Default.SamlClaimsIdentity,
                SigningCredentials = KeyingMaterial.DefaultX509SigningCreds_2048_RsaSha2_Sha2,
            });
            theoryData.Saml2Token = theoryData.Saml2SecurityTokenHandler.ReadSaml2Token(saml2Token.Assertion.CanonicalString);

            theoryData.ValidationParameters!.IssuerSigningKeys.Add(theoryData.SigningKey);

            try
            {
                ValidationResult<ValidatedToken> validationResult = await theoryData.Saml2SecurityTokenHandler.ValidateTokenAsync(
                    theoryData.Saml2Token!,
                    theoryData.ValidationParameters!,
                    theoryData.CallContext,
                    CancellationToken.None);

                if (validationResult.IsValid)
                {
                    // We expect the validation to fail, but it passed
                    context.AddDiff("ValidationResult is Valid.");
                }
                else
                {
                    ValidationError validationError = validationResult.UnwrapError();

                    if (validationError is SignatureValidationError signatureValidationError &&
                        signatureValidationError.InnerValidationError is not null)
                    {
                        IdentityComparer.AreValidationErrorsEqual(
                            signatureValidationError.InnerValidationError,
                            theoryData.ValidationError,
                            context);
                    }
                    else
                    {
                        IdentityComparer.AreValidationErrorsEqual(
                            validationError,
                            theoryData.ValidationError,
                            context);
                    }

                    var exception = validationError.GetException();
                    theoryData.ExpectedException.ProcessException(exception, context);
                    // Process the inner exception since invalid algorithm exceptions are wrapped inside
                    // invalid signature exceptions
                    if (theoryData.ExpectedInnerException is not null)
                        theoryData.ExpectedInnerException.ProcessException(exception.InnerException, context);
                }
            }
            catch (Exception ex)
            {
                // We expect the validation to fail, but it threw an exception
                context.AddDiff($"ValidateTokenAsync threw exception: {ex}");
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<AlgorithmExtensibilityTheoryData> Algorithm_ExtensibilityTestCases
        {
            get
            {
                var theoryData = new TheoryData<AlgorithmExtensibilityTheoryData>();
                CallContext callContext = new CallContext();
                var utcNow = DateTime.UtcNow;
                var utcPlusOneHour = utcNow + TimeSpan.FromHours(1);

                #region return CustomAlgorithmValidationError
                // Test cases where delegate is overridden and return a CustomAlgorithmValidationError
                // CustomAlgorithmValidationError : AlgorithmValidationError, ExceptionType: SecurityTokenInvalidAlgorithmException
                theoryData.Add(new AlgorithmExtensibilityTheoryData(
                    "CustomAlgorithmValidatorDelegate",
                    utcNow,
                    CustomAlgorithmValidationDelegates.CustomAlgorithmValidatorDelegate,
                    extraStackFrames: 1)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidSignatureException),
                        "IDX10518:",
                        typeof(SecurityTokenInvalidAlgorithmException)),
                    ExpectedInnerException = new ExpectedException(
                        typeof(SecurityTokenInvalidAlgorithmException),
                        nameof(CustomAlgorithmValidationDelegates.CustomAlgorithmValidatorDelegate)),
                    ValidationError = new CustomAlgorithmValidationError(
                        new MessageDetail(
                            nameof(CustomAlgorithmValidationDelegates.CustomAlgorithmValidatorDelegate), null),
                        typeof(SecurityTokenInvalidAlgorithmException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 160),
                        "algorithm",
                        validationFailureType: ValidationFailureType.AlgorithmValidationFailed)
                });

                // CustomAlgorithmValidationError : AlgorithmValidationError, ExceptionType: CustomSecurityTokenInvalidAlgorithmException : SecurityTokenInvalidAlgorithmException
                theoryData.Add(new AlgorithmExtensibilityTheoryData(
                    "CustomAlgorithmValidatorCustomExceptionDelegate",
                    utcNow,
                    CustomAlgorithmValidationDelegates.CustomAlgorithmValidatorCustomExceptionDelegate,
                    extraStackFrames: 1)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidSignatureException),
                        "IDX10518:",
                        typeof(CustomSecurityTokenInvalidAlgorithmException)),
                    ExpectedInnerException = new ExpectedException(
                        typeof(CustomSecurityTokenInvalidAlgorithmException),
                        nameof(CustomAlgorithmValidationDelegates.CustomAlgorithmValidatorCustomExceptionDelegate)),
                    ValidationError = new CustomAlgorithmValidationError(
                        new MessageDetail(
                            nameof(CustomAlgorithmValidationDelegates.CustomAlgorithmValidatorCustomExceptionDelegate), null),
                        typeof(CustomSecurityTokenInvalidAlgorithmException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 175),
                        "algorithm",
                        validationFailureType: ValidationFailureType.AlgorithmValidationFailed),
                });

                // CustomAlgorithmValidationError : AlgorithmValidationError, ExceptionType: NotSupportedException : SystemException
                theoryData.Add(new AlgorithmExtensibilityTheoryData(
                    "CustomAlgorithmValidatorUnknownExceptionDelegate",
                    utcNow,
                    CustomAlgorithmValidationDelegates.CustomAlgorithmValidatorUnknownExceptionDelegate,
                    extraStackFrames: 1)
                {
                    // CustomAlgorithmValidationError does not handle the exception type 'NotSupportedException'
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidSignatureException),
                        "IDX10518:",
                        typeof(SecurityTokenException)),
                    ExpectedInnerException = ExpectedException.SecurityTokenException(
                        LogHelper.FormatInvariant(
                            Tokens.LogMessages.IDX10002, // "IDX10002: Unknown exception type returned. Type: '{0}'. Message: '{1}'.";
                            typeof(NotSupportedException),
                            nameof(CustomAlgorithmValidationDelegates.CustomAlgorithmValidatorUnknownExceptionDelegate))),
                    ValidationError = new CustomAlgorithmValidationError(
                        new MessageDetail(
                            nameof(CustomAlgorithmValidationDelegates.CustomAlgorithmValidatorUnknownExceptionDelegate), null),
                        typeof(NotSupportedException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 205),
                        "algorithm",
                        validationFailureType: ValidationFailureType.AlgorithmValidationFailed),
                });

                // CustomAlgorithmValidationError : AlgorithmValidationError, ExceptionType: NotSupportedException : SystemException, ValidationFailureType: CustomAudienceValidationFailureType
                theoryData.Add(new AlgorithmExtensibilityTheoryData(
                    "CustomAlgorithmValidatorCustomExceptionCustomFailureTypeDelegate",
                    utcNow,
                    CustomAlgorithmValidationDelegates.CustomAlgorithmValidatorCustomExceptionCustomFailureTypeDelegate,
                    extraStackFrames: 1)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidSignatureException),
                        "IDX10518:",
                        typeof(CustomSecurityTokenInvalidAlgorithmException)),
                    ExpectedInnerException = new ExpectedException(
                        typeof(CustomSecurityTokenInvalidAlgorithmException),
                        nameof(CustomAlgorithmValidationDelegates.CustomAlgorithmValidatorCustomExceptionCustomFailureTypeDelegate)),
                    ValidationError = new CustomAlgorithmValidationError(
                        new MessageDetail(
                            nameof(CustomAlgorithmValidationDelegates.CustomAlgorithmValidatorCustomExceptionCustomFailureTypeDelegate), null),
                        typeof(CustomSecurityTokenInvalidAlgorithmException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 190),
                        "algorithm",
                        validationFailureType: CustomAlgorithmValidationError.CustomAlgorithmValidationFailureType),
                });
                #endregion

                #region return AlgorithmValidationError
                // Test cases where delegate is overridden and return an AlgorithmValidationError
                // AlgorithmValidationError : ValidationError, ExceptionType:  SecurityTokenInvalidAlgorithmException
                theoryData.Add(new AlgorithmExtensibilityTheoryData(
                    "AlgorithmValidatorDelegate",
                    utcNow,
                    CustomAlgorithmValidationDelegates.AlgorithmValidatorDelegate,
                    extraStackFrames: 1)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidSignatureException),
                        "IDX10518:",
                        typeof(SecurityTokenInvalidAlgorithmException)),
                    ExpectedInnerException = new ExpectedException(
                        typeof(SecurityTokenInvalidAlgorithmException),
                        nameof(CustomAlgorithmValidationDelegates.AlgorithmValidatorDelegate)),
                    ValidationError = new AlgorithmValidationError(
                        new MessageDetail(
                            nameof(CustomAlgorithmValidationDelegates.AlgorithmValidatorDelegate), null),
                        typeof(SecurityTokenInvalidAlgorithmException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 235),
                        "algorithm",
                        validationFailureType: ValidationFailureType.AlgorithmValidationFailed)
                });

                // AlgorithmValidationError : ValidationError, ExceptionType:  CustomSecurityTokenInvalidAlgorithmException : SecurityTokenInvalidAlgorithmException
                theoryData.Add(new AlgorithmExtensibilityTheoryData(
                    "AlgorithmValidatorCustomAlgorithmExceptionTypeDelegate",
                    utcNow,
                    CustomAlgorithmValidationDelegates.AlgorithmValidatorCustomAlgorithmExceptionTypeDelegate,
                    extraStackFrames: 1)
                {
                    // AlgorithmValidationError does not handle the exception type 'CustomSecurityTokenInvalidAlgorithmException'
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidSignatureException),
                        "IDX10518:",
                        typeof(SecurityTokenException)),
                    ExpectedInnerException = ExpectedException.SecurityTokenException(
                        LogHelper.FormatInvariant(
                            Tokens.LogMessages.IDX10002, // "IDX10002: Unknown exception type returned. Type: '{0}'. Message: '{1}'.";
                            typeof(CustomSecurityTokenInvalidAlgorithmException),
                            nameof(CustomAlgorithmValidationDelegates.AlgorithmValidatorCustomAlgorithmExceptionTypeDelegate))),
                    ValidationError = new AlgorithmValidationError(
                        new MessageDetail(
                            nameof(CustomAlgorithmValidationDelegates.AlgorithmValidatorCustomAlgorithmExceptionTypeDelegate), null),
                        typeof(CustomSecurityTokenInvalidAlgorithmException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 259),
                        "algorithm",
                        validationFailureType: ValidationFailureType.AlgorithmValidationFailed)
                });

                // AlgorithmValidationError : ValidationError, ExceptionType:  CustomSecurityTokenException : SystemException
                theoryData.Add(new AlgorithmExtensibilityTheoryData(
                    "AlgorithmValidatorCustomExceptionTypeDelegate",
                    utcNow,
                    CustomAlgorithmValidationDelegates.AlgorithmValidatorCustomExceptionTypeDelegate,
                    extraStackFrames: 1)
                {
                    // AlgorithmValidationError does not handle the exception type 'CustomSecurityTokenException'
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidSignatureException),
                        "IDX10518:",
                        typeof(SecurityTokenException)),
                    ExpectedInnerException = ExpectedException.SecurityTokenException(
                        LogHelper.FormatInvariant(
                            Tokens.LogMessages.IDX10002, // "IDX10002: Unknown exception type returned. Type: '{0}'. Message: '{1}'.";
                            typeof(CustomSecurityTokenException),
                            nameof(CustomAlgorithmValidationDelegates.AlgorithmValidatorCustomExceptionTypeDelegate))),
                    ValidationError = new AlgorithmValidationError(
                        new MessageDetail(
                            nameof(CustomAlgorithmValidationDelegates.AlgorithmValidatorCustomExceptionTypeDelegate), null),
                        typeof(CustomSecurityTokenException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 274),
                        "algorithm",
                        validationFailureType: ValidationFailureType.AlgorithmValidationFailed)
                });

                // SignatureValidationError : ValidationError, ExceptionType: SecurityTokenInvalidSignatureException, inner: CustomSecurityTokenInvalidAlgorithmException
                theoryData.Add(new AlgorithmExtensibilityTheoryData(
                    "AlgorithmValidatorThrows",
                    utcNow,
                    CustomAlgorithmValidationDelegates.AlgorithmValidatorThrows,
                    extraStackFrames: 2)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidSignatureException),
                        "IDX10273:",
                        typeof(CustomSecurityTokenInvalidAlgorithmException)),
                    ExpectedInnerException = new ExpectedException(
                        typeof(CustomSecurityTokenInvalidAlgorithmException),
                        nameof(CustomAlgorithmValidationDelegates.AlgorithmValidatorThrows)),
                    ValidationError = new SignatureValidationError(
                        new MessageDetail(
                            string.Format(Tokens.LogMessages.IDX10273), null),
                        typeof(SecurityTokenInvalidSignatureException),
                        new StackFrame("Saml2SecurityTokenHandler.ValidateSignature.cs", 250),
                        null, // no inner validation error
                        ValidationFailureType.AlgorithmValidatorThrew,
                        new CustomSecurityTokenInvalidAlgorithmException(nameof(CustomAlgorithmValidationDelegates.AlgorithmValidatorThrows), null)
                    )
                });
                #endregion

                return theoryData;
            }
        }

        public class AlgorithmExtensibilityTheoryData : TheoryDataBase
        {
            internal AlgorithmExtensibilityTheoryData(string testId, DateTime utcNow, AlgorithmValidationDelegate algorithmValidator, int extraStackFrames) : base(testId)
            {
                ValidationParameters = new ValidationParameters
                {
                    // We leave the default signature validator to call the custom algorithm validator
                    AlgorithmValidator = algorithmValidator,
                    AudienceValidator = SkipValidationDelegates.SkipAudienceValidation,
                    IssuerValidatorAsync = SkipValidationDelegates.SkipIssuerValidation,
                    IssuerSigningKeyValidator = SkipValidationDelegates.SkipIssuerSigningKeyValidation,
                    LifetimeValidator = SkipValidationDelegates.SkipLifetimeValidation,
                    TokenReplayValidator = SkipValidationDelegates.SkipTokenReplayValidation,
                    TokenTypeValidator = SkipValidationDelegates.SkipTokenTypeValidation,
                };

                ExtraStackFrames = extraStackFrames;
            }

            public Saml2SecurityToken? Saml2Token { get; set; }

            public Saml2SecurityTokenHandler Saml2SecurityTokenHandler { get; } = new Saml2SecurityTokenHandler();

            public bool IsValid { get; set; }

            internal ValidationError? ValidationError { get; set; }

            public ExpectedException? ExpectedInnerException { get; set; }

            internal int ExtraStackFrames { get; }

            internal ValidationParameters ValidationParameters { get; }

            public SecurityKey SigningKey { get; set; } = KeyingMaterial.DefaultX509SigningCreds_2048_RsaSha2_Sha2.Key;
        }
    }
}
#nullable restore
