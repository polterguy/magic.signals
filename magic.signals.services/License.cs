/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2021, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Linq;
using magic.node;
using magic.signals.contracts;

namespace magic.signals.services
{
    /// <summary>
    /// [license] slot for returning license information.
    /// </summary>
    [Slot(Name = "license")]
    public class License : ISlot
    {
        /// <summary>
        /// Handles the signal for the class.
        /// </summary>
        /// <param name="signaler">Signaler used to signal the slot.</param>
        /// <param name="input">Root node for invocation.</param>
        public void Signal(ISignaler signaler, Node input)
        {
            if (Signaler._licenseData != null)
            {
                input.AddRange(
                    Signaler._licenseData.Children

                        // Notice, we don't return price information by default, to avoid focusing on this in front of developers.
                        .Where(x => x.Name != "price" && x.Name != "currency")
                        .Select(x => x.Clone()));
            }
        }
    }
}
