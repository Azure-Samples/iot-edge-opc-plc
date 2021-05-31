namespace OpcPlc.DeterministicAlarms.Configuration
{
    using System.Collections.Generic;

    public class Folder
    {
        public string Name { get; set; }

        public List<Source> Sources { get; set; }
    }
}