// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.Logging;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>
/// Logging formatter compatible with syslogs format.
/// </summary>
public sealed class SyslogFormatter : ConsoleFormatter, IDisposable
{
    private const int _initialLength = 256;

    /// <summary>
    /// Map of <see cref="LogLevel"/> to syslog severity.
    /// </summary>
    private static readonly string[] _syslogMap = new string[] {
        /* Trace */ "<7>",
        /* Debug */ "<7>",
        /* Info  */ "<6>",
        /* Warn */  "<4>",
        /* Error */ "<3>",
        /* Crit  */ "<3>",
        };

    private readonly IDisposable _optionsReloadToken;

    private string _timestampFormat;
    private string _serviceId;
    private bool _includeScopes;

    private SyslogFormatterOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyslogFormatter"/> class.
    /// </summary>
    public SyslogFormatter(IOptionsMonitor<SyslogFormatterOptions> options)
        : base(nameof(SyslogFormatter))
    {
        _optionsReloadToken = options.OnChange(opt =>
        {
            _options = opt;
            _serviceId = opt.ServiceId;
            _timestampFormat = opt.TimestampFormat ?? SyslogFormatterOptions.DefaultTimestampFormat;
            _includeScopes = opt.IncludeScopes;
        });
        _options = options.CurrentValue;
        _serviceId = _options.ServiceId;
        _timestampFormat = _options.TimestampFormat ?? SyslogFormatterOptions.DefaultTimestampFormat;
        _includeScopes = _options.IncludeScopes;
    }

    /// <inheritdoc/>
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider scopeProvider,
        TextWriter textWriter)
    {
        string message =
            logEntry.Formatter?.Invoke(
                logEntry.State, logEntry.Exception);

        if (message is null)
        {
            return;
        }

        var messageBuilder = new StringBuilder(_initialLength);
        messageBuilder.Append(_syslogMap[(int)logEntry.LogLevel]);
        messageBuilder.Append(DateTime.UtcNow.ToString(_timestampFormat, CultureInfo.InvariantCulture));

        if (_includeScopes && scopeProvider != null && !string.IsNullOrEmpty(_serviceId))
        {
            bool structuredData = false;
            scopeProvider.ForEachScope(
                (scope, state) =>
                {
                    StringBuilder builder = state;
                    if (!structuredData)
                    {
                        messageBuilder.Append('[');
                        messageBuilder.Append(_serviceId);
                        structuredData = true;
                    }

                    builder.Append(' ').Append(scope);
                },
                messageBuilder);
            if (structuredData)
            {
                messageBuilder.Append("] ");
            }
        }

        messageBuilder.Append("- ");
        messageBuilder.AppendLine(message);

        if (logEntry.Exception != null)
        {
            // TODO: syslog format does not support stack traces
            messageBuilder.AppendLine(logEntry.Exception.ToString());
        }

        textWriter.Write(messageBuilder.ToString());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _optionsReloadToken?.Dispose();
    }
}
