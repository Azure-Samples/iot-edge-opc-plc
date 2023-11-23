// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.Logging;

using Microsoft.Extensions.Logging.Console;

/// <summary>
/// Options for <see cref="SyslogFormatter"/> log formatter.
/// </summary>
public sealed class SyslogFormatterOptions : ConsoleFormatterOptions
{
    /// <summary>
    /// The default timestamp format for all IoT compatible logging events..
    /// </summary>
    public static readonly string DefaultTimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";

    /// <summary>
    /// Initializes a new instance of the <see cref="SyslogFormatterOptions"/> class.
    /// </summary>
    public SyslogFormatterOptions()
    {
        ServiceId = "opcua@311";
        UseUtcTimestamp = true;
        IncludeScopes = true;
        TimestampFormat = DefaultTimestampFormat;
    }

    /// <summary>
    /// Gets the service id which is added to the syslog output, e.g. 'service@311'.
    /// see https://www.iana.org/assignments/enterprise-numbers/?q=microsoft for enterprise ids.
    /// </summary>
    public string ServiceId { get; }
}
