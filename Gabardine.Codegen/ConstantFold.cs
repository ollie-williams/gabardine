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
using System.Reflection.Emit;

namespace Gabardine.Codegen
{
    public class ConstantFoldOpportunity : OneStepTendril
    {
        readonly Address path;
        readonly Term matched;
        readonly ConstantFold folder;

        public ConstantFoldOpportunity(Address path, Term matched, ConstantFold folder)
        {
            this.path = path;
            this.matched = matched;
            this.folder = folder;
        }

        public override Term Matched(Term root)
        {
            return path.Get(root);
        }

        public override Term Transform(Term root)
        {
            Term folded = folder.Fold(Matched(root));
            return path.Replace(root, folded);
        }
    }


    public class ConstantFold : Transformer<ConstantFoldOpportunity>
    {
        static readonly ForeignLibrary emptyLibrary = new ForeignLibrary();
        readonly RewriteSystem rw;

        public ConstantFold(RewriteSystem rw)
        {
            this.rw = rw;
        }

        public IEnumerable<ConstantFoldOpportunity> FindTransformations(Term root)
        {
            var args = new Stack<Tuple<Term, bool>>();
            var stack = new Stack<Tuple<Term, SLNode<int>>>();
            stack.Push(Tuple.Create<Term,SLNode<int>>(root, null));

            while (stack.Count > 0) {
                var head = stack.Pop();
                if (ReferenceEquals(head, null)) {
                    head = stack.Pop();
                    var a = args.PopFIFO(head.Item1.Arity);
                    bool foldable = a.All(x => x.Item2);
                    args.Push(Tuple.Create(head.Item1, foldable));

                    if (foldable) {
                        Address path = new Address(head.Item2.Enumerate().Reverse());
                        yield return new ConstantFoldOpportunity(path, head.Item1, this);
                    }
                    continue;
                }

                if (head.Item1.Arity == 0) {
                    args.Push(Tuple.Create(head.Item1, head.Item1.Op.IsConstant));
                    continue;
                }

                stack.Push(head);
                stack.Push(null);
                for (int i = 0; i < head.Item1.Arity; ++i) {
                    var path = new SLNode<int>(i, head.Item2);
                    stack.Push(Tuple.Create(head.Item1[i], path));
                }
            }
        }

        public Term Fold(Term term)
        {
            LowLevelType returnType;
            if (!TryInferType(term, out returnType)) {
                return term;
            }

            Type T = returnType.ToManagedType();

            Type cf = typeof(ConstantFold);
            var mthd = cf.GetMethod("InnerFold").MakeGenericMethod(T);
            return (Term)mthd.Invoke(this, new object[] { term });
        }

        public Term InnerFold<T>(Term term)
        {
            DynamicMethod method = new DynamicMethod("constantFold", typeof(T), new Type[0]);
            ILGenerator gen = method.GetILGenerator();
            CILEmitter emitter = new CILEmitter(gen, emptyLibrary);
            Emission.EmitFunction(emitter, rw, "", Enumerable.Empty<Operator>(), OutputDirectives.ReturnValue[term]);

            Func<T> folder = (Func<T>)method.CreateDelegate(typeof(Func<T>));
            T value = folder();

            return new Constant<T>(value);
        }

        private bool TryInferType(Term term, out LowLevelType type)
        {
            Term query = Special.Typeof[term];
            query = rw.RewriteUnordered(query);

            if (query == Special.Real) {
                type = LowLevelType.Float64;
                return true;
            }

            if (query == Special.Integer) {
                type = LowLevelType.Integer(32, true);
                return true;
            }

            type = null;
            return false;
        }
    }
}
