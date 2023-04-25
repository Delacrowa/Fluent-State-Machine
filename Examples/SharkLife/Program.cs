using System;
using System.Threading;
using RSG;

namespace Example1
{
    public static class Program
    {
        // How hungry the shark is now
        private static int _hunger;

        private static void Main(string[] args)
        {
            Console.WriteLine("Shark state machine. Press Ctrl-C to exit.");

            // Set up the state machine
            var rootState = new StateMachineBuilder()
                .State<NormalState>("Swimming")
                .Update((state, time) =>
                {
                    state.OnUpdate();
                    if (_hunger > 5)
                    {
                        state.PushState("Hunting");
                    }
                })
                .State<HungryState>("Hunting")
                .Update((state, time) =>
                {
                    state.OnUpdate();
                    if (_hunger <= 5)
                    {
                        state.Parent.PopState();
                    }
                })
                .End()
                .End()
                .Build();

            // Set the initial state.
            rootState.ChangeState("Swimming");

            // Update the state machine at a set interval.
            while (true)
            {
                rootState.Update(1.0f);
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        ///     State while the shark is just swimming around.
        /// </summary>
        private sealed class NormalState : AbstractState
        {
            public void OnUpdate()
            {
                Console.WriteLine("Swimming around...");
                _hunger++;
            }
        }

        /// <summary>
        ///     State for when the shark is hungry and decides to look for something to eat.
        /// </summary>
        private sealed class HungryState : AbstractState
        {
            private readonly Random _random;

            public HungryState() =>
                _random = new Random();

            public void OnUpdate()
            {
                if (_random.Next(5) <= 1)
                {
                    Console.WriteLine("Feeding");
                    _hunger -= 5; // Decrease hunger
                }
                else
                {
                    Console.WriteLine("Hunting");
                }
                _hunger++;
            }
        }
    }
}