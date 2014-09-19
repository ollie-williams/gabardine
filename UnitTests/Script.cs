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
using Gabardine;
using Gabardine.Codegen;
using Gabardine.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gabardine.UnitTests
{
    using Namespace = Gabardine.Parser.Namespace;

    [TestClass]
    public class Script
    {
        [TestMethod]
        public void TypeParser()
        {
            var types = new Tuple<string, LowLevelType>[]
            {
                Tuple.Create("int32", LowLevelType.Integer(32, true)),
                Tuple.Create("int15", LowLevelType.Integer(15, true)),
                Tuple.Create("uint64", LowLevelType.Integer(64, false)),
                Tuple.Create("float64", LowLevelType.Float64),
                Tuple.Create("int32*", LowLevelType.Integer(32,true).Pointer()),
                Tuple.Create("int32***", LowLevelType.Integer(32, true).Pointer().Pointer().Pointer()),
                Tuple.Create("void", LowLevelType.Void)
            };

            foreach (var pair in types) {
                LowLevelType parsed = Gabardine.Parser.TypeParser.ParseType(pair.Item1);
                LowLevelType expected = pair.Item2;
                Assert.AreEqual(expected, parsed);
            }
        }

        [TestMethod]
        public void ParseRules()
        {
            const string script = @"
operator f 2
operator g 1 


              forall x,   f(x,x) -> g(x)
g_idempotent: forall z,   g(g(z)) -> g(z)
f_zero:                   f(0,_) -> 0
f_comm:       forall x y, f(x,y) = f(y,x)

operator symmetric 1
operator trans 1
cond:         forall A,  symmetric(A) => trans(A) -> A

param P, Q, R
symmetric(P) -> true
symmetric(Q) -> true


operator posdef 1
operator inv 1 
operator cholInv 1
cond2: forall A, symmetric(A) => posdef(A) => inv(A) -> cholInv(A)

posdef(P) -> true

";
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parser returned false, indicating partial failure.");

            Term A = (new Operator(0, OperatorKind.Function, "A"));
            Term B = (new Operator(0, OperatorKind.Function, "B"));
            Term _3 = Term.Const(3);

            var ns = ctx.RootNamespace;
            ns.TryGetOperator("f", 2, out Operator f);
            ns.TryGetOperator("g", 1, out Operator g);
            ns.TryGetOperator("P", 0, out Operator p);
            ns.TryGetOperator("Q", 0, out Operator q);
            ns.TryGetOperator("R", 0, out Operator r);
            ns.TryGetOperator("trans", 1, out Operator trans);
            ns.TryGetOperator("inv", 1, out Operator inv);
            ns.TryGetOperator("cholInv", 1, out Operator cholInv);
            Term P = p;
            Term Q = q;
            Term R = r;

            Tuple<Term, Term>[] tests = {
                Tuple.Create(f[A,A], g[A]),
                Tuple.Create(f[B,_3], f[B,_3]),
                Tuple.Create(trans[R], trans[R]),
                Tuple.Create(trans[P], P),
                Tuple.Create(inv[Q], inv[Q]),
                Tuple.Create(inv[P], cholInv[P])
            };

            var rw = ctx.Rewriter;
            foreach (var rule in rw.Rules) {
                Terminal.WriteLine(rule);
            }

            foreach (var test in tests) {
                Term rewrite = rw.RewriteUnordered(test.Item1);
                Assert.AreEqual<Term>(test.Item2, rewrite);
            }
        }

        [TestMethod]
        public void Requires()
        {
            const string script = @"
require ..\..\..\scripts\gmm.cnut
";
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parser returned false, indicating partial failure.");

            // Check for some operators from these files
            Assert.IsTrue(ctx.RootNamespace.TryGetOperator("log", 1, out Operator log));
            Assert.IsTrue(ctx.RootNamespace.TryGetOperator("multiplyadd", 3, out Operator muladd));
            Assert.IsTrue(ctx.RootNamespace.TryGetOperator("^", 2, out Operator hat));
        }

        void ScriptTest(string script)
        {
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            bool success = parser.ParseFile(script);
            Assert.IsTrue(success, "Parser returned false, indicating partial failure.");
            //foreach (var r in ctx.Rewriter.Rules) {
            //    Console.WriteLine(r);
            //}
        }

        [TestMethod]
        public void Logic()
        {
            ScriptTest(@"..\..\..\scripts\logic.cnut");
        }

        [TestMethod]
        public void Real()
        {
            ScriptTest(@"..\..\..\scripts\real.cnut");
        }

        [TestMethod]
        public void Sound()
        {
            ScriptTest(@"..\..\..\scripts\sound.cnut");
        }

        [TestMethod]
        public void Macros()
        {
            const string script = @"
operator + left 60
def f(x, y, z) -> x + y + z
";

            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parser returned false, indicating partial failure.");

            bool lookup = ctx.RootNamespace.TryGetOperator("f", 3, out Operator f);
            Assert.IsTrue(lookup, "Operator wasn't created in def statement.");

            lookup = ctx.RootNamespace.TryGetOperator("+", 2, out Operator plus);
            Assert.IsTrue(lookup, "Operator wasn't found.");

            Term A = (new Operator(0, OperatorKind.Function, "A"));
            Term B = (new Operator(0, OperatorKind.Function, "B"));
            Term _3 = Term.Const(3);

            Term F = f[A, B, _3];

            Term expected = plus[plus[A, B], _3];
            F = ctx.Rewriter.RewriteUnordered(F);

            Assert.AreEqual<Term>(expected, F, "Rewrite didn't result in expected form.");
        }

        [TestMethod]
        public void Aux()
        {
            const string script = @"
operator + left 60
operator * left 70
operator matrixAdd 2
operator rows 1
operator cols 1
param a, b
def t -> matrixAdd(a, b)
operator Matrix 0
typeof(a) -> Matrix
rows(a) -> 3
param n
cols(a) -> n
operator saxpy 3
forall u v, matrixAdd(u, v) -> saxpy(u, v, rows(u)*cols(u))
rewrite verbose t


forall u, typeof(u) -> typeof(inherit(u))  {priority -1}
forall u, rows(u) -> rows(inherit(u))     {priority -1}
forall u, cols(u) -> cols(inherit(u))     {priority -1}
";

            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);

            var htmlBuilder = new System.Text.StringBuilder();
            var sw = new System.IO.StringWriter(htmlBuilder);
            Fancy.html = sw;

            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parser returned false, indicating partial failure.");

            Console.WriteLine(htmlBuilder.ToString());
        }
    }
}
