﻿/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;
using magic.node;

namespace magic.signals.contracts
{
    /// <summary>
    /// Interface allowing you to signal slots, passing in nodes as generic arguments.
    /// </summary>
    public interface ISignaler
    {
        /// <summary>
        /// Signals the slot with the name from the input node's Name property.
        /// </summary>
        /// <param name="input">Input arguments to slot, including its name as name of node.</param>
        void Signal(Node input);

        /// <summary>
        /// Signals the slot with the name from the input node's Name property.
        /// </summary>
        /// <param name="name">Name of slot to invoke.</param>
        /// <param name="input">Input arguments to slot.</param>
        void Signal(string name, Node input);

        /// <summary>
        /// Adds the given stack value unto the stack with the given name, and invokes functor,
        /// making sure the object is popped from the stack after the functor has been evaluated.
        /// </summary>
        /// <param name="name">Name of stack object, allowing you to retrieve it from recursively invoked slots.</param>
        /// <param name="value">Value of stack object. Use Peek to retrieve the object in recursively invoked slots.</param>
        /// <param name="functor">Callback evaluated while object is on the stack.</param>
        void Scope(string name, object value, Action functor);

        /// <summary>
        /// Returns the last stack object previously pushed with the spcified name.
        /// </summary>
        /// <typeparam name="T">Type to return object as.</typeparam>
        /// <param name="name">Name of stack object to retrieve.</param>
        /// <returns></returns>
        T Peek<T>(string name) where T : class;
    }
}
