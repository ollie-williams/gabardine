/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System.Collections.Generic;
using System.Diagnostics.Debug;

namespace Gabardine
{
    using System.Linq;
    using OccurrenceTable = Dictionary<Operator, LinkedList<int>>;

    public static class RewriteRuleFactory
    {
        static int ruleCount = 0;

        public static RewriteRule Create(Term pattern, Term target, int priority = 0, bool isDefault = true, string name="")
        {
            return Create(pattern, target, Enumerable.Empty<ConditionPattern>(), priority, isDefault, name);
        }

        public static RewriteRule Create(Term pattern, Term target, IEnumerable<ConditionPattern> cps, int priority = 0, bool isDefault = true, string name = "")
        {
            PatternMatcher pm = PatternMatcherFactory.Create(pattern);
            return Create(pm, target, cps, priority, isDefault, name);
        }

        public static RewriteRule Create(PatternMatcher pm, Term target, IEnumerable<ConditionPattern> cps, int priority = 0, bool isDefault = true, string name = "")
        {
            int id = System.Threading.Interlocked.Increment(ref ruleCount);
            TargetBuilder tb = CreateTargetBuilder(pm, target);
            var conds = cps.Select(cp => cp.Create(pm));
            return new RewriteRule(name, id, target, pm, conds, tb, priority, isDefault);
        }

#if false
        /// <summary>
        /// If the sets of variables on both sides of the rule are identical, the
        /// rule can be reversed.
        /// </summary>
        bool TryReverse(RewriteRule rule, out RewriteRule reversed)
        {
            var lhsVars = Replacement.Find(rule.Pattern, t => t.Op.Kind == OperatorKind.PatternVariable);
            var rhsVars = Replacement.Find(rule.Target, t => t.Op.Kind == OperatorKind.PatternVariable);

            int delta = lhsVars.Except(rhsVars).Count();
            if (delta != 0) {
                reversed = null;
                return false;
            }

            reversed = Create(rule.Target, rule.Pattern, rule.Conditions, rule.Priority, false);
            return true;
        }
 #endif

        /// <summary>
        /// Creates a builder for the right-hand-side of a rewrite rule
        /// (target). The variables matched in the left hand side will be
        /// ordered. The order is held in map. The aim is to process target as
        /// much as possible at this point to make rewriting fast.
        /// </summary>
        internal static TargetBuilder CreateTargetBuilder(PatternMatcher matcher, Term target)
        {
            List<TargetBuilderStep> steps = new List<TargetBuilderStep>();
            List<int> variables = new List<int>();
            OccurrenceTable variableOccurrences = new OccurrenceTable();


            // Walk the tree to build the reverse of the rebuilding order.
            Stack<Term> stack = new Stack<Term>();
            stack.Push(target);
            while (stack.Count > 0) {
                Term head = stack.Pop();

                if (head.Op.Kind == OperatorKind.PatternVariable) {
                    variables.Add(matcher.GetVariableIndex(head.Op));
                    AppendOccurrence(variableOccurrences, head.Op, steps.Count);
                }
                steps.Add(new TargetBuilderStep(head.Op, StepSignificance.None));

                // Push children onto stack.
                for (int i = 0; i < head.Arity; ++i) {
                    stack.Push(head[i]);
                }
            }

            // For each variable occurrance, set its significance. Since the
            // sequence will be reversed, the "first" instance, should be the
            // last one set.
            foreach (var vo in variableOccurrences) {
                var list = vo.Value;
                Assert(list.Count > 0, "Expected at least one occurrence.");

                if (list.Count == 1) {
                    steps[list.Last.Value].Significance = StepSignificance.OnlyVariableInstance;
                    continue;
                }

                steps[list.Last.Value].Significance = StepSignificance.OnlyVariableInstance;
                var node = list.Last.Previous;
                while (node != null) {
                    steps[node.Value].Significance = StepSignificance.OnlyVariableInstance;
                    node = node.Previous;
                }
            }

            steps.Reverse();
            variables.Reverse();
            return new TargetBuilder(steps.ToArray(), variables.ToArray());
        }

        private static void AppendOccurrence(OccurrenceTable variableOccurrences, Operator op, int index)
        {
            LinkedList<int> list;
            if (!variableOccurrences.TryGetValue(op, out list)) {
                list = new LinkedList<int>();
                variableOccurrences.Add(op, list);
            }
            list.AddLast(index);
        }
    }
}
