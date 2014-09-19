/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace Gabardine
{

    struct Candidate 
    {
        public PatternMatcher matcher;

        /// <summary>
        ///   Seed candidates are formed from a single, non-variable,
        ///   operator. They're the base case for abstractable terms,
        ///   but we don't propose lifting these as abstractions,
        ///   since this wouldn't achieve anything that the function
        ///   doesn't already do on its own.
        /// </summary>
        public bool isSeed;

        public Candidate(PatternMatcher matcher, bool isSeed)
        {
            this.matcher = matcher;
            this.isSeed = isSeed;
        }            
    }

    public class Abstraction
    {
        string[] variableNames = new string[] { "α", "β", "γ", "δ", "ε", "ζ", "η", "θ" };
        
        List<PatternMatcher> final = new List<PatternMatcher>();
        Queue<Candidate> candidates;

        Term MakeLambda(PatternMatcher pattern)
        {
            // Match against self to get variables
            pattern.Match(pattern.Pattern);
            Term fun = pattern.Pattern;
            foreach (Term v in pattern.VariableBindings) {                
                Term lvar = new LambdaVariable(v.Op.Name);
                fun = Replacement.Replace(fun, v, lvar);
                fun = Special.Lambda[lvar, fun];
            }
            return fun;
        }

        public Term Lift(Term root, PatternMatcher pattern)
        {
            Term fun = MakeLambda(pattern);
            Term letVar = new LetVariable("fun", fun);

            pattern.Match(pattern.Pattern);
            Term rhs = letVar;
            foreach (Term v  in pattern.VariableBindings) {
                rhs = Special.Eval[rhs, v];
            }
            TargetBuilder builder = RewriteRuleFactory.CreateTargetBuilder(pattern, rhs);

            Term body = root;
            pattern.MatchEverywhere(root, (wc, pm) =>
            {
                Term rewrite = builder.Build(pm.VariableBindings);
                body = wc.GetAddress().Replace(body, rewrite);
            });

                       
            Term letexpr = Special.Let[letVar, fun, body];
            return letexpr;
        }

        Term GetVariable(int index)
        {
            string name = variableNames[index % variableNames.Length];
            int instance = index / variableNames.Length;
            if (instance != 0) { name += instance; }
            return new Operator(0, OperatorKind.PatternVariable, name);
        }

        Term MakePattern(Operator op, int startIndex = 0)
        {
            var variables = Enumerable.Range(0, op.Arity).Select(i => GetVariable(i + startIndex));
            return op.CreateTerm(variables);
        }

        static void AddCount<T>(Dictionary<T, int> dict, T item)
        {
            if (dict.TryGetValue(item, out int c)) {
                dict[item] = c + 1;
            }
            else {
                dict.Add(item, 1);
            }
        }

        /// <summary>
        ///   Attempt to extend the pattern by replacing variables
        ///   with concrete operators to result in a pattern that
        ///   still matches in multiple locations of root.
        /// </summary>
        bool TryToGrow(Term root, PatternMatcher cand)
        {
            // Determine the indices of the variables on the cheap
            bool ok = cand.Match(cand.Pattern);
            Debug.Assert(ok);
            Term[] variables = new Term[cand.VariableBindings.Length];
            cand.VariableBindings.CopyTo(variables, 0);

            // Count occurrences of each operator in each variable
            // slot.
            Dictionary<Operator, int>[] dicts = new Dictionary<Operator, int>[cand.VariableBindings.Length];
            for (int i = 0; i < dicts.Length; ++i) {
                dicts[i] = new Dictionary<Operator, int>();
            }
            cand.MatchEverywhere(root, (wc, pm) =>
            {
                for (int i = 0; i < dicts.Length; ++i) {
                    AddCount(dicts[i], cand.VariableBindings[i].Op);
                }
            });

            // Any occurring more than once can be made into a new pattern
            bool subsumed = false;
            for (int i = 0; i < dicts.Length; ++i) {
                // If there was only ever one operator in this slot,
                // then the original candidate was too general and can
                // be subsumed.
                subsumed |= dicts[i].Count == 1;
                var multiple = dicts[i].Where(kv => kv.Value > 1);
                foreach (var kv in multiple) {
                    Term stub = MakePattern(kv.Key, variables.Length);
                    Term pattern = Replacement.Replace(cand.Pattern, variables[i], stub);
                    candidates.Enqueue(new Candidate(PatternMatcherFactory.Create(pattern), false));
                }
            }

            return subsumed;
        }

        void CreateSeedCandidates(Term root)
        {
            // Count occurrences of operators
            var opCounts = new Dictionary<Operator, int>();
            Walk.VisitNodes(root, t =>
                {
                    if (t.Arity == 0) return true;
                    AddCount(opCounts, t.Op);
                    return true;
                });

            candidates = new Queue<Candidate>(
                opCounts
                .Where(kv => kv.Value > 1)
                .Select(kv => PatternMatcherFactory.Create(MakePattern(kv.Key)))
                .Select(pm => new Candidate(pm, true))
                );

        }

        public IEnumerable<Term> Process(Term root)
        {
            CreateSeedCandidates(root);
            while (candidates.Count > 0) {
                Candidate cand = candidates.Dequeue();
                bool subsumed = TryToGrow(root, cand.matcher);

                // If the more general candidate accounted for more
                // instances of the pattern than its more specialized
                // derivatives, then it might be worth keeping around.
                if (!subsumed && !cand.isSeed) {
                    final.Add(cand.matcher);
                }
            }

            return final.Select(p => Lift(root, p));
        }
    }

}