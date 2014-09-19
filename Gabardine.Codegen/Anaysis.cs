/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Gabardine.Codegen
{
    static class Analysis
    {
        class StackInfo
        {
            public Dictionary<Term, Term> dict = new Dictionary<Term, Term>();
            public Dictionary<string, string> basic_blocks = new Dictionary<string, string>();
            public Term retval = null;

            public string GetOrAddBasicBlockName(string old_name, ref int bbCount)
            {
                string new_name;
                if (!basic_blocks.TryGetValue(old_name, out new_name)) {
                    new_name = string.Format("{0}{1}", old_name, bbCount++);
                    basic_blocks.Add(old_name, new_name);
                }
                return new_name;
            }
        }

        public static IEnumerable<Term> Full(IEnumerable<Term> program)
        {
            program = RemoveStacks(program);
            program = RemoveBindings(program);
            //program = RemoveAliases(program);
            //program = RemoveStructs(program);
            program = RemoveAliases(program);

            ControlFlow cf = ControlFlow.Create(program);

            OptimizeAllocations(cf);

            program = cf.LinearizedStatements.Select(x => x.Statement);
            program = RemoveAliases(program);
            return program;
        }

        static Term Replace(Term term, Dictionary<Term, Term> dict)
        {
            foreach (var kv in dict) {
                Term old_term = kv.Key;
                Term new_term = kv.Value;
                term = Replacement.Replace(term, old_term, new_term);
            }
            return term;
        }

        static IEnumerable<Term> RemoveStacks(IEnumerable<Term> nested)
        {
            int count = 0;
            int bbCount = 0;
            var stack = new Stack<StackInfo>();
            stack.Push(new StackInfo());

            foreach (Term s in nested) {

                Term t = s;
                if (stack.Count > 0) {
                    t = Replace(t, stack.Peek().dict);
                }

                switch (Statements.GetKind(t.Op)) {
                    case Statements.Kind.Push:
                        stack.Push(new StackInfo());
                        break;

                    case Statements.Kind.TmpAssign:
                        {
                            Term old_name = t[0];
                            string new_name = string.Format("%{0}", count++);

                            stack.Peek().dict.Add(Arguments.Tmp[old_name], Arguments.Tmp[Term.Const(new_name)]);

                            Term rhs = t[1];
                            if (rhs == Instructions.Pop) {
                                rhs = stack.Peek().retval;
                                stack.Peek().retval = null;
                                Debug.Assert(!ReferenceEquals(rhs, null), "Expected a return value to be available to pop.");
                            }
                            yield return Statements.TmpAssign[Term.Const(new_name), rhs];
                        }
                        break;

                    case Statements.Kind.Return:
                        stack.Pop();
                        Debug.Assert(stack.Count > 0, "Expected a stack entry to remain.");
                        stack.Peek().retval = t[0];
                        break;

                    case Statements.Kind.BasicBlock:
                        {
                            string old_name = (t[0].Op as Constant<string>).Value;
                            string new_name = stack.Peek().GetOrAddBasicBlockName(old_name, ref bbCount);
                            yield return Statements.BasicBlock[Term.Const(new_name)];
                        }
                        break;

                    case Statements.Kind.Goto:
                        {
                            string old_name = (t[0].Op as Constant<string>).Value;
                            string new_name = stack.Peek().GetOrAddBasicBlockName(old_name, ref bbCount);
                            yield return Statements.Goto[Term.Const(new_name)];
                        }
                        break;

                    case Statements.Kind.If:
                        {
                            string then_name = (t[1].Op as Constant<string>).Value;
                            string else_name = (t[2].Op as Constant<string>).Value;

                            then_name = stack.Peek().GetOrAddBasicBlockName(then_name, ref bbCount);
                            else_name = stack.Peek().GetOrAddBasicBlockName(else_name, ref bbCount);
                            yield return Statements.If[t[0], Term.Const(then_name), Term.Const(else_name)];
                        }
                        break;

                    default:
                        yield return t;
                        break;
                }
            }

            // Is there a dangling return we actually want to keep?
            if (!ReferenceEquals(stack.Peek().retval, null)) {
                yield return Statements.Return[stack.Peek().retval];
            }

            yield break;
        }

        static IEnumerable<Term> RemoveStructs(IEnumerable<Term> program)
        {
            Dictionary<Term, Term> mapping = new Dictionary<Term, Term>();

            foreach (Term s in program) {

                Term step = Replace(s, mapping);

                if (step.Op != Statements.TmpAssign) {
                    yield return step;
                    continue;
                }

                Term rhs = step[1];
                if (rhs.Op != Instructions.Struct) {
                    yield return step;
                    continue;
                }

                Term lhs = step[0];
                string baseName = ((Constant<string>)lhs.Op).Value;

                int i = 0;
                foreach (Term cpt in ConsUtils.UnconsMany(rhs[0])) {
                    string elementName = string.Format("{0}.{1}", baseName, i);
                    yield return Statements.TmpAssign[Term.Const(elementName), cpt];

                    Term lookup = Instructions.Field[Term.Const(i), Arguments.Tmp[Term.Const(baseName)]];
                    Term direct = Arguments.Tmp[Term.Const(elementName)];
                    mapping.Add(lookup, direct);
                    ++i;
                }
            }
            yield break;
        }

        static IEnumerable<Term> RemoveBindings(IEnumerable<Term> program)
        {
            Dictionary<Term, Term> bindings = new Dictionary<Term, Term>();

            foreach (Term s in program) {
                Term step = Replace(s, bindings);

                if (step.Op == Statements.Bind) {
                    Term arg = step[1];
                    Term param = Instructions.Param[step[0]];
                    bindings.Add(param, arg);
                    continue;
                }

                if (step.Op == Statements.Unbind) {
                    Term param = Instructions.Param[step[0]];
                    bindings.Remove(param);
                    continue;
                }

                yield return step;
            }

            if (bindings.Count > 0) {
                throw new System.InvalidProgramException("There should not be any dangling bindings.");
            }
            yield break;
        }

        static Term t = new Operator(0, OperatorKind.PatternVariable, "t");
        static Term r = new Operator(0, OperatorKind.PatternVariable, "r");
        static Term ty = new Operator(0, OperatorKind.PatternVariable, "ty");
        static RewriteRule[] aliasRules = new RewriteRule[] {
                RewriteRuleFactory.Create(Statements.TmpAssign[t, Arguments.Tmp[r]], Arguments.Tmp[r]),
                RewriteRuleFactory.Create(Statements.TmpAssign[t, Arguments.Const[r]], Arguments.Const[r]),
                RewriteRuleFactory.Create(Statements.TmpAssign[t, Arguments.Literal[r,ty]], Arguments.Literal[r,ty]),
            };

        static IEnumerable<Term> RemoveAliases(IEnumerable<Term> program)
        {
            Dictionary<Term, Term> aliases = new Dictionary<Term, Term>();
            foreach (Term s in program) {
                Term step = Replace(s, aliases);

                bool matched = false;
                for (int i = 0; i < aliasRules.Length && !matched; ++i) {
                    if (aliasRules[i].TryRewrite(step, null, out Term original)) {
                        Term alias = aliasRules[i].Matcher.GetBinding(t);
                        aliases.Add(Arguments.Tmp[alias], original);
                        matched = true;
                    }
                }
                if (!matched) {
                    yield return step;
                }
            }
        }

        static PatternMatcher alloc_pattern = PatternMatcherFactory.Create(Statements.TmpAssign[t, Instructions.Alloc[Special.Wildcard, Special.Wildcard]]);

        static void OptimizeAllocations(ControlFlow cf)
        {
            var node = cf.First;
            while (node != null) {

                if (alloc_pattern.Match(node.Statement)) {
                    node = OptimizeAllocation(cf, node);
                }
                else {
                    node = node.Next;
                }
            }

            //CheckForLeaks(prog);
            //return prog;
        }

#if false
        static bool CheckForLeaks(LinkedList<Term> program)
        {
            var node = program.First;
            while (node != null) {

                if (alloc_pattern.Match(node.Value)) {
                    Term tmp = alloc_pattern.GetBinding(t);
                    var dependents = FindDependents(node, tmp).ToArray();
                    var last = dependents.Last();
                    if (last.Value.Op != Statements.Free) {
                        throw new InvalidProgramException(string.Format("There is no free statement to go with: {0}", node.Value));
                    }
                }

                node = node.Next;
            }
            return true;
        }
#endif

        static PatternMatcher copy_pattern = PatternMatcherFactory.Create(Statements.Copy[r, t, Special.Wildcard]);

        private static BlockStatement OptimizeAllocation(ControlFlow cf, BlockStatement first)
        {
            Term tmp = alloc_pattern.GetBinding(t);
            var dependents = FindDependents(first, tmp);
            var last = dependents.Last();

#if true
            // If the last use of our allocated buffer is to copy it into another one,
            // we know that the target is writable and can just be used in place of
            // what we allocated.
            if (copy_pattern.Match(last.Statement)) {
                return RemoveCopy(first, last);
            }
#endif

            BasicBlock allocBlock = first.Block;
            BasicBlock lastBlock = last.Block;
            Term freeStmt = Statements.Free[Arguments.Tmp[tmp]];

            if (cf.PostDominates(allocBlock, lastBlock)) {
                last.InsertAfter(freeStmt);
            }
            else {
                // Need to find a basic block which postdominates both the allocation, and the last usage.
                BasicBlock freeBlock = cf.ClosestMutualSuccessor(allocBlock, lastBlock);
                freeBlock.InsertAfterBlockLabel(freeStmt);
            }

            return first.Next;
        }

        private static BlockStatement RemoveCopy(BlockStatement first, BlockStatement last)
        {
            // Get everything required to compute the destination
            Term dst = copy_pattern.GetBinding(r);
            var requirements = Requirements(last, first, dst).ToArray();

            // Move these to before the allocation
            var insert = first;
            foreach (var node in requirements) {
                node.Remove();
                insert.InsertBefore(node.Statement);
                insert = insert.Previous;
            }

            // Replace the allocation with an alias
            Debug.Assert(first.Statement.Op == Statements.TmpAssign, "Expected a temporary assignment.");
            first.Statement = Statements.TmpAssign[first.Statement[0], dst];

            // Remove the copy
            last.Remove();

            // Return the new location corresponding to where first was in the program.
            return insert;
        }

        static IEnumerable<BlockStatement> Requirements(BlockStatement last, BlockStatement first, Term search)
        {
            HashSet<Term> searches = new HashSet<Term>();
            searches.Add(search[0]);

            while (last != first) {
                Term stmt = last.Statement;
                if (stmt.Op != Statements.TmpAssign) {
                    last = last.Previous;
                    continue;
                }

                if (!searches.Contains(stmt[0])) {
                    last = last.Previous;
                    continue;
                }

                yield return last;
                searches.Remove(stmt[0]);
                foreach (var arg in GetArguments(stmt[1])) {
                    searches.Add(arg);
                }

                last = last.Previous;
            }
            yield break;
        }

        static IEnumerable<Term> GetArguments(Term assignmentRhs)
        {
            return Replacement.Find(assignmentRhs, t => t.Op == Arguments.Tmp).Select(t => t[0]);
        }

        static IEnumerable<BlockStatement> FindDependents(BlockStatement node, Term searchTerm)
        {
            HashSet<Term> dependents = new HashSet<Term>();
            dependents.Add(searchTerm);

            while (node != null) {

                Term statement = node.Statement;

                if (!Replacement.ContainsAny(statement, dependents)) {
                    node = node.Next;
                    continue;
                }

                // Term is referenced somewhere, so this is the last reference seen so far.
                yield return node;

                // Look for taint propagation via a store instruction
                if (statement.Op == Statements.Store) {
                    dependents.Add(statement[0]);
                    node = node.Next;
                    continue;
                }

                // Look for taint propagation via assignment to a temporary
                if (statement.Op != Statements.TmpAssign) {
                    node = node.Next;
                    continue;
                }

                Term rhs = statement[1];

                // Check for an alias
                if (rhs.Op == Arguments.Tmp) {
                    dependents.Add(statement[0]);
                    node = node.Next;
                    continue;
                }

                switch (Instructions.GetKind(rhs.Op)) {
                    // Call instructions promise not to capture a pointer.
                    case Instructions.Kind.Call:
                        break;

                    // Everything else is tainted.
                    default:
                        dependents.Add(statement[0]);
                        break;
                }
                node = node.Next;
            }
        }
    }


}
