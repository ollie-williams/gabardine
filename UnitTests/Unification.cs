/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gabardine.UnitTests
{
    [TestClass]
    public class Unification
    {
        [TestMethod]
        public void Unify()
        {
            Unifier unifier = new Unifier();

            Operator f = new Operator(2, OperatorKind.Function, "f");
            Operator g = new Operator(1, OperatorKind.Function, "g");
            Term a = new Operator(0, OperatorKind.Function, "a");
            Term b = new Operator(0, OperatorKind.Function, "b");
            Term x = new Operator(0, OperatorKind.PatternVariable, "x");
            Term y = new Operator(0, OperatorKind.PatternVariable, "y");

            bool ok;

            ok = unifier.Unify(f[a, x], f[y, b]);
            Assert.IsTrue(ok, "Expression is unifiable.");
            Assert.AreEqual(unifier.Reduce(x), b);
            Assert.AreEqual(unifier.Reduce(y), a);
        }
    }
}
