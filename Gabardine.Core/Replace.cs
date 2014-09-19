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
using System.Linq;

namespace Gabardine
{
    public static class Replacement
    {
        public static Term Replace(Term root, Term old, Term replacement)
        {
            Stack<Term> args = new Stack<Term>();
            Stack<Term> stack = new Stack<Term>();
            stack.Push(root);
            while (stack.Count > 0) {
                Term head = stack.Pop();

                if (ReferenceEquals(head, null)) {
                    head = stack.Pop();
                    args.Push(new Term(head.Op, args.PopFIFO(head.Arity)));
                    continue;
                }

                if (head.MaxDepth < old.MaxDepth || head.Size < old.Size) {
                    // There won't be a match after this point.
                    args.Push(head);
                    continue;
                }

                // Look for a match
                if (head == old) {
                    args.Push(replacement);
                    continue;
                }

                // Continue with children
                stack.Push(head);
                stack.Push(null);
                for (int i = head.Arity - 1; i >= 0; --i) {
                    stack.Push(head[i]);
                }
            }

            Assert(args.Count == 1, "Expected only result to remain on stack.");
            return args.Pop();
        }

        public static bool Contains(Term root, Term searchTerm)
        {
            Stack<Term> stack = new Stack<Term>();
            stack.Push(root);

            while (stack.Count > 0) {
                Term head = stack.Pop();

                if (head.MaxDepth < searchTerm.MaxDepth || head.Size < searchTerm.Size) {
                    continue;
                }

                if (head == searchTerm) {
                    return true;
                }

                for (int i = 0; i < head.Arity; ++i) {
                    stack.Push(head[i]);
                }
            }

            return false;
        }

        public static bool ContainsAny(Term root, IEnumerable<Term> searchTerms)
        {
            if (searchTerms.Count() == 0) {
                return false;
            }

            Stack<Term> stack = new Stack<Term>();
            stack.Push(root);

            var minDepth = searchTerms.Select(x => x.MaxDepth).Min();
            var minSize = searchTerms.Select(x => x.Size).Min();

            while (stack.Count > 0) {
                Term head = stack.Pop();

                if (head.MaxDepth < minDepth || head.Size < minSize) {
                    continue;
                }

                if (searchTerms.Contains(head)) {
                    return true;
                }

                for (int i = 0; i < head.Arity; ++i) {
                    stack.Push(head[i]);
                }
            }

            return false;
        }

        public static IEnumerable<Address> Find(Term root, Term search)
        {
            var stack = new Stack<Tuple<Term, SLNode<int>>>();
            stack.Push(Tuple.Create<Term, SLNode<int>>(root, null));

            while (stack.Count > 0) {
                var head = stack.Pop();

                if (head.Item1 == search) {
                    yield return new Address(head.Item2.Enumerate().Reverse());
                }

                if (head.Item1.MaxDepth < search.MaxDepth || head.Item1.Size < search.Size) {
                    // There won't be a match after this point.
                    continue;
                }

                for (int i = 0; i < head.Item1.Arity; ++i) {
                    stack.Push(Tuple.Create(head.Item1[i], new SLNode<int>(i, head.Item2)));
                }
            }
        }

        public static IEnumerable<Term> Find(Term root, Func<Term, bool> predicate)
        {
            Stack<Term> stack = new Stack<Term>();
            stack.Push(root);

            while (stack.Count > 0) {
                Term head = stack.Pop();
                if (predicate(head)) {
                    yield return head;
                    continue;
                }

                for (int i = 0; i < head.Arity; ++i) {
                    stack.Push(head[i]);
                }
            }

            yield break;
        }
    }
}
