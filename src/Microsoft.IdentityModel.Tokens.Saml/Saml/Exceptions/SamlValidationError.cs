// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

#nullable enable
namespace Microsoft.IdentityModel.Tokens.Saml
{
    internal class SamlValidationError : ValidationError
    {
        internal SamlValidationError(
            MessageDetail messageDetail,
            Type exceptionType,
            StackFrame stackFrame,
            ValidationFailureType failureType,
            Exception? innerException = null)
            : base(messageDetail, exceptionType, stackFrame, failureType, innerException)
        {
        }

        internal override Exception GetException()
        {
            if (ExceptionType == typeof(SamlSecurityTokenReadException))
            {
                var exception = new SamlSecurityTokenReadException(MessageDetail.Message, InnerException);
                return exception;
            }

            return base.GetException();
        }
    }
}
#nullable restore
