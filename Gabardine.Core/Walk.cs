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
    class WalkContext
    {
        readonly Term term;
        readonly SLNode<Term> lambdas;
        readonly SLNode<Term> lets;
        readonly SLNode<int> address;
        
        public WalkContext(Term term)
        {
            this.term = term;
            this.lambdas = null;
            this.lets = null;
            this.address = null;
        }

        protected WalkContext(WalkContext other)
        {
            this.term = other.term;
            this.lambdas = other.lambdas;
            this.lets = other.lets;
            this.address = other.address;
        }

        WalkContext(Term term, SLNode<Term> lambdas, SLNode<Term> lets, SLNode<int> address)
        {
            this.term = term;
            this.lambdas = lambdas;
            this.lets = lets;
            this.address = address;
        }

        public int Arity { get { return term.Arity; } }

        public Term Term { get { return term; } }

        public SLNode<Term> Lambdas { get { return lambdas; } }
                
        public SLNode<Term> Lets { get { return lets; } }

        public WalkContext MoveToChild(int index) 
        {
            if (index >= Arity) throw new IndexOutOfRangeException();

            switch(Special.GetKind(term.Op)) {

            case Special.Kind.Lambda:
                return new WalkContext(term[index],
                                       lambdas.Append(term),
                                       lets,
                                       address.Append(index));
                
            case Special.Kind.Let:
                return new WalkContext(term[index],
                                       lambdas,
                                       lets.Append(term),
                                       address.Append(index));
                
            default:
                return new WalkContext(term[index], lambdas, lets, address.Append(index));
            }        
        }

        public WalkContext WithTerm(Term newTerm)
        {
            return new WalkContext(newTerm, lambdas, lets, address);
        }

        public Address GetAddress()
        {
            return new Address(address.Enumerate().Reverse());
        }

        /// <summary>
        ///   Is this node dependent on <paramref name="dependency"/>?
        /// </summary>
        public bool IsDependentOn(Term dependency)
        {
            // The set of terms known to be dependent
            HashSet<Term> dependecies = new HashSet<Term>();
            dependecies.Add(dependency);

            // Search let bindings from outermost to innermost collecting
            // all those which are "tainted".
            var orderedLets = lets.Enumerate().Reverse().ToArray();
            for (int i = 0; i < orderedLets.Length; ++i) {
                if (Replacement.ContainsAny(orderedLets[i][1], dependecies)) {
                    dependecies.Add(orderedLets[i][0]);
                }
            }

            // Test our candidate
            return Replacement.ContainsAny(term, dependecies);        
        }

        
    }
    
    public static class Walk
    {
        public static void VisitNodes(Term term, Func<Term,bool> action)
        {
            Stack<Term> stack = new Stack<Term>();
            stack.Push(term);

            while (stack.Count > 0) {
                var head = stack.Pop();
                if (action(head)) {
                    for (int i = 0; i < head.Arity; ++i) {
                        stack.Push(head[i]);
                    }
                }
            }
        }

        internal static void VisitNodes(Term term, Func<WalkContext, bool> action)
        {
            Stack<WalkContext> stack = new Stack<WalkContext>();
            stack.Push(new WalkContext(term));

            while (stack.Count > 0) {
                var head = stack.Pop();
                if (action(head)) {
                    for (int i = 0; i < head.Arity; ++i) {
                        stack.Push(head.MoveToChild(i));
                    }
                }
            }
        }
    }
}
