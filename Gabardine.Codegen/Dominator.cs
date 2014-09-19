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

namespace Gabardine.Codegen
{
    interface INode<T> : IEquatable<T>
        where T : INode<T>
    {
        int InDegree { get; }
        T InEdge(int i);

        int OutDegree { get; }
        T OutEdge(int i);
    }

    enum DominatorDirection { Dominator, PostDominator }

    class Dominator<T> where T : INode<T>
    {
        readonly DominatorDirection direction;
        readonly IEnumerable<T> graph;
        readonly Dictionary<T, HashSet<T>> sets = new Dictionary<T, HashSet<T>>();

        public Dominator(IEnumerable<T> graph, DominatorDirection direction)
        {
            this.direction = direction;
            this.graph = graph;
            foreach (T n in graph) {
                sets.Add(n, new HashSet<T>());
            }
        }

        public void Compute(T start)
        {
            var notStart = graph.Where(x => !x.Equals(start));

            Dom(start).Add(start);
            foreach (T n in notStart) {
                Dom(n).UnionWith(graph);
            }

            bool changed = true;
            while (changed) {
                changed = false;
                foreach (T n in notStart) {
                    HashSet<T> newSet = new HashSet<T>();
                    if (PredecessorCount(n) > 0) {
                        newSet.UnionWith(Dom(GetPredecessor(n, 0)));
                    }
                    for (int i = 1; i < PredecessorCount(n); ++i) {
                        newSet.IntersectWith(Dom(GetPredecessor(n, i)));
                    }
                    newSet.Add(n);

                    if (newSet.Count != Dom(n).Count) {
                        changed = true;
                        SetDom(n, newSet);
                    }
                }
            }
        }

        /// <summary>
        /// Does <paramref name="query"/> (post)dominate <paramref name="node"/>?
        /// </summary>
        public bool Dominates(T node, T query)
        {
            return sets[node].Contains(query);
        }

        public HashSet<T> Domintors(T node)
        {
            return sets[node];
        }

        int PredecessorCount(T node)
        {
            switch (direction) {
                case DominatorDirection.Dominator:
                    return node.InDegree;
                case DominatorDirection.PostDominator:
                    return node.OutDegree;
            }
            throw new InvalidProgramException();
        }

        T GetPredecessor(T node, int i)
        {
            switch (direction) {
                case DominatorDirection.Dominator:
                    return node.InEdge(i);
                case DominatorDirection.PostDominator:
                    return node.OutEdge(i);
            }
            throw new InvalidProgramException();
        }

        void SetDom(T n, HashSet<T> newSet)
        {
            var set = Dom(n);
            set.Clear();
            set.UnionWith(newSet);
        }

        private HashSet<T> Dom(T n)
        {
            return sets[n];
        }

        
    }
}
