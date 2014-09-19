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
    public class RewriteRule : Rule<Term>
    {
        readonly int id;
        readonly int priority;
        readonly Term target;
        readonly TargetBuilder targetBuilder;
        readonly bool isDefault;

        internal RewriteRule(string name, int id,
                             Term target,
                             PatternMatcher patternMatcher,
                             IEnumerable<Condition> conditions,
                             TargetBuilder targetBuilder,
                             int priority,
                             bool isDefault)
            : base(name, patternMatcher, conditions)
        {
            this.id = id;
            this.priority = priority;
            this.target = target;
            this.targetBuilder = targetBuilder;
            this.isDefault = isDefault;
        }

        public int Id { get { return id; } }
        public int Priority { get { return priority; } }
        public Term Target { get { return target; } }
        public bool IsDefault { get { return isDefault; } }

        /// <summary>
        /// We assume that <paramref name="candidate"/> has a shape that corresponds to 
        /// the lhs. The only reason rewriting might fail is
        /// if there are two different subterms of <paramref name="candidate"/> which are
        /// destined for the same variable. 
        /// </summary>
        /// <param name="candidate">A term with shape matching that of the lhs.</param>
        /// <returns><paramref name="candidate"/> rewritten according to the rule.</returns>
        public override bool TryRewrite(Term candidate, RewriteSystem rw, out Term rewrite)
        {
            if (!Match(candidate, rw)) {
                rewrite = null;
                return false;
            }
            rewrite = targetBuilder.Build(Matcher.VariableBindings);
            return true;
        }

        /// <summary>
        /// Gets the rule composed with both sides joined by the Special.MapsTo (->) operator, 
        /// and the conditions prepended with Special.Implies (=>)
        /// </summary>
        public Term Composed()
        {
            Term expr = Special.MapsTo[Pattern, Target];
            for(int i = Conditions.Length -1; i >= 0; --i) {
                expr = Special.Implies[Conditions[i].Composed(), expr];
            }
            return expr;
        }

        public override string ToString()
        {
            return Composed().ToString();
        }
    }
}

