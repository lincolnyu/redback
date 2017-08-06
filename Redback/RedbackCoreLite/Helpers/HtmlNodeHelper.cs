using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Redback.Helpers
{
    public static class HtmlNodeHelper
    {
        public delegate bool TerminateDelegate(string tag);

        public class NodeInfo
        {
            public enum EndingTypes
            {
                Matched,
                ClosedBySame,
                Mismatched,
                ClosingTagOnly,
                NoTags,
                EndOfString
            }

            public int Start;
            public int End;
            public int ContentStart;
            public int ContentEnd;
            public EndingTypes EndingType;
            public string OpeningTag;
            public string ClosingTag;

            public bool ContentOnly => Start == ContentStart && End == ContentEnd;
            public bool HasContent => ContentEnd > ContentStart;
            public bool HasOpeningTag => ContentStart > Start;
            public bool HasClosingTag => End > ContentEnd;
            public int ContentLength => ContentEnd - ContentStart;
        }

        /// <summary>
        ///  Gets the node the tag of which the parser first encounters
        /// </summary>
        /// <param name="s">The string to parse from</param>
        /// <param name="start">The starting parsing point</param>
        /// <param name="terminate">whether the tag </param>
        /// <param name="ignoreCase">If the case should be ignored when matching opening and closing tags</param>
        /// <returns>The info of the matched block</returns>
        public static NodeInfo GetNextNode(this string s, int start, TerminateDelegate terminate,
            bool ignoreCase = true)
        {
            var rex = new Regex("<([^/][^ >]*)[> ]|</([^ >]+)>");
            var m = rex.Match(s, start);
            var sc = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (m.Groups[1].Success) // the next is a <sometag ...>
            {
                var tag = m.Groups[1].Value;
                var stack = new Stack<string>();
                stack.Push(tag);
                var openTagEnd = m.Value.EndsWith(">") ? m.Index + m.Length : s.IndexOf('>', m.Index + m.Length) + 1;
                var p = openTagEnd;
                while (true)
                {
                    var m2 = rex.Match(s, p);
                    if (m2.Success)
                    {
                        var open = m2.Groups[1];
                        var close = m2.Groups[2];
                        if (open.Success)
                        {
                            if (open.Value.Equals(tag, sc) && terminate(tag))
                            {
                                // It encounters a opening tag that's same as the root one
                                // The user requires that this tag not need a closing one to close
                                // but a new opening one conclude the previous
                                return new NodeInfo
                                {
                                    Start = m.Index,
                                    End = m2.Index,
                                    ContentStart = openTagEnd,
                                    ContentEnd = m2.Index,
                                    OpeningTag = tag,
                                    ClosingTag = "",
                                    EndingType = NodeInfo.EndingTypes.ClosedBySame
                                };
                            }
                            // We stack it
                            stack.Push(open.Value);
                        }
                        else
                        {
                            // We encounter a closing tag of the root
                            string ot;
                            bool matching;
                            do
                            {
                                ot = stack.Pop();
                                matching = close.Value.Equals(ot, sc);
                            } while (stack.Count > 0 && !matching);
                            if (stack.Count == 0)
                            {
                                return new NodeInfo
                                {
                                    Start = m.Index,
                                    End = m2.Index + m2.Length,
                                    ContentStart = openTagEnd,
                                    ContentEnd = m2.Index,
                                    OpeningTag = tag,
                                    ClosingTag = close.Value,
                                    EndingType = matching ? NodeInfo.EndingTypes.Matched : NodeInfo.EndingTypes.Mismatched
                                };
                            }
                        }
                        p = m2.Index + m2.Length;
                    }
                    else
                    {
                        // We reached the end of the string without other valid terminating condition
                        return new NodeInfo
                        {
                            Start = m.Index,
                            End = s.Length,
                            ContentStart = openTagEnd,
                            ContentEnd = s.Length,
                            OpeningTag = tag,
                            ClosingTag = string.Empty,
                            EndingType = NodeInfo.EndingTypes.EndOfString
                        };
                    }
                }
            }
            else if (m.Groups[2].Success)
            {
                // The next is a </sometag>, returns everything from start till that tag
                return new NodeInfo
                {
                    Start = start,
                    End = m.Index + m.Length,
                    ContentStart = start,
                    ContentEnd = m.Index,
                    OpeningTag = string.Empty,
                    ClosingTag = m.Groups[2].Value,
                    EndingType = NodeInfo.EndingTypes.ClosingTagOnly
                };
            }
            else
            {
                // No opening or closing tag found, returns everything till the end of the string
                return new NodeInfo
                {
                    Start = start,
                    End = s.Length,
                    ContentStart = start,
                    ContentEnd = s.Length,
                    OpeningTag = string.Empty,
                    ClosingTag = string.Empty,
                    EndingType = NodeInfo.EndingTypes.NoTags
                };
            }
        }
    }
}
