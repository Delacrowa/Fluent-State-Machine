using System;
using Combat.Core;

namespace Combat.Brain
{
    public interface ICombatantBrain
    {
        void Decide();

        void SetReaction(Action<Part> decisionMade);
    }
}