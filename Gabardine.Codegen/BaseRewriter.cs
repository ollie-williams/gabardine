/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System.Diagnostics.Debug;
using Gabardine.ConsUtils;

namespace Gabardine.Codegen
{
    static class BaseRewriter
    {
        static RewriteSystem before = null;
        static RewriteSystem after = null;

        public static RewriteSystem Before { get { return GetBefore(); } }
        public static RewriteSystem After { get { return GetAfter(); } }

        static RewriteSystem GetBefore()
        {
            if (ReferenceEquals(before, null)) {
                CreateRewriters();
            }
            Assert(!ReferenceEquals(before, null), "Expected rewriter to exist.");
            return before;
        }

        static RewriteSystem GetAfter()
        {
            if (ReferenceEquals(after, null)) {
                CreateRewriters();
            }
            Assert(!ReferenceEquals(after, null), "Expected rewriter to exist.");
            return after;
        }

        static Term Tmp(string name)
        {
            return Arguments.Tmp[Term.Const(name)];
        }

        static Term CreateVariable(string name)
        {
            return new Operator(0, OperatorKind.PatternVariable, name);
        }

        static void CreateRewriters()
        {
            Assert(ReferenceEquals(before, null), "Create has already been called (by another thread?).");
            before = new RewriteSystem();
            after = new RewriteSystem();

            Term u = CreateVariable("u");
            Term v = CreateVariable("v");
            Term h = CreateVariable("h");
            Term t = CreateVariable("t");

            after.AddRule(
                Statements.TmpAssign[u, Special.Cons[h, t]],
                Special.Cons[h, Statements.TmpAssign[u, t]]
                );
            after.AddRule(
                Statements.TmpAssign[u, Special.Cons[v, Special.Nil]],
                Cons(v, Statements.TmpAssign[u, Instructions.Pop])
                );
            after.AddRule(RewriteRuleFactory.Create(
                Instructions.Generate[u],
                Arguments.Const[u],
                new ConditionPattern[] { new ConditionPattern(Special.IsConst[u]) }));
            after.AddRule(RewriteRuleFactory.Create(
                Instructions.Generate[u],
                Instructions.Param[u],
                new ConditionPattern[] { new ConditionPattern(Special.IsParam[u]) }));
            after.AddRule(RewriteRuleFactory.Create(
                Instructions.Generate[u],
                Instructions.GenerateFail[u],
                -1));


            before.AddRule(
                OutputDirectives.OutParam[v],
                Cons(
                    Statements.Push,
                    Statements.TmpAssign[Term.Const("v"), Instructions.Generate[v]],
                    Statements.CopyOut[Tmp("v")]
                    )
                );
            before.AddRule(
                OutputDirectives.ReturnValue[u],
                Cons(
                    Statements.Push,
                    Statements.TmpAssign[Term.Const("u"), Instructions.Generate[u]],
                    Statements.Return[Tmp("u")]
                    )
                );
        }
    }
}
