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
    public class ArithmeticTests
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int FuncA(int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate double FuncB(double x, double y, double z);

        [TestMethod]
        public void Arithmetic()
        {
            Gabardine.UserSettings.LoadSettings(@"..\..\..\user_settings.json");

            const string script = @"
require ../../../scripts/sound.cnut
require ../../../scripts/real.cnut

Integer -> <int32>
Real -> <float64>

forall u, isConst(u) => typeof(u) -> breakout(typeof(u)) {priority -1}

module ArithmeticTests {
    function funcA x y {
        return x * 7 + y
    }

    function funcB x y z {
        return x*y - z*42.0
    }
}
";
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            bool success = parser.Parse(script);
            Assert.IsTrue(success);

            using (DynamicLoader loader = new DynamicLoader("ArithmeticTests.dll")) {

                FuncA funcA = loader.GetFunction<FuncA>("funcA");
                FuncB funcB = loader.GetFunction<FuncB>("funcB");
                Random rng = new Random();

                for (int i = 0; i < 100; ++i) {
                    {
                        int x = rng.Next();
                        int y = rng.Next();
                        int actual = funcA(x, y);
                        int expected = x * 7 + y;
                        Assert.AreEqual(expected, actual);
                    }

                    {
                        double x = rng.NextDouble();
                        double y = rng.NextDouble();
                        double z = rng.NextDouble();

                        double actual = funcB(x, y, z);
                        double expected = x * y - z * 42.0;
                        Assert.AreEqual(expected, actual);
                    }
                }


            }


        }
    }
}
