namespace OpcPlc.DeterministicAlarms.Configuration
{
    public class Step
    {
        public @Event Event { get; set; }

        public int SleepInSeconds { get; set; }
    }
}