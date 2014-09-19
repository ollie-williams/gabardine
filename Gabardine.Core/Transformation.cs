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

namespace Gabardine
{
    /// <summary>
    ///   A single-step transformation of a term.
    /// </summary>
    public interface Transformation
    {
        Term Matched(Term root);
        Term Transform(Term root);
    }

    /// <summary>
    ///   A sequence of steps.
    /// </summary>
    public interface Tendril
    {
        IEnumerable<Term> Expand(Term root);
    }

    /// <summary>
    ///   Wrapper for single-step tendrils.
    /// </summary>
    public abstract class OneStepTendril : Tendril, Transformation
    {
        public IEnumerable<Term> Expand(Term root)
        {
            yield return this.Transform(root);
        }
        
        public abstract Term Matched(Term root);
        public abstract Term Transform(Term root);           
    }
}
