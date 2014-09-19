/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Gabardine
{
    public class RewriteStep
    {
        readonly RewriteRule rule;
        readonly Term root;
        readonly Term before;
        readonly Term after;
        readonly Address location;

        public RewriteStep(RewriteRule rule, Term root, Term before, Term after, Address location)
        {
            this.rule = rule;
            this.root = root;
            this.before = before;
            this.after = after;
            this.location = location;
        }

        public RewriteRule Rule { get { return rule; } }
        public Term Root {  get { return root; } }
        public Term Before { get { return before; } }
        public Term After { get { return after; } }
        public Address Location { get { return location; } }

        public void Print(Terminal terminal, PrintFormat format = PrintFormat.Infix)
        {
            terminal.PushFormat(TerminalFormat.Cyan);
            terminal.SendLine(PrettyPrinter.Format(rule.Composed(), format));
            terminal.PopFormat();

            PrettyPrinter.Print(Location.Replace(root, PrettyPrinter.Highlight[before]), format, terminal, TerminalFormat.Red);
            terminal.SendLine();

            PrettyPrinter.Print(Location.Replace(root, PrettyPrinter.Highlight[after]), format, terminal, TerminalFormat.Green);
            terminal.SendLine();
            terminal.SendLine();
        }
    }

    public class RewriteHistory
    {
        readonly List<RewriteStep> steps = new List<RewriteStep>();

        public IEnumerable<RewriteStep> Steps { get { return steps; } }

        internal void AddStep(Stack<WalkContext> stack,
                               Stack<Term> args,
                               WalkContext oldterm,
                               Term after,
                               RewriteRule rule)
        {
            Term root = Rebuild(stack, args, oldterm.Term);
            RewriteStep step = new RewriteStep(rule, root, oldterm.Term, after, oldterm.GetAddress());
            steps.Add(step);
        }

        // TODO: Now that we're using WalkContext, this can probably be sped up..?
        Term Rebuild(Stack<WalkContext> stack, Stack<Term> args, Term newTerm)
        {
            var stackCopy = new Stack<WalkContext>(stack.Reverse());
            Stack<Term> argsCopy = new Stack<Term>(args.Reverse());
            argsCopy.Push(newTerm);

            while (stackCopy.Count > 0) {
                var head = stackCopy.Pop();

                if (ReferenceEquals(head, null)) {
                    head = stackCopy.Pop();
                    Term built = new Term(head.Term.Op, argsCopy.PopFIFO(head.Arity));
                    argsCopy.Push(built);
                    continue;
                }

                argsCopy.Push(head.Term);
            }
            Debug.Assert(argsCopy.Count == 1, "Expected only result to reamin.");
            return argsCopy.Pop();
        }
    }
}