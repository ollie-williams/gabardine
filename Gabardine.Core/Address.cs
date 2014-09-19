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
using System.Linq;

namespace Gabardine
{
    public class Address
    {
        readonly int[] path;

        Address(int[] path)
        {
            this.path = path;
        }

        public Address(IEnumerable<int> path)
            : this(path.ToArray())
        {}

        public int Depth { get { return path.Length; } }

        public int this[int index] {  get { return path[index]; } }

        public Term Get(Term root)
        {
            return AtDepth(root, Depth);
        }

        public Address ToDepth(int depth)
        {
            if (depth > Depth) {
                throw new ArgumentException("Invalid depth.");
            }
            int[] newpath = new int[depth];
            Array.Copy(path, newpath, depth);
            return new Address(newpath);
        }

        public Address ParentAddress()
        {
            return ToDepth(Depth - 1);
        }

        public Term AtDepth(Term root, int depth)
        {
            for (int i = 0; i < depth; ++i) {
                root = root[path[i]];
            }
            return root;
        }

        public Term Parent(Term root)
        {
            return AtDepth(root, Depth - 1);
        }

        public IEnumerable<Term> Trace(Term root)
        {
            for (int i = 0; i < path.Length; ++i) {
                yield return root;
                root = root[path[i]];
            }
            yield return root;
        }

        public Term Replace(Term root, Term replacement)
        {
            Term[] trace = Trace(root).ToArray();
            Term retval = replacement;
            for (int i = Depth-1; i >= 0; --i) {
                var repChildren = trace[i].Children().ReplaceAt(path[i], retval);
                retval = trace[i].Op.CreateTerm(repChildren);
            }
            return retval;
        }
    }
}
