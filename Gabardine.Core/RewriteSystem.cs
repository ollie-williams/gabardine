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
using System.Diagnostics;
using System.Linq;

namespace Gabardine
{
    public class RewriteResult : OneStepTendril
    {
        readonly Address address;        
        readonly RewriteRule rule;
        readonly Term rewrite;

        public RewriteResult(Address address, RewriteRule rule, Term rewrite)
        {
            this.address = address;
            this.rule = rule;
            this.rewrite = rewrite;
        }

        public RewriteRule Rule { get { return rule; } }

        public override Term Matched(Term root)
        {
            return address.Get(root);
        }

        public override Term Transform(Term root)
        {
            return address.Replace(root, rewrite);
        }
    }    

    public class RewriteSystem : Transformer<RewriteResult>
    {        
        readonly Dictionary<Term, Term> normalForms = new Dictionary<Term, Term>();
        readonly Dictionary<Operator, List<RewriteRule>> rules = new Dictionary<Operator, List<RewriteRule>>();

        public void AddRule(RewriteRule rule)
        {
            //RewriteRule rule = RewriteRuleFactory.Create(id, lhs, rhs, Enumerable.Empty<ConditionPattern>(), priority, isDefault);
            List<RewriteRule> list;
            if (!rules.TryGetValue(rule.Pattern.Op, out list)) {
                list = new List<RewriteRule>();
                rules.Add(rule.Pattern.Op, list);
            }
            list.Add(rule);
            list.Sort(RuleComparison);
        }

        public void AddRule(Term lhs, Term rhs)
        {
            AddRule(RewriteRuleFactory.Create(lhs, rhs, 0, true));
        }

        public void AddEquivalence(Term lhs, Term rhs)
        {
            AddRule(RewriteRuleFactory.Create(lhs, rhs, 0, false));
            AddRule(RewriteRuleFactory.Create(rhs, lhs, 0, false));
        }

        public IEnumerable<RewriteRule> Rules
        {
            get
            {
                foreach (var lst in rules.Values) {
                    foreach (var rule in lst) {
                        yield return rule;
                    }
                }
            }
        }

        public IEnumerable<RewriteRule> OrderedRules
        {
            get
            {
                return Rules.OrderBy(r => r.Id);
            }
        }

        int RuleComparison(RewriteRule a, RewriteRule b)
        {
            int p_delta = b.Priority - a.Priority;
            if (p_delta == 0) {
                return b.Id - a.Id;
            }
            return p_delta;
        }

        /// <summary>
        ///   Gets the list of rules which might match the term at <paramref name="node"/>,
        ///   in priority order.
        /// </summary>
        IEnumerable<RewriteRule> GetPossibleRules(WalkContext node, bool includeReversible)
        {
            if (rules.TryGetValue(node.Term.Op, out List<RewriteRule> list)) {
                if (!includeReversible) {
                    return list.Where(x => x.IsDefault);
                }
                return list;
            }
            return Enumerable.Empty<RewriteRule>();
        }

        /// <summary>
        ///   Gets all possible rewrites anchored at <paramref
        ///   name="node"/>, including reverse direction.
        /// </summary>
        IEnumerable<RewriteResult> RootRewrites(WalkContext node)
        {
            var seq = GetPossibleRules(node, true);
            foreach (var rule in seq) {
                if (rule.TryRewrite(node.Term, this, out Term rewrite)) {
                    yield return new RewriteResult(node.GetAddress(), rule, rewrite);
                }
            }            
        }


        /// <summary>
        ///   Gets the highest priority rewrite of the term at <paramref name="node"/>,
        ///   only taking the default direction into consideration. If successful,
        ///   <paramref name="rule"/> is set to the matching rule, and the rewrite is
        ///   returned. Otherwise null.
        /// </summary>
        Term GetDirectRewrite(WalkContext node, out RewriteRule rule)
        {
            rule = null;
            var seq = GetPossibleRules(node, false);
            foreach (var r in seq) {
                if (r.TryRewrite(node.Term, this, out Term rewrite)) {
                    rule = r;
                    return rewrite;
                }
            }
            return null;
        }

        /// <summary>
        ///   Implements <code>Transformer&lt;RewriteResult&gt;</code> by returning all
        ///   possible rewrites, in either direction, of <paramref name="root"/>, at any
        ///   location.
        /// </summary>
        public IEnumerable<RewriteResult> FindTransformations(Term root)
        {
            var stack = new Stack<WalkContext>();
            Stack<Term> args = new Stack<Term>();
            stack.Push(new WalkContext(root));

            while (stack.Count > 0) {
                var head = stack.Pop();

                if (ReferenceEquals(head, null)) {
                    head = stack.Pop();
                    args.PopFIFO(head.Arity);
                    var rws = RootRewrites(head);
                    foreach (var rw in rws) {
                        yield return rw;
                    }
                    args.Push(head.Term);
                    continue;
                }

                stack.Push(head);
                stack.Push(null);
                for (int i = head.Arity - 1; i >= 0; --i) {
                    stack.Push(head.MoveToChild(i));
                }
            }

            yield break;
        }

        Term RewriteUnordered(WalkContext src)
        {
            WalkContext node = src;

            bool changed = true;
            while(changed) {
                Term[] args = new Term[node.Arity];
                for (int i = 0; i < args.Length; ++i) {
                    args[i] = RewriteUnordered(node.MoveToChild(i));
                }
                node = node.WithTerm(new Term(node.Term.Op, args));                
                
                Term rw = GetDirectRewrite(node, out RewriteRule rule);
                if (object.ReferenceEquals(rw, null)) {
                    break;
                }

                bool failure = rw.Op == Special.FailWith;
                if (failure) {
                    var message = ConsUtils.UnconsMany(rw[0]).Select(x => x.ToString()).Aggregate((u, v) => u + v);
                    throw new Exception(message);
                }
                node = node.WithTerm(rw);
                
            }

            return node.Term;
        }

        /// <summary>
        ///   Repeatedly rewrites <paramref name="source"/> to termination (i.e., until no
        ///   more rules match anywhere).
        /// </summary>
#if false
        public Term RewriteUnordered(Term source, bool dump_rewrites = false)
        {
            return RewriteUnordered(new WalkContext(source));
        }

#else
        public Term RewriteUnordered(Term source, bool dump_rewrites = false)
        {
            if (!dump_rewrites) {
                return RewriteUnordered(source, null);
            }

            RewriteHistory history = new RewriteHistory();
            Term retval = RewriteUnordered(source, history);
            foreach (var step in history.Steps) {
                step.Print(Terminal.Stdout);
            }
            return retval;
        }

        public Term RewriteUnordered(Term source, RewriteHistory history)
        {
            var stack = new Stack<WalkContext>();
            var args = new Stack<Term>();
            var trace = new Stack<Term>();
            var start = new Stack<int>();

            bool dumpRewrites = !object.ReferenceEquals(history, null);

            stack.Push(new WalkContext(source));
            start.Push(0);
            while (stack.Count > 0) {

                var head = stack.Pop();

                // Children have been processed
                if (ReferenceEquals(head, null)) {

                    // Rebuild term in terms of fully-rewritten children
                    head = stack.Pop();
                    Operator op = head.Term.Op;
                    head = head.WithTerm(new Term(op, args.PopFIFO(op.Arity)));

                    // Can we rewrite this new version?
                    Term rewrite = GetDirectRewrite(head, out RewriteRule matched);
                    if (ReferenceEquals(rewrite, null)) {
                        // This subterm has terminated.
                        args.Push(head.Term);
                        int lim = start.Pop();
                        while (trace.Count > lim) {
                            normalForms.Add(trace.Pop(), head.Term);
                        }
                        continue;
                    }

                    bool failure = rewrite.Op == Special.FailWith;

                    if (dumpRewrites) {
                        history.AddStep(stack, args, head, rewrite, matched);
                    }

                    if (failure) {
                        var message = ConsUtils.UnconsMany(rewrite[0]).Select(x => x.ToString()).Aggregate((u, v) => u + v);
                        throw new Exception(message);
                    }

                    head = head.WithTerm(rewrite);

                    // Rewrite successful. Reschedule children.
                }
      
                // Has this term been processed? If so, push its terminal form to argument
                // stack. If not, process it.
                if (!dumpRewrites) {
                    if (normalForms.TryGetValue(head.Term, out Term nf)) {
                        args.Push(nf);
                        int lim = start.Pop();
                        while (trace.Count > lim) {
                            normalForms.Add(trace.Pop(), nf);
                        }
                        continue;
                    }
                }

                // Visit children with return marker
                stack.Push(head);
                stack.Push(null);
                for (int i = head.Arity - 1; i >= 0; --i) {
                    start.Push(trace.Count);
                    stack.Push(head.MoveToChild(i));
                }
            }

            Debug.Assert(args.Count == 1, "Only result should remain.");
            return args.Pop();
        }
#endif
        

        


        
    }
}