/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;

namespace Gabardine.Codegen
{
    public static class OutputDirectives
    {
        public static readonly Operator OutParam = new Operator(1, OperatorKind.Function, "out_param");
        public static readonly Operator ReturnValue = new Operator(1, OperatorKind.Function, "return_value");
    }

    public static class Statements
    {
        public enum Kind { TmpAssign, Return, Copy, CopyOut, VoidCall, Free, Store, BasicBlock, If, Goto, Bind, Unbind, Push, Comment };
        public static readonly Operator TmpAssign = new Operator(2, OperatorKind.Function, "assign");
        public static readonly Operator Copy = new Operator(3, OperatorKind.Function, "copy");
        public static readonly Operator CopyOut = new Operator(1, OperatorKind.Function, "copyOut");
        public static readonly Operator Return = new Operator(1, OperatorKind.Function, "return");
        public static readonly Operator VoidCall = new Operator(2, OperatorKind.Function, "call");
        public static readonly Operator Push = new Operator(0, OperatorKind.Function, "push");
        public static readonly Operator Free = new Operator(1, OperatorKind.Function, "free");
        public static readonly Operator Store = new Operator(2, OperatorKind.Function, "store");
        public static readonly Operator BasicBlock = new Operator(1, OperatorKind.Function, "bb");
        public static readonly Operator If = new Operator(3, OperatorKind.Function, "if");
        public static readonly Operator Goto = new Operator(1, OperatorKind.Function, "goto");
        public static readonly Operator Bind = new Operator(2, OperatorKind.Function, "bind");
        public static readonly Operator Unbind = new Operator(1, OperatorKind.Function, "unbind");
        public static readonly Operator Comment = new Operator(1, OperatorKind.Function, "comment");

        public static Kind GetKind(Operator op)
        {
            if (op == TmpAssign) { return Kind.TmpAssign; }
            if (op == Return) { return Kind.Return; }
            if (op == Copy) { return Kind.Copy; }
            if (op == CopyOut) { return Kind.CopyOut; }
            if (op == VoidCall) { return Kind.VoidCall; }
            if (op == Free) { return Kind.Free; }
            if (op == Store) { return Kind.Store; }
            if (op == BasicBlock) { return Kind.BasicBlock; }
            if (op == If) { return Kind.If; }
            if (op == Goto) { return Kind.Goto; }
            if (op == Bind) { return Kind.Bind; }
            if (op == Unbind) { return Kind.Unbind; }
            if (op == Push) { return Kind.Push; }
            if (op == Comment) { return Kind.Comment; }
            throw new ArgumentException("This operator isn't a statement.", "op");
        }
    }

    public static class Instructions
    {
        public enum Kind { Generate, Binary, Call, Alloc, Pop, Cast, Offset, GenerateFail, Local, Load, Struct, Field, Param };
        public static readonly Operator Generate = new Operator(1, OperatorKind.Function, "generate");
        public static readonly Operator GenerateFail = new Operator(1, OperatorKind.Function, "gen_fail");
        public static readonly Operator Binary = new Operator(3, OperatorKind.Function, "binary");
        public static readonly Operator Offset = new Operator(2, OperatorKind.Function, "offset");
        public static readonly Operator Call = new Operator(2, OperatorKind.Function, "call");
        public static readonly Operator Alloc = new Operator(2, OperatorKind.Function, "alloc");
        public static readonly Operator Cast = new Operator(2, OperatorKind.Function, "cast");
        public static readonly Operator Pop = new Operator(0, OperatorKind.Function, "pop");
        public static readonly Operator Local = new Operator(1, OperatorKind.Function, "local");
        public static readonly Operator Load = new Operator(1, OperatorKind.Function, "load");
        public static readonly Operator Struct = new Operator(1, OperatorKind.Function, "struct");
        public static readonly Operator Field = new Operator(2, OperatorKind.Function, "field");
        public static readonly Operator Param = new Operator(1, OperatorKind.Function, "param");

        public static Kind GetKind(Operator op)
        {
            if (op == Generate) { return Kind.Generate; }
            if (op == GenerateFail) { return Kind.GenerateFail; }
            if (op == Binary) { return Kind.Binary; }
            if (op == Offset) { return Kind.Offset; }
            if (op == Call) { return Kind.Call; }
            if (op == Alloc) { return Kind.Alloc; }
            if (op == Pop) { return Kind.Pop; }
            if (op == Cast) { return Kind.Cast; }
            if (op == Local) { return Kind.Local; }
            if (op == Load) { return Kind.Load; }
            if (op == Struct) { return Kind.Struct; }
            if (op == Field) { return Kind.Field; }
            if (op == Param) { return Kind.Param; }
            throw new ArgumentException(string.Format("This operator, {0}, isn't an instruction.", op));
        }
    }

    public static class Arguments
    {
        public enum Kind { Tmp, Const, Literal };
        public static readonly Operator Tmp = new Operator(1, OperatorKind.Function, "tmp");
        public static readonly Operator Const = new Operator(1, OperatorKind.Function, "const");
        public static readonly Operator Literal = new Operator(2, OperatorKind.Function, "literal");

        public static Kind GetKind(Operator op)
        {
            if (op == Tmp) { return Kind.Tmp; }
            if (op == Const) { return Kind.Const; }
            if (op == Literal) { return Kind.Literal; }
            throw new ArgumentException("This operator isn't an argument.", "op");
        }
    }

    public static class TypeInference
    {
        public enum Kind { Static, Typeof };
        public static readonly Operator Static = new Operator(1, OperatorKind.Function, "stype");
        public static readonly Operator Typeof = new Operator(1, OperatorKind.Function, "typeof");

        public static Kind GetKind(Operator op)
        {
            if (op == Static) { return Kind.Static; }
            if (op == Typeof) { return Kind.Typeof; }
            throw new ArgumentException("This operator isn't a type operator.", "op");
        }
    }
}
