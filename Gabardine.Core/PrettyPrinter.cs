/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics.Debug;
using System.Linq;
using System.Text;

namespace Gabardine
{
    public enum PrintFormat { Lisp, Infix , Tree}

    public static class PrettyPrinter
    {
        public static readonly Operator Highlight = new Operator(1, OperatorKind.Function, "highlight");

        public static void Print(Term term, PrintFormat format, Terminal terminal, TerminalFormat highlight = TerminalFormat.LightGreen)
        {
            switch (format) {
                case PrintFormat.Lisp:
                    PrintLispFormat(term, terminal, highlight);
                    break;
                case PrintFormat.Infix:
                    PrintInfixFormat(term, terminal, highlight);
                    break;
            }
        }

        public static string Format(Term term, PrintFormat format)
        {
            switch(format) {
                case PrintFormat.Lisp:
                    return LispFormat(term);
                case PrintFormat.Infix:
                    return InfixFormat(term);
            }
            throw new NotImplementedException();
        }

        public static void PrintInfixFormat(Term term, Terminal terminal, TerminalFormat highlight = TerminalFormat.LightGreen)
        {
            string str = InfixFormat(term);
            var segments = str.Split(new string[] { "@#$" }, StringSplitOptions.None);

            switch (segments.Length) {
                case 1:
                    terminal.Send(str);
                    break;
                case 3:
                    terminal.Send(segments[0]);
                    terminal.PushFormat(highlight);
                    terminal.Send(segments[1]);
                    terminal.PopFormat();
                    terminal.Send(segments[2]);
                    break;
                default:
                    throw new Exception("Bad highlighting");
            }
        }

        public static void PrintLispFormat(Term term, Terminal terminal, TerminalFormat highlight = TerminalFormat.LightGreen)
        {
            Stack<Term> stack = new Stack<Term>();
            stack.Push(term);

            while (stack.Count > 0) {
                Term head = stack.Pop();

                if (ReferenceEquals(head, null)) {
                    // If we've been here before, and are getting a nullptr
                    // marker, write postamble.
                    terminal.Send(')');
                    continue;
                }


                // Don't insert unnecessary space at very start of output.
                if (stack.Count > 0) {
                    terminal.Send(' ');
                }

                if (head.Op == Highlight) {
                    string subexpr = LispFormat(head[0]);
                    terminal.PushFormat(highlight);
                    terminal.Send(subexpr);
                    terminal.PopFormat();
                    continue;
                }

                // Don't insert brackets for nullary operators.
                if (head.Arity == 0) {
                    terminal.Send(head.Op);
                    continue;
                }

                // If first time seeing a node, write preamble, insert
                // null marker, and enqueue children.
                terminal.Send('(');
                terminal.Send(head.Op);
                stack.Push(null);
                for (int i = head.Arity - 1; i >= 0; --i) {
                    stack.Push(head[i]);
                }
            }
        }

        public static string LispFormat(Term term)
        {
            StringBuilder sb = new StringBuilder();
            Stack<Term> stack = new Stack<Term>();
            stack.Push(term);

            while (stack.Count > 0) {
                Term head = stack.Pop();

                if (ReferenceEquals(head, null)) {
                    // If we've been here before, and are getting a nullptr
                    // marker, write postamble.
                    sb.Append(')');
                    continue;
                }


                // Don't insert unnecessary space at very start of output.
                if (sb.Length > 0) {
                    sb.Append(' ');
                }
#if false
                    //  Detect a special operator
                    if (head->Op() == trigger) {
                        action(head);
                        continue;
                    }
#endif

                // Don't insert brackets for nullary operators.
                if (head.Arity == 0) {
                    sb.Append(head.Op);
                    continue;
                }

                //  If first time seeing a node, write preamble, insert
                //  null marker, and enqueue children.
                sb.Append('(').Append(head.Op);
                stack.Push(null);
                for (int i = head.Arity - 1; i >= 0; --i) {
                    stack.Push(head[i]);
                }
            }

            return sb.ToString();
        }

        struct PrecString
        {
            public string Text { get; set; }
            public int Prec { get; set; }
        }

        static void PrintLet(PrecString binding, PrecString body, StringBuilder sb)
        {
            sb.Append("let ");
            sb.Append(binding.Text);
            sb.Append(" in ");
            sb.Append(body.Text);
        }

        static void PrintLambda(PrecString child, StringBuilder sb)
        {
            sb.Append('λ');
            sb.Append(child.Text);
        }

        static void PrintFunctionCall(string name, PrecString[] children, StringBuilder sb)
        {
            sb.Append(name);
            sb.Append('(');
            sb.Append(children[0].Text);
            for (int i = 1; i < children.Length; ++i) {
                sb.Append(", ");
                sb.Append(children[i].Text);
            }
            sb.Append(')');
        }

        static void PrintInfix(OperatorSyntax syntax, PrecString left, PrecString right, StringBuilder sb)
        {
            bool parens = NeedsParens(left.Prec, 0, syntax);
            if (parens) sb.Append('(');
            sb.Append(left.Text);
            if (parens) sb.Append(')');

            sb.Append(' ');
            sb.Append(syntax.Name);
            sb.Append(' ');

            parens = NeedsParens(right.Prec, 1, syntax);
            if (parens) sb.Append('(');
            sb.Append(right.Text);
            if (parens) sb.Append(')');
        }

        static void PrintPrefix(OperatorSyntax syntax, PrecString arg, StringBuilder sb)
        {
            bool parens = NeedsParens(arg.Prec, 1, syntax);
            sb.Append(syntax.Name);
            if (parens) sb.Append('(');
            sb.Append(arg.Text);
            if (parens) sb.Append(')');
        }

        static void PrintPostfix(OperatorSyntax syntax, PrecString arg, StringBuilder sb)
        {
            bool parens = NeedsParens(arg.Prec, 0, syntax);
            if (parens) sb.Append('(');
            sb.Append(arg.Text);
            if (parens) sb.Append(')');
            sb.Append(syntax.Name);
        }

        static readonly OperatorSyntax indexSyntax = new OperatorSyntax { Style = OperatorSyntax.Fix.LeftAssociative, Precedence = 100 };
        static void PrintIndex(PrecString _base, PrecString offset, StringBuilder sb)
        {
            bool parens = NeedsParens(_base.Prec, 0, indexSyntax);
            if (parens) sb.Append('(');
            sb.Append(_base.Text);
            if (parens) sb.Append(')');
            sb.Append('[');
            sb.Append(offset.Text);
            sb.Append(']');
        }

        public static string InfixFormat(Term term)
        {
            StringBuilder sb = new StringBuilder();

            var args = new Stack<PrecString>();
            var stack = new Stack<Term>();
            stack.Push(term);
            while (stack.Count > 0) {

                Term head = stack.Pop();

                if (object.ReferenceEquals(head, null)) {

                    head = stack.Pop();

                    if (head.Op == PrettyPrinter.Highlight) {
                        var child = args.Pop();
                        sb.Append("@#$");
                        sb.Append(child.Text);
                        sb.Append("@#$");
                        child.Text = sb.ToString();
                        sb.Clear();
                        args.Push(child);
                        continue;
                    }

                    PrecString result = new PrecString();
                    result.Prec = int.MaxValue;

                    switch (Special.GetKind(head.Op)) {

                        case Special.Kind.Let:
                            var body = args.Pop();
                            var binding = args.Pop();
                            PrintLet(binding, body, sb);
                            break;

                        case Special.Kind.Lambda:
                            PrintLambda(args.Pop(), sb);
                            break;

                        case Special.Kind.Index:
                            var offset = args.Pop();
                            var _base = args.Pop();
                            PrintIndex(_base, offset, sb);
                            break;

                        default:

                            var children = args.PopFIFO(head.Arity);
                            var syntax = head.Op.Syntax;

                            switch (syntax.Style) {
                                case OperatorSyntax.Fix.FunctionCall:
                                    PrintFunctionCall(syntax.Name, children, sb);
                                    break;
                                case OperatorSyntax.Fix.LeftAssociative:
                                case OperatorSyntax.Fix.RightAssociative:
                                    PrintInfix(syntax, children[0], children[1], sb);
                                    result.Prec = syntax.Precedence;
                                    break;
                                case OperatorSyntax.Fix.Prefix:
                                    PrintPrefix(syntax, children[0], sb);
                                    result.Prec = syntax.Precedence;
                                    break;
                                case OperatorSyntax.Fix.Postfix:
                                    PrintPostfix(syntax, children[0], sb);
                                    result.Prec = syntax.Precedence;
                                    break;
                            }

                            break;
                    }

                    result.Text = sb.ToString();
                    sb.Clear();
                    args.Push(result);
                    continue;
                }

                // Nullarys pass through
                if (head.Arity == 0) {
                    args.Push(new PrecString { Text = head.Op.Name, Prec = int.MaxValue });
                    continue;
                }

                // Return marker
                stack.Push(head);
                stack.Push(null);

                // Schedule children
                switch (Special.GetKind(head.Op)) {
                    case Special.Kind.Let:
                        stack.Push(head[2]);
                        stack.Push(Special.Equals[head[0], head[1]]);
                        break;
                    case Special.Kind.Lambda:
                        stack.Push(Special.MapsTo[head[0], head[1]]);
                        break;
                    default:
                        for (int i = head.Arity - 1; i >= 0; --i) {
                            stack.Push(head[i]);
                        }
                        break;
                }
            }

            return args.Pop().Text;
        }

        static bool NeedsParens(int pChild, int childIndex, OperatorSyntax syntax)
        {
            if (pChild != syntax.Precedence) {
                return pChild < syntax.Precedence;
            }

            switch (syntax.Style) {
                case OperatorSyntax.Fix.LeftAssociative:
                case OperatorSyntax.Fix.Postfix:
                    return childIndex == 1;
                case OperatorSyntax.Fix.RightAssociative:
                case OperatorSyntax.Fix.Prefix:
                    return childIndex == 0;
                default:
                    System.Diagnostics.Debug.Assert(false, "Condition shouldn't arise.");
                    return false;
            }
        }

        class NameArity
        {
            public string Name { get; set; }
            public int Arity { get; set; }
        }

        static string SafeName(Operator op, Dictionary<NameArity, List<Operator>> dict)
        {
            var na = new NameArity { Name = op.Name, Arity = op.Arity };

            List<Operator> list = null;
            if (dict.TryGetValue(na, out list)) {
                int ind = list.IndexOf(op);
                if (-1 == ind) {
                    ind = list.Count;
                    list.Add(op);
                }
                if (ind == 0) {
                    return op.Name;
                }
                return string.Format("{0} ({1})", op.Name, ind);
            }

            list = new List<Operator>();
            list.Add(op);
            dict.Add(na, list);
            return op.Name;
        }

        public static string PrintTree(Term term)
        {
            const string vertline = "\x2502  ";
            const string blankspace = "  ";
            const string lastbranch = "\x2514\x2500 ";
            const string innerbranch = "\x251c\x2500 ";

            var clashCounter = new Dictionary<NameArity, List<Operator>>();

            StringBuilder sb = new StringBuilder();
            sb.Append(term.Op).AppendLine();

            Stack<Term> stack = new Stack<Term>();
            for (int i = term.Arity - 1; i >= 0; --i) {
                stack.Push(term[i]);
            }

            List<int> columns = new List<int>();
            columns.Add(term.Arity);

            while (stack.Count > 0) {
                var head = stack.Pop();

                // Remove columns with no current entries
                while (0 == columns.Last()) {
                    columns.RemoveAt(columns.Count - 1);
                }

                // Fill-in columns
                for (int i = 0; i < columns.Count - 1; ++i) {
                    if (columns[i] != 0) {
                        sb.Append(vertline);
                    }
                    else {
                        sb.Append(blankspace);
                    }
                }
                if (1 == columns.Last()) {
                    sb.Append(lastbranch);
                }
                else {
                    sb.Append(innerbranch);
                }

                // Print operator name
                //sb.Append(head.Op).AppendLine();
                string name = SafeName(head.Op, clashCounter);
                sb.Append(name).AppendLine();

                // Used up one sibling
                Assert(columns.Last() > 0, "Miscounted siblings.");
                columns[columns.Count - 1] -= 1;

                // Make a note of the number of children and extend columns.
                if (head.Arity > 0) {
                    columns.Add(head.Arity);
                }
                // push_back children to stack.
                for (int i = head.Arity - 1; i >= 0; --i) {
                    stack.Push(head[i]);
                }
            }

            return sb.ToString();
        }
    }
}