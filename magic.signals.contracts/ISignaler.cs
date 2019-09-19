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

        /// <summary>
        /// Pushes an object unto the stack of the signaler.
        /// </summary>
        /// <param name="name">Name to later reference the object by.</param>
        /// <param name="value">the actual object to push unto the stack.</param>
        void Push(string name, object value);

        /// <summary>
        /// Returns the last stack object previously pushed with the spcified name.
        /// </summary>
        /// <typeparam name="T">Type to return object as</typeparam>
        /// <param name="name">Name of stack object to retrieve</param>
        /// <returns></returns>
        T Peek<T>(string name) where T : class;

        /// <summary>
        /// Pops the last stack object pushed with the specified name.
        /// </summary>
        /// <param name="name">Name o fstack object to pop</param>
        void Pop();
    }
}
