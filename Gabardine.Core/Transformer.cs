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

namespace Gabardine
{
    public interface Transformer<out T> where T : Tendril
    {
        IEnumerable<T> FindTransformations(Term root);
    }

    public class TransformerGroup : Transformer<Tendril>
    {
        readonly List<Transformer<Tendril>> members = new List<Transformer<Tendril>>();

        public void AddMember(Transformer<Tendril> trans)
        {
            members.Add(trans);
        }

        public IEnumerable<Tendril> FindTransformations(Term root)
        {
            return members.SelectMany(x => x.FindTransformations(root));
        }
    }
}
