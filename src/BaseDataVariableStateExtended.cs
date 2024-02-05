namespace OpcPlc;

using Opc.Ua;
using System;

/// <summary>
/// Extended BaseDataVariableState class to hold additional parameters for simulation.
/// </summary>
public class BaseDataVariableStateExtended : BaseDataVariableState
{
    public bool Randomize { get; }
    public object StepSize { get; }
    public object MinValue { get; }
    public object MaxValue { get; }

    public BaseDataVariableStateExtended(NodeState nodeState, bool randomize, object stepSize, object minValue, object maxValue) : base(nodeState)
    {
        ArgumentNullException.ThrowIfNull(nodeState);

        Randomize = randomize;
        StepSize = stepSize;
        MinValue = minValue;
        MaxValue = maxValue;
    }
}
