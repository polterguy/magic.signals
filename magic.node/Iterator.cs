﻿/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace magic.node
{
    /// <summary>
    /// A single iterator component for an expression. Basically, an expression is really nothing but
    /// a chain of iterators.
    /// </summary>
    public class Iterator
    {
        readonly Func<Node, IEnumerable<Node>, IEnumerable<Node>> _evaluate;

        /// <summary>
        /// Creates an iterator from its given string representation.
        /// </summary>
        /// <param name="value">String declaration of iterator</param>
        public Iterator(string value)
        {
            Value = value;
            _evaluate = CreateEvaluator(Value);
        }

        /// <summary>
        /// Returns the string representation of the iterator.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Evaluates the iterator from the given identity node, with the given input,
        /// resulting in a new node set being returned by the evaluation.
        /// </summary>
        /// <param name="identity">Identity node from which the original expression was evaluated from.</param>
        /// <param name="input">A collection of nodes passed in from the result of the evaluation of the previous iterator.</param>
        /// <returns></returns>
        public IEnumerable<Node> Evaluate(Node identity, IEnumerable<Node> input)
        {
            return _evaluate(identity, input);
        }

        #region [ -- Overrides -- ]

        public override string ToString()
        {
            return Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Iterator it))
                return false;
            return Value.Equals(it.Value);
        }

        #endregion

        #region [ -- Private helper methods -- ]

        /*
         * Creates the evaluator, which is simply a function, taking an identity node, an enumerable of
         * nodes, resuolting in a new enumerable of nodes.
         */
        Func<Node, IEnumerable<Node>, IEnumerable<Node>> CreateEvaluator(string value)
        {
            switch (value)
            {
                case "*":
                    return (identiy, input) => input.SelectMany(x => x.Children);

                case "#":
                    return (identiy, input) => input.Select(x => x.Get<Node>());

                case "-":
                    return (identiy, input) => input.Select(x => x.Previous ?? x.Parent.Children.Last());

                case "+":
                    return (identiy, input) => input.Select(x => x.Next ?? x.Parent.Children.First());

                case ".":
                    return (identiy, input) => input.Select(x => x.Parent).Distinct();

                case "..":
                    return (identiy, input) =>
                    {
                        // Notice, input might be a "no sequence enumerable", so we'll have to accommodate for "null returns".
                        var idx = input.FirstOrDefault();

                        if (idx == null)
                            return new Node[] { };

                        while (idx.Parent != null)
                            idx = idx.Parent;

                        return new Node[] { idx };
                    };

                case "**":
                    return (identiy, input) =>
                    {
                        return AllDescendants(input);
                    };

                default:
                    return CreateParametrizedIterator(value);
            }
        }

        /*
         * Creates a parametrized iterator. A parametrized iterator is an iterator that requires 
         * some sort of dynamic parameter or argument(s).
         */
        Func<Node, IEnumerable<Node>, IEnumerable<Node>> CreateParametrizedIterator(string value)
        {
            if (value.StartsWith("\\", StringComparison.InvariantCulture))
            {
                var lookup = value.Substring(1);
                return (identiy, input) => input.Where(x => x.Name == value);
            }

            if (value.StartsWith("{", StringComparison.InvariantCulture) &&
                value.EndsWith("}", StringComparison.InvariantCulture))
            {
                var index = int.Parse(value.Substring(1, value.Length - 2));
                return (identity, input) => input.Where(x => x.Name == identity.Children.Skip(index).First().Get<string>());
            }

            if (value.StartsWith("=", StringComparison.InvariantCulture))
            {
                var lookup = value.Substring(1);
                return (identiy, input) => input.Where(x =>
                {
                    var val = x.Value;
                    if (val == null)
                        return lookup.Length == 0; // In case we're looking for null values

                    if (val is string)
                        return lookup.Equals(val);

                    return lookup.Equals(Convert.ToString(val, CultureInfo.InvariantCulture));
                });
            }

            if (value.StartsWith("[", StringComparison.InvariantCulture) &&
                value.EndsWith("]", StringComparison.InvariantCulture))
            {
                var ints = value.Substring(1, value.Length - 2).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var start = int.Parse(ints[0]);
                var count = int.Parse(ints[1]);
                return (identiy, input) => input.Skip(start).Take(count);
            }

            if (value.StartsWith("@", StringComparison.InvariantCulture))
            {
                var lookup = value.Substring(1);
                return (identiy, input) =>
                {
                    var cur = input.FirstOrDefault()?.Previous ?? input.FirstOrDefault()?.Parent;
                    while (cur != null && cur.Name != lookup)
                    {
                        var previous = cur.Previous;
                        if (previous == null)
                            cur = cur.Parent;
                        else
                            cur = previous;
                    }

                    if (cur == null)
                        return new Node[] { };

                    return new Node[] { cur };
                };
            }

            if (int.TryParse(value, out int number))
                return (identiy, input) => input.SelectMany(x => x.Children.Skip(number).Take(1));

            return (identiy, input) => input.Where(x => x.Name == value);
        }

        /*
         * Helper method to return all descendants recursively for the '**' iterator.
         */
        IEnumerable<Node> AllDescendants(IEnumerable<Node> input)
        {
            foreach (var idx in input)
            {
                yield return idx;
                foreach (var idxInner in AllDescendants(idx.Children))
                {
                    yield return idxInner;
                }
            }
        }

        #endregion
    }
}