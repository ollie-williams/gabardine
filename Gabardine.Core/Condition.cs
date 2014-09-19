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
    public class ConditionPattern
    {
        readonly Term lhs, rhs;

        public ConditionPattern(Term expression)
        {
            this.lhs = expression;
            this.rhs = Special.True;
        }

        public Condition Create(PatternMatcher pm)
        {
            TargetBuilder tb = RewriteRuleFactory.CreateTargetBuilder(pm, lhs);
            return new Condition(lhs, tb, rhs);
        }
    }

    public class Condition
    {
        readonly Term pattern;
        readonly TargetBuilder lhs;
        readonly Term rhs;

        internal Condition(Term pattern, TargetBuilder lhs, Term rhs)
        {
            this.pattern = pattern;
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public bool Test(RewriteSystem rw, PatternMatcher pm)
        {
            Term start = lhs.Build(pm.VariableBindings);
            Term end = rw.RewriteUnordered(start);
            return end == rhs;
        }

        public Term Composed()
        {
            return pattern;
        }

        public override string ToString()
        {
            return Composed().ToString();
        }
    }
}
