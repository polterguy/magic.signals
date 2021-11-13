/*
 * Magic Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
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
