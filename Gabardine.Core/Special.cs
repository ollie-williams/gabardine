/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
namespace Gabardine
{
    public static class Special
    {
        public static readonly Operator IsConst = new Operator(1, OperatorKind.Function, "isConst");
        public static readonly Operator IsParam = new Operator(1, OperatorKind.Function, "isParam");
        public static readonly Operator Subs = new Operator(3, OperatorKind.Function, "subs");
        public static readonly Operator Typeof = new Operator(1, OperatorKind.Function, "typeof");
        public static readonly Operator Inherit = new Operator(1, OperatorKind.Function, "inherit");
        public static readonly Operator Size = new Operator(1, OperatorKind.Function, "size");
        public static readonly Operator FailWith = new Operator(1, OperatorKind.Function, "failwith");
        public static readonly Operator Lambda = new LambdaOperator();
        public static readonly Operator Let = new LetOperator();
        public static readonly Operator Fresh = new Operator(1, OperatorKind.Function, "fresh");
        public static readonly Operator Cons = new Operator(2, OperatorKind.Function, "cons");
        public static readonly Operator Index = new Operator(2, OperatorKind.Function, "index");
        public static readonly Operator Breakout = new Operator(1, OperatorKind.Function, "breakout");
        public static readonly Operator Eval = new Operator(2, OperatorKind.Function, "eval");
        public static readonly Operator Sound = new Operator(1, OperatorKind.Function, "sound");
        public static readonly Operator And = new Operator(2, OperatorKind.Function, new OperatorSyntax {
            Name = "∧",
            AlternateNames = new string[] { @"/\", "&", "and" },
            Style = OperatorSyntax.Fix.RightAssociative,
            Precedence = 30
        });
        public static readonly Operator MapsTo = new Operator(2, OperatorKind.Function,
                                                              new OperatorSyntax {
            Name = "->",
            Style = OperatorSyntax.Fix.RightAssociative,
            Precedence = 20
        });
        public static new readonly Operator Equals = new Operator(2, OperatorKind.Function,
                                                                  new OperatorSyntax {
            Name = "=",
            Style = OperatorSyntax.Fix.RightAssociative,
            Precedence = 20
        });
        public static readonly Operator Implies = new Operator(2, OperatorKind.Function,
                                                               new OperatorSyntax {
            Name = "=>",
            Style = OperatorSyntax.Fix.RightAssociative,
            Precedence = 10
        });


        public static readonly Operator Nil = new Operator(0, OperatorKind.Function, "nil");
        public static readonly Operator True = new Operator(0, OperatorKind.Function, "true");
        public static readonly Operator False = new Operator(0, OperatorKind.Function, "false");
        public static readonly Operator Wildcard = new Operator(0, OperatorKind.PatternVariable, "_");

        public static readonly Operator Real = new Operator(0, OperatorKind.Function, "Real");
        public static readonly Operator Integer = new Operator(0, OperatorKind.Function, "Integer");

        public static readonly Operator[] All = {
            IsConst, IsParam, Subs, Typeof, Inherit, Size, FailWith, Lambda, Let, Fresh, Cons, Index,
            Breakout, Eval, Sound, And, MapsTo, Equals, Implies, 
            Nil, True, False, Wildcard,
            Real, Integer
        };

        public enum Kind
        {
            NotSpecial = -1,
            IsConst = 0, IsParam, Subs, Typeof, Inherit, Size, FailWith, Lambda, Let, Fresh, Cons, Index,
            Breakout, Eval, Sound, And, MapsTo, Equals, Implies,
            Nil, True, False, Wildcard,
            Real, Integer
        };

        public static Kind GetKind(Operator op)
        {
            int index = System.Array.IndexOf(All, op);
            return (Kind)index;
        }
    }
}