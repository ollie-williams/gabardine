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
    interface BreakoutHandler
    {
        //public abstract Term VisitConstant<T>(T value, Term query);
        Term Handle(Term query);
    }

    class TypeofHandler: BreakoutHandler, ConstantVisitor<Term>
    {
        readonly Dictionary<Type, Term> builtin = new Dictionary<Type, Term>();

        public TypeofHandler()
        {
            builtin.Add(typeof(double), Special.Real);
            builtin.Add(typeof(int), Special.Integer);
        }

        public Term Handle(Term query)
        {
            if (query.Op != Special.Typeof) {
                throw new ArgumentException("query");
            }

            if (query[0].Op.IsConstant) {
                return query[0].Op.ConstantVisit(this);
            }

            return Special.FailWith[ConsUtils.Cons(Term.Const("No type inference available for non-constant term"), query[0])];
        }

        public Term VisitConstant<T>(T value)
        {
            if (builtin.TryGetValue(typeof(T), out Term result)) {
                return result;
            }
            string message = string.Format("No type inference available for native type {0}", typeof(T));
            return Special.FailWith[Term.Const(message)];
        }
    }

    class IsConstHandler: BreakoutHandler
    {
        public Term Handle(Term query)
        {
            if (query.Op != Special.IsConst) {
                throw new ArgumentException("query");
            }
            return query[0].Op.IsConstant ? Special.True : Special.False;
        }
    }

    static class BreakoutDispatcher
    {
        static readonly Dictionary<Operator, BreakoutHandler> handlers = new Dictionary<Operator, BreakoutHandler>();

        static BreakoutDispatcher()
        {
            handlers.Add(Special.Typeof, new TypeofHandler());
            handlers.Add(Special.IsConst, new IsConstHandler());
        }

        public static Term Handle(Term query)
        {
            if (handlers.TryGetValue(query.Op, out BreakoutHandler handler)) {
                return handler.Handle(query);
            }
            return Special.FailWith[ConsUtils.Cons(Term.Const("No breakout handler for query "), 
                                                   query)];
        }
    }
}
