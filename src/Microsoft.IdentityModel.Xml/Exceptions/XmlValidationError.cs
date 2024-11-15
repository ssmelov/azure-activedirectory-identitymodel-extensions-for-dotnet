// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.IdentityModel.Tokens;

#nullable enable
namespace Microsoft.IdentityModel.Xml
{
    internal class XmlValidationError : ValidationError
    {
        public XmlValidationError(
            MessageDetail messageDetail,
            Type exceptionType,
            StackFrame stackFrame,
            ValidationFailureType validationFailureType,
            Exception? innerException = null) :
            base(messageDetail, exceptionType, stackFrame, validationFailureType, innerException)
        {

        }

        internal override Exception GetException()
        {
            if (ExceptionType == typeof(XmlValidationException))
            {
                XmlValidationException exception = new(MessageDetail.Message, InnerException);
                exception.SetValidationError(this);
                return exception;
            }

            return base.GetException();
        }
    }
}
#nullable restore
