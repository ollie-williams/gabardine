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
using System.Diagnostics;

namespace Gabardine
{
    public class HoistingOpportunity : Tendril
    {
        readonly Address address;

        public HoistingOpportunity(Address address)
        {
            this.address = address;
        }

        /// <summary>
        ///   Expand the hoisting opportunity by spawning a new let
        ///   expression, then incrementally moving it up until it
        ///   reaches a lambda.
        /// </summary>
        public IEnumerable<Term> Expand(Term root)
        {
            Term term = CodeMotion.Spawn(root, address);
            yield return term;

            Address a = address;
            while (!TouchingLambda(term, a)) {
                term = CodeMotion.MoveLetUp(term, ref a);
                yield return term;
            }

            term = CodeMotion.Hoist(term, ref a);
            yield return term;
        }

        /// <summary>
        ///   Is the parent of the term at address <paramref
        ///   name="a"/> in <paramref name="root"/> a lambda?
        /// </summary>
        private bool TouchingLambda(Term root, Address a)
        {
            return a.Parent(root).Op == Special.Lambda;
        }
    }
    
    public class Hoister : Transformer<HoistingOpportunity>
    {     
        public IEnumerable<HoistingOpportunity> FindTransformations(Term root)
        {
            Stack<HoistContext> stack = new Stack<HoistContext>();
            stack.Push(new HoistContext(root));
            while (stack.Count > 0) {
                var head = stack.Pop();
                if (head.IsHoistingCandidate()) {
                    yield return new HoistingOpportunity(head.GetAddress());
                }
                for (int i = 0; i < head.Arity; ++i) {
                    stack.Push(head.MoveToChild(i));
                }
            }    
            yield break;
        }

        class HoistContext : WalkContext
        {
            bool receptive;

            HoistContext(WalkContext ctx, bool receptive)
                : base(ctx)
            {
                this.receptive = receptive;
            }

            public HoistContext(Term term)
              : base(term)
            {
                this.receptive = false;
            }

            public new HoistContext MoveToChild(int index)
            {
                bool r = receptive;
                if (Term.Op == Special.Lambda) {
                    r = true;
                } else if (Term.Op == Special.Let) {
                    r = false;
                }
                return new HoistContext(base.MoveToChild(index), r);
            }

            /// <summary>
            ///   Can this node be hoisted?
            /// </summary>
            public bool IsHoistingCandidate()
            {
                if (!receptive || Arity == 0) {
                    return false;
                }

                Debug.Assert(Lambdas != null, "Expected to be inside a lambda.");
                Term prevailling = Lambdas.Item[0];
                bool hoistable = !IsDependentOn(prevailling);
                receptive &= !hoistable;
                return hoistable;
            }
        }
    }
}    
