namespace OpcPlc.DeterministicAlarms.SimBackend
{
    using OpcPlc.DeterministicAlarms.Configuration;
    using System;
    using System.Collections.Generic;

    public class SimSourceNodeBackend
    {
        public Dictionary<string, SimAlarmStateBackend> Alarms { get; set; } = new Dictionary<string, SimAlarmStateBackend>();

        public string Name { get; internal set; }

        public AlarmChangedEventHandler OnAlarmChanged;

        public delegate void AlarmChangedEventHandler(SimAlarmStateBackend alarm);

        public void CreateAlarms(List<Alarm> alarmsFromConfiguration)
        {
            foreach (var alarmConfiguration in alarmsFromConfiguration)
            {
                var alarmStateBackend = new SimAlarmStateBackend
                {
                    Source = this,
                    Id = alarmConfiguration.Id,
                    Name = alarmConfiguration.Name,
                    Time = DateTime.UtcNow,
                    AlarmType = alarmConfiguration.ObjectType
                };

                lock (Alarms)
                {
                    Alarms.Add(alarmConfiguration.Id, alarmStateBackend);
                }
            }
        }

        internal void Refresh()
        {
            var snapshots = new List<SimAlarmStateBackend>();

            lock (Alarms)
            {
                foreach (var alarm in Alarms.Values)
                {
                    snapshots.Add(alarm.CreateSnapshot());
                }
            }

            foreach (var snapshotAlarm in snapshots)
            {
                OnAlarmChanged!.Invoke(snapshotAlarm);
            }
        }
    }
}