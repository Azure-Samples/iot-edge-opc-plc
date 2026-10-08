namespace OpcPlc;

using Opc.Ua;
using System;

public class SimulatedVariableNode<T> : IDisposable
{
    private static readonly TypeInfo ValueType = TypeInfo.Construct(typeof(T));
    private readonly ISystemContext _context;
    private readonly BaseDataVariableState _variable;
    private ITimer _timer;
    private readonly TimeService _timeService;

    public T Value
    {
        get
        {
            Variant value = _variable.Value;
            if (value.IsNull && default(T) is null)
            {
                return default;
            }

            if (value.TypeInfo != ValueType)
            {
                throw new ServiceResultException(StatusCodes.BadTypeMismatch);
            }

            return VariantHelper.CastTo<T>(value);
        }
        set => SetValue(_variable, value);
    }

    public SimulatedVariableNode(ISystemContext context, BaseDataVariableState variable, TimeService timeService)
    {
        _context = context;
        _variable = variable;
        _timeService = timeService;
    }

    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    /// Start periodic update.
    /// The update Func gets the current value as input and should return the updated value.
    /// </summary>
    public void Start(Func<T, T> update, int periodMs)
    {
        _timer = _timeService.NewTimer((s, o) =>
        {
            Value = update(Value);
        },
        (uint)periodMs);
    }

    public void Stop()
    {
        if (_timer == null)
        {
            return;
        }

        _timer.Enabled = false;
    }

    private void SetValue(BaseDataVariableState variable, T value)
    {
        variable.Value = VariantHelper.CastFrom(value);
        variable.Timestamp = _timeService.Now();
        variable.ClearChangeMasks(_context, false);
    }
}
