namespace OpcPlc.DeterministicAlarms.Configuration;

using System.Collections.Generic;

public class Source
{
    public SourceObjectState ObjectType { get; set; }

    public string Name { get; set; }

    public List<Alarm> Alarms { get; set; }
}
