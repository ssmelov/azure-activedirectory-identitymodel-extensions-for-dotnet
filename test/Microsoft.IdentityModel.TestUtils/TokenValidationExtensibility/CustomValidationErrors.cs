﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.IdentityModel.Tokens;

#nullable enable
namespace Microsoft.IdentityModel.TestUtils
{
    #region IssuerValidationErrors
    internal class CustomIssuerValidationError : IssuerValidationError
    {
        /// <summary>
        /// A custom validation failure type.
        /// </summary>
        public static readonly ValidationFailureType CustomIssuerValidationFailureType = new IssuerValidatorFailure("CustomIssuerValidationFailureType");
        private class IssuerValidatorFailure : ValidationFailureType { internal IssuerValidatorFailure(string name) : base(name) { } }

        public CustomIssuerValidationError(
            MessageDetail messageDetail,
            Type exceptionType,
            StackFrame stackFrame,
            string? invalidIssuer,
            ValidationFailureType? validationFailureType = null,
            Exception? innerException = null)
            : base(messageDetail, exceptionType, stackFrame, invalidIssuer, validationFailureType, innerException)
        {
        }

        internal override Exception GetException()
        {
            if (ExceptionType == typeof(CustomSecurityTokenInvalidIssuerException))
            {
                var exception = new CustomSecurityTokenInvalidIssuerException(MessageDetail.Message, InnerException) { InvalidIssuer = InvalidIssuer };
                exception.SetValidationError(this);

                return exception;
            }

            return base.GetException();
        }
    }

    internal class CustomIssuerWithoutGetExceptionValidationOverrideError : IssuerValidationError
    {
        public CustomIssuerWithoutGetExceptionValidationOverrideError(MessageDetail messageDetail,
            Type exceptionType,
            StackFrame stackFrame,
            string? invalidIssuer) :
            base(messageDetail, exceptionType, stackFrame, invalidIssuer)
        {
        }
    }
    #endregion

    #region AudienceValidationErrors
    internal class CustomAudienceValidationError : AudienceValidationError
    {
        /// <summary>
        /// A custom validation failure type.
        /// </summary>
        public static readonly ValidationFailureType CustomAudienceValidationFailureType = new AudienceValidatorFailure("CustomAudienceValidationFailureType");
        private class AudienceValidatorFailure : ValidationFailureType { internal AudienceValidatorFailure(string name) : base(name) { } }

        public CustomAudienceValidationError(
            MessageDetail messageDetail,
            Type exceptionType,
            StackFrame stackFrame,
            IList<string>? tokenAudiences,
            IList<string>? validAudiences,
            ValidationFailureType? validationFailureType = null,
            Exception? innerException = null)
            : base(messageDetail, exceptionType, stackFrame, tokenAudiences, validAudiences, validationFailureType, innerException)
        {
        }

        internal override Exception GetException()
        {
            if (ExceptionType == typeof(CustomSecurityTokenInvalidAudienceException))
            {
                var exception = new CustomSecurityTokenInvalidAudienceException(MessageDetail.Message, InnerException) { InvalidAudience = Utility.SerializeAsSingleCommaDelimitedString(TokenAudiences) };
                exception.SetValidationError(this);

                return exception;
            }

            return base.GetException();
        }
    }

    internal class CustomAudienceWithoutGetExceptionValidationOverrideError : AudienceValidationError
    {
        public CustomAudienceWithoutGetExceptionValidationOverrideError(
            MessageDetail messageDetail,
            Type exceptionType,
            StackFrame stackFrame,
            IList<string>? tokenAudiences,
            IList<string>? validAudiences,
            ValidationFailureType? failureType = null,
            Exception? innerException = null) :
            base(messageDetail, exceptionType, stackFrame, tokenAudiences, validAudiences, failureType, innerException)
        {
        }
    }
    #endregion

    #region LifetimeValidationErrors
    internal class CustomLifetimeValidationError : LifetimeValidationError
    {
        /// <summary>
        /// A custom validation failure type.
        /// </summary>
        public static readonly ValidationFailureType CustomLifetimeValidationFailureType = new LifetimeValidationFailure("CustomLifetimeValidationFailureType");
        private class LifetimeValidationFailure : ValidationFailureType { internal LifetimeValidationFailure(string name) : base(name) { } }

        public CustomLifetimeValidationError(
            MessageDetail messageDetail,
            Type exceptionType,
            StackFrame stackFrame,
            DateTime? notBefore,
            DateTime? expires,
            ValidationFailureType? validationFailureType = null,
            Exception? innerException = null)
            : base(messageDetail, exceptionType, stackFrame, notBefore, expires, validationFailureType, innerException)
        {
        }

        internal override Exception GetException()
        {
            if (ExceptionType == typeof(CustomSecurityTokenInvalidLifetimeException))
            {
                var exception = new CustomSecurityTokenInvalidLifetimeException(MessageDetail.Message, InnerException) { NotBefore = NotBefore, Expires = Expires };
                exception.SetValidationError(this);

                return exception;
            }

            return base.GetException();
        }
    }

    internal class CustomLifetimeWithoutGetExceptionValidationOverrideError : LifetimeValidationError
    {
        public CustomLifetimeWithoutGetExceptionValidationOverrideError(
            MessageDetail messageDetail,
            Type exceptionType,
            StackFrame stackFrame,
            DateTime? notBefore,
            DateTime? expires,
            ValidationFailureType? validationFailureType = null,
            Exception? innerException = null)
            : base(messageDetail, exceptionType, stackFrame, notBefore, expires, validationFailureType, innerException)
        {
        }
    }
    #endregion

    // Other custom validation errors to be added here for signature validation, issuer signing key, etc.
}
#nullable restore