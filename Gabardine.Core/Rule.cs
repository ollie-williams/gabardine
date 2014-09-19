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
    public abstract class Rule<T>
    {
        readonly string name;
        readonly PatternMatcher matcher;
        readonly Condition[] conditions;

        internal Rule(string name, PatternMatcher matcher, IEnumerable<Condition> conditions)
        {
            this.name = name;
            this.matcher = matcher;
            this.conditions = conditions.ToArray();
        }

        public string Name { get { return name; } }
        public Term Pattern { get { return matcher.Pattern; } }
        public PatternMatcher Matcher { get { return matcher; } }
        public Condition[] Conditions { get { return conditions; } }


        public bool Match(Term candidate, RewriteSystem rw)
        {
            if (!matcher.Match(candidate)) {
                return false;
            }

            foreach(var condition in conditions) {
                if (!condition.Test(rw, matcher)) {
                    return false;
                }
            }

            return true;
        }

        public abstract bool TryRewrite(Term candidate, RewriteSystem rw, out T rewrite);
    }



}