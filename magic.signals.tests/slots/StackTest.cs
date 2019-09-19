/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System.Linq;
using magic.node;
using magic.signals.contracts;

namespace magic.signals.tests.slots
{
    [Slot(Name = "stack.test")]
    public class StackTest : ISlot
    {
        public void Signal(ISignaler signaler, Node input)
        {
            input.Value = signaler.Peek<string>("value");
        }
    }
}
