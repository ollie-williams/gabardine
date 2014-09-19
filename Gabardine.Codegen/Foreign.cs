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

namespace Gabardine.Codegen
{
    public class ForeignFunction
    {
        readonly string name;
        readonly LowLevelType returnType;
        readonly LowLevelType[] argTypes;

        public static ForeignFunction Create(string name, LowLevelType returnType, params LowLevelType[] argTypes)
        {
            return new ForeignFunction(name, argTypes, returnType);
        }

        public ForeignFunction(string name, IEnumerable<LowLevelType> argTypes, LowLevelType returnType)
        {
            this.name = name;
            this.returnType = returnType;
            this.argTypes = argTypes.ToArray();
        }

        public string Name { get { return name; } }
        public LowLevelType ReturnType { get { return returnType; } }
        public LowLevelType[] ArgTypes { get { return argTypes; } }

        public int Arity { get { return argTypes.Length; } }
    }
}
