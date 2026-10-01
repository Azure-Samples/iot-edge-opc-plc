namespace AlarmCondition;

using Opc.Ua;
using Opc.Ua.Test;

public partial class SourceState
{
    public SourceState(
        AlarmConditionServerNodeManager nodeManager,
        NodeId nodeId,
        string sourcePath,
        DataGenerator generator)
        : this(
            nodeManager.SystemContext,
            nodeManager.SyncRoot,
            (context, value) => nodeManager.Server.ReportEvent(context, value),
            nodeManager.Server.TypeTree,
            nodeId,
            sourcePath,
            generator)
    {
    }
}