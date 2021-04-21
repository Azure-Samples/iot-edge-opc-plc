using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpcPlc.DeterministicAlarms.Configuration
{
    public class Source
    {
        public SourceObjectState ObjectType { get; set; }
        public string Name { get; set; }
        public List<Alarm> Alarms { get; set; }
    }
}
