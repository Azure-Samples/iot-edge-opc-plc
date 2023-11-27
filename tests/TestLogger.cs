// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.Tests;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using System;
using System.IO;

public class TestLogger<T> : ILogger<T>
{
    private readonly TextWriter _outputWriter;
    private readonly ConsoleFormatter _formatter;
    private readonly string _category = typeof(T).FullName;

    public TestLogger(TextWriter outputWriter, ConsoleFormatter formatter)
    {
        _outputWriter = outputWriter;
        _formatter = formatter;
    }

    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Debug;

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= MinimumLogLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (logLevel < MinimumLogLevel)
        {
            return;
        }

        _formatter.Write(new LogEntry<TState>(logLevel, _category, eventId, state, exception, formatter), null, _outputWriter);
    }
}
