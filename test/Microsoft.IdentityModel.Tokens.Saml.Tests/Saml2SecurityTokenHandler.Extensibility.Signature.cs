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
namespace Microsoft.IdentityModel.Tokens.Saml2.Extensibility.Tests
{
    public partial class Saml2SecurityTokenHandlerValidateTokenAsyncTests
    {
        [Theory, MemberData(nameof(Signature_ExtensibilityTestCases), DisableDiscoveryEnumeration = true)]
        public async Task ValidateTokenAsync_SignatureValidator_Extensibility(SignatureExtensibilityTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.{nameof(ValidateTokenAsync_SignatureValidator_Extensibility)}", theoryData);
            context.IgnoreType = false;
            for (int i = 0; i < theoryData.ExtraStackFrames; i++)
                theoryData.SignatureValidationError!.AddStackFrame(new StackFrame(false));

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
                    IdentityComparer.AreValidationErrorsEqual(validationError, theoryData.SignatureValidationError, context);
                    theoryData.ExpectedException.ProcessException(validationError.GetException(), context);
                }
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<SignatureExtensibilityTheoryData> Signature_ExtensibilityTestCases
        {
            get
            {
                var theoryData = new TheoryData<SignatureExtensibilityTheoryData>();
                CallContext callContext = new CallContext();
                var utcNow = DateTime.UtcNow;
                var utcPlusOneHour = utcNow + TimeSpan.FromHours(1);

                #region return CustomSignatureValidationError
                // Test cases where delegate is overridden and return a CustomSignatureValidationError
                // CustomSignatureValidationError : SignatureValidationError, ExceptionType: SecurityTokenInvalidSignatureException
                theoryData.Add(new SignatureExtensibilityTheoryData(
                    "CustomSignatureValidatorDelegate",
                    utcNow,
                    CustomSignatureValidationDelegates.CustomSignatureValidatorDelegate,
                    extraStackFrames: 2)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidSignatureException),
                        nameof(CustomSignatureValidationDelegates.CustomSignatureValidatorDelegate)),
                    SignatureValidationError = new CustomSignatureValidationError(
                        new MessageDetail(
                            nameof(CustomSignatureValidationDelegates.CustomSignatureValidatorDelegate), null),
                        ValidationFailureType.SignatureValidationFailed,
                        typeof(SecurityTokenInvalidSignatureException),
                        new StackFrame("CustomSignatureValidationDelegates.cs", 160))
                });

                // CustomSignatureValidationError : SignatureValidationError, ExceptionType: CustomSecurityTokenInvalidSignatureException : SecurityTokenInvalidSignatureException
                theoryData.Add(new SignatureExtensibilityTheoryData(
                    "CustomSignatureValidatorCustomExceptionDelegate",
                    utcNow,
                    CustomSignatureValidationDelegates.CustomSignatureValidatorCustomExceptionDelegate,
                    extraStackFrames: 2)
                {
                    ExpectedException = new ExpectedException(
                        typeof(CustomSecurityTokenInvalidSignatureException),
                        nameof(CustomSignatureValidationDelegates.CustomSignatureValidatorCustomExceptionDelegate)),
                    SignatureValidationError = new CustomSignatureValidationError(
                        new MessageDetail(
                            nameof(CustomSignatureValidationDelegates.CustomSignatureValidatorCustomExceptionDelegate), null),
                        ValidationFailureType.SignatureValidationFailed,
                        typeof(CustomSecurityTokenInvalidSignatureException),
                        new StackFrame("CustomSignatureValidationDelegates.cs", 175)),
                });

                // CustomSignatureValidationError : SignatureValidationError, ExceptionType: NotSupportedException : SystemException
                theoryData.Add(new SignatureExtensibilityTheoryData(
                    "CustomSignatureValidatorUnknownExceptionDelegate",
                    utcNow,
                    CustomSignatureValidationDelegates.CustomSignatureValidatorUnknownExceptionDelegate,
                    extraStackFrames: 2)
                {
                    // CustomSignatureValidationError does not handle the exception type 'NotSupportedException'
                    ExpectedException = ExpectedException.SecurityTokenException(
                        LogHelper.FormatInvariant(
                            Tokens.LogMessages.IDX10002, // "IDX10002: Unknown exception type returned. Type: '{0}'. Message: '{1}'.";
                            typeof(NotSupportedException),
                            nameof(CustomSignatureValidationDelegates.CustomSignatureValidatorUnknownExceptionDelegate))),
                    SignatureValidationError = new CustomSignatureValidationError(
                        new MessageDetail(
                            nameof(CustomSignatureValidationDelegates.CustomSignatureValidatorUnknownExceptionDelegate), null),
                        ValidationFailureType.SignatureValidationFailed,
                        typeof(NotSupportedException),
                        new StackFrame("CustomSignatureValidationDelegates.cs", 205)),
                });

                // CustomSignatureValidationError : SignatureValidationError, ExceptionType: NotSupportedException : SystemException, ValidationFailureType: CustomAudienceValidationFailureType
                theoryData.Add(new SignatureExtensibilityTheoryData(
                    "CustomSignatureValidatorCustomExceptionCustomFailureTypeDelegate",
                    utcNow,
                    CustomSignatureValidationDelegates.CustomSignatureValidatorCustomExceptionCustomFailureTypeDelegate,
                    extraStackFrames: 2)
                {
                    ExpectedException = new ExpectedException(
                        typeof(CustomSecurityTokenInvalidSignatureException),
                        nameof(CustomSignatureValidationDelegates.CustomSignatureValidatorCustomExceptionCustomFailureTypeDelegate)),
                    SignatureValidationError = new CustomSignatureValidationError(
                        new MessageDetail(
                            nameof(CustomSignatureValidationDelegates.CustomSignatureValidatorCustomExceptionCustomFailureTypeDelegate), null),
                        CustomSignatureValidationError.CustomSignatureValidationFailureType,
                        typeof(CustomSecurityTokenInvalidSignatureException),
                        new StackFrame("CustomSignatureValidationDelegates.cs", 190)),
                });
                #endregion

                #region return SignatureValidationError
                // Test cases where delegate is overridden and return an SignatureValidationError
                // SignatureValidationError : ValidationError, ExceptionType:  SecurityTokenInvalidSignatureException
                theoryData.Add(new SignatureExtensibilityTheoryData(
                    "SignatureValidatorDelegate",
                    utcNow,
                    CustomSignatureValidationDelegates.SignatureValidatorDelegate,
                    extraStackFrames: 2)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidSignatureException),
                        nameof(CustomSignatureValidationDelegates.SignatureValidatorDelegate)),
                    SignatureValidationError = new SignatureValidationError(
                        new MessageDetail(
                            nameof(CustomSignatureValidationDelegates.SignatureValidatorDelegate), null),
                        ValidationFailureType.SignatureValidationFailed,
                        typeof(SecurityTokenInvalidSignatureException),
                        new StackFrame("CustomSignatureValidationDelegates.cs", 235))
                });

                // SignatureValidationError : ValidationError, ExceptionType:  CustomSecurityTokenInvalidSignatureException : SecurityTokenInvalidSignatureException
                theoryData.Add(new SignatureExtensibilityTheoryData(
                    "SignatureValidatorCustomSignatureExceptionTypeDelegate",
                    utcNow,
                    CustomSignatureValidationDelegates.SignatureValidatorCustomSignatureExceptionTypeDelegate,
                    extraStackFrames: 2)
                {
                    // SignatureValidationError does not handle the exception type 'CustomSecurityTokenInvalidSignatureException'
                    ExpectedException = ExpectedException.SecurityTokenException(
                        LogHelper.FormatInvariant(
                            Tokens.LogMessages.IDX10002, // "IDX10002: Unknown exception type returned. Type: '{0}'. Message: '{1}'.";
                            typeof(CustomSecurityTokenInvalidSignatureException),
                            nameof(CustomSignatureValidationDelegates.SignatureValidatorCustomSignatureExceptionTypeDelegate))),
                    SignatureValidationError = new SignatureValidationError(
                        new MessageDetail(
                            nameof(CustomSignatureValidationDelegates.SignatureValidatorCustomSignatureExceptionTypeDelegate), null),
                        ValidationFailureType.SignatureValidationFailed,
                        typeof(CustomSecurityTokenInvalidSignatureException),
                        new StackFrame("CustomSignatureValidationDelegates.cs", 259))
                });

                // SignatureValidationError : ValidationError, ExceptionType:  CustomSecurityTokenException : SystemException
                theoryData.Add(new SignatureExtensibilityTheoryData(
                    "SignatureValidatorCustomExceptionTypeDelegate",
                    utcNow,
                    CustomSignatureValidationDelegates.SignatureValidatorCustomExceptionTypeDelegate,
                    extraStackFrames: 2)
                {
                    // SignatureValidationError does not handle the exception type 'CustomSecurityTokenException'
                    ExpectedException = ExpectedException.SecurityTokenException(
                        LogHelper.FormatInvariant(
                            Tokens.LogMessages.IDX10002, // "IDX10002: Unknown exception type returned. Type: '{0}'. Message: '{1}'.";
                            typeof(CustomSecurityTokenException),
                            nameof(CustomSignatureValidationDelegates.SignatureValidatorCustomExceptionTypeDelegate))),
                    SignatureValidationError = new SignatureValidationError(
                        new MessageDetail(
                            nameof(CustomSignatureValidationDelegates.SignatureValidatorCustomExceptionTypeDelegate), null),
                        ValidationFailureType.SignatureValidationFailed,
                        typeof(CustomSecurityTokenException),
                        new StackFrame("CustomSignatureValidationDelegates.cs", 274))
                });

                // SignatureValidationError : ValidationError, ExceptionType: SecurityTokenInvalidSignatureException, inner: CustomSecurityTokenInvalidSignatureException
                theoryData.Add(new SignatureExtensibilityTheoryData(
                    "SignatureValidatorThrows",
                    utcNow,
                    CustomSignatureValidationDelegates.SignatureValidatorThrows,
                    extraStackFrames: 1)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidSignatureException),
                        string.Format(Tokens.LogMessages.IDX10272),
                        typeof(CustomSecurityTokenInvalidSignatureException)),
                    SignatureValidationError = new SignatureValidationError(
                        new MessageDetail(
                            string.Format(Tokens.LogMessages.IDX10272), null),
                        ValidationFailureType.SignatureValidatorThrew,
                        typeof(SecurityTokenInvalidSignatureException),
                        new StackFrame("Saml2SecurityTokenHandler.ValidateSignature.cs", 250),
                        null, // no inner validation error
                        new SecurityTokenInvalidSignatureException(nameof(CustomSignatureValidationDelegates.SignatureValidatorThrows))
                    )
                });
                #endregion

                return theoryData;
            }
        }

        public class SignatureExtensibilityTheoryData : TheoryDataBase
        {
            internal SignatureExtensibilityTheoryData(string testId, DateTime utcNow, SignatureValidationDelegate signatureValidator, int extraStackFrames) : base(testId)
            {
                Saml2Token = (Saml2SecurityToken)Saml2SecurityTokenHandler.CreateToken(
                    new SecurityTokenDescriptor()
                    {
                        Subject = Default.SamlClaimsIdentity,
                        Issuer = Default.Issuer,
                    });

                ValidationParameters = new ValidationParameters
                {
                    AlgorithmValidator = SkipValidationDelegates.SkipAlgorithmValidation,
                    AudienceValidator = SkipValidationDelegates.SkipAudienceValidation,
                    IssuerValidatorAsync = SkipValidationDelegates.SkipIssuerValidation,
                    IssuerSigningKeyValidator = SkipValidationDelegates.SkipIssuerSigningKeyValidation,
                    LifetimeValidator = SkipValidationDelegates.SkipLifetimeValidation,
                    SignatureValidator = signatureValidator,
                    TokenReplayValidator = SkipValidationDelegates.SkipTokenReplayValidation,
                    TokenTypeValidator = SkipValidationDelegates.SkipTokenTypeValidation
                };

                ExtraStackFrames = extraStackFrames;
            }

            public Saml2SecurityToken Saml2Token { get; }

            public Saml2SecurityTokenHandler Saml2SecurityTokenHandler { get; } = new Saml2SecurityTokenHandler();

            public bool IsValid { get; set; }

            internal ValidationParameters ValidationParameters { get; }

            internal SignatureValidationError? SignatureValidationError { get; set; }

            internal int ExtraStackFrames { get; }
        }
    }
}
#nullable restore
