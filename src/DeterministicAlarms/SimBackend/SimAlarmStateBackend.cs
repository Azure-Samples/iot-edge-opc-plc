namespace OpcPlc.DeterministicAlarms.SimBackend
{
    using Opc.Ua;
    using OpcPlc.DeterministicAlarms.Configuration;
    using OpcPlc.DeterministicAlarms.Model;
    using System;

    public class SimAlarmStateBackend
    {
        public string Name { get; internal set; }

        public AlarmObjectStates AlarmType { get; set; }

        public DateTime Time { get; internal set; }

        public string Reason { get; internal set; }

        public SimConditionStatesEnum State { get; internal set; }

        public LocalizedText Comment { get; internal set; }

        public string UserName { get; internal set; }

        public EventSeverity Severity { get; internal set; }

        public DateTime EnableTime { get; internal set; }

        public DateTime ActiveTime { get; internal set; }

        public SimSourceNodeBackend Source { get; internal set; }

        public string Id { get; internal set; }

        internal SimAlarmStateBackend CreateSnapshot()
        {
            return (SimAlarmStateBackend)MemberwiseClone();
        }

        internal bool SetStateBits(SimConditionStatesEnum bits, bool isSet)
        {
            if (isSet)
            {
                bool currentlySet = ((this.State & bits) == bits);
                this.State |= bits;
                return !currentlySet;
            }

            bool currentlyCleared = ((this.State & ~bits) == this.State);
            this.State &= ~bits;
            return !currentlyCleared;
        }
    }
}