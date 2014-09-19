/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
namespace CodegenHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            //(new Gabardine.UnitTests.TypeInference()).Ident();
            //(new Gabardine.UnitTests.Script()).Aux();
            //(new Gabardine.UnitTests.Script()).Sound();
            // (new Gabardine.UnitTests.Script()).ParseRules();
            //(new Gabardine.UnitTests.InfixTests()).Lambda();
            //(new Gabardine.UnitTests.TypeInference()).Ident();
            //(new Gabardine.UnitTests.TypeInference()).TypingLambdaExpression();
            //(new Gabardine.UnitTests.Script()).Requires();
            //(new Gabardine.UnitTests.MatrixTests()).MatrixSummation();
            //(new Gabardine.UnitTests.MatrixTests()).Multiply();
            //(new Gabardine.UnitTests.Iteration()).NestedLoops();
            //(new Gabardine.UnitTests.Gradient()).Rosenbrock();
            //(new Gabardine.UnitTests.Gaussian()).Multivariate();
            //(new Gabardine.UnitTests.Struct()).Pair();
            //(new Gabardine.UnitTests.Iteration()).IntegerLoop();
            //(new Gabardine.UnitTests.ArithmeticTests()).Arithmetic();
            //(new Gabardine.UnitTests.MatrixTests()).BuildMatrixArray();
            (new Gabardine.UnitTests.MatrixTests()).Saxpy();
            //(new Gabardine.UnitTests.MatrixTests()).IndexMatrixArray();
            //(new Gabardine.UnitTests.Struct()).StructGen();

        }
    }
}
