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

namespace Gabardine.UnitTests
{
    [TestClass]
    public class CodeGeneration
    {
        [TestMethod]
        public void PolyGeneration()
        {

#if false
forall u v, typeof(u) ~> Integer => codegen u + v
{
    ...
}

forall u v, typeof(v) ~> Float => codegen u + v
{
    ...
}

forall u, u is Scalar => codegen a[n]
{
    ...
}

forall u, ~(u is Scalar) => codegen a[n]
{
    ...
}




Difftype(Real, Real) -> Real
Difftype(Matrix, Real) -> Matrix
Difftype(Real, Matrix) -> Matrix
Difftype(Matrix, Matrix) -> Matrix

Operator diff (Diffable a, Diffable b) => a -> b -> Difftype(a,b)

Diff_zero(x:Matrix) -> zeros(rows(x), cols(x))
Diff_zero(x:Real) -> 0.0

/****** In Haskell *************

class Diffable a where
    Diff_zero :: a -> a

instance Diffable Real where
    Diff_zero(_) = 0.0

instance Diffable Matrix where
    Diff_zero(x) = zeros(rows(x), cols(x))

*******************************/
#endif
        }
    }
}
