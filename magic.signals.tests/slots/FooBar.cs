/*
 * Magic, Copyright(c) Thomas Hansen 2019, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using magic.node;
using magic.node.extensions;
using magic.signals.contracts;

namespace magic.signals.tests.slots
{
    [Slot(Name = "foo.bar")]
    public class FooBar : ISlot
    {
        public void Signal(ISignaler signaler, Node input)
        {
            input.Value = input.Get<string>() + "world";
        }
    }
}
