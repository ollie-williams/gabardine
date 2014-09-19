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
using System.Diagnostics;
using System.Linq;

namespace Gabardine.Codegen
{
    class BasicBlock : INode<BasicBlock>
    {
        readonly string name;
        BlockStatement first = null;
        BlockStatement last = null;
        List<BasicBlock> inEdges = new List<BasicBlock>();
        List<BasicBlock> outEdges = new List<BasicBlock>();

        public BasicBlock(string name)
        {
            this.name = name;
        }

        public void InsertAtEnd(BlockStatement bs)
        {
            if (first == null) {
                Debug.Assert(last == null);
                first = bs;
                last = bs;
                return;
            }

            Debug.Assert(last != null);
            last.InsertAfter(bs);
        }
        public void InsertAtEnd(Term statement)
        {
            InsertAtEnd(new BlockStatement(this, statement));
        }

        public void InsertAfterBlockLabel(Term stmt)
        {
            first.InsertAfter(stmt);
        }

        public BlockStatement Last
        {
            get { return last; }
            internal set { last = value; }
        }

        public BlockStatement First
        {
            get { return first; }
            internal set { first = value; }
        }

        public IEnumerable<BlockStatement> Statements
        {
            get
            {
                BlockStatement node = First;
                while (node != Last) {
                    yield return node;
                    node = node.Next;
                }
                yield return Last;
            }
        }

        public int InDegree { get { return inEdges.Count; } }

        public int OutDegree { get { return outEdges.Count; } }

        public string Name { get { return name; } }

        public BasicBlock InEdge(int i)
        {
            return inEdges[i];
        }

        public BasicBlock OutEdge(int i)
        {
            return outEdges[i];
        }

        public void AddInEdge(BasicBlock src)
        {
            inEdges.Add(src);
        }

        public void AddOutEdge(BasicBlock dest)
        {
            outEdges.Add(dest);
        }

        public bool Equals(BasicBlock other)
        {
            return other.name == name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override string ToString()
        {
            return name;
        }
    }

    class BlockStatement
    {
        BasicBlock block;
        Term statement;
        BlockStatement next = null;
        BlockStatement prev = null;

        public BlockStatement(BasicBlock block, Term statement)
        {
            this.block = block;
            this.statement = statement;
        }

        public Term Statement
        {
            get { return statement; }
            set { statement = value; }
        }

        public BasicBlock Block { get { return block; } }

        public BlockStatement Next { get { return next; } }
        public BlockStatement Previous { get { return prev; } }

        public void InsertAfter(BlockStatement stmt)
        {
            if (next != null) {
                next.prev = stmt;
            }
            stmt.prev = this;
            stmt.next = next;
            this.next = stmt;

            if (stmt.Block == block) {
                if (this == block.Last) {
                    block.Last = next;
                }
            }
        }

        public void InsertBefore(BlockStatement stmt)
        {
            if (prev != null) {
                prev.next = stmt;
            }
            stmt.next = this;
            stmt.prev = prev;
            this.prev = stmt;

            if (stmt.Block == block && this == block.First) {
                block.First = stmt;
            }
        }

        internal void Remove()
        {
            if (prev != null) {
                prev.next = this.next;
            }
            if (next != null) {
                next.prev = this.prev;
            }
        }

        public void InsertAfter(Term statement)
        {
            InsertAfter(new BlockStatement(block, statement));
        }

        internal void InsertBefore(Term statement)
        {
            InsertBefore(new BlockStatement(block, statement));
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Block, Statement);
        }
    }

    class ControlFlow
    {
        BasicBlock entry, exit;
        readonly Dictionary<string, BasicBlock> blocks = new Dictionary<string, BasicBlock>();
        Dominator<BasicBlock> postdominator;

        /// <summary>
        /// Is <paramref name="block"/> postdominated by <paramref name="candidate"/>?
        /// </summary>
        public bool PostDominates(BasicBlock block, BasicBlock candidate)
        {
            return postdominator.Dominates(block, candidate);
        }

        public BlockStatement First { get { return entry.First; } }

        public IEnumerable<BlockStatement> LinearizedStatements
        {
            get
            {
                BlockStatement node = First;
                while (node != null) {
                    yield return node;
                    node = node.Next;
                }
            }
        }

        void Split(IEnumerable<Term> program)
        {
            entry = new BasicBlock("entry");
            blocks.Add("entry", entry);

            BasicBlock current = entry;
            foreach (Term t in program) {
                switch (Statements.GetKind(t.Op)) {

                    case Statements.Kind.BasicBlock:
                        string name = GetConst<string>(t[0]);
                        BasicBlock newBlock = new BasicBlock(name);
                        BlockStatement stmt = new BlockStatement(newBlock, t);
                        current.Last.InsertAfter(stmt);
                        current = newBlock;

                        current.InsertAtEnd(stmt);
                        blocks.Add(name, current);
                        break;

                    default:
                        current.InsertAtEnd(t);
                        break;
                }
            }

            exit = current;
            return;
        }

        void ConstructGraph(IEnumerable<Term> program)
        {
            Split(program);
            foreach (BasicBlock bb in blocks.Values) {
                AddEdges(bb);
            }
            Debug.Assert(exit.OutDegree == 0, "Exit node should be a sink.");
            Debug.Assert(entry.InDegree == 0, "Entry node should be a source.");

            postdominator = new Dominator<BasicBlock>(blocks.Values, DominatorDirection.PostDominator);
            postdominator.Compute(exit);

            return;
        }

        void AddEdges(BasicBlock bb)
        {
            foreach (var stmt in bb.Statements) {
                Debug.Assert(stmt.Block == bb);
                Term t = stmt.Statement;
                switch (Statements.GetKind(t.Op)) {
                    case Statements.Kind.Goto:
                        string destName = GetConst<string>(t[0]);
                        AddEdge(bb, destName);
                        break;
                    case Statements.Kind.If:
                        string thenName = GetConst<string>(t[1]);
                        string elseName = GetConst<string>(t[2]);
                        AddEdge(bb, thenName);
                        AddEdge(bb, elseName);
                        break;
                    default:
                        break;
                }
            }
        }

        void AddEdge(BasicBlock src, string destName)
        {
            BasicBlock dest = blocks[destName];
            src.AddOutEdge(dest);
            dest.AddInEdge(src);
        }

        static T GetConst<T>(Term t)
        {
            return (t.Op as Constant<T>).Value;
        }

        public static ControlFlow Create(IEnumerable<Term> program)
        {
            ControlFlow retval = new ControlFlow();
            retval.ConstructGraph(program);
            return retval;
        }

        public BasicBlock ClosestMutualSuccessor(BasicBlock first, BasicBlock last)
        {
            // First is assumed to dominate all blocks matching the predicate
            // Collect all blocks after first from which blocks satisfying the condition can be reached.
            HashSet<BasicBlock> tainted = new HashSet<BasicBlock>();
            tainted.Add(first);

            Queue<BasicBlock> queue = new Queue<BasicBlock>();
            queue.Enqueue(last);

            while (queue.Count > 0) {
                BasicBlock bb = queue.Dequeue();
                if (tainted.Contains(bb)) {
                    continue;
                }

                tainted.Add(bb);
                for (int i = 0; i < bb.InDegree; ++i) {
                    queue.Enqueue(bb.InEdge(i));
                }
            }

            HashSet<BasicBlock> frontier = new HashSet<BasicBlock>();
            foreach (BasicBlock bb in tainted) {
                for (int i = 0; i < bb.OutDegree; ++i) {
                    BasicBlock dst = bb.OutEdge(i);
                    if (!tainted.Contains(dst)) {
                        frontier.Add(dst);
                    }
                }
            }

            BasicBlock succ = frontier.Single();
            Debug.Assert(PostDominates(first, succ), "Expected the result node to postdominate the first.");
            return succ;
        }
    }
}
