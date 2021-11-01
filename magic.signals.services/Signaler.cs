/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2021, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using magic.node;
using magic.signals.contracts;

namespace magic.signals.services
{
    /// <summary>
    /// Default implementation service class for the ISignaler contract/interface.
    /// </summary>
    public class Signaler : ISignaler
    {
        readonly IServiceProvider _provider;
        readonly ISignalsProvider _signals;
        readonly List<Tuple<string, object>> _stack = new List<Tuple<string, object>>();

        /// <summary>
        /// Creates a new instance of the default ISignaler service class.
        /// </summary>
        /// <param name="provider">Service provider to use for retrieving services.</param>
        /// <param name="signals">Implementation class to use for retrieving
        /// types from their string representations.</param>
        public Signaler(IServiceProvider provider, ISignalsProvider signals)
        {
            _provider = provider;
            _signals = signals;
        }

        #region [ -- Interface implementation -- ]

        /// <summary>
        /// Invokes the slot with the specified name,
        /// passing in the node itself as arguments to the slot.
        /// </summary>
        /// <param name="name">Name of slot to invoke.</param>
        /// <param name="input">Arguments being passed in to slot.</param>
        /// <param name="functor">Optional function that will be executed after slot has been invoked.</param>
        public void Signal(string name, Node input, Action functor = null)
        {
            var type = _signals.GetSlot(name) ?? throw new ArgumentException($"No slot exists for [{name}]");
            var raw = _provider.GetService(type);

            // Basic sanity checking.
            if (raw is ISlot slot)
                slot.Signal(this, input);
            else
                throw new ArgumentException($"I couldn't find a synchronous version of the [{name}] slot?");

            // Invoking callback if caller provided a callback to be executed after invocation of slot is done.
            functor?.Invoke();
        }

        /// <summary>
        /// Invokes the slot with the specified name,
        /// passing in the node itself as arguments to the slot.
        /// Notice, the ISlotAsync interface must have been implemented on your type
        /// to signal it using the async Signal method.
        /// </summary>
        /// <param name="name">Name of slot to invoke.</param>
        /// <param name="input">Arguments being passed in to slot.</param>
        /// <returns>An awaitable task.</returns>
        /// <param name="functor">Optional function that will be executed after slot has been invoked.</param>
        public async Task SignalAsync(string name, Node input, Action functor = null)
        {
            var type = _signals.GetSlot(name) ?? throw new ArgumentException($"No slot exists for [{name}]");
            var raw = _provider.GetService(type);

            // Basic sanity checking.
            if (raw is ISlotAsync asyncSlot)
            {
                // Returning task associated with slot to caller.
                await asyncSlot.SignalAsync(this, input);

                // Invoking callback if caller provided a callback to be executed after invocation of slot is done.
                functor?.Invoke();

                // Returning to avoid throwing exception further down.
                return;
            }

            if (raw is ISlot syncSlot)
            {
                syncSlot.Signal(this, input);

                // Invoking callback if caller provided a callback to be executed after invocation of slot is done.
                functor?.Invoke();

                // Returning to avoid throwing exception further down.
                return;
            }
            throw new ArgumentException($"I couldn't find the [{name}] slot, have you registered it?");
        }

        /// <summary>
        /// Pushes the specified object unto the stack with the given key name,
        /// for then to evaluate the given functor. Useful for evaluating some piece of code
        /// making sure the evaluation has access to some stack object during its evaluation process.
        /// </summary>
        /// <param name="name">Name to push value unto the stack as.</param>
        /// <param name="value">Actual object to push unto the stack. Notice, object will be automatically disposed at
        /// the end of the scope if the object implements IDisposable.</param>
        /// <param name="functor">Callback evaluated while stack object is on the stack.</param>
        public void Scope(string name, object value, Action functor)
        {
            _stack.Add(new Tuple<string, object>(name, value));
            try
            {
                functor();
            }
            finally
            {
                var obj = _stack.Last();
                _stack.RemoveAt(_stack.Count - 1);
                if (obj.Item2 is IDisposable disp)
                    disp.Dispose();
            }
        }

        /// <summary>
        /// Pushes the specified object unto the stack with the given key name,
        /// for then to evaluate the given functor. Useful for evaluating some
        /// piece of code making sure the evaluation has access to some stack
        /// object during its evaluation process.
        /// </summary>
        /// <param name="name">Name to push value unto the stack as.</param>
        /// <param name="value">Actual object to push unto the stack. Notice,
        /// object will be automatically disposed at the end of the scope if
        /// the object implements IDisposable.</param>
        /// <param name="functor">Callback evaluated while stack object is on
        /// the stack.</param>
        /// <returns>An awaitable task.</returns>
        public async Task ScopeAsync(string name, object value, Func<Task> functor)
        {
            _stack.Add(new Tuple<string, object>(name, value));
            try
            {
                await functor();
            }
            finally
            {
                var obj = _stack.Last();
                _stack.RemoveAt(_stack.Count - 1);
                if (obj.Item2 is IDisposable disp)
                    disp.Dispose();
            }
        }

        /// <summary>
        /// Retrieves the last stack object pushed unto the stack with the
        /// specified name.
        /// </summary>
        /// <typeparam name="T">Type to return stack object as. Notice, no
        /// conversion will be attempted. Make sure you use the correct type
        /// when retrieving your stack object.</typeparam>
        /// <param name="name">Name stack object was pushed as.</param>
        /// <returns>The first stack object with the specified name, or null if
        /// none are found.</returns>
        public T Peek<T>(string name) where T : class
        {
            return _stack.LastOrDefault(x => x.Item1 == name)?.Item2 as T;
        }

        #endregion
    }
}
