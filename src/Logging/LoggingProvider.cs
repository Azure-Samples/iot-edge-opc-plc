// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.Logging;

using Microsoft.Extensions.Logging;
using System;

/// <summary>
/// Provides utility for creating logger factory.
/// </summary>
public static class LoggingProvider
{
    /// <summary>
    /// Create ILoggerFactory object with default configuration.
    /// </summary>
    public static ILoggerFactory CreateDefaultLoggerFactory(LogLevel level)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(options => options.FormatterName = nameof(SyslogFormatter))
            .SetMinimumLevel(level)
            .AddConsoleFormatter<
                SyslogFormatter,
                SyslogFormatterOptions>();
        });

        return loggerFactory;
    }
}
