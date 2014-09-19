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
using System.Text;
using System.Threading.Tasks;

namespace Gabardine
{
    public class CSEOpportunity : Tendril
    {
        readonly Term candidate;

        public CSEOpportunity(Term candidate)
        {
            this.candidate = candidate;
        }

        public IEnumerable<Term> Expand(Term root)
        {
            // Find all instances
            Address[] paths = Replacement.Find(root, candidate).ToArray();

            // Find least common ancestor
            int lca = -1;
            bool matching = true;
            while (matching) {
                ++lca;
                for (int i = 0; i < paths.Length; ++i) {
                    if (lca >= paths[i].Depth) {
                        matching = false;
                        break;
                    }
                    if (paths[i][lca] != paths[0][lca]) {
                        matching = false;
                        break;
                    }
                }
            }

            Address lcaPath = paths[0].ToDepth(lca);
            Term tmp = CodeMotion.MakeLetVariable(candidate);
            Term lcaRoot = lcaPath.Get(root);
            Term lifted = Special.Let[tmp, candidate, Replacement.Replace(lcaRoot, candidate, tmp)];
            yield return lcaPath.Replace(root, lifted);            
        }
    }

    public class Lifter : Transformer<CSEOpportunity>
    {
        public IEnumerable<CSEOpportunity> FindTransformations(Term root)
        {
            return FindTransformations(root, EqualityComparer<Term>.Default);
        }

        public IEnumerable<CSEOpportunity> FindTransformations(Term root, IEqualityComparer<Term> comparer)
        {
            Dictionary<Term, int> counts = new Dictionary<Term, int>(comparer);

            var stack = new Stack<Term>();
            stack.Push(root);
            while (stack.Count > 0) {

                var head = stack.Pop();
                if (head.Op.Name == "exp") { System.Diagnostics.Debugger.Break(); }

                if (counts.TryGetValue(head, out int count)) {
                    counts[head]++;
                    continue;
                }

                // If this is the first time seeing this subexpression then continue to
                // process children. If this is a common subexpression (count > 0), then
                // we do not visit the children as they have already been counted on the
                // first encounter with head.
                counts.Add(head, 1);
                for (int i = 0; i < head.Arity; ++i) {
                    stack.Push(head[i]);
                }

            }

            return counts
                .Where(kv => kv.Value > 1)
                .Where(kv => kv.Key.Size > 1)
                .Select(kv => new CSEOpportunity(kv.Key));
        }
    }
}
