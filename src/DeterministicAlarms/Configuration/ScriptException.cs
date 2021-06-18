namespace OpcPlc.DeterministicAlarms.Configuration
{
    using System;

    class ScriptException : Exception
    {
#nullable enable
        public ScriptException(string? message) : base(message)
        {
        }
    }
}