using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlarmCondition
{
    public class AreaConfiguration
    {
        public string Name { get; set; }
        public AreaConfigurationCollection SubAreas { get; set; }
        public StringCollection SourcePaths { get; set; }
    }
}
