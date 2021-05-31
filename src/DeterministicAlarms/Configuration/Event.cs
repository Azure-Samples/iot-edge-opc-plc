namespace OpcPlc.DeterministicAlarms.Configuration
{
    using Opc.Ua;
    using System.Collections.Generic;

    public class Event
    {
        public string AlarmId { get; set; }

        public string Reason { get; set; }

        public EventSeverity Severity { get; set; }

        public string EventId { get; set; }

        public List<StateChange> StateChanges { get; set; }
    }
}