/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;
using System.Runtime.InteropServices;
using Gabardine.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MathNet.Numerics.Random;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using Gabardine.Codegen;

namespace Gabardine.UnitTests
{
    [TestClass]
    public unsafe class Gaussian
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate double GaussMVFunc(int d, double* mu, double* L, double* X);

        [TestMethod]
        public void Multivariate()
        {
            Gabardine.UserSettings.LoadSettings(@"..\..\..\user_settings.json");
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            var files = new string[] {
                @"..\..\..\scripts\type.cnut",
                @"..\..\..\scripts\boolean.cnut",
                @"..\..\..\scripts\real.cnut",
                @"..\..\..\scripts\nat.cnut",
                @"..\..\..\scripts\list.cnut",
                @"..\..\..\scripts\matrix.cnut",
                @"..\..\..\scripts\mkl.cnut",
            };

            const string test_script = @"
let inv_root_2_pi = 0.39894228040143267793994605993439

operator mvgauss 3
    mvgauss(mu, L, x) ->
        let n = size(x)
        let delta = msub(x, mu)
        let mahal = mmul(L, delta)
        inv_root_2_pi^n * det(L) * exp(-0.5 * dot(mahal, mahal))

module Gauss {

    function gauss_mv {
        in native d
            typeof(d) -> Integer
        in double* mu
            typeof(mu) -> Matrix            
            rows(mu) -> d
            cols(mu) -> 1
        in double* L
            typeof(L) -> Matrix            
            rows(L) -> d
            cols(L) -> d
        in double* X
            typeof(X) -> Matrix            
            rows(X) -> d
            cols(X) -> 1

        return double mvgauss(mu, L, X)
    }
}";
            parser.ParseFiles(files);
            bool success = parser.Parse(test_script);
            Assert.IsTrue(success, "Parser failed.");


            using(DynamicLoader loader = new DynamicLoader("Gauss.dll")) {
                GaussMVFunc gauss_mv = loader.GetFunction<GaussMVFunc>("gauss_mv");

                //MathNet.Numerics.Random.S
                Random rng = new System.Random();

                int n = 7;
                double[] L = new double[] { 0.706644, 0.896136, 0.919934, 0.814418, 0.367993, 0.675905, 0.012755, 0.000731, 0.617264, 0.143349, 0.694183, 0.863898, 0.861077, 0.986655, 0.024150, 0.264388, 0.917795, 0.434940, 0.090543, 0.545614, 0.307029, 0.001597, 0.055090, 0.333908, 0.973696, 0.063559, 0.810768, 0.063838, 0.446873, 0.486398, 0.951193, 0.235079, 0.801584, 0.047667, 0.975726, 0.977026, 0.178280, 0.688954, 0.569282, 0.605326, 0.703184, 0.419755, 0.669704, 0.221587, 0.137551, 0.985457, 0.999725, 0.186777, 0.101080 };
                //const double detL = -0.18668192527125585;

                double[] X = new double[n];
                double[] mu = new double[n];

                for (int i = 0; i < n; ++i) {
                    X[i] = rng.NextDouble();
                    mu[i] = rng.NextDouble();
                }

                Vector<double> _X = DenseVector.OfEnumerable(X);
                Vector<double> _mu = DenseVector.OfEnumerable(mu);
                Matrix<double> _L = DenseMatrix.OfColumnMajor(n, n, L);

                double detL =  _L.Determinant();
                var delta = _X - _mu;
                var mh = _L * delta;
                double dot = mh.DotProduct(mh);

                double expected = Math.Exp(-0.5 * dot) * detL * Math.Pow(2 * Math.PI, -0.5 * n);
                double actual;
                fixed(double* __x = &X[0], __mu = &mu[0], __L = &L[0])
                {
                    actual = gauss_mv(n, __mu, __L, __x);
                }
                Terminal.Write("{0}  {1}", expected, actual);
                Assert.AreEqual(expected, actual, 1e-6);
            }

        }
    }
}
