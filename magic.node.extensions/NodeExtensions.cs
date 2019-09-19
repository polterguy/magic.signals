/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using magic.node.expressions;

namespace magic.node.extensions
{
    /// <summary>
    /// Extension class extending the Node class with convenience methods.
    /// </summary>
    public static class NodeExtensions
    {
        /// <summary>
        /// Evaluates the expression found in the node's value, and returns the results of the evaluation
        /// </summary>
        /// <returns>Result of evaluation</returns>
        public static IEnumerable<Node> Evaluate(this Node node)
        {
            if (!(node.Value is Expression ex))
                throw new ApplicationException($"'{node.Value}' is not a valid Expression");

            return ex.Evaluate(node);
        }

        /// <summary>
        /// Returns the value of the node as typeof(T)
        /// </summary>
        /// <typeparam name="T">Type to return value as, might imply conversion if value is not already of the specified type.</typeparam>
        /// <returns>The node's value as an object of type T</returns>
        public static T Get<T>(this Node node)
        {
            if (typeof(T) == typeof(Expression) && node.Value is Expression)
                return (T)node.Value;

            var value = node.Value;
            if (value is T result)
                return result;

            // Converting, the simple version.
            return (T)Convert.ChangeType(node.Value, typeof(T), CultureInfo.InvariantCulture);
        }

        public static T GetEx<T>(this Node node)
        {
            if (node.Value is Expression exp)
            {
                var value = exp.Evaluate(node);
                if (value.Count() > 1)
                    throw new ApplicationException("Multiple resulting nodes from expression");
                if (!value.Any())
                    return default(T);
                return value.First().GetEx<T>();
            }
            return node.Get<T>();
        }
    }
}
