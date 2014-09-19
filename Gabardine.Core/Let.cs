/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
namespace Gabardine 
{
    public class LetOperator : Operator
    {
        public LetOperator()
        : base(3, OperatorKind.Function, "let")
        {}

        public override Term CreateTerm(params Term[] children)
        {
            switch(children[0].Op.Kind) {
            case OperatorKind.LetVariable:
            case OperatorKind.PatternVariable:
                return base.CreateTerm(children);
            default:
                    throw new System.ArgumentException("Let expression cannot have this kind of operator as its first argument.");
            }
        }
    }
   

    public class LetVariable : Operator 
    {
        readonly Term binding;

        public LetVariable(string name, Term binding)
            : base(0, OperatorKind.LetVariable, name)
        {
            this.binding = binding;
        }

        public Term Binding { get { return binding; } }
        
    }
}