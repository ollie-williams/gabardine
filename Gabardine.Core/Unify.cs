/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;
using System.Linq;
using System.Collections.Generic;

namespace Gabardine
{
    public class Unifier
    {
        Dictionary<Operator, Term> assignments = new Dictionary<Operator, Term>();
        bool isClean = true;

        public bool Unify(Term x, Term y)
        {
            var stack = new Stack<Tuple<Term, Term>>();
            stack.Push(Tuple.Create(x, y));
            while (stack.Count > 0) {
                var top = stack.Pop();
                var head = Map(top, Reduce);
                var ops = Map(head, t => t.Op);
                var isvar = Map(head, IsVar);

                if (head.Item1 == head.Item2) {
                    continue;
                }

                if (isvar.Item1 && isvar.Item2) {
                    Union(ops.Item1, head.Item2);
                    continue;
                }

                if (isvar.Item2) {
                    head = Swap(head);
                    ops = Swap(ops);
                    isvar = Swap(isvar);
                }

                if (isvar.Item1) {
                    if (Occurs(ops.Item1, head.Item2)) {
                        return false;
                    }
                    Union(ops.Item1, head.Item2);
                    continue;
                }

                if (ops.Item1 != ops.Item2) {
                    return false;
                }

                for (int i = 0; i < ops.Item1.Arity; ++i) {
                    stack.Push(Map(head, t => t[i]));
                }
            }

            return true;
        }

        public void Clear()
        {
            assignments.Clear();
            isClean = true;
        }

        public Term Find(Operator op)
        {
            Term term = op;
            do {
                if (assignments.TryGetValue(term.Op, out Term lookup)) {
                    term = lookup;
                }
                else {
                    break;
                }
            } while (IsVar(term));

            return term;
        }

        public Term Reduce(Term term)
        {
            foreach(var kv in assignments) {
                term = Replacement.Replace(term, kv.Key.CreateTerm(), kv.Value);
            }
            return term;
        }

        public void Eliminate(Term variable)
        {
            if (!IsVar(variable)) {
                throw new ArgumentException("variable must be a variable.");
            }

            CleanupAssignments();
            Term rhs = Find(variable.Op);
            
            // Easy case: variable lives on the left hand side and we
            // can simply drop its record.
            if (rhs != variable) {                                
                assignments.Remove(variable.Op);
                return;
            }

            // Hard case: variable lives on right hand side and needs
            // to be swapped-out.
            KeyValuePair<Operator, Term> swap;
            bool gotSwap = assignments.TryGetFirst(kv => kv.Value == variable, out swap);
            if (!gotSwap) {
                if (assignments.Values.Any(t => Replacement.Contains(t, variable))) {
                    throw new Exception(string.Format("Can't eliminate {0}", variable));
                }
                return;
            }

            Term replacement = swap.Key.CreateTerm();
            Dictionary<Operator, Term> result = new Dictionary<Operator, Term>();
            foreach(var kv in assignments) {
                if (kv.Key != swap.Key) {
                    result.Add(kv.Key, Replacement.Replace(kv.Value, variable, replacement));
                }                
            }
            assignments = result;
            isClean = false;
            CleanupAssignments();
        }

        void CleanupAssignments()
        {
            if (isClean) {
                return;
            }

            Dictionary<Operator, Term> updated = new Dictionary<Operator, Term>();
            foreach (var key in assignments.Keys) {
                Term reduction = Reduce(key.CreateTerm());
                updated.Add(key, reduction);
            }
            assignments = updated;
            isClean = true;
        }

        bool IsVar(Term term)
        {
            return term.Op.Kind == OperatorKind.PatternVariable;
        }

        bool Occurs(Operator variable, Term term)
        {
            return GetVars(term).Contains(variable);
        }

        IEnumerable<Operator> GetVars(Term term)
        {
            var stack = new Stack<Term>();
            stack.Push(term);
            while (stack.Count > 0) {
                var head = stack.Pop();
                if (IsVar(head)) {
                    if (assignments.TryGetValue(head.Op, out Term lookup)) {
                        stack.Push(lookup);
                    }
                    else {
                        yield return head.Op;
                    }
                    continue;
                }

                for (int i = 0; i < head.Arity; ++i) {
                    stack.Push(head[i]);
                }
            }
        }

        void Union(Operator variable, Term term)
        {
            assignments.Add(variable, term);
            isClean = false;
        }

        static Tuple<T, T> Map<V, T>(Tuple<V, V> pair, Func<V, T> f)
        {
            return Tuple.Create(f(pair.Item1), f(pair.Item2));
        }

        static Tuple<T, V> Swap<V, T>(Tuple<V, T> pair)
        {
            return Tuple.Create(pair.Item2, pair.Item1);
        }
    }
}