namespace OpcPlc.DeterministicAlarms.SimBackend;

using System.Collections.Generic;
using OpcPlc.DeterministicAlarms.Configuration;
using static OpcPlc.DeterministicAlarms.SimBackend.SimSourceNodeBackend;

public class SimBackendService
{
    private readonly object _lock = new object();
    public Dictionary<string, SimSourceNodeBackend> SourceNodes = new Dictionary<string, SimSourceNodeBackend>();

    public SimSourceNodeBackend CreateSourceNodeBackend(string name, List<Alarm> alarms, AlarmChangedEventHandler alarmChangeCallback)
    {
        SimSourceNodeBackend simSourceNodeBackend;

        lock (_lock)
        {
            simSourceNodeBackend = new SimSourceNodeBackend
            {
                Name = name,
                OnAlarmChanged = alarmChangeCallback
            };

            simSourceNodeBackend.CreateAlarms(alarms);

            SourceNodes[name] = simSourceNodeBackend;
        }

        return simSourceNodeBackend;
    }
}
