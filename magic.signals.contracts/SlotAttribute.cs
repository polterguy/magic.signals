/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;

namespace magic.signals.contracts
{
    /// <summary>
    /// Attribute class you need to mark your signals with, to associate your slot with a string/name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class SlotAttribute : Attribute
    {
        /// <summary>
        /// Name of slot.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether or not slot is async or not. The default is false.
        /// </summary>
        public bool Async { get; set; }
    }
}
