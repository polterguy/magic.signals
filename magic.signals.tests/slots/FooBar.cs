/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
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
