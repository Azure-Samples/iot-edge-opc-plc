using System;
using System.Collections.Generic;
using System.Text;

namespace OpcPlc.DeterministicAlarms.Configuration
{
    class ScriptException : Exception
    {
#nullable enable
        public ScriptException(string? message) : base(message) { }
    }
}
