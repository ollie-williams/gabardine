/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;

namespace Gabardine
{
    public static class CodeMotion 
    {
        static int tmpCount = 0;

        public static Term MakeLetVariable(Term binding)
        {
            string name = string.Format("tmp{0}", tmpCount++);
            return new LetVariable(name, binding).CreateTerm();
        }
        
        /// <summary>
        ///   Replace the subterm at <pararef name="address"/> in <paramref name="root"/> with a
        ///   trivial let(x, y, x) let expression.
        /// </summary>
        public static Term Spawn(Term root, Address address)
        {
            Term original = address.Get(root);
            Term tmp = MakeLetVariable(original);
            Term decorated = Special.Let[tmp, original, tmp];
            return address.Replace(root, decorated);
        }

        internal static Term MoveLetUp(Term root, ref Address a)
        {
            // Move to the parent
            int childIndex = a[a.Depth - 1];
            a = a.ParentAddress();

            // Get the players in this transformation and sanity check
            Term parent = a.Get(root);
            Term let = parent[childIndex];
            if (let.Op != Special.Let) {
                throw new InvalidOperationException("Address does not point to a let expression.");
            }            
            if (parent.Op == Special.Lambda) {
                throw new InvalidOperationException("Cannot move a let upwards past a lambda expression.");
            }

            // Lets passing
            if (parent.Op == Special.Lambda) {
                return MoveLetDown(root, a);
            }

            // Build the new expression
            Term[] args = new Term[parent.Arity];
            for (int i = 0; i < args.Length; ++i) {
                if (i == childIndex) {
                    args[i] = let[2];
                } else {
                    args[i] = parent[i];
                }
            }             
            Term newlet = Special.Let[let[0], let[1], new Term(parent.Op, args)];

            // Replace the old one
            return a.Replace(root, newlet);
        }

        internal static Term MoveLetDown(Term root, Address address)
        {
            // Get the let to move down
            Term let = address.Get(root);
            if (let.Op != Special.Let) {
                throw new InvalidOperationException("Address does not point to a let expression.");
            }

            // Push lets into the body
            Term body = let[2];
            Term[] args = new Term[body.Arity];
            for (int i = 0; i < args.Length; ++i) {
                args[i] = Special.Let[let[0], let[1], body[i]];
            }
            if (body.Op == Special.Let) {
                args[0] = body[0];
            }
            body = new Term(body.Op, args);
            
            return address.Replace(root, body);
        }

        internal static Term Hoist(Term root, ref Address a)
        {
            // Move to the parent
            const int childIndex = 1;
            a = a.ParentAddress();

            // Get the players in this transformation and sanity check
            Term lambda = a.Get(root);
            Term let = lambda[childIndex];
            if (let.Op != Special.Let) {
                throw new InvalidOperationException("Address does not point to a let expression.");
            }
            if (lambda.Op != Special.Lambda) {                
                throw new InvalidOperationException("Parent should be a lambda.");
            }

            // Check independence
            if (Replacement.Contains(let[1], lambda[0])) {
                throw new InvalidOperationException("Cannot hoist this let expression because its binding is not independent of the lambda variable.");
            }

            // Replace
            Term newlambda = Special.Lambda[lambda[0], let[2]];
            Term newlet = Special.Let[let[0], let[1], newlambda];
            return a.Replace(root, newlet);
        }

        

#if false
        public interface CodeMotionTransformation : Transformation { }

        /// <summary>
        /// Creates a trivial let expression.
        /// </summary>
        class Spawn : CodeMotionTransformation
        {
            readonly Address where;
            readonly Term matched;

            public Term Matched {  get { return matched; } }

            public Term Transform(Term root)
            {
                Term tmp = MakeTmp();
                Term spawned = Special.Let[tmp, matched, tmp];
                return where.Replace(root, spawned);
            }
        }

        class MoveUp : CodeMotionTransformation
        {
            public Term Matched {  get { throw new NotImplementedException(); } }
            public Term Transform(Term root)
            {
                throw new NotImplementedException();
            }
        }

        class MoveDown : CodeMotionTransformation
        {
            public Term Matched {  get { throw new NotImplementedException(); } }
            public Term Transform(Term root)
            {
                throw new NotImplementedException();
            }
        }

        class Hoist : CodeMotionTransformation
        {
            public Term Matched { get { throw new NotImplementedException(); } }
            public Term Transform(Term root)
            {
                throw new NotImplementedException();
            }
        }

        class Fuse : CodeMotionTransformation
        {
            public Term Matched { get { throw new NotImplementedException(); } }
            public Term Transform(Term root)
            {
                throw new NotImplementedException();
            }
        }

        public class CodeMotionTransformer : Transformer<CodeMotionTransformation>
        {
            IEnumerable<Spawn> FindHoistingOpportunities(Term root)
            {

            }

            public IEnumerable<CodeMotionTransformation> FindTransformations(Term root)
            {
                throw new NotImplementedException();
            }
        }
#endif
    }

}
