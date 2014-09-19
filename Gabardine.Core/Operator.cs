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
using System.Diagnostics;
using System.Linq;

namespace Gabardine
{
    public enum OperatorKind
    {
        Function, Parameter, PatternVariable, LetVariable, LambdaVariable
    }

    public class OperatorSyntax
    {
        public enum Fix { Prefix, LeftAssociative, RightAssociative, Postfix, FunctionCall };

        public string Name { get; set; }
        public string[] AlternateNames { get; set; }
        public Fix Style { get; set; }
        public int Precedence { get; set; }

        public OperatorSyntax()
            : this("", Fix.FunctionCall, 0)
        {}

        public OperatorSyntax(string name)
            : this(name, Fix.FunctionCall, 0)
        {}

        public OperatorSyntax(string name, Fix style, int precendence, params string[] alternateNames)
        {
            Name = name;
            Style = style;
            Precedence = precendence;
            AlternateNames = alternateNames;
        }

        public static implicit operator OperatorSyntax(string name)
        {
            return new OperatorSyntax(name);
        }
    }

    public class Operator : IEquatable<Operator>
    {
        readonly OperatorKind kind;
        readonly int arity;
        readonly int uid;
        readonly OperatorSyntax syntax;

        static int globalCount = 0;

        public Operator(int arity, OperatorKind kind, OperatorSyntax syntax)
        {
            this.arity = arity;
            this.kind = kind;
            this.uid = System.Threading.Interlocked.Increment(ref globalCount);
            this.syntax = syntax;
        }

        public OperatorKind Kind { get { return kind; } }

        public int Arity { get { return arity; } }

        public string Name { get { return syntax.Name; } }
        public OperatorSyntax Syntax { get { return syntax; } }

        public Term this[params Term[] children]
        {
            get { return CreateTerm(children); }
        }

        /// <summary>
        ///   Overriding this in specialized operators gives the
        ///   opportunity to validate arguments early.
        /// </summary>
        public virtual Term CreateTerm(params Term[] children)
        {
            return new Term(this, children);
        }

        public Term CreateTerm()
        {
            return CreateTerm(new Term[0]);
        }

        public static implicit operator Term(Operator op)
        {
            if (op.Arity != 0) { throw new InvalidCastException("Cannot implicitly convert an operator with non-zero arity to a term."); }
            return op.CreateTerm();
        }

        public Term CreateTerm(IEnumerable<Term> children)
        {
            return CreateTerm(children.ToArray());
        }

        public virtual bool Equals(Operator other)
        {
            if (other.IsConstant) {
                // If this is a constant, we won't be in this method; we'll be in the overload. 
                // Therefore, this operator isn't a constant, so the two are not equal.
                Debug.Assert(!this.IsConstant, "Should be in overloaded method Constant<T>.Equals.");
                return false;
            }
            return this.uid == other.uid;
        }
        public static bool operator ==(Operator a, Operator b)
        {
            return a.Equals(b);
        }
        public static bool operator !=(Operator a, Operator b)
        {
            return !a.Equals(b);
        }

        public override bool Equals(object obj)
        {
            Debug.Assert(false, "Derek said it wouldn't get called.");
            return false;
        }

        public override int GetHashCode()
        {
            return uid;
        }

        public override string ToString()
        {
            return Name;
        }

        public virtual bool IsConstant { get { return false; } }

        public virtual TRet ConstantVisit<TRet>(ConstantVisitor<TRet> visitor)
        {
            throw new InvalidOperationException("Cannot be applied to a basic operator.");
        }

        public virtual TRet ConstantVisit<TRet, TAux>(ConstantVisitor<TRet, TAux> visitor, TAux auxData)
        {
            throw new InvalidOperationException("Cannot be applied to a basic operator.");
        }
    }
}