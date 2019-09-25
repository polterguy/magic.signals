/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using magic.signals.contracts;

namespace magic.signals.services
{
    /// <summary>
    /// Implementation service class for the ISignalsProvider interface.
    /// </summary>
    public class SignalsProvider : ISignalsProvider
    {
        readonly Dictionary<string, Type> _slots = new Dictionary<string, Type>();

        #region [ -- Interface implementation -- ]

        /// <summary>
        /// Creates an instance of the signals provider class, 
        /// </summary>
        /// <param name="types">Types to initially use for resolving slots. Notice, each type has to have at least one Slot attribute, declaring
        /// the name of the slot.</param>
        public SignalsProvider(IEnumerable<Type> types)
        {
            foreach (var idxType in types)
            {
                foreach (var idxAtrName in idxType.GetCustomAttributes(true).OfType<SlotAttribute>().Select(x => x.Name))
                {
                    if (string.IsNullOrEmpty(idxAtrName))
                        throw new ArgumentNullException($"No name specified for type '{idxType}' in Slot attribute");

                    if (_slots.ContainsKey(idxAtrName))
                        throw new ApplicationException($"Slot [{idxAtrName}] already taken by another type");

                    _slots[idxAtrName] = idxType;
                }
            }
        }

        /// <summary>
        /// Returns all slots, or rather all slot names to be specific.
        /// </summary>
        public IEnumerable<string> Keys => _slots.Keys;

        /// <summary>
        /// Returns the slot with the specified name.
        /// </summary>
        /// <param name="name">Name for slot to retrieve.</param>
        /// <returns></returns>
        public Type GetSlot(string name)
        {
            _slots.TryGetValue(name, out Type result);
            return result;
        }

        #endregion
    }
}
