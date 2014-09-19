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
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gabardine.UnitTests
{
    [TestClass]
    public unsafe class MatrixTests
    {

        const string saxpyscript = @"
// Matrix addition
operator madd 2
operator saxpy 4

forall x y, madd(x,y) -> saxpy(1.0, x, y, size(x))

forall n, size(saxpy(_,_,_,n)) -> n
forall a b c n, 
    sound(saxpy(a, b, c, n)) -> sound(a) /\ sound(b) /\ sound(c) /\ sound(n)


foreign void cblas_daxpy(native N, double alpha, double* X,
                         native incX, double* Y, native incY)
forall a x y n, 
  codegen saxpy(a, x, y, n) {
    %y = generate y
    %n = generate n
    %result = alloc float64* %n
    copy %result <- %y %n
    %x = generate x
    %a = generate a
    call cblas_daxpy(%n, %a, %x, native 1, %result, native 1)
    return %result
}
";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void AddFunc(double* x, double* y, int n, double* result);

        [TestMethod]
        public void Saxpy()
        {
            Gabardine.UserSettings.LoadSettings(@"..\..\..\user_settings.json");

            const string script = @"
require ../../../scripts/sound.cnut

module SaxpyTests {
    function addition x y n {

        typeof(x) -> <double*>
        typeof(y) -> <double*>
        typeof(n) -> <native>

        size(x) -> n
        size(y) -> n
        out madd(x, y)
    }
}";
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            bool success = parser.Parse(saxpyscript);
            Assert.IsTrue(success, "Parser failed.");
            success = parser.Parse(script);
            Assert.IsTrue(success, "Parser failed.");


            using (DynamicLoader loader = new DynamicLoader("SaxpyTests.dll")) {
                AddFunc addition = loader.GetFunction<AddFunc>("addition");

                Random rng = new Random();

                for (int j = 0; j < 10; ++j) {
                    int n = rng.Next(10, 1000);
                    double[] x = new double[n];
                    double[] y = new double[n];
                    double[] z = new double[n];

                    for (int i = 0; i < n; ++i) {
                        x[i] = rng.NextDouble();
                        y[i] = rng.NextDouble();
                    }

                    fixed (double* _x = &x[0], _y = &y[0], _z = &z[0])
                    {
                        addition(_x, _y, n, _z);
                    }

                    Vector<double> X = DenseVector.OfEnumerable(x);
                    Vector<double> Y = DenseVector.OfEnumerable(y);
                    Vector<double> Actual = DenseVector.OfEnumerable(z);
                    Vector<double> Expected = X + Y;

                    for (int i = 0; i < n; ++i) {
                        Assert.AreEqual(Actual[i], Expected[i], 1e-4, "Vector entries don't match.");
                    }
                }
            }
        }

        const string indexscript = @"
codegen array[ordinal] {
    %array = generate array
    %ordinal = generate ordinal
    %elementSize = generate size(index(array, 0))
    %off = mul %elementSize %ordinal
    %result = alloc double* %elementSize
    %src = offset %array %off
    copy %result <- %src %elementSize
    return %result
}
";


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void BuildFunc(double* x, double* y, int n, int m, double* result);

        [TestMethod]
        public void BuildMatrixArray()
        {
            Gabardine.UserSettings.LoadSettings(@"..\..\..\user_settings.json");

            const string script =
                @"
require ..\..\..\scripts\array.cnut
require ..\..\..\scripts\matrix.cnut
require ..\..\..\scripts\mkl.cnut

module MatrixGenerate {
    function build {
        in double* x
        in double* y
        in native n
        in native m
        typeof(x) -> Array(Matrix)
        length(x) -> m
        rows(index(x,_)) -> n
        cols(index(x,_)) -> 1
        size(y) -> n

        out double* build(m, i => madd(x[i], y))
    }
}
";
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parser failed.");

            using (DynamicLoader loader = new DynamicLoader("MatrixGenerate.dll")) {
                BuildFunc build = loader.GetFunction<BuildFunc>("build");

                Random rng = new Random();

                for (int k = 0; k < 10; ++k) {
                    int n = rng.Next(10, 100);
                    int m = rng.Next(10, 100);
                    double[] x = new double[n * m];
                    double[] y = new double[n];
                    double[] result = new double[n * m];

                    for (int i = 0; i < x.Length; ++i) {
                        x[i] = rng.NextDouble();
                    }
                    for (int i = 0; i < y.Length; ++i) {
                        y[i] = rng.NextDouble();
                    }

                    fixed (double* _x = &x[0], _y = &y[0], _r = &result[0])
                    {
                        build(_x, _y, n, m, _r);
                    }

                    for (int i = 0; i < x.Length; ++i) {
                        int j = i % y.Length;
                        Assert.AreEqual(x[i] + y[j], result[i]);
                    }
                }
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void IndexFunc(double* x, int n, double* result);


        [TestMethod]
        public void IndexMatrixArray()
        {
            Gabardine.UserSettings.LoadSettings(@"..\..\..\user_settings.json");

            const string script = indexscript + @"
module MatrixIndex {
    function index {
        in double* x
        in native n
        size(index(x,_)) -> n

        out double* x[2]
    }
}
";
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parser failed.");

            using (DynamicLoader loader = new DynamicLoader("MatrixIndex.dll")) {

                var index = loader.GetFunction<IndexFunc>("index");

                Random rng = new Random();
                int n = 10;

                double[] x = new double[n * 5];
                for (int i = 0; i < x.Length; ++i) {
                    x[i] = rng.NextDouble();
                }

                double[] result = new double[n];

                fixed (double* _x = &x[0], _result = &result[0])
                {
                    index(_x, n, _result);
                }

                int j = n * 2;
                for (int i = 0; i < n; ++i) {
                    Assert.AreEqual(x[j++], result[i]);
                }

            }
        }

        

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void MultiplyFunc(double* A, double* B, int m, int n, int k, double* result1, double* result2, double* kron);


        [TestMethod]
        public void Multiply()
        {
            Gabardine.UserSettings.LoadSettings(@"..\..\..\user_settings.json");

            var files = new string[] {
                @"..\..\..\scripts\type.cnut",
                @"..\..\..\scripts\boolean.cnut",
                @"..\..\..\scripts\real.cnut",
                @"..\..\..\scripts\nat.cnut",
                @"..\..\..\scripts\list.cnut",
                @"..\..\..\scripts\matrix.cnut",
                @"..\..\..\scripts\mkl.cnut",
            };

            const string script = @"
module linear {
	function matrix_multiply {
		in double* A
		in double* B
		in native m
		in native n
		in native k

		typeof(A) -> Matrix
		typeof(B) -> Matrix
		rows(A) -> m
		cols(A) -> k
		rows(B) -> k
		cols(B) -> n

		out double* mmul(A,B)
		out double* mmul(trans(B),trans(A))
        out double* kron(A,B)
	}
}
";

            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            Gabardine.Parser.Namespace root = ctx.RootNamespace;
            parser.ParseFiles(files);
            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parser failed.");

            using (DynamicLoader loader = new DynamicLoader("linear.dll")) {
                MultiplyFunc matrix_multiply = loader.GetFunction<MultiplyFunc>("matrix_multiply");

                Random rng = new Random();
                var unif = new MathNet.Numerics.Distributions.ContinuousUniform();

                for (int trial = 0; trial < 10; ++trial) {
                    int m = rng.Next(2, 10);
                    int n = rng.Next(2, 10);
                    int k = rng.Next(2, 10);

                    Matrix<double> A = DenseMatrix.CreateRandom(m, k, unif);
                    Matrix<double> B = DenseMatrix.CreateRandom(k, n, unif);
                    Matrix<double> E1 = A * B;
                    Matrix<double> E2 = B.TransposeThisAndMultiply(A.Transpose());
                    Matrix<double> EKron = A.KroneckerProduct(B);

                    double[] a = A.ToColumnWiseArray();
                    double[] b = B.ToColumnWiseArray();
                    double[] r1 = new double[m * n];
                    double[] r2 = new double[m * n];
                    double[] kron = new double[m * n * k * k];

                    fixed (double* _a = &a[0], _b = &b[0], _r1 = &r1[0], _r2 = &r2[0], _kron = &kron[0])
                    {
                        matrix_multiply(_a, _b, m, n, k, _r1, _r2, _kron);
                    }

                    Matrix<double> R1 = DenseMatrix.OfColumnMajor(m, n, r1);
                    Matrix<double> R2 = DenseMatrix.OfColumnMajor(n, m, r2);
                    for (int i = 0; i < m; ++i) {
                        for (int j = 0; j < n; ++j) {
                            Assert.AreEqual(E1[i, j], R1[i, j], 1e-4, "Multiplication error.");
                            Assert.AreEqual(E2[j, i], R2[j, i], 1e-4, "Multiplication error.");
                        }
                    }

                    Matrix<double> Kron = DenseMatrix.OfColumnMajor(m * k, k * n, kron);
                    for (int i = 0; i < m * k; ++i) {
                        for (int j = 0; j < n * k; ++j) {
                            Assert.AreEqual(EKron[i, j], Kron[i, j], 1e-4, "Kronecker product error.");
                        }
                    }
                }
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void SummationFunc(int n, int r, int c, double* x, double* result);



        [TestMethod]
        public void MatrixSummation()
        {
            Gabardine.UserSettings.LoadSettings(@"..\..\..\user_settings.json");

            var files = new string[] {
                @"..\..\..\scripts\type.cnut",
                @"..\..\..\scripts\boolean.cnut",
                @"..\..\..\scripts\real.cnut",
                @"..\..\..\scripts\nat.cnut",
                @"..\..\..\scripts\list.cnut",
                @"..\..\..\scripts\matrix.cnut",
                @"..\..\..\scripts\mkl.cnut",
                @"..\..\..\scripts\array.cnut",
                @"..\..\..\scripts\pair.cnut",
                @"..\..\..\scripts\lambda.cnut",
                @"..\..\..\scripts\iterate.cnut",
            };

            const string script = @"
module summation {
	function matrix_summation {
        in native n
        in native r
        in native c
        in double* x
        typeof(x) -> Array(Matrix)
        length(x) -> n
        rows(x[_]) -> r
        cols(x[_]) -> c

        out double* sum(0, n, i => x[i])
	}
}
";

            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            parser.ParseFiles(files);
            bool success = parser.Parse(script);
            Assert.IsTrue(success, "Parser failed.");

            using (DynamicLoader loader = new DynamicLoader("summation.dll")) {
                SummationFunc summation = loader.GetFunction<SummationFunc>("matrix_summation");

                Random rng = new Random();

                int n = 10;
                int r = 3;
                int c = 5;

                double[] expected = new double[r * c];
                double[] x = new double[n * r * c];
                for (int i = 0; i < x.Length; ++i) {
                    x[i] = rng.NextDouble();
                    expected[i % expected.Length] += x[i];
                }

                double[] result = new double[r * c];
                fixed(double* _x =&x[0], _r=&result[0])
                {
                    summation(n, r, c, _x, _r);
                }

                for (int i = 0; i < expected.Length; ++i) {
                    Assert.AreEqual(expected[i], result[i], 1e-4);
                }
            }
        }
    }
}
