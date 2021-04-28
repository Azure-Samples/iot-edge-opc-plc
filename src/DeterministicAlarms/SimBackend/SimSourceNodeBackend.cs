using OpcPlc.DeterministicAlarms.Configuration;
using System;
using System.Collections.Generic;

namespace OpcPlc.DeterministicAlarms.SimBackend
{
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
                SimAlarmStateBackend alarmStateBackend = new SimAlarmStateBackend
                {
                    Source = this,
                    Id = alarmConfiguration.Id,
                    Name = alarmConfiguration.Name,
                    //State = SimConditionStatesEnum.Enabled,
                    //EnableTime = DateTime.UtcNow,
                    //Reason = "Alarm Enabled.",
                    Time = DateTime.UtcNow,
                    AlarmType = alarmConfiguration.ObjectType
                };

                lock (this.Alarms)
                {
                    this.Alarms.Add(alarmConfiguration.Id, alarmStateBackend);
                }
            }
        }

        internal void Refresh()
        {
            List<SimAlarmStateBackend> snapshots = new List<SimAlarmStateBackend>();

            lock(this.Alarms)
            {
                foreach (var alarm in this.Alarms.Values)
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
