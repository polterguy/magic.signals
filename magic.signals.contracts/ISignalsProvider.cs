/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;
using System.Collections.Generic;

namespace magic.signals.contracts
{
    /// <summary>
    /// The class responsible for feeding your signaler with signals, implying strings to types mappings.
    /// </summary>
    public interface ISignalsProvider
    {
        /// <summary>
        /// Returns a type from the specified name.
        /// </summary>
        /// <param name="name">Slot name for the type to return.</param>
        /// <returns></returns>
        Type GetSlot(string name);

        /// <summary>
        /// Returns all keys, implying names registered for your signals implementation.
        /// </summary>
        IEnumerable<string> Keys { get; }
    }
}
