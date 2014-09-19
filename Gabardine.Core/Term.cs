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
using System.Collections.ObjectModel;
using System.Linq;
namespace Gabardine
{
    public class Term : IEquatable<Term>
    {
        readonly Operator op;
        readonly Term[] children;
        readonly int hash;
        readonly int max_depth;
        readonly int size;

        internal Term(Operator op, Term[] children)
        {
            this.op = op;
            this.children = children;
            if (op.Arity != this.children.Length) {
                string message = "Number of provided children does not match operator arity.\n";
                message += string.Format("op:       {0}\n", op);
                message += string.Format("arity:    {0}\n", op.Arity);
                message += string.Format("children: ");
                foreach (Term ch in children) {
                    message += ch.ToString() + "; ";
                }
                throw new System.ArgumentException(message);
            }

            this.hash = Hash.MakeHash(children.Select(x => x.hash).Append(op.GetHashCode()));
            max_depth = 1;
            size = 1;
            if (children.Length > 0) {
                max_depth += children.Max(t => t.max_depth);
                size += children.Sum(t => t.size);
            }
        }

        public Term this[int index] { get { return children[index]; } }

        public IEnumerable<Term> Children()
        {
            return children;
        }

        public Operator Op { get { return op; } }
        public int Arity { get { return children.Length; } }
        public int MaxDepth { get { return max_depth; } }
        public int Size { get { return size; } }

        public static bool operator ==(Term a, Term b)
        {
            return a.Equals(b);
        }
        public static bool operator !=(Term a, Term b)
        {
            return !a.Equals(b);
        }
        public bool Equals(Term other)
        {
            if (other.hash != this.hash) {
                return false;
            }

            if (other.Op != this.Op) {
                return false;
            }

            for (int i = 0; i < Arity; ++i) {
                if (this[i] != other[i]) {
                    return false;
                }
            }

            return true;
        }
        public override bool Equals(object obj)
        {
            Term other = obj as Term;
            if (ReferenceEquals(other, null)) {
                return false;
            }
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return hash;
        }

        public override string ToString()
        {
            return PrettyPrinter.LispFormat(this);
        }

        public static Term Const<T>(T v) 
        {
            return new Constant<T>(v).CreateTerm();
        }
    }
}
