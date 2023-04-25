﻿namespace RSG
{
    /// <summary>
    ///     State with no extra functionality used for root of state hierarchy.
    /// </summary>
    public sealed class State : AbstractState
    {
        public static readonly IState Empty = new State();
    }
}