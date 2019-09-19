/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using magic.node.expressions;

namespace magic.node.extensions.hyperlambda
{
    public sealed class Stringifier
    {
        public static string GetHyper(IEnumerable<Node> nodes)
        {
            var result = new StringBuilder();
            GetHyper(result, nodes, 0);
            return result.ToString();
        }

        #region [ -- Private helper methods -- ]

        static void GetHyper(StringBuilder builder, IEnumerable<Node> nodes, int level)
        {
            foreach (var idx in nodes)
            {
                int idxLevel = level;
                while (idxLevel-- > 0)
                    builder.Append("   ");

                var name = idx.Name;
                if (name.Contains("\n"))
                    name = "@\"" + name.Replace("\"", "\"\"") + "\"";
                else if (name.Contains("\"") || name.Contains(":"))
                    name = "\"" + name.Replace("\"", "\\\"") + "\"";
                else if (idx.Value == null && name == "")
                    name = @"""""";
                builder.Append(name);

                if (idx.Value != null)
                {
                    var value = Converter.ConvertToString(idx, out string type);
                    builder.Append(":");
                    if (!string.IsNullOrEmpty(type) && type != "string")
                        builder.Append(type + ":");
                    builder.Append(value);
                }
                builder.Append("\r\n");
                GetHyper(builder, idx.Children, level + 1);
            }
        }

        #endregion
    }
}
