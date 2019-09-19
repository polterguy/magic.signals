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
    /*
     * Implementation service class for the ISgnaler interface.
     */
    internal class Signaler : ISignaler
    {
        readonly IServiceProvider _provider;
        readonly ISignalsProvider _signals;
        readonly List<Tuple<string, object>> _stack = new List<Tuple<string, object>>();

        public Signaler(IServiceProvider provider, ISignalsProvider signals)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _signals = signals ?? throw new ArgumentNullException(nameof(signals));
        }

        #region [ -- Interface implementations -- ]

        public void Signal(string name, Node input)
        {
            var type = _signals.GetSignaler(name);
            if (type == null)
                throw new ApplicationException($"No slot exists for [{name}]");

            var instance = _provider.GetService(type) as ISlot;
            instance.Signal(this, input);
        }

        public void Push(string name, object value)
        {
            _stack.Add(new Tuple<string, object>(name, value));
        }

        public T Peek<T>(string name) where T : class
        {
            return _stack.AsEnumerable().Reverse().FirstOrDefault(x => x.Item1 == name)?.Item2 as T ??
                throw new ArgumentException($"No stack object named '{name}'");
        }

        public void Pop()
        {
            var toRemove = _stack.Last();
            _stack.Remove(toRemove);
        }

        public IEnumerable<string> Slots => _signals.Keys;

        #endregion
    }
}
