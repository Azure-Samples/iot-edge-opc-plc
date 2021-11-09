namespace AlarmCondition;

using Opc.Ua;

public class AreaConfiguration
{
    public string Name { get; set; }
    public AreaConfigurationCollection SubAreas { get; set; }
    public StringCollection SourcePaths { get; set; }
}
