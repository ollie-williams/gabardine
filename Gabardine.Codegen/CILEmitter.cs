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
using System.Reflection.Emit;
using Gabardine.Codegen.Dump;

namespace Gabardine.Codegen
{

    static class Dump
    {
        public static void WriteLine(string format, params object[] args)
        {
            System.Diagnostics.Trace.WriteLine(string.Format(format, args));
        }
    }

    interface CILValue : Value
    {
        void PushToStack(ILGenerator gen);
    }

    class CILEmitter : IEmitter<CILValue>
    {
        class Local : CILValue
        {
            readonly LowLevelType type;
            readonly LocalBuilder lb;

            public Local(LowLevelType type, LocalBuilder lb)
            {
                this.type = type;
                this.lb = lb;
            }

            public LowLevelType Type { get { return type; } }

            public void PushToStack(ILGenerator gen)
            {
                WriteLine("ldloc {0}", lb.LocalIndex);
                gen.Emit(OpCodes.Ldloc, lb);
            }
        }

        class Const<T> : CILValue
        {
            readonly LowLevelType type;
            protected readonly T val;

            public Const(LowLevelType type, T val)
            {
                this.type = type;
                this.val = val;
            }

            public LowLevelType Type { get { return type; } }

            public virtual void PushToStack(ILGenerator gen)
            {
                if (typeof(T) == typeof(int)) {
                    WriteLine("ldc_i4 {0}", val.ToString());
                    gen.Emit(OpCodes.Ldc_I4, Convert.ToInt32(val));
                    return;
                }

                throw new NotImplementedException();
            }
        }

        readonly ILGenerator gen;

        public CILEmitter(ILGenerator gen, ForeignLibrary library)
        {
            this.gen = gen;
        }

        CILValue MakeLocal(LowLevelType type)
        {
            LocalBuilder lb = gen.DeclareLocal(type.ToManagedType());
            WriteLine("stloc {0}", lb.LocalIndex);
            gen.Emit(OpCodes.Stloc, lb);
            return new Local(type, lb);
        }

        public void FinalizeFunction(string functionName)
        {
            throw new NotImplementedException();
        }

        public CILValue AddInputParameter(string name, LowLevelType type)
        {
            throw new NotImplementedException();
        }

        public void Return(CILValue arg)
        {
            throw new NotImplementedException();
        }

        public void Copy(CILValue dst, CILValue src, CILValue count)
        {
            throw new NotImplementedException();
        }

        public void Store(CILValue dst, CILValue src)
        {
            throw new NotImplementedException();
        }

        public void CallVoidFunction(string name, IEnumerable<CILValue> args)
        {
            throw new NotImplementedException();
        }

        public void FreeArray(CILValue buf)
        {
            throw new NotImplementedException();
        }

        public void StartBasicBlock(string v)
        {
            throw new NotImplementedException();
        }

        public void ConditionalJump(CILValue cond, string thenBranch, string elseBranch)
        {
            throw new NotImplementedException();
        }

        public void Jump(string v)
        {
            throw new NotImplementedException();
        }

        public void Comment(string v)
        {
            throw new NotImplementedException();
        }

        public CILValue Binary(BinaryOp op, CILValue left, CILValue right)
        {
            throw new NotImplementedException();
        }

        public CILValue Offset(CILValue _base, CILValue offset)
        {
            throw new NotImplementedException();
        }

        public CILValue CallFunction(string name, IEnumerable<CILValue> args)
        {
            throw new NotImplementedException();
        }

        public CILValue AllocateByteArray(LowLevelType type, CILValue size)
        {
            throw new NotImplementedException();
        }

        public CILValue Cast(CILValue src, LowLevelType type)
        {
            throw new NotImplementedException();
        }

        public CILValue DeclareLocal(LowLevelType lowLevelType)
        {
            throw new NotImplementedException();
        }

        public CILValue Load(CILValue ptr)
        {
            throw new NotImplementedException();
        }

        public CILValue EmitStruct(IEnumerable<CILValue> fields)
        {
            throw new NotImplementedException();
        }

        public CILValue ExtractField(int index, CILValue arg)
        {
            throw new NotImplementedException();
        }

        public CILValue Constant<T>(T cnst, LowLevelType type)
        {
            throw new NotImplementedException();
        }
#if false
        public override CILValue AddParameter(Term parameter, LowLevelType type)
        {
            throw new NotImplementedException();
        }

        protected override CILValue AllocateByteArray(LowLevelType type, CILValue size)
        {
            throw new NotImplementedException();
        }

        protected override CILValue Binary(BinaryOp op, CILValue value1, CILValue value2)
        {
            if (value1.Type != value2.Type) {
                throw new Exception("Types should match.");
            }

            value1.PushToStack(gen);
            value2.PushToStack(gen);

            switch (op) {
                case BinaryOp.Add:
                    WriteLine("add");
                    gen.Emit(OpCodes.Add);
                    break;
                case BinaryOp.Mul:
                    WriteLine("mul");
                    gen.Emit(OpCodes.Mul);
                    break;
                case BinaryOp.Sub:
                    WriteLine("sub");
                    gen.Emit(OpCodes.Sub);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return MakeLocal(value1.Type);
        }

        protected override CILValue CallFunction(string functionName, IEnumerable<CILValue> args)
        {
            throw new NotImplementedException();
        }

        protected override void CallVoidFunction(string functionName, IEnumerable<CILValue> args)
        {
            throw new NotImplementedException();
        }

        protected override CILValue Cast(CILValue src, LowLevelType targetType)
        {
            throw new NotImplementedException();
        }

        protected override void Comment(string cmt)
        {
            throw new NotImplementedException();
        }

        protected override void ConditionalJump(CILValue condition, string thenBranch, string elseBranch)
        {
            throw new NotImplementedException();
        }

        protected override CILValue Constant<T>(T value, LowLevelType type)
        {
            return new Const<T>(LowLevelType.Integer(32, false), value);
        }

        protected override void Copy(CILValue dst, CILValue src, CILValue count)
        {
            throw new NotImplementedException();
        }

        protected override CILValue DeclareLocal(LowLevelType type)
        {
            throw new NotImplementedException();
        }

        protected override void EmitDefinitionEnd()
        {
        }

        protected override void EmitDefinitionStart(string functionName, LowLevelType returnType, IEnumerable<FunctionParameter> prms)
        {
            System.Diagnostics.Trace.WriteLine("Ugly... implement better pattern for definitions.");
        }

        protected override CILValue EmitStruct(IEnumerable<CILValue> fields)
        {
            throw new NotImplementedException();
        }

        protected override CILValue ExtractField(int index, CILValue arg)
        {
            throw new NotImplementedException();
        }

        protected override void FreeArray(CILValue ptr)
        {
            throw new NotImplementedException();
        }

        protected override void Jump(string target)
        {
            throw new NotImplementedException();
        }

        protected override CILValue Load(CILValue src)
        {
            throw new NotImplementedException();
        }

        protected override CILValue Offset(CILValue _base, CILValue offset)
        {
            throw new NotImplementedException();
        }

        protected override void Reset()
        {
        }

        protected override void Return(CILValue value)
        {
            value.PushToStack(gen);
            WriteLine("ret");
            gen.Emit(OpCodes.Ret);
        }

        protected override void StartBasicBlock(string name)
        {
            throw new NotImplementedException();
        }

        protected override void Store(CILValue dst, CILValue src)
        {
            throw new NotImplementedException();
        }
#endif
    }
}
