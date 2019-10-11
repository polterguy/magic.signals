/*
 * Magic, Copyright(c) Thomas Hansen 2019, thomas@gaiasoul.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Threading.Tasks;
using magic.node;
using magic.node.extensions;
using magic.signals.contracts;

namespace magic.signals.tests.slots
{
    [Slot(Name = "foo.bar.async")]
    public class FooBarAsync : ISlotAsync
    {
        public Task SignalAsync(ISignaler signaler, Node input)
        {
            return Task.Run(() => input.Value = input.Get<string>() + "world");
        }
    }
}
