namespace AlarmCondition;

using System.Collections.Generic;

public class AreaConfiguration
{
    public string Name { get; set; }
    public AreaConfigurationCollection SubAreas { get; set; }
    public List<string> SourcePaths { get; set; }
}
