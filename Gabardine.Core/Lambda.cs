/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System.Collections.Generic;
using System.Linq;

namespace Gabardine {
    
    class LambdaOperator : Operator 
    {
        public LambdaOperator()
          : base(2, OperatorKind.Function, "lambda")
        {}

        public override Term CreateTerm(params Term[] children)
        {
            switch (children[0].Op.Kind) {
                // Lambda variables or pattern variables are permitted
                // (in concrete expressions and patterns,
                // respectively). Everything else is an error.
                case OperatorKind.LambdaVariable:
                case OperatorKind.PatternVariable:
                    return base.CreateTerm(children);
                case OperatorKind.Function:
                    if (children[0].Op == Special.Fresh) {
                        return base.CreateTerm(children);
                    }
                    break;
                default:
                    break;
            }

            throw new System.ArgumentException("Lambda expression cannot have this kind of operator as its first argument.");
        }
    }

    public class LambdaVariable : Operator 
    {
        public LambdaVariable(string name)
          : base(0, OperatorKind.LambdaVariable, name)
        {}
    }

           
}