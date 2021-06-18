namespace OpcPlc.DeterministicAlarms.Configuration
{
    using System.Collections.Generic;

    public class Script
    {
        public int WaitUntilStartInSeconds { get; set; }

        public bool IsScriptInRepeatingLoop { get; set; }

        public int RunningForSeconds { get; set; }

        public List<Step> Steps { get; set; }
    }
}