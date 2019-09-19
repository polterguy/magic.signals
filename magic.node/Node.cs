/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;
using System.Linq;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;

namespace magic.node
{
    /// <summary>
    /// Graph class allowing you to declare tree structures as name/value/children collections.
    /// 
    /// Note, contrary to JSON, and similar formats, hte name is not a "key", and can be duplicated
    /// multiple times in the same "scope".
    /// </summary>
    public class Node : ICloneable
    {
        readonly List<Node> _children;
        string _name;

        /// <summary>
        /// Creates an empty node, with a "" name, a null value, and zero children.
        /// </summary>
        public Node()
        {
            _name = "";
            _children = new List<Node>();
        }

        /// <summary>
        /// Creates a new node with the specified name, null value, and zero children.
        /// </summary>
        /// <param name="name">Name for node</param>
        public Node(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _children = new List<Node>();
        }

        /// <summary>
        /// Creates a new node with the given name, given value, and zero children.
        /// </summary>
        /// <param name="name">Name for node</param>
        /// <param name="value">Value for node</param>
        public Node(string name, object value)
        {
            Name = name;
            Value = value;
            _children = new List<Node>();
        }

        /// <summary>
        /// Creates a new node with the given name, value and children.
        /// </summary>
        /// <param name="name">Name for node</param>
        /// <param name="value">Value for node</param>
        /// <param name="children">Initial children collection for node</param>
        public Node(string name, object value, IEnumerable<Node> children)
        {
            Name = name;
            Value = value;
            _children = new List<Node>(children);
            foreach (var idx in _children)
            {
                idx.Parent = this;
            }
        }

        /// <summary>
        /// Name of your node.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value ?? throw new ArgumentNullException(nameof(value)); }
        }

        /// <summary>
        /// Value of your node.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Children of your node.
        /// </summary>
        public IEnumerable<Node> Children
        {
            get { return _children; }
        }

        /// <summary>
        /// Your node's parent node, if any.
        /// </summary>
        public Node Parent { get; private set; }

        /// <summary>
        /// Returns the "elder sibling" for your node, if any.
        /// </summary>
        public Node Previous
        {
            get
            {
                if (Parent == null)
                    return null;

                var indexOfThis = Parent._children.IndexOf(this);
                if (indexOfThis == 0)
                    return null;
                return Parent._children[indexOfThis - 1];
            }
        }

        /// <summary>
        /// Returns the "younger sibling" for your node, if any.
        /// </summary>
        public Node Next
        {
            get
            {
                if (Parent == null)
                    return null;

                var indexOfThis = Parent._children.IndexOf(this);
                if (indexOfThis == Parent._children.Count - 1)
                    return null;
                return Parent._children[indexOfThis + 1];
            }
        }

        /// <summary>
        /// Returns the value of your node, possibly implying evaluating any expressions found in its value,
        /// unless you 
        /// </summary>
        /// <param name="evaluate"></param>
        /// <returns></returns>
        public object Get(bool evaluate = true)
        {
            if (evaluate && Value is Expression ex)
            {
                var nodes = ex.Evaluate(this);

                if (nodes.Count() > 1)
                    throw new ApplicationException("Multiple values returned from Expression in Get");

                if (nodes.Any())
                    return nodes.First().Get();

                return null;
            }
            return Value;
        }

        public IEnumerable<Node> Evaluate()
        {
            if (!(Value is Expression ex))
                throw new ApplicationException($"'{Value}' is not a valid Expression");

            return ex.Evaluate(this);
        }

        public T Get<T>()
        {
            if (typeof(T) == typeof(Expression) && Value is Expression)
                return (T)Value;

            var value = Get();
            if (value is T result)
                return result;

            // Converting, the simple version.
            return (T)Convert.ChangeType(Value, typeof(T), CultureInfo.InvariantCulture);
        }

        public IEnumerable<T> GetList<T>()
        {
            // Verifying we've got anything at all here, returning an empty enumerator if not.
            if (Value == null)
                yield break;

            if (Value is Expression ex)
            {
                foreach (var idx in ex.Evaluate(this))
                {
                    if (idx.Value is Expression exInner)
                    {
                        foreach (var idxInner in idx.GetList<T>())
                        {
                            yield return idxInner;
                        }
                    }
                    else
                    {
                        yield return idx.Get<T>();
                    }
                }
            }
            else
            {
                foreach (var idx in Value as IEnumerable)
                {
                    if (idx == null)
                        yield return default(T);
                    else if (typeof(T) == idx.GetType())
                        yield return (T)idx;
                    else
                        yield return (T)Convert.ChangeType(idx, typeof(T), CultureInfo.InvariantCulture);
                }
            }
        }

        public void Add(Node value)
        {
            if (value.Parent != null)
                value.Parent.Remove(value); // Removing from its original parent.

            value.Parent = this;
            _children.Add(value);
        }

        public void InsertAfter(Node value)
        {
            if (Parent == null)
                throw new ApplicationException("Cannot insert after since current node is a root node");

            var indexOfThis = Parent._children.IndexOf(this);
            Parent.Insert(indexOfThis + 1, value);
        }

        public void InsertBefore(Node value)
        {
            if (Parent == null)
                throw new ApplicationException("Cannot insert before since current node is a root node");

            var indexOfThis = Parent._children.IndexOf(this);
            Parent.Insert(indexOfThis, value);
        }

        public void Insert(int index, Node value)
        {
            if (value.Parent != null)
                value.Parent.Remove(value); // Removing from its original parent.

            value.Parent = this;
            _children.Insert(index, value);
        }

        public void AddRange(IEnumerable<Node> values)
        {
            foreach (var idx in values)
            {
                Add(idx);
            }
        }

        public void Remove(Node value)
        {
            value.Parent = null;
            _children.Remove(value);
        }

        public void Clear()
        {
            _children.Clear();
        }

        public Node Clone()
        {
            var value = Value;
            if (value is ICloneable cloner)
                value = cloner.Clone();

            var result = new Node(Name, value);
            foreach(var idx in Children)
            {
                result.Add(idx.Clone());
            }
            return result;
        }

        public void UnTie()
        {
            Parent._children.Remove(this);
            Parent = null;
        }

        #region [ -- Interface implementations -- ]

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion
    }
}
