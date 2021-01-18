namespace OpcPlc
{
    using Opc.Ua;
    using System;
    using System.Threading;

    public class SimulatedVariableNode<T> : IDisposable
    {
        private ISystemContext _context;
        private BaseDataVariableState _variable;
        private Timer _timer;

        public T Value
        {
            get => (T)_variable.Value;
            set => SetValue(_variable, value);
        }

        public SimulatedVariableNode(ISystemContext context, BaseDataVariableState variable)
        {
            _context = context;
            _variable = variable;
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start(Func<T, T> update, int periodMs)
        {
            _timer = new Timer(s =>
            {
                Value = update(Value);
            },
            null,
            dueTime: 0,
            period: periodMs);
        }

        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void SetValue(BaseDataVariableState variable, T value)
        {
            variable.Value = value;
            variable.Timestamp = DateTime.Now;
            variable.ClearChangeMasks(_context, false);
        }
    }
}
