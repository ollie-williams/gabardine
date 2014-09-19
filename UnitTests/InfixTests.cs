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

namespace Gabardine.UnitTests
{

    using Namespace = Gabardine.Parser.Namespace;


    [TestClass]
    public class InfixTests
    {
        [TestMethod]
        public void Leaves()
        {
            Namespace ns = new Namespace();
            Term x = new Operator(0, OperatorKind.PatternVariable, "x");
            Term y = new Operator(0, OperatorKind.Parameter, "y");
            Term foo = new Operator(0, OperatorKind.Function, "foo");
            ns.add(x.Op); ns.add(y.Op); ns.add(foo.Op);

            Tuple<string, Term>[] tests = new Tuple<string, Term>[] {
                Tuple.Create("43.79", Term.Const(43.79)),
                Tuple.Create("1", Term.Const(1)),
                Tuple.Create("7", Term.Const(7)),
                Tuple.Create("37", Term.Const(37)),
                Tuple.Create("-932", Term.Const(-932)),
                Tuple.Create("x", x),
                Tuple.Create("y", y),
                Tuple.Create("foo", foo),
            };

            Infix parser = new Infix();
            foreach (var pair in tests) {
                Term parsed = parser.Parse(ns, pair.Item1);
                Assert.AreEqual<Term>(pair.Item2, parsed);
            }
        }

        [TestMethod]
        public void NoParens()
        {
            Namespace ns = new Namespace();
            Operator add = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "+", Precedence = 60, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(add);
            Operator sub = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "-", Precedence = 60, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(sub);
            Operator mul = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "*", Precedence = 70, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(mul);
            Term x = new Operator(0, OperatorKind.PatternVariable, "x");
            ns.add(x.Op);
            Term y = new Operator(0, OperatorKind.PatternVariable, "y");
            ns.add(y.Op);
            Term z = new Operator(0, OperatorKind.Parameter, "z");
            ns.add(z.Op);
            Term foo = new Operator(0, OperatorKind.PatternVariable, "foo");
            ns.add(foo.Op);


            Tuple<string, Term>[] tests = new Tuple<string, Term>[] {
                Tuple.Create(" 3 * 7", mul[Term.Const(3), Term.Const(7)]),
                Tuple.Create("3 * 7 * x", mul[mul[Term.Const(3), Term.Const(7)], x]),
                Tuple.Create("1 + 2", add[Term.Const(1), Term.Const(2)]),
                Tuple.Create("1 - 2 + x", add[sub[Term.Const(1), Term.Const(2)], x]),
                Tuple.Create("3 + 7 * 11", add[Term.Const(3), mul[Term.Const(7), Term.Const(11)]]),
                Tuple.Create("3 * 7 + 11", add[mul[Term.Const(3), Term.Const(7)], Term.Const(11)]),
                Tuple.Create("x + 4", add[x, Term.Const(4)]),
                Tuple.Create("x + y + 42 + foo", add[add[add[x,y], Term.Const(42)], foo]),
                Tuple.Create("x+y + 42+foo", add[add[add[x,y], Term.Const(42)], foo]),
            };

            Infix parser = new Infix();
            parser.AddOperator(add);
            parser.AddOperator(sub);
            parser.AddOperator(mul);
            foreach (var pair in tests) {
                Term parsed = parser.Parse(ns, pair.Item1);
                Assert.AreEqual<Term>(pair.Item2, parsed);
            }
        }

        [TestMethod]
        public void NoFuncs()
        {
            Namespace ns = new Namespace();
            Operator add = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "+", Precedence = 60, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(add);
            Operator sub = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "-", Precedence = 60, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(sub);
            Operator mul = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "*", Precedence = 70, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(mul);
            Term x = new Operator(0, OperatorKind.PatternVariable, "x");
            ns.add(x.Op);
            Term y = new Operator(0, OperatorKind.PatternVariable, "y");
            ns.add(y.Op);
            Term z = new Operator(0, OperatorKind.Parameter, "z");
            ns.add(z.Op);
            Term foo = new Operator(0, OperatorKind.PatternVariable, "foo");
            ns.add(foo.Op);


            Tuple<string, Term>[] tests = new Tuple<string, Term>[] {
                Tuple.Create("(x + y)", add[x,y]),
                Tuple.Create("(z * 7)", mul[z,Term.Const(7)]),
                Tuple.Create("(z) * (7)", mul[z,Term.Const(7)]),
                Tuple.Create("((z)) * ((7))", mul[z,Term.Const(7)]),
                Tuple.Create("(((z)) * ((7)))", mul[z,Term.Const(7)]),
                Tuple.Create("x + (y + z)", add[x,add[y,z]]),
                Tuple.Create("(x + y) + z", add[add[x,y],z]),
                Tuple.Create("3 * foo + 11", add[mul[Term.Const(3),foo], Term.Const(11)]),
                Tuple.Create("3 * (foo + 11)", mul[Term.Const(3), add[foo,Term.Const(11)]]),
            };

            Infix parser = new Infix();
            parser.AddOperator(add);
            parser.AddOperator(sub);
            parser.AddOperator(mul);
            foreach (var pair in tests) {
                Term parsed = parser.Parse(ns, pair.Item1);
                Assert.AreEqual<Term>(pair.Item2, parsed);
            }
        }

        [TestMethod]
        public void Funcs()
        {
            Namespace ns = new Namespace();
            Operator add = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "+", Precedence = 60, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(add);
            Operator sub = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "-", Precedence = 60, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(sub);
            Operator mul = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "*", Precedence = 70, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(mul);
            Term x = new Operator(0, OperatorKind.PatternVariable, "x");
            ns.add(x.Op);
            Term y = new Operator(0, OperatorKind.PatternVariable, "y");
            ns.add(y.Op);
            Term z = new Operator(0, OperatorKind.Parameter, "z");
            ns.add(z.Op);
            Term foo = new Operator(0, OperatorKind.PatternVariable, "foo");
            ns.add(foo.Op);
            Operator f = new Operator(2, OperatorKind.Function, "f");
            Operator g = new Operator(1, OperatorKind.Function, "g");
            Operator h = new Operator(1, OperatorKind.Function, "h");
            ns.add(f); ns.add(g); ns.add(h);


            Tuple<string, Term>[] tests = new Tuple<string, Term>[] {
                Tuple.Create("g(x)", g[x]),
                Tuple.Create("f(x,y)", f[x,y]),
                Tuple.Create("f( x , y ) ", f[x,y]),
                Tuple.Create("f(3, 7)", f[Term.Const(3), Term.Const(7)]),
                Tuple.Create("g(x + y)", g[add[x,y]]),
                Tuple.Create("g(x) + g(y)", add[g[x], g[y]]),
                Tuple.Create("g(x) * (f(x, y) + h(4))", mul[g[x], add[f[x,y], h[Term.Const(4)]]]),
                Tuple.Create("g(h(x))", g[h[x]]),
                Tuple.Create("f(g(x), h(y) * (7 + foo + f(z,z)))", f[g[x], mul[h[y], add[add[Term.Const(7), foo], f[z,z]]]]),
                Tuple.Create("z()", z),
            };

            Infix parser = new Infix();
            parser.AddOperator(add);
            parser.AddOperator(sub);
            parser.AddOperator(mul);
            foreach (var pair in tests) {
                Term parsed = parser.Parse(ns, pair.Item1);
                Assert.AreEqual<Term>(pair.Item2, parsed);
            }
        }

        [TestMethod]
        public void Arrow()
        {
            Namespace ns = new Namespace();
            ns.addSpecials();
            Operator add = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "+", Precedence = 60, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(add);
            Operator sub = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "-", Precedence = 60, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(sub);
            Operator gt = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = ">", Precedence = 50, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(gt);
            Term x = (new Operator(0, OperatorKind.PatternVariable, "x"));
            Term y = (new Operator(0, OperatorKind.PatternVariable, "y"));
            Term z = (new Operator(0, OperatorKind.Parameter, "z"));
            Term foo = (new Operator(0, OperatorKind.PatternVariable, "foo"));
            Operator f = new Operator(2, OperatorKind.Function, "f");
            Operator g = new Operator(1, OperatorKind.Function, "g");
            Operator h = new Operator(1, OperatorKind.Function, "h");
            ns.add(x.Op); ns.add(y.Op); ns.add(z.Op); ns.add(foo.Op);
            ns.add(f); ns.add(g); ns.add(h);

            var maps = Special.MapsTo;
            Tuple<string, Term>[] tests = new Tuple<string, Term>[] {
                Tuple.Create("g(x) -> x", maps[g[x], x]),
                Tuple.Create("f(x,y) + z -> f(z,y) - g(foo)", maps[add[f[x,y],z], sub[f[z,y],g[foo]]]),
                Tuple.Create("f(x,y) - z > foo -> f(z,y) - g(foo)", maps[ gt[sub[f[x,y],z], foo], sub[f[z,y],g[foo]]]),
            };

            Infix parser = new Infix();
            parser.AddOperator(add);
            parser.AddOperator(sub);
            parser.AddOperator(gt);
            foreach (var pair in tests) {
                Term parsed = parser.Parse(ns, pair.Item1);
                Assert.AreEqual<Term>(pair.Item2, parsed);
            }
        }

        [TestMethod]
        public void Lambda()
        {
            Namespace ns = new Namespace();
            ns.addSpecials();
            Operator add = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "+", Precedence = 60, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(add);
            Term z = (new Operator(0, OperatorKind.Parameter, "z"));
            ns.add(z.Op);

            Term a = new Operator(0, OperatorKind.PatternVariable, "a");
            Term b = new Operator(0, OperatorKind.PatternVariable, "b");

            Tuple<string, Term>[] tests = new Tuple<string, Term>[] {
                Tuple.Create("λx -> x + z", Special.Lambda[a, add[a,z]]),
                Tuple.Create("λy -> y + z", Special.Lambda[a, add[a,z]]),
                Tuple.Create("λx -> λy -> x + y", Special.Lambda[a, Special.Lambda[b, add[a,b]]]),
            };

            Infix parser = new Infix();
            parser.AddOperator(add);
            foreach (var pair in tests) {
                Term parsed = parser.Parse(ns, pair.Item1);
                PatternMatcher pattern = PatternMatcherFactory.Create(pair.Item2);
                Assert.IsTrue(pattern.Match(parsed), "Expected {0} to match pattern {1}.", parsed, pattern.Pattern);
            }
        }

        [TestMethod]
        public void Let()
        {
            Namespace ns = new Namespace();
            Operator add = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "+", Precedence = 60, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(add);
            Term z = (new Operator(0, OperatorKind.Parameter, "z"));
            ns.add(z.Op);

            Term a = new Operator(0, OperatorKind.PatternVariable, "a");

            Tuple<string, Term>[] tests = new Tuple<string, Term>[] {
                Tuple.Create("let x = z + z in z + x", Special.Let[a, add[z,z], add[z,a]]),
                Tuple.Create("let x = z + z  z + x", Special.Let[a, add[z,z], add[z,a]]),
            };

            Infix parser = new Infix();
            parser.AddOperator(add);
            foreach (var pair in tests) {
                Term parsed = parser.Parse(ns, pair.Item1);
                PatternMatcher pattern = PatternMatcherFactory.Create(pair.Item2);
                Assert.IsTrue(pattern.Match(parsed), "Expected {0} to match pattern {1}.", parsed, pattern.Pattern);
            }
        }

        [TestMethod]
        public void PrettyInfix()
        {
            Namespace ns = new Namespace();
            ns.addSpecials();
            Infix parser = new Infix();
            Operator add = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "+", Precedence = 60, Style = OperatorSyntax.Fix.LeftAssociative });
            ns.add(add);
            parser.AddOperator(add);
            Operator sub = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "-", Style = OperatorSyntax.Fix.LeftAssociative, Precedence = 60 });
            ns.add(sub);
            parser.AddOperator(sub);
            Operator mul = new Operator(2, OperatorKind.Function, new OperatorSyntax { Name = "*", Style = OperatorSyntax.Fix.LeftAssociative, Precedence = 70 });
            parser.AddOperator(mul);
            Operator not = new Operator(1, OperatorKind.Function, new OperatorSyntax { Name = "¬", Style = OperatorSyntax.Fix.Prefix, Precedence = 90 });
            ns.add(not);
            parser.AddOperator(not);

            Operator f = new Operator(2, OperatorKind.Function, "f");
            ns.add(f);
            Operator g = new Operator(1, OperatorKind.Function, "g");
            ns.add(g);

            Term x = new Operator(0, OperatorKind.Parameter, "x");
            Term y = new Operator(0, OperatorKind.Parameter, "y");
            Term z = new Operator(0, OperatorKind.Parameter, "z");
            ns.add(x.Op); ns.add(y.Op); ns.add(z.Op);

            // Using a pattern variable so that let doesn't create its own local version
            Term t = new Operator(0, OperatorKind.PatternVariable, "t");
            Term s = new Operator(0, OperatorKind.PatternVariable, "s");
            ns.add(t.Op); ns.add(s.Op);

            string[] tests = new string[] {
                  "x + y",
                  "x * y",
                  "x + y * z",
                  "x * y + z",
                  "x * (y + z)",
                  "x + y + z",
                  "(x + y) + z",
                  "x + (y + z)",
                  "32 * f(x + y, z) + 1e-06",
                  "x * g(z) + y",
                  "g(g(x * (x + f(y, y)))) + 22.56",
                  "x + (y - z) - (x - y)",
                  "λs -> λt -> s + t",
                  "let t = y - z * 4 in t * (2 - z)",
                  "¬y",
                  "¬(x + 3)",
                  "¬x + 3",
                  "x[y]",
                  "x[y + z]",
                  "(x + z)[g(y)]"

            };

            foreach (string source in tests) {
                Terminal.WriteLine("source:  {0}", source);
                Term from_source = parser.Parse(ns, source);
                Terminal.WriteLine("parsed1: {0}", from_source);
                string printed = PrettyPrinter.InfixFormat(from_source);
                Terminal.WriteLine("pretty:  {0}", printed);
                Term from_pretty = parser.Parse(ns, printed);
                Terminal.WriteLine("parsed2: {0}", from_pretty);
                Terminal.WriteLine();
                Assert.AreEqual(from_source, from_pretty, "Infix parsing/printing didn't correctly perform a round trip.");
                Assert.IsTrue(printed.Length <= source.Length, "Pretty printer is wasteful.");
            }
        }
    }
}
