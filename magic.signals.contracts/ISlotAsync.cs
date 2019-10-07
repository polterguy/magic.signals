﻿/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System.Threading.Tasks;
using magic.node;

namespace magic.signals.contracts
{
    /// <summary>
    /// Interface you need to implement on classe syou want to be able to dynamically invoke as signals.
    /// </summary>
    public interface ISlotAsync
    {
        /// <summary>
        /// Invoked whenever the specified signal for your slot is signaled.
        /// </summary>
        /// <param name="signaler">The signaler that invoked your slot.</param>
        /// <param name="input">Input arguments to your slot.</param>
        /// <returns>Awaitable task.</returns>
        Task SignalAsync(ISignaler signaler, Node input);
    }
}