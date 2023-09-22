namespace OpcPlc.DeterministicAlarms.Configuration;

using System;

public class ScriptException : Exception
{
#nullable enable
    public ScriptException(string? message) : base(message)
    {
    }
}
