// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.TestUtils;

using Xunit;

#nullable enable
namespace Microsoft.IdentityModel.Tokens.Saml.Extensibility.Tests
{
    public partial class SamlSecurityTokenHandlerValidateTokenAsyncTests
    {
        [Theory, MemberData(nameof(TokenReplay_ExtensibilityTestCases), DisableDiscoveryEnumeration = true)]
        public async Task ValidateTokenAsync_TokenReplayValidator_Extensibility(TokenReplayExtensibilityTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.{nameof(ValidateTokenAsync_TokenReplayValidator_Extensibility)}", theoryData);
            context.IgnoreType = false;
            for (int i = 0; i < theoryData.ExtraStackFrames; i++)
                theoryData.TokenReplayValidationError!.AddStackFrame(new StackFrame(false));

            try
            {
                ValidationResult<ValidatedToken> validationResult = await theoryData.SamlSecurityTokenHandler.ValidateTokenAsync(
                    theoryData.SamlToken!,
                    theoryData.ValidationParameters!,
                    theoryData.CallContext,
                    CancellationToken.None);

                if (validationResult.IsValid)
                {
                    context.Diffs.Add("validationResult.IsValid is true, expected false");
                }
                else
                {
                    ValidationError validationError = validationResult.UnwrapError();
                    IdentityComparer.AreValidationErrorsEqual(validationError, theoryData.TokenReplayValidationError, context);
                    theoryData.ExpectedException.ProcessException(validationError.GetException(), context);
                }
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<TokenReplayExtensibilityTheoryData> TokenReplay_ExtensibilityTestCases
        {
            get
            {
                var theoryData = new TheoryData<TokenReplayExtensibilityTheoryData>();
                CallContext callContext = new CallContext();
                var utcNow = DateTime.UtcNow;
                var expirationTime = utcNow + TimeSpan.FromHours(1);

                #region return CustomTokenReplayValidationError
                // Test cases where delegate is overridden and return a CustomTokenReplayValidationError
                // CustomTokenReplayValidationError : TokenReplayValidationError, ExceptionType: SecurityTokenReplayDetectedException
                theoryData.Add(new TokenReplayExtensibilityTheoryData(
                    "CustomTokenReplayValidationDelegate",
                    utcNow,
                    CustomTokenReplayValidationDelegates.CustomTokenReplayValidationDelegate,
                    extraStackFrames: 1)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenReplayDetectedException),
                        nameof(CustomTokenReplayValidationDelegates.CustomTokenReplayValidationDelegate)),
                    TokenReplayValidationError = new CustomTokenReplayValidationError(
                        new MessageDetail(
                            nameof(CustomTokenReplayValidationDelegates.CustomTokenReplayValidationDelegate), null),
                        ValidationFailureType.TokenReplayValidationFailed,
                        typeof(SecurityTokenReplayDetectedException),
                        new StackFrame("CustomTokenReplayValidationDelegates.cs", 160),
                        expirationTime)
                });

                // CustomTokenReplayValidationError : TokenReplayValidationError, ExceptionType: CustomSecurityTokenReplayDetectedException : SecurityTokenReplayDetectedException
                theoryData.Add(new TokenReplayExtensibilityTheoryData(
                    "CustomTokenReplayValidatorCustomExceptionDelegate",
                    utcNow,
                    CustomTokenReplayValidationDelegates.CustomTokenReplayValidatorCustomExceptionDelegate,
                    extraStackFrames: 1)
                {
                    ExpectedException = new ExpectedException(
                        typeof(CustomSecurityTokenReplayDetectedException),
                        nameof(CustomTokenReplayValidationDelegates.CustomTokenReplayValidatorCustomExceptionDelegate)),
                    TokenReplayValidationError = new CustomTokenReplayValidationError(
                        new MessageDetail(
                            nameof(CustomTokenReplayValidationDelegates.CustomTokenReplayValidatorCustomExceptionDelegate), null),
                        ValidationFailureType.TokenReplayValidationFailed,
                        typeof(CustomSecurityTokenReplayDetectedException),
                        new StackFrame("CustomTokenReplayValidationDelegates.cs", 175),
                        expirationTime),
                });

                // CustomTokenReplayValidationError : TokenReplayValidationError, ExceptionType: NotSupportedException : SystemException
                theoryData.Add(new TokenReplayExtensibilityTheoryData(
                    "CustomTokenReplayValidatorUnknownExceptionDelegate",
                    utcNow,
                    CustomTokenReplayValidationDelegates.CustomTokenReplayValidatorUnknownExceptionDelegate,
                    extraStackFrames: 1)
                {
                    // CustomTokenReplayValidationError does not handle the exception type 'NotSupportedException'
                    ExpectedException = ExpectedException.SecurityTokenException(
                        LogHelper.FormatInvariant(
                            Tokens.LogMessages.IDX10002, // "IDX10002: Unknown exception type returned. Type: '{0}'. Message: '{1}'.";
                            typeof(NotSupportedException),
                            nameof(CustomTokenReplayValidationDelegates.CustomTokenReplayValidatorUnknownExceptionDelegate))),
                    TokenReplayValidationError = new CustomTokenReplayValidationError(
                        new MessageDetail(
                            nameof(CustomTokenReplayValidationDelegates.CustomTokenReplayValidatorUnknownExceptionDelegate), null),
                        ValidationFailureType.TokenReplayValidationFailed,
                        typeof(NotSupportedException),
                        new StackFrame("CustomTokenReplayValidationDelegates.cs", 205),
                        expirationTime),
                });

                // CustomTokenReplayValidationError : TokenReplayValidationError, ExceptionType: NotSupportedException : SystemException, ValidationFailureType: CustomAudienceValidationFailureType
                theoryData.Add(new TokenReplayExtensibilityTheoryData(
                    "CustomTokenReplayValidatorCustomExceptionCustomFailureTypeDelegate",
                    utcNow,
                    CustomTokenReplayValidationDelegates.CustomTokenReplayValidatorCustomExceptionCustomFailureTypeDelegate,
                    extraStackFrames: 1)
                {
                    ExpectedException = new ExpectedException(
                        typeof(CustomSecurityTokenReplayDetectedException),
                        nameof(CustomTokenReplayValidationDelegates.CustomTokenReplayValidatorCustomExceptionCustomFailureTypeDelegate)),
                    TokenReplayValidationError = new CustomTokenReplayValidationError(
                        new MessageDetail(
                            nameof(CustomTokenReplayValidationDelegates.CustomTokenReplayValidatorCustomExceptionCustomFailureTypeDelegate), null),
                        CustomTokenReplayValidationError.CustomTokenReplayValidationFailureType,
                        typeof(CustomSecurityTokenReplayDetectedException),
                        new StackFrame("CustomTokenReplayValidationDelegates.cs", 190),
                        expirationTime),
                });
                #endregion

                #region return TokenReplayValidationError
                // Test cases where delegate is overridden and return an TokenReplayValidationError
                // TokenReplayValidationError : ValidationError, ExceptionType:  SecurityTokenReplayDetectedException
                theoryData.Add(new TokenReplayExtensibilityTheoryData(
                    "TokenReplayValidationDelegate",
                    utcNow,
                    CustomTokenReplayValidationDelegates.TokenReplayValidationDelegate,
                    extraStackFrames: 1)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenReplayDetectedException),
                        nameof(CustomTokenReplayValidationDelegates.TokenReplayValidationDelegate)),
                    TokenReplayValidationError = new TokenReplayValidationError(
                        new MessageDetail(
                            nameof(CustomTokenReplayValidationDelegates.TokenReplayValidationDelegate), null),
                        ValidationFailureType.TokenReplayValidationFailed,
                        typeof(SecurityTokenReplayDetectedException),
                        new StackFrame("CustomTokenReplayValidationDelegates.cs", 235),
                        expirationTime)
                });

                // TokenReplayValidationError : ValidationError, ExceptionType:  CustomSecurityTokenReplayDetectedException : SecurityTokenReplayDetectedException
                theoryData.Add(new TokenReplayExtensibilityTheoryData(
                    "TokenReplayValidatorCustomTokenReplayDetectedExceptionTypeDelegate",
                    utcNow,
                    CustomTokenReplayValidationDelegates.TokenReplayValidatorCustomTokenReplayDetectedExceptionTypeDelegate,
                    extraStackFrames: 1)
                {
                    // TokenReplayValidationError does not handle the exception type 'CustomSecurityTokenReplayDetectedException'
                    ExpectedException = ExpectedException.SecurityTokenException(
                        LogHelper.FormatInvariant(
                            Tokens.LogMessages.IDX10002, // "IDX10002: Unknown exception type returned. Type: '{0}'. Message: '{1}'.";
                            typeof(CustomSecurityTokenReplayDetectedException),
                            nameof(CustomTokenReplayValidationDelegates.TokenReplayValidatorCustomTokenReplayDetectedExceptionTypeDelegate))),
                    TokenReplayValidationError = new TokenReplayValidationError(
                        new MessageDetail(
                            nameof(CustomTokenReplayValidationDelegates.TokenReplayValidatorCustomTokenReplayDetectedExceptionTypeDelegate), null),
                        ValidationFailureType.TokenReplayValidationFailed,
                        typeof(CustomSecurityTokenReplayDetectedException),
                        new StackFrame("CustomTokenReplayValidationDelegates.cs", 259),
                        expirationTime)
                });

                // TokenReplayValidationError : ValidationError, ExceptionType:  CustomSecurityTokenException : SystemException
                theoryData.Add(new TokenReplayExtensibilityTheoryData(
                    "TokenReplayValidatorCustomExceptionTypeDelegate",
                    utcNow,
                    CustomTokenReplayValidationDelegates.TokenReplayValidatorCustomExceptionTypeDelegate,
                    extraStackFrames: 1)
                {
                    // TokenReplayValidationError does not handle the exception type 'CustomSecurityTokenException'
                    ExpectedException = ExpectedException.SecurityTokenException(
                        LogHelper.FormatInvariant(
                            Tokens.LogMessages.IDX10002, // "IDX10002: Unknown exception type returned. Type: '{0}'. Message: '{1}'.";
                            typeof(CustomSecurityTokenException),
                            nameof(CustomTokenReplayValidationDelegates.TokenReplayValidatorCustomExceptionTypeDelegate))),
                    TokenReplayValidationError = new TokenReplayValidationError(
                        new MessageDetail(
                            nameof(CustomTokenReplayValidationDelegates.TokenReplayValidatorCustomExceptionTypeDelegate), null),
                        ValidationFailureType.TokenReplayValidationFailed,
                        typeof(CustomSecurityTokenException),
                        new StackFrame("CustomTokenReplayValidationDelegates.cs", 274),
                        expirationTime)
                });

                // TokenReplayValidationError : ValidationError, ExceptionType: SecurityTokenReplayDetectedException, inner: CustomSecurityTokenReplayDetectedException
                theoryData.Add(new TokenReplayExtensibilityTheoryData(
                    "TokenReplayValidatorThrows",
                    utcNow,
                    CustomTokenReplayValidationDelegates.TokenReplayValidatorThrows,
                    extraStackFrames: 0)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenReplayDetectedException),
                        string.Format(Tokens.LogMessages.IDX10276),
                        typeof(CustomSecurityTokenReplayDetectedException)),
                    TokenReplayValidationError = new TokenReplayValidationError(
                        new MessageDetail(
                            string.Format(Tokens.LogMessages.IDX10276), null),
                        ValidationFailureType.TokenReplayValidatorThrew,
                        typeof(SecurityTokenReplayDetectedException),
                        new StackFrame("SamlSecurityTokenHandler.ValidateToken.Internal.cs", 250),
                        expirationTime,
                        new SecurityTokenReplayDetectedException(nameof(CustomTokenReplayValidationDelegates.TokenReplayValidatorThrows))
                    )
                });
                #endregion

                return theoryData;
            }
        }

        public class TokenReplayExtensibilityTheoryData : TheoryDataBase
        {
            internal TokenReplayExtensibilityTheoryData(string testId, DateTime utcNow, TokenReplayValidationDelegate tokenReplayValidator, int extraStackFrames) : base(testId)
            {
                SamlToken = (SamlSecurityToken)SamlSecurityTokenHandler.CreateToken(
                    new SecurityTokenDescriptor()
                    {
                        Subject = Default.SamlClaimsIdentity,
                        Issuer = Default.Issuer,
                        IssuedAt = utcNow,
                        NotBefore = utcNow,
                        Expires = utcNow + TimeSpan.FromHours(1),
                    });

                ValidationParameters = new ValidationParameters
                {
                    AlgorithmValidator = SkipValidationDelegates.SkipAlgorithmValidation,
                    AudienceValidator = SkipValidationDelegates.SkipAudienceValidation,
                    IssuerValidatorAsync = SkipValidationDelegates.SkipIssuerValidation,
                    IssuerSigningKeyValidator = SkipValidationDelegates.SkipIssuerSigningKeyValidation,
                    LifetimeValidator = SkipValidationDelegates.SkipLifetimeValidation,
                    SignatureValidator = SkipValidationDelegates.SkipSignatureValidation,
                    TokenReplayValidator = tokenReplayValidator,
                    TokenTypeValidator = SkipValidationDelegates.SkipTokenTypeValidation
                };

                ExtraStackFrames = extraStackFrames;
            }

            public SamlSecurityToken SamlToken { get; }

            public SamlSecurityTokenHandler SamlSecurityTokenHandler { get; } = new SamlSecurityTokenHandler();

            public bool IsValid { get; set; }

            internal ValidationParameters? ValidationParameters { get; set; }

            internal TokenReplayValidationError? TokenReplayValidationError { get; set; }

            internal int ExtraStackFrames { get; }
        }
    }
}
#nullable restore
