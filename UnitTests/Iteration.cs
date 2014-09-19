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
using Gabardine.Codegen;
using Gabardine.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gabardine.UnitTests
{
    [TestClass]
    public unsafe class Iteration
    {

        string[] files = new string[] {
                @"..\..\..\scripts\type.cnut",
                @"..\..\..\scripts\boolean.cnut",
                @"..\..\..\scripts\real.cnut",
                @"..\..\..\scripts\matrix.cnut",
                @"..\..\..\scripts\pair.cnut",
                @"..\..\..\scripts\array.cnut",
                @"..\..\..\scripts\lambda.cnut",
                @"..\..\..\scripts\iterate.cnut",
            };

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int IntLoopFunc(int n);

        [TestMethod]
        public void IntegerLoop()
        {
            Gabardine.UserSettings.LoadSettings(@"..\..\..\user_settings.json");

            const string script = @"
require ..\..\..\scripts\real.cnut
require ..\..\..\scripts\iterate.cnut

module IntegerLoop {
    function loop {
        in native n
        return native iterate( 
                    λi -> i < n, 
                    λi -> i + 1,
                    0
                    )
    }
}";
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parser failed.");

            using (DynamicLoader loader = new DynamicLoader("IntegerLoop.dll")) {
                IntLoopFunc loop = loader.GetFunction<IntLoopFunc>("loop");

                int result = loop(10);
                Assert.AreEqual(result, 10);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate double NestFunc(double* x, int n);

        [TestMethod]
        public void NestedLoops()
        {
            Gabardine.UserSettings.LoadSettings(@"..\..\..\user_settings.json");

            const string script = @"
module NestedLoop {
    function nest {
        in double* x
            typeof(x) -> Array(Real)
        in native n
            typeof(n) -> Integer
        return double sum(0, n, i => sum(i, n, j => x[j]))
    }
}";
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            parser.ParseFiles(files);
            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parser failed.");

            using (DynamicLoader loader = new DynamicLoader("NestedLoop")) {
                NestFunc nest = loader.GetFunction<NestFunc>("nest");

                Random rng = new Random();
                int n = 100;

                double[] x = new double[n];
                for (int i = 0; i < n; ++i) {
                    x[i] = rng.NextDouble();
                }

                double actual;
                fixed (double* _x = &x[0])
                {
                    actual = nest(_x, n);
                }

                double sum = 0;
                for (int i = 0; i < n; ++i) {
                    for (int j = i; j < n; ++j) {
                        sum += x[j];
                    }
                }

                Assert.AreEqual(sum, actual, 1e-3);
            }
        }
    }
}
