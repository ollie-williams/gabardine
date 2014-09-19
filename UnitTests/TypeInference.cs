/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using Gabardine.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gabardine.UnitTests
{

    class TypeContext
    {
        readonly RewriteSystem rw;

        public TypeContext(RewriteSystem rw)
        {
            this.rw = rw;
        }

        public void AddSignature(Operator op, params Term[] types)
        {
            throw new System.NotImplementedException();
        }

        public void AddSignature(Term t, params Term[] types)
        {
            throw new System.NotImplementedException();
        }

        public Term Check(Term term)
        {
            throw new System.NotImplementedException();
        }
    }

    [TestClass]
    public class TypeInference
    {
        [TestMethod]
        public void DownwardTypeInference()
        {
            // Create some terms
            Term x = new Operator(0, OperatorKind.Parameter, "x");
            Term y = new Operator(0, OperatorKind.Parameter, "y");

            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);

            const string script = @"
require ..\..\..\scripts\sound.cnut

// Some types
constant P
constant Q
constant R

// f :: P -> Q -> R
operator f 2
typeof(f(_,_)) -> R
forall u v, sound(f(u,v)) ->
    sound(u) /\ sound(v) /\ (typeof(u) = P) /\ (typeof(v) = Q)
";

            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parsing problem");

            ctx.RootNamespace.TryGetOperator("f", 2, out Operator f);
            Term t = f[x, y];

            ModelChecker checker = new ModelChecker(ctx.Rewriter);
            var sound = checker.CheckAndIngest(t);
            Assert.AreEqual(Special.True, sound);

            ctx.RootNamespace.TryGetOperator("P", 0, out Operator p);
            ctx.RootNamespace.TryGetOperator("Q", 0, out Operator q);
            ctx.RootNamespace.TryGetOperator("R", 0, out Operator r);
            Assert.AreEqual<Term>(p, ctx.Rewriter.RewriteUnordered(Special.Typeof[x]));
            Assert.AreEqual<Term>(q, ctx.Rewriter.RewriteUnordered(Special.Typeof[y]));
            Assert.AreEqual<Term>(r, ctx.Rewriter.RewriteUnordered(Special.Typeof[t]));
        }

        [TestMethod]
        public void PolyFunction()
        {
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);

            const string script = @"
require ..\..\..\scripts\sound.cnut

// Some types
constant P
constant R

// f :: Q -> Q -> Q
operator f 2
forall u, typeof(f(u,_)) -> typeof(u)
forall u v, sound(f(u,v)) ->
    sound(u) /\ sound(v) /\ (typeof(u) = typeof(v))

param x1, x2
typeof(x1) -> P
typeof(x2) -> R
";

            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parsing problem");

            RewriteSystem rw = ctx.Rewriter;

            ctx.RootNamespace.TryGetOperator("f", 2, out Operator f);
            ctx.RootNamespace.TryGetOperator("x1", 0, out Operator x1);
            ctx.RootNamespace.TryGetOperator("x2", 0, out Operator x2);
            ctx.RootNamespace.TryGetOperator("P", 0, out Operator p);
            ctx.RootNamespace.TryGetOperator("R", 0, out Operator r);

            ModelChecker checker = new ModelChecker(ctx.Rewriter);

            {
                Term y = new Operator(0, OperatorKind.Parameter, "y");
                Term t1 = f[x1, y];
                var sound = checker.CheckAndIngest(t1);
                Assert.AreEqual(Special.True, sound);
                Assert.AreEqual<Term>(p, rw.RewriteUnordered(Special.Typeof[x1]));
                Assert.AreEqual<Term>(p, rw.RewriteUnordered(Special.Typeof[y]));
                Assert.AreEqual<Term>(p, rw.RewriteUnordered(Special.Typeof[t1]));
            }

            {
                Term y = new Operator(0, OperatorKind.Parameter, "y");
                Term t2 = f[x2, y];
                var sound = checker.CheckAndIngest(t2);
                Assert.AreEqual(Special.True, sound);
                Assert.AreEqual<Term>(r, rw.RewriteUnordered(Special.Typeof[x2]));
                Assert.AreEqual<Term>(r, rw.RewriteUnordered(Special.Typeof[y]));
                Assert.AreEqual<Term>(r, rw.RewriteUnordered(Special.Typeof[t2]));
            }
        }

        [TestMethod]
        public void PolyLength()
        {
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);

            const string script = @"
require ..\..\..\scripts\sound.cnut

// Some types
operator List 1
constant Nat
constant String
constant Double

operator isList 1
isList(_) -> false
isList(List(_)) -> true

operator length 1
typeof(length(_)) -> Nat
forall x, sound(length(x)) -> sound(x) /\ isList(typeof(x))

operator addNat 2
typeof(addNat(_,_)) -> Nat
forall u v, sound(addNat(u,v)) ->   
    sound(u) /\ sound(v) /\ (typeof(u) = Nat) /\ (typeof(v) = Nat)

param x, y
typeof(x) -> List(String)
typeof(y) -> List(Double)
";

            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parsing problem");
            RewriteSystem rw = ctx.Rewriter;
            ModelChecker checker = new ModelChecker(rw);


            ctx.RootNamespace.TryGetOperator("addNat", 2, out Operator addNat);
            ctx.RootNamespace.TryGetOperator("length", 1, out Operator length);
            ctx.RootNamespace.TryGetOperator("x", 0, out Operator x);
            ctx.RootNamespace.TryGetOperator("y", 0, out Operator y);
            Term t = addNat[length[x], length[y]];
            var sound = checker.CheckAndIngest(t);
            Assert.AreEqual(Special.True, sound);

            System.Console.WriteLine("{0} :: {1}", x, rw.RewriteUnordered(Special.Typeof[x]));
            System.Console.WriteLine("{0} :: {1}", y, rw.RewriteUnordered(Special.Typeof[y]));
            System.Console.WriteLine("{0} :: {1}", t, rw.RewriteUnordered(Special.Typeof[t]));
        }

        [TestMethod]
        public void TypingLetExpression()
        {
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            const string script = @"
require ..\..\..\scripts\sound.cnut
constant Nat

operator addNat 2
typeof(addNat(_,_)) -> Nat
forall u v, sound(addNat(u,v)) ->   
    sound(u) /\ sound(v) /\ (typeof(u) = Nat) /\ (typeof(v) = Nat)
";

            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parsing problem");
            RewriteSystem rw = ctx.Rewriter;
            ModelChecker checker = new ModelChecker(rw);

            Term x = new Operator(0, OperatorKind.LetVariable, "x");
            Term a = new Operator(0, OperatorKind.Parameter, "a");
            Term b = new Operator(0, OperatorKind.Parameter, "b");
            ctx.RootNamespace.TryGetOperator("addNat", 2, out Operator add);
            ctx.RootNamespace.TryGetOperator("Nat", 0, out Operator Nat);
            Term t = Special.Let[x, b, add[a, x]];
            var sound = checker.CheckAndIngest(t);
            Assert.AreEqual(Special.True, sound);

            System.Action<Term> tell = s => System.Console.WriteLine("{0} :: {1}", s, CheckType(rw, s));
            tell(x);
            tell(a);
            tell(b);
            tell(t);

            Assert.AreEqual(Nat, CheckType(rw, x));
            Assert.AreEqual(Nat, CheckType(rw, b));
            Assert.AreEqual(Nat, CheckType(rw, a));
        }

        Term CheckType(RewriteSystem rw, Term t)
        {
            return rw.RewriteUnordered(Special.Typeof[t]);
        }

        [TestMethod]
        public void Ident()
        {
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            const string script = @"
require ..\..\..\scripts\sound.cnut
constant Nat
constant Double
";

            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parsing problem");
            RewriteSystem rw = ctx.Rewriter;
            ModelChecker checker = new ModelChecker(rw);

            Term x = new LambdaVariable("x");
            Term ident = Special.Lambda[x, x];

            Term a = new Operator(0, OperatorKind.Parameter, "a");
            Term b = new Operator(0, OperatorKind.Parameter, "b");

            ctx.RootNamespace.TryGetOperator("Nat", 0, out Operator Nat);
            ctx.RootNamespace.TryGetOperator("Double", 0, out Operator Double);

            rw.AddRule(Special.Typeof[a], Nat);
            rw.AddRule(Special.Typeof[b], Double);

            Term i1 = Special.Eval[ident, a];
            Term i2 = Special.Eval[ident, b];
            var i1Sound = checker.CheckAndIngest(i1);
            Assert.AreEqual(Special.True, i1Sound);
            var i2Sound = checker.CheckAndIngest(i2);
            Assert.AreEqual(Special.True, i2Sound); 

            System.Action<Term> tell = s => System.Console.WriteLine("{0} :: {1}", s, CheckType(rw, s));
            tell(ident);
            tell(a);
            tell(b);
            tell(i1);
            tell(i2);

            Assert.AreEqual(Nat, CheckType(rw, i1));
            Assert.AreEqual(Double, CheckType(rw, i2));
        }

        [TestMethod]
        //[ExpectedException(typeof(System.InvalidProgramException))]
        public void TypingLambdaExpression()
        {
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            const string script = @"
require ..\..\..\scripts\sound.cnut

constant Nat
constant Double

param a, b, c
typeof(b) -> Nat
typeof(c) -> Double

operator add 2
forall u, typeof(add(u, _)) -> typeof(u)
forall u v, sound(add(u,v)) -> sound(u) /\ sound(v) /\ (typeof(u) = typeof(v))
";

            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parsing problem");
            RewriteSystem rw = ctx.Rewriter;
            ModelChecker checker = new ModelChecker(rw);

            Term x = new LambdaVariable("x");
            ctx.RootNamespace.TryGetOperator("add", 2, out Operator add);
            ctx.RootNamespace.TryGetOperator("a", 0, out Operator a);
            ctx.RootNamespace.TryGetOperator("b", 0, out Operator b);

            Term t = Special.Lambda[x, add[x, a]];
            var sound = checker.CheckAndIngest(t);
            Assert.AreEqual(Special.True, sound);


            System.Action<Term> tell = s => System.Console.WriteLine("{0} :: {1}", s, CheckType(rw, s));
            tell(t);
            tell(a);

            Term t2 = Special.Eval[t, b];
            var sound2 = checker.CheckAndIngest(t2);
            Assert.AreEqual(Special.True, sound2);


            ctx.RootNamespace.TryGetOperator("Nat", 0, out Operator Nat);
            Assert.AreEqual(Nat, CheckType(rw, a));
            Assert.AreEqual(Special.MapsTo[Nat, Nat], CheckType(rw, t));

            // This should fail
            ctx.RootNamespace.TryGetOperator("c", 0, out Operator c);
            Term t3 = Special.Eval[t, c];
            var result = checker.CheckAndIngest(t3);
            Assert.AreEqual(Special.False, result);

        }
    }
}
