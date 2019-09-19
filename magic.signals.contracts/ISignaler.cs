/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using magic.node;
using System.Collections.Generic;

namespace magic.signals.contracts
{
    /// <summary>
    /// Interface allowing you to signal slots, passing in nodes as generic arguments.
    /// </summary>
    public interface ISignaler
    {
        /// <summary>
        /// Signals the slot with the specified name.
        /// </summary>
        /// <param name="name">Name of slot to signal</param>
        /// <param name="input">Input arguments to slot</param>
        void Signal(string name, Node input);

        /// <summary>
        /// Returns a list of all registered slots in the AppDomain.
        /// </summary>
        IEnumerable<string> Slots { get; }
    }
}
