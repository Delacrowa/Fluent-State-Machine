using System;

namespace RSG
{
    /// <summary>
    ///     Data structure for associating a condition with an action.
    /// </summary>
    public sealed class StateCondition
    {
        private readonly Func<bool> _predicate;
        private readonly Action _action;

        public StateCondition(Func<bool> predicate, Action action)
        {
            _predicate = predicate;
            _action = action;
        }

        public void Execute()
        {
            if (_predicate.Invoke())
            {
                _action.Invoke();
            }
        }
    }
}