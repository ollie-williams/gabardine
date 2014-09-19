/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System.Collections.Generic;
using System.Linq;
    
namespace Gabardine
{
    public static class ConsUtils
    {
        public static Term RightAssociate(Term seed, Operator aggregator, params Term[] elements)
        {
            if (aggregator.Arity != 2) { throw new System.ArgumentException("aggregator must be binary."); }
            Term retval = seed;
            for (int i = elements.Length - 1; i >= 0; --i) {
                retval = aggregator[elements[i], retval];
            }
            return retval;
        }

        public static Term RightAssociate(Operator aggregator, params Term[] elements)
        {
            if (aggregator.Arity != 2) { throw new System.ArgumentException("aggregator must be binary."); }
            Term retval = elements[elements.Length-1];
            for (int i = elements.Length - 2; i >= 0; --i) {
                retval = aggregator[elements[i], retval];
            }
            return retval;
        }

        public static Term Cons(params Term[] elements)
        {
            return RightAssociate(Special.Nil, Special.Cons, elements);
        }

        public static Term RightAssociate(Term seed, Operator aggregator, IEnumerable<Term> elements)
        {
            return RightAssociate(seed, aggregator, elements.ToArray());
        }

        public static Term RightAssociate(Operator aggregator, IEnumerable<Term> elements)
        {
            return RightAssociate(aggregator, elements.ToArray());
        }

        public static Term Cons(IEnumerable<Term> elements)
        {
            return Cons(elements.ToArray());
        }

        public static IEnumerable<Term> UnconsMany(Term consList)
        {
            return DisaggregateMany(Special.Nil, Special.Cons, consList);
        }

        public static IEnumerable<Term> DisaggregateMany(Term seed, Operator aggregator, Term raGroup)
        {
            if (aggregator.Arity != 2) { throw new System.ArgumentException("aggregator must be binary."); }
            while (raGroup.Op == aggregator) {
                foreach (Term t in DisaggregateMany(seed, aggregator, raGroup[0])) {
                    yield return t;
                }
                raGroup = raGroup[1];
            }

            if (raGroup == seed) {
                yield break;
            }
            else {
                yield return raGroup;
            }
        }

        public static IEnumerable<Term> DisaggregateMany(Operator aggregator, Term raGroup)
        {
            if (aggregator.Arity != 2) { throw new System.ArgumentException("aggregator must be binary."); }
            while (raGroup.Op == aggregator) {
                foreach (Term t in DisaggregateMany(aggregator, raGroup[0])) {
                    yield return t;
                }
                raGroup = raGroup[1];
            }

            yield return raGroup;
        }

        public static IEnumerable<Term> Disaggregate(Operator aggregator, Term raGroup)
        {
            if (aggregator.Arity != 2) { throw new System.ArgumentException("aggregator must be binary."); }
            while (raGroup.Op == aggregator) {
                yield return raGroup[0];
                raGroup = raGroup[1];
            }
            yield return raGroup;
        }
    }
}
