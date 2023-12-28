namespace OpcPlc.DeterministicAlarms.Configuration;

using System;

#nullable enable
public class ScriptException(string? message) : Exception(message)
{
}
