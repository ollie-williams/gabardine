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
using System.Diagnostics;
using System.Collections.Generic;

namespace Gabardine
{
    public static class LambdaSanity
    {
        public static void Check(Term root)
        {
            Walk.VisitNodes(root, (WalkContext wc) =>
            {
                if (wc.Term.Op == Special.Lambda) {
                    if (wc.Lambdas.Enumerate().Any(lm => lm[0] == wc.Term[0])) {
                        throw new InvalidProgramException(string.Format("Lambda variable {0}, is bound twice in overlapping contexts.", wc.Term[0]));
                    }
                }
                return true;
            });
        }
    }

    public static class LambdaUtils
    {
        public static Term AlphaConvert(Term rootLambda)
        {
            if (rootLambda.Op != Special.Lambda) {
                throw new ArgumentException("Expected rootLambda to be a lambda expression.");
            }

            Term new_var = new LambdaVariable(rootLambda[0].Op.Name + '\'');
            return Replacement.Replace(rootLambda, rootLambda[0], new_var);
        }
    }

    public class LambdaNormalizer
    {
        readonly List<Term> newVariables = new List<Term>();

        static readonly LambdaNormalizer singleton = new LambdaNormalizer();
        public static Term Normalize(Term root)
        {
            return singleton._Normalize(root);
        }

        Term _Normalize(Term root)
        {
            var stack = new Stack<Term>();
            var args = new Stack<Term>();
            var nesting = new Stack<int>(); 

            stack.Push(root);
            while (stack.Count > 0) {

                Debug.Assert(args.Count == nesting.Count, "Expected output stacks to be same size.");

                var head = stack.Pop();
                if (object.ReferenceEquals(head, null)) {
                    head = stack.Pop();
                    
                    // Catch lambda and alpha-convert
                    if (head.Op == Special.Lambda) {
                        int last = nesting.Pop();
                        Term variable = GetNewVar(last);
                        nesting.Push(last + 1);
                        Term body = Replacement.Replace(args.Pop(), head[0], variable);
                        args.Push(Special.Lambda.CreateTerm(variable, body));
                        continue;
                    }

                    int a = nesting.PopLIFO(head.Arity).Max();
                    nesting.Push(a);
                    args.Push(head.Op.CreateTerm(args.PopFIFO(head.Arity)));
                    continue;
                }

                // Nullary node can go directly to output stack.
                if (head.Arity == 0) {
                    nesting.Push(0);
                    args.Push(head);
                    continue;
                }

                // Special treatment for lambda
                if (head.Op == Special.Lambda) {
                    stack.Push(head);
                    stack.Push(null);
                    stack.Push(head[1]);
                    continue;
                }

                // Recursively explore children
                stack.Push(head);
                stack.Push(null);
                for (int i = head.Arity-1; i >= 0; --i) {
                    stack.Push(head[i]);
                }                
            }

            Debug.Assert(args.Count == 1, "Expected only result to remain.");
            return args.Pop();
        }

        Term GetNewVar(int index)
        {
            while (index >= newVariables.Count) {
                newVariables.Add(new LambdaVariable(string.Format("n_{0}", newVariables.Count)));
            }
            return newVariables[index];
        }
    }
}