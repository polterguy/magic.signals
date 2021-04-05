/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2021, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

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
