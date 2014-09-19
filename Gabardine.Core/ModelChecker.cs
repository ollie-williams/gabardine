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
using System.Linq;

namespace Gabardine
{
    public class ModelChecker
    {
        readonly RewriteSystem rw;

        public ModelChecker(RewriteSystem rw)
        {
            this.rw = rw;
        }

        public Term CheckAndIngest(Term t)
        {
            Term model = Special.Sound[t];
            model = rw.RewriteUnordered(model);
            return DigestModel(model);
        }

        Term DigestModel(Term model)
        {
            while (true) {
                Console.WriteLine(model);
                var clauses = ConsUtils.DisaggregateMany(Special.And, model);
                clauses = clauses.Select(c => DigestClause(c));
                Term newModel = ConsUtils.RightAssociate(Special.And, clauses);
                newModel = rw.RewriteUnordered(newModel);
                if (newModel == model) {
                    break;
                }
                model = newModel;
            }
            return model;
        }

        Term DigestClause(Term clause)
        {
            clause = rw.RewriteUnordered(clause);
            if (clause.Op != Special.Equals) {
                return clause;
            }

            Term lhs = clause[0];
            Term rhs = clause[1];

            int ord = LPO(lhs, rhs, mdo.Compare);

            if (ord >= 0) {
                rw.AddRule(lhs, rhs);
            }
            else {
                rw.AddRule(rhs, lhs);
            }

            return Special.True;
        }

        class MetaDataOrdering : IComparer<Operator>
        {
            HashSet<Operator> metaOps = new HashSet<Operator>();

            public MetaDataOrdering()
            {
                metaOps.Add(Special.Typeof);
                metaOps.Add(Special.MapsTo);
            }

            public int Compare(Operator x, Operator y)
            {
                bool xin = metaOps.Contains(x);
                bool yin = metaOps.Contains(y);
                if (xin == yin) {
                    return 0;
                }
                if (xin) {
                    return +1;
                }
                return -1;
            }
        }

        static MetaDataOrdering mdo = new MetaDataOrdering();

        static int Lex(Comparison<Term> ordering, IEnumerable<Tuple<Term, Term>> pairs)
        {
            foreach (var pair in pairs) {
                int ord = ordering(pair.Item1, pair.Item2);
                if (ord > 0) { return +1; }
                if (ord < 0) { return -1; }
            }
            return 0;
        }

        static int LPO(Term s, Term t, Comparison<Operator> ordering)
        {
            if (s == t) {
                return 0;
            }

            if (s.Arity > 0) {
                if (s.Children().Any(si => LPO(si, t, ordering) >= 0)) {
                    return +1;
                }
            }

            int ord = ordering(s.Op, t.Op);
            if (ord < 0) {
                return -1;
            }

            bool sti = t.Children().All(ti => LPO(s, ti, ordering) > 0);
            if (ord > 0) {
                return sti ? +1 : -1;
            }

            System.Diagnostics.Debug.Assert(ord == 0);
            if (sti) {
                return Lex((u, v) => LPO(u, v, ordering), s.Children().Zip(t.Children(), (si, ti) => Tuple.Create(si, ti)));
            }
            else {
                return -1;
            }

        }
    }
}
