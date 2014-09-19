/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gabardine;
using Gabardine.Parser;
using System.Linq;

namespace Gabardine.UnitTests
{
    [TestClass]
    public class PatternMatcherTests
    {

        [TestMethod]
        public void Replacement()
        {
            Operator f = new Operator(2, OperatorKind.Function, "f");
            Operator g = new Operator(1, OperatorKind.Function, "g");
            var A = new Operator(0, OperatorKind.Function, "A").CreateTerm();
            var B = new Operator(0, OperatorKind.Function, "B").CreateTerm();

            Tuple<Term, Term, Term, Term>[] tests = {
                Tuple.Create(A, B, A, B),
                Tuple.Create(A, B, g[A], g[B]),
                Tuple.Create(A, B, f[A,B], f[B,B]),
                Tuple.Create(A, B, f[A,A], f[B,B]),
                Tuple.Create(A, B, f[B,B], f[B,B]),
                Tuple.Create(A, g[B], A, g[B]),
                Tuple.Create(A, g[B], f[A,B], f[g[B],B]),
            };

            foreach(var test in tests) {
                Term result = Gabardine.Replacement.Replace(test.Item3, test.Item1, test.Item2);
                Assert.AreEqual<Term>(test.Item4, result);
            }
        }

        [TestMethod]
        public void Matching()
        {
            Term x = new Operator(0, OperatorKind.PatternVariable, "x");
            Term y = new Operator(0, OperatorKind.PatternVariable, "y");
            Operator f = new Operator(2, OperatorKind.Function, "f");
            Operator g = new Operator(1, OperatorKind.Function, "g");
            Term A = new Operator(0, OperatorKind.Function, "A");
            Term B = new Operator(0, OperatorKind.Function, "B");
            Term _3 = Term.Const(3);
            Term _4 = Term.Const(4);

            Tuple<Term, Term, bool, Tuple<Term, Term>[]>[] tests = {
                Tuple.Create(f[x,x], f[A,A], true, new [] {Tuple.Create(x,A) }),
                Tuple.Create(f[x,x], f[A,B], false, (Tuple<Term, Term>[])null),
                Tuple.Create(f[x,y], f[A,B], true, new [] {Tuple.Create(x,A), Tuple.Create(y,B)}),
                Tuple.Create(f[x,y], f[A,A], true, new [] {Tuple.Create(x,A), Tuple.Create(y,A)}),
                Tuple.Create(f[x,x], f[_3,_3], true, new [] {Tuple.Create(x,_3)}),
                Tuple.Create(f[x,x], f[g[_3],g[_3]], true, new[] {Tuple.Create(x, g[_3])}),
                Tuple.Create(f[x,x], f[g[_3],g[_4]], false, (Tuple<Term, Term>[])null),
                Tuple.Create(f[g[x],x], f[g[A], A], true, new [] {Tuple.Create(x,A) }),
        };

            foreach (var test in tests) {
                PatternMatcher pm = PatternMatcherFactory.Create(test.Item1);
                Term candidate = test.Item2;
                bool success = pm.Match(candidate);
                Assert.AreEqual(success, test.Item3);
                if (test.Item3) {
                    foreach (var binding in test.Item4) {
                        int i = pm.GetVariableIndex(binding.Item1.Op);
                        Assert.AreEqual<Term>(binding.Item2, pm.VariableBindings[i]);
                    }
                }
            }
        }

        [TestMethod]
        public void RewriteRule()
        {
            Term x = new Operator(0, OperatorKind.PatternVariable, "x");
            Term y = new Operator(0, OperatorKind.PatternVariable, "y");
            Operator f = new Operator(2, OperatorKind.Function, "f");
            Operator g = new Operator(1, OperatorKind.Function, "g");
            Term A = new Operator(0, OperatorKind.Function, "A");
            Term B = new Operator(0, OperatorKind.Function, "B");
            Term _3 = Term.Const(3);
            Term _4 = Term.Const(4);

            Tuple<Term, Term, Term, Term>[] tests = {
                Tuple.Create(f[x,x], g[x], f[A,A], g[A]),
                Tuple.Create(f[x,y], f[g[f[y,y]], x], f[B,_3], f[g[f[_3,_3]], B]),
            };

            foreach(var test in tests) {
                RewriteRule rule = RewriteRuleFactory.Create(test.Item1, test.Item2);
                bool success = rule.TryRewrite(test.Item3, null, out Term rewrite);
                Assert.IsTrue(success, "Failed to match candidate {0} to rule {1} -> {2}", test.Item3, test.Item1, test.Item2);
                Assert.AreEqual<Term>(test.Item4, rewrite);
            }
        }

        [TestMethod]
        public void ConditionalRule()
        {
            RewriteSystem rw = new RewriteSystem();

            Term a = new Operator(0, OperatorKind.Function, "a");
            Term b = new Operator(0, OperatorKind.Function, "b");
            Term x = new Operator(0, OperatorKind.Function, "x");
            Term y = new Operator(0, OperatorKind.Function, "y");
            Term condition = x;
            ConditionPattern[] conds = { new ConditionPattern(condition) };
            var rule = RewriteRuleFactory.Create(a, b, conds);
            rw.AddRule(rule);

            Term a1 = rw.RewriteUnordered(a);
            Assert.AreEqual(a, a1);

            rw.AddRule(x, Special.True);
            Term a2 = rw.RewriteUnordered(a);
            Assert.AreEqual(b, a2);
        }

        [TestMethod]
        public void AllRewrites()
        {
            const string script = @"
operator f 2
operator g 1
operator h 0

operator a 0
operator b 0

forall x y, f(x,y) -> f(y,x)
forall x, f(x,x) -> g(x)
forall x, g(x) -> f(x, h)

";

            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parser failed.");

            var root = ctx.RootNamespace;
            root.TryGetOperator("f", 2, out Operator f);
            root.TryGetOperator("g", 1, out Operator g);
            root.TryGetOperator("a", 0, out Operator a);
            root.TryGetOperator("b", 0, out Operator b);

            Term cand = f[g[a], g[b]];

            var rws = ctx.Rewriter.FindTransformations(cand);
            foreach(var result in rws) {
                Terminal.WriteLine("{0}: {1}  {2}", result.Rule, result.Matched(cand), result.Transform(cand));
            }
        }

        [TestMethod]
        public void IsConst()
        {
            Term isconst3 = Special.IsConst[Term.Const(3)];
            Term isconstfoo = Special.IsConst[Term.Const("foo")];
            Term C = new Operator(0, OperatorKind.Function, "C");
            Term isconstC = Special.IsConst[C];

            Operator add = new Operator(2, OperatorKind.Function, "add");
            Term expr = add[C, Term.Const(4)];
            Term isconstexpr = Special.IsConst[expr];

            RewriteSystem rw = new RewriteSystem();
            rw.AddRule(isconstC, Special.True);
            Term u = new Operator(0, OperatorKind.PatternVariable, "u");
            rw.AddRule(RewriteRuleFactory.Create(Special.IsConst[u], Special.Breakout[Special.IsConst[u]], -1));

            Tuple<Term, bool>[] tests = new Tuple<Term, bool>[]
            {
                Tuple.Create(isconst3, true),
                Tuple.Create(isconstfoo, true),
                Tuple.Create(isconstC, true),
                Tuple.Create(isconstexpr, false)
            };

            foreach (var test in tests) {
                var result = rw.RewriteUnordered(test.Item1);
                if (test.Item2) {
                    Assert.AreEqual(Special.True, result);
                } else {
                    Assert.AreEqual(Special.False, result);
                }
            }
        }
    }
}
