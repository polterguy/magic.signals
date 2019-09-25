/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using magic.node;
using magic.signals.contracts;

namespace magic.signals.services
{
    /// <summary>
    /// Implementation service class for the ISgnaler interface.
    /// </summary>
    public class Signaler : ISignaler
    {
        readonly IServiceProvider _provider;
        readonly ISignalsProvider _signals;
        readonly List<Tuple<string, object>> _stack = new List<Tuple<string, object>>();

        /// <summary>
        /// Creates a new instance of the default Signaler ISignaler service class.
        /// </summary>
        /// <param name="provider">Service provider to use for retrieving services.</param>
        /// <param name="signals">Implementation class to use for retrieving types from their string representations.</param>
        public Signaler(IServiceProvider provider, ISignalsProvider signals)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _signals = signals ?? throw new ArgumentNullException(nameof(signals));
        }

        #region [ -- Interface implementation -- ]

        /// <summary>
        /// Invokes the slot specified in the name property of the input node,
        /// passing in the node itself as arguments to the slot.
        /// </summary>
        /// <param name="input">Parameters to slot.</param>
        public void Signal(Node input)
        {
            var type = _signals.GetSlot(input.Name);
            if (type == null)
                throw new ApplicationException($"No slot exists for [{input.Name}]");

            var instance = _provider.GetService(type) as ISlot;
            instance.Signal(this, input);
        }

        /// <summary>
        /// Pushes the specified object unto the stack with the given key name,
        /// for then to evaluate the given functor. Useful for evaluating some piece of code
        /// making sure the evaluation has access to some stack object during its evaluation process.
        /// </summary>
        /// <param name="name">Name to push value unto the stack as.</param>
        /// <param name="value">Actual object to push unto the stack.</param>
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
                _stack.RemoveAt(_stack.Count - 1);
            }
        }

        /// <summary>
        /// Retrieves the last stack object pushed unto the stack with the specified name. Will
        /// throw a null reference exception if the specified stack object is not found.
        /// </summary>
        /// <typeparam name="T">Type to return stack object as. Notice, no conversion will be attempted. Make sure you use the correct type.</typeparam>
        /// <param name="name">Name stack object was pushed as.</param>
        /// <returns></returns>
        public T Peek<T>(string name) where T : class
        {
            return _stack.AsEnumerable().Reverse().FirstOrDefault(x => x.Item1 == name)?.Item2 as T ??
                throw new NullReferenceException($"No stack object named '{name}' found");
        }

        #endregion
    }
}
