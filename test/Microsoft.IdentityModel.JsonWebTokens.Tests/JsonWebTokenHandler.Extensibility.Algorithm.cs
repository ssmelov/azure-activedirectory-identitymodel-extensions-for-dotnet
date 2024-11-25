// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens.Tests;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.TestUtils;
using Microsoft.IdentityModel.Tokens;
using Xunit;

#nullable enable
namespace Microsoft.IdentityModel.JsonWebTokens.Extensibility.Tests
{
    public partial class JsonWebTokenHandlerValidateTokenAsyncTests
    {
        [Theory, MemberData(nameof(Algorithm_ExtensibilityTestCases), DisableDiscoveryEnumeration = true)]
        public async Task ValidateTokenAsync_AlgorithmValidator_Extensibility(AlgorithmExtensibilityTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.{nameof(ValidateTokenAsync_AlgorithmValidator_Extensibility)}", theoryData);
            context.IgnoreType = false;
            for (int i = 0; i < theoryData.ExtraStackFrames; i++)
                theoryData.ValidationError!.AddStackFrame(new StackFrame(false));

            theoryData.ValidationParameters!.IssuerSigningKeys.Add(theoryData.SigningKey);

            try
            {
                ValidationResult<ValidatedToken> validationResult = await theoryData.JsonWebTokenHandler.ValidateTokenAsync(
                    theoryData.JsonWebToken!,
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
                        ValidationFailureType.AlgorithmValidationFailed,
                        typeof(SecurityTokenInvalidAlgorithmException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 160),
                        "algorithm")
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
                        ValidationFailureType.AlgorithmValidationFailed,
                        typeof(CustomSecurityTokenInvalidAlgorithmException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 175),
                        "algorithm"),
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
                        ValidationFailureType.AlgorithmValidationFailed,
                        typeof(NotSupportedException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 205),
                        "algorithm"),
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
                        CustomAlgorithmValidationError.CustomAlgorithmValidationFailureType,
                        typeof(CustomSecurityTokenInvalidAlgorithmException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 190),
                        "algorithm"),
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
                        ValidationFailureType.AlgorithmValidationFailed,
                        typeof(SecurityTokenInvalidAlgorithmException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 235),
                        "algorithm")
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
                        ValidationFailureType.AlgorithmValidationFailed,
                        typeof(CustomSecurityTokenInvalidAlgorithmException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 259),
                        "algorithm")
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
                        ValidationFailureType.AlgorithmValidationFailed,
                        typeof(CustomSecurityTokenException),
                        new StackFrame("CustomAlgorithmValidationDelegates.cs", 274),
                        "algorithm")
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
                        ValidationFailureType.AlgorithmValidatorThrew,
                        typeof(SecurityTokenInvalidSignatureException),
                        new StackFrame("JsonWebTokenHandler.ValidateSignature.cs", 250),
                        null, // no inner validation error
                        new CustomSecurityTokenInvalidAlgorithmException(nameof(CustomAlgorithmValidationDelegates.AlgorithmValidatorThrows), null)
                    )
                });
                #endregion

                return theoryData;
            }
        }

        public class AlgorithmExtensibilityTheoryData : ValidateTokenAsyncBaseTheoryData
        {
            internal AlgorithmExtensibilityTheoryData(string testId, DateTime utcNow, AlgorithmValidationDelegate algorithmValidator, int extraStackFrames) : base(testId)
            {
                // The token is never read by the custom delegtes, so we create a dummy token
                JsonWebToken = JsonWebTokenHandler.ReadJsonWebToken(JsonWebTokenHandler.CreateToken(new SecurityTokenDescriptor()
                {
                    SigningCredentials = Default.SymmetricSigningCredentials,
                }));

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

            public JsonWebToken JsonWebToken { get; }

            public JsonWebTokenHandler JsonWebTokenHandler { get; } = new JsonWebTokenHandler();

            public bool IsValid { get; set; }

            internal ValidationError? ValidationError { get; set; }

            public ExpectedException? ExpectedInnerException { get; set; }

            internal int ExtraStackFrames { get; }

            public SecurityKey SigningKey { get; set; } = Default.SymmetricSigningKey;
        }
    }
}
#nullable restore
