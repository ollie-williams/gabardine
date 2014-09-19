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

namespace Gabardine
{
    class MatcherStep
    {
        public int VariableIndex { get; set; }
        public Operator Op { get; set; }
    }

    public class PatternMatcher
    {
        readonly MatcherStep[] steps;
        readonly Dictionary<Operator, int> variableMap;
        readonly Term[] variableBindings;
        readonly Term pattern;

        internal PatternMatcher(MatcherStep[] steps, Dictionary<Operator, int> variableMap, Term pattern)
        {
            this.steps = steps;
            this.variableMap = variableMap;
            variableBindings = new Term[variableMap.Count];
            this.pattern = pattern;
        }

        public Term[] VariableBindings { get { return variableBindings; } }

        public Term Pattern {  get { return pattern; } }

        public int GetVariableIndex(Operator op)
        {
            return variableMap[op];
        }

        public Term GetBinding(Term variable)
        {
            Operator op = variable.Op;
            if (op.Kind != OperatorKind.PatternVariable) {
                throw new ArgumentException("Expected a variable. Got: " + variable);
            }

            int index = GetVariableIndex(op);
            return VariableBindings[index];
        }

        public bool Match(Term candidate)
        {
            Array.Clear(variableBindings, 0, variableBindings.Length);

            Stack<Term> stack = new Stack<Term>();
            stack.Push(candidate);
            int i = -1;
            while (stack.Count > 0) {
                Term head = stack.Pop();
                ++i;

                // Are we binding a variable?
                int vi = steps[i].VariableIndex;
                if (vi >= 0) {
                    if (ReferenceEquals(variableBindings[vi], null)) {
                        variableBindings[vi] = head;
                    }
                    else if (variableBindings[vi] != head) {
                        return false;
                    }
                    continue;
                }

                Operator op = steps[i].Op;

                // Is this a wildcard variable?
                if (ReferenceEquals(op, null)) {
                    continue;
                }

                // Ensure operators match
                if (op != head.Op) {
                    return false;
                }

                for (int j = 0; j < head.Arity; ++j) {
                    stack.Push(head[j]);
                }
            }

            return true;
        }

        internal void MatchEverywhere(Term root, Func<WalkContext, PatternMatcher, bool> action)
        {
            Stack<WalkContext> stack = new Stack<WalkContext>();
            stack.Push(new WalkContext(root));
            while (stack.Count > 0) {
                var head = stack.Pop();

                // Abort search when we run out of size
                if (head.Term.Size < Pattern.Size || head.Term.MaxDepth < Pattern.MaxDepth) {
                    continue;
                }

                // Do we match?
                if (Match(head.Term)) {
                    if (!action(head, this)) {
                        return;
                    }
                }

                // Visit children
                for (int i = 0; i < head.Arity; ++i) {
                    stack.Push(head.MoveToChild(i));
                }
            }
        }

        internal void MatchEverywhere(Term root, Action<WalkContext, PatternMatcher> action)
        {
            MatchEverywhere(root, (wc, pm) => { action(wc, pm); return true; });
        }

        public void MatchEverywhere(Term root, Func<Address, Term, PatternMatcher, bool> action)
        {
            MatchEverywhere(root, (wc, pm) => action(wc.GetAddress(), wc.Term, pm));
        }

        public bool MatchAnywhere(Term root, out Address address)
        {
            Address ad = null;
            bool success = false;
            MatchEverywhere(root, (wc, pm) => { ad = wc.GetAddress(); success = true;  return false; });
            address = ad;
            return success;
        }

        public bool MatchAnywhere(Term root)
        {
            bool success = false;
            MatchEverywhere(root, (wc, pm) => { success = true; return false; });
            return success;
        }

        public override string ToString()
        {
            return pattern.ToString();
        }
    }

    public static class PatternMatcherFactory
    {
        /// <summary>
        /// Attempt to insert the variable as a new value. If the variable
        /// already exists in the map, then the existing entry is returned.
        /// </summary>
        private static int GetOrAddVariableIndex(Dictionary<Operator, int> variableMap, Operator variable)
        {
            Assert(variable.Kind == OperatorKind.PatternVariable, "Expected a variable.");
            int index;
            if (!variableMap.TryGetValue(variable, out index)) {
                index = variableMap.Count;
                variableMap.Add(variable, index);
            }
            return index;
        }

        public static PatternMatcher Create(Term pattern)
        {
            List<MatcherStep> steps = new List<MatcherStep>();
            Dictionary<Operator, int> variableMap = new Dictionary<Operator, int>();

            Stack<Term> stack = new Stack<Term>();
            stack.Push(pattern);
            while (stack.Count > 0) {
                Term head = stack.Pop();

                if (head == Special.Wildcard) {
                    steps.Add(new MatcherStep { VariableIndex = -1, Op = null });
                    continue;
                }

                Operator op = head.Op;

                if (op.Kind == OperatorKind.PatternVariable) {
                    int index = GetOrAddVariableIndex(variableMap, head.Op);
                    steps.Add(new MatcherStep { VariableIndex = index, Op = null });
                    continue;
                }

                steps.Add(new MatcherStep { VariableIndex = -1, Op = op });
                for (int i = 0; i < head.Arity; ++i) {
                    stack.Push(head[i]);
                }
            }

            return new PatternMatcher(steps.ToArray(), variableMap, pattern);
        }
    }
}