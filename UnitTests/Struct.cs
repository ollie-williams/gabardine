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
using Gabardine;
using Gabardine.Codegen;
using Gabardine.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gabardine.UnitTests
{
    [TestClass]
    public unsafe class Struct
    {

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct SwapPair1
        {
            public Int16 first;
            public Int64 second;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct SwapPair2
        {
            public Int64 first;
            public Int16 second;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void SwapFunc(SwapPair1 pair, ref SwapPair2 result);


        [TestMethod]
        public void Pair()
        {
            Gabardine.UserSettings.LoadSettings(@"..\..\..\user_settings.json");

            const string script = @"
operator pair 2
operator first 1
operator second 1

size(pair(_,_)) -> 1 // Not in general..?

codegen pair(u,v) {
	%u = generate u
	%v = generate v
	%result = struct %u %v
	return %result
}

codegen first(p) {
	%pair = generate p
	%result = field 0 %pair
	return %result
}

codegen second(p) {
    %pair = generate p
	%result = field 1 %pair
	return %result
}

module Pair {
    function swap {
        in <int16, int64> p
        out <int64, int16>* pair(second(p),first(p))
    }

    function nest {
        in <double, <int32, double>> triple
        return double second(second(triple))
    }
}
";
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parser failed.");

            using (DynamicLoader loader = new DynamicLoader("Pair")) {

                SwapPair1 pair;
                pair.first = 47;
                pair.second = 82;

                SwapPair2 expected;
                expected.first = pair.second;
                expected.second = pair.first;

                SwapFunc swap = loader.GetFunction<SwapFunc>("swap");

                //var actual = swap(pair);
                SwapPair2 actual = new SwapPair2();
                swap(pair, ref actual);

                Assert.AreEqual(expected, actual);

            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void PackFunc(double* a, double x, double* b, int n, double* result);

        [TestMethod]
        public void StructGen()
        {
            Gabardine.UserSettings.LoadSettings(@"..\..\..\user_settings.json");

            const string script = @"

size(cons(h,t)) -> size(h) + size(t)
size(nil) -> 0u

codegen cons(head,tail) {
    %h = generate head
    %t = generate tail
    %hs = generate size(head)
    %ts = generate size(tail)
    %size = add %hs %ts
    %result = alloc double* %size // Yes, I know. Need to figure out size issues
    copy %result <- %h %hs
    %off = offset %result %hs
    copy %off <- %t %ts
    return %result
}


module Structs {
    function pack {
        in double* a
        in double x
        in double* b
        in native n

        size(a) -> n
        size(b) -> n
        size(x) -> 1u // yuck

        out double* cons(a, cons(x, b))
    }
}
";

            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parser failed.");

            using (DynamicLoader loader = new DynamicLoader("Structs.dll")) {
                PackFunc pack = loader.GetFunction<PackFunc>("pack");

                Random rng = new Random();

                for (int k = 0; k < 10; ++k) {
                    int n = rng.Next(10, 1000);
                    double[] a = new double[n];
                    double x = rng.NextDouble();
                    double[] b = new double[n];
                    for (int i = 0; i < n; ++i) {
                        a[i] = rng.NextDouble();
                        b[i] = rng.NextDouble();
                    }

                    double[] result = new double[2 * n + 1];
                    fixed (double* _a = &a[0], _b = &b[0], _result = &result[0])
                    {
                        pack(_a, x, _b, n, _result);
                    }

                    int j = 0;
                    for (int i = 0; i < n; ++i) {
                        Assert.AreEqual(a[i], result[j++]);
                    }
                    Assert.AreEqual(x, result[j++]);
                    for (int i = 0; i < n; ++i) {
                        Assert.AreEqual(b[i], result[j++]);
                    }
                }
            }
        }
    }
}
