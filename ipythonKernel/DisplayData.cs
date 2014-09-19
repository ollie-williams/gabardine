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

namespace ipythonKernel
{
    struct DisplayData : ITypedMessage
    {
        // Who create the data
        public string source { get; set; }

        // The data dict contains key/value pairs, where the keys are MIME
        // types and the values are the raw data of the representation in that
        // format.
        public Dictionary<string, object> data { get; set; }

        // Any metadata that describes the data
        public Dictionary<string, object> metadata { get; set; }

        public ShellMessageType Type { get { return ShellMessageType.display_data; } }
    }
}
