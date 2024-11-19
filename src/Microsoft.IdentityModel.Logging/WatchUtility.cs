// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.IdentityModel.Logging
{
    /// <summary>
    /// A utility class to measure time.
    /// </summary>
    internal class WatchUtility
    {
        internal static readonly Stopwatch Watch = Stopwatch.StartNew();
    }
}
