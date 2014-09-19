/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;
using System.Collections.Generic;

namespace Gabardine.Codegen
{
    public class ForeignLibrary
    {
        readonly Dictionary<string, ForeignFunction> functions = new Dictionary<string, ForeignFunction>();

        public ForeignLibrary()
        {
            var i8ptr = LowLevelType.Integer(8, true).Pointer();
            AddFunction(new ForeignFunction("malloc", new LowLevelType[] { LowLevelType.NativeInteger }, i8ptr));
            AddFunction(new ForeignFunction("free", new LowLevelType[] { i8ptr }, LowLevelType.Void));
            AddFunction(new ForeignFunction("malloc_leak", new LowLevelType[] { LowLevelType.NativeInteger, LowLevelType.Integer(32, true) }, i8ptr));
            AddFunction(new ForeignFunction("free_leak", new LowLevelType[] { i8ptr }, LowLevelType.Void));
            AddFunction(new ForeignFunction("memcpy", new LowLevelType[] { i8ptr, i8ptr, LowLevelType.NativeInteger }, LowLevelType.Void));
            AddFunction(new ForeignFunction("dump_leaks", new LowLevelType[0], LowLevelType.Void));
        }

        public void AddFunction(ForeignFunction function)
        {
            functions.Add(function.Name, function);
        }

        public ForeignFunction GetFunction(string name)
        {
            return functions[name];
        }
    }
}
