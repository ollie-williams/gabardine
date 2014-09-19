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
using System.Text;

namespace Gabardine.Codegen
{
    public class llValue : Value
    {
        readonly LowLevelType type;
        readonly string text;

        public llValue(LowLevelType type, string text)
        {
            this.type = type;
            this.text = text;
        }

        public LowLevelType Type { get { return type; } }

        public override string ToString()
        {
            return text;
        }
    }

    public static class LLVMFactory
    {
        public static IEmitter<llValue> Create(ForeignLibrary library)
        {
            return new LLVMEmitter(library);
        }
    }

    static class BuilderExtension
    {
        public static StringBuilder AppendType(this StringBuilder builder, LowLevelType type)
        {
            switch (type.Kind) {
                case LowLevelType.TypeKind.Struct:
                    {
                        Struct st = type as Struct;

                        builder.Append('{');
                        for (int i = 0; i < st.Length; ++i) {
                            if (i > 0) builder.Append(", ");
                            builder.AppendType(st[i]);
                        }
                        builder.Append('}');
                    }
                    break;
                default:
                    builder.Append(type.ToString().TrimStart('u'));
                    break;
            }
            return builder;
        }
    }

    class LLVMEmitter : IEmitter<llValue>
    {
        readonly ForeignLibrary library;

        readonly StringBuilder module = new StringBuilder();
        HashSet<string> declared = new HashSet<string>();

        readonly StringBuilder body = new StringBuilder();
        int var_count = 1;
        readonly List<llValue> inputParams = new List<llValue>();
        readonly List<llValue> outputParams = new List<llValue>();
        LowLevelType returnType = LowLevelType.Void;

        int malloc_counter = 0;

        public LLVMEmitter(ForeignLibrary library)
        {
            this.library = library;
        }

        public override string ToString()
        {
            return module.ToString();
        }

        public void FinalizeFunction(string functionName)
        {
            module.Append("define ")
                  .AppendType(returnType)
                  .Append(" @")
                  .Append(functionName)
                  .Append('(');

            for (int i = 0; i < inputParams.Count + outputParams.Count; ++i) {

                llValue prm = null;
                if (i >= inputParams.Count) {
                    prm = outputParams[i - inputParams.Count];
                }
                else {
                    prm = inputParams[i];
                }

                if (i > 0) {
                    module.Append(", ");
                }
                module.AppendType(prm.Type)
                      .Append(' ')
                      .Append(prm.ToString());

                if (prm.Type.Kind == LowLevelType.TypeKind.Pointer) {
                    module.Append("nocapture ");
                    if (i < inputParams.Count) {
                        module.Append("readonly ");
                    }
                }
            }

            module.Append(')').AppendLine().AppendLine("{");
            module.Append(body.ToString());
            if (returnType == LowLevelType.Void) {
                module.AppendLine("ret void");
            }
            module.AppendLine("}");

            Reset();
        }

        /// <summary>
        /// Erases all function-level state before emission of a new function.
        /// </summary>
        void Reset()
        {
            body.Clear();
            inputParams.Clear();
            outputParams.Clear();
            returnType = LowLevelType.Void;
            var_count = 1;
        }

        public llValue AddInputParameter(string name, LowLevelType type)
        {
            llValue value = new llValue(type, "%" + name);
            inputParams.Add(value);
            return value;
        }

        public void Return(llValue arg)
        {
            returnType = arg.Type;
            body.Append("ret ")
                .AppendType(arg.Type)
                .Append(' ')
                .Append(arg)
                .AppendLine();
        }

        #region Function calls

        LowLevelType CallBody(ForeignFunction func, IEnumerable<llValue> args)
        {
            Declare(func);
            body.Append("call ")
                .AppendType(func.ReturnType)
                .Append(" @")
                .Append(func.Name)
                .Append('(');

            int i = 0;
            foreach (var arg in args) {
                body.AppendType(arg.Type)
                    .Append(' ')
                    .Append(arg.ToString());

                if (++i < func.Arity) {
                    body.Append(", ");
                }
            }
            body.AppendLine(")");

            return func.ReturnType;
        }

        void Declare(ForeignFunction func)
        {
            if (declared.Contains(func.Name)) {
                return;
            }

            module.Append("declare ");
            module.AppendType(func.ReturnType)
                        .Append(" @")
                        .Append(func.Name)
                        .Append('(');

            if (func.ArgTypes.Length > 0) {
                module.AppendType(func.ArgTypes[0]);
            }
            for (int i = 1; i < func.ArgTypes.Length; ++i) {
                module.Append(", ");
                module.AppendType(func.ArgTypes[i]);
            }
            module.Append(')').AppendLine();

            declared.Add(func.Name);
        }

        public void CallVoidFunction(string functionName, IEnumerable<llValue> args)
        {
            ForeignFunction func = library.GetFunction(functionName);

            LowLevelType rettype = CallBody(func, args);

            // LLVM is very particular about temporary numbering, and will count one extra if we 
            // have a return, even if we choose not to use it.
            if (rettype != LowLevelType.Void) {
                ++var_count;
            }
        }

        public llValue CallFunction(string functionName, IEnumerable<llValue> args)
        {
            ForeignFunction func = library.GetFunction(functionName);
            string var_name = string.Format("%{0}", var_count++);
            body.Append(var_name).Append(" = ");
            var type = CallBody(func, args);
            return new llValue(type, var_name);
        }

        llValue CallFunction(string functionName, params llValue[] args)
        {
            return CallFunction(functionName, (IEnumerable<llValue>)args);
        }

        #endregion

        #region Memory/storage management

        public void Copy(llValue dst, llValue src, llValue count)
        {
            throw new NotImplementedException("Don't implement until sizes etc. have been given some more serious thought.");
        }

        public llValue DeclareLocal(LowLevelType lowLevelType)
        {
            throw new NotImplementedException();
        }

        public llValue Load(llValue ptr)
        {
            throw new NotImplementedException();
        }

        public void Store(llValue dst, llValue src)
        {
            LowLevelType dstT = dst.Type;
            if (!dstT.IsPointerType) {
                throw new ArgumentException("Destination must be a pointer.", "dst");
            }

            // TODO: I don't like this automatic coercion
            //src = Cast(src, dstT.ElementType);
            if (dstT.ElementType != src.Type) {
                throw new InvalidProgramException();
            }

            body.Append("store ")
                .AppendType(src.Type)
                .Append(' ')
                .Append(src)
                .Append(", ")
                .AppendType(dstT)
                .Append(' ')
                .Append(dst)
                .AppendLine();
        }

        public llValue AllocateByteArray(LowLevelType type, llValue size)
        {
            if (!type.IsPointerType) {
                throw new ArgumentException("Allocation type must be a pointer.", "type");
            }

            llValue sizeInBytes = Binary(BinaryOp.Mul, size, Constant(type.ElementType.SizeInBytes, LowLevelType.NativeInteger));
            llValue malloc_id = this.Constant(malloc_counter++, LowLevelType.Integer(32, true));
            llValue ptr = CallFunction("malloc_leak", sizeInBytes, malloc_id);
            //Scalar ptr = CallFunction("malloc", sizeInBytes);
            return Cast(ptr, type);
        }

        public void FreeArray(llValue buf)
        {
            throw new NotImplementedException();
        }

        public llValue Offset(llValue _base, llValue offset)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Control flow

        public void StartBasicBlock(string v)
        {
            throw new NotImplementedException();
        }

        public void ConditionalJump(llValue cond, string thenBranch, string elseBranch)
        {
            throw new NotImplementedException();
        }

        public void Jump(string v)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Aggregrates

        public llValue EmitStruct(IEnumerable<llValue> fields)
        {
            throw new NotImplementedException();
        }

        public llValue ExtractField(int index, llValue arg)
        {
            throw new NotImplementedException();
        }

        #endregion

        public llValue Binary(BinaryOp op, llValue left, llValue right)
        {
            if (left.Type.Kind != right.Type.Kind) {
                throw new ArgumentException("Types of Scalars should be identical.");
            }

            switch (left.Type.Kind) {
                case LowLevelType.TypeKind.Float64:
                case LowLevelType.TypeKind.Integer:
                    break;
                default:
                    throw new ArgumentException("Cannot perform arithmetic on type " + left.Type);
            }

            string op_name = string.Empty;
            switch (op) {
                case BinaryOp.Add:
                    op_name = "{0}add ";
                    break;
                case BinaryOp.Sub:
                    op_name = "{0}sub ";
                    break;
                case BinaryOp.Mul:
                    op_name = "{0}mul ";
                    break;
                case BinaryOp.Div:
                    op_name = "{3}div ";
                    break;
                case BinaryOp.LT:
                    op_name = "{1}cmp {2}lt ";
                    break;
                case BinaryOp.GT:
                    op_name = "{1}cmp {2}gt ";
                    break;
                default:
                    throw new NotImplementedException();
            }

            string prefix = string.Empty;
            char fullprefix = '?';
            if (left.Type == LowLevelType.Float64) {
                prefix = "f";
                fullprefix = 'f';
            }

            char typecode = left.Type.Kind == LowLevelType.TypeKind.Integer ? 'i' : 'f';
            char signed = 'o';
            if (left.Type.Kind == LowLevelType.TypeKind.Integer) {
                signed = ((Integer)left.Type).Signed ? 's' : 'u';
                fullprefix = signed;
            }

            op_name = string.Format(op_name, prefix, typecode, signed, fullprefix);

            string var_name = string.Format("%{0}", var_count++);
            body.Append(var_name)
                .Append(" = ")
                .Append(op_name)
                .AppendType(left.Type)
                .Append(' ')
                .Append(left.ToString())
                .Append(", ")
                .AppendLine(right.ToString());

            return new llValue(left.Type, var_name);
        }

        public llValue Cast(llValue src, LowLevelType targetType)
        {
            if (TryCast(src, targetType, out llValue result)) {
                return result;
            }
            throw new InvalidCastException();
        }

        bool TryCast(llValue src, LowLevelType targetType, out llValue result)
        {
            result = null;

            if (src.Type == targetType) {
                result = src;
                return true;
            }

            LowLevelType st = src.Type;
            var srcKind = st.Kind;
            var dstKind = targetType.Kind;

            string op = string.Empty;

            if (srcKind == LowLevelType.TypeKind.Pointer && dstKind == LowLevelType.TypeKind.Pointer) {
                op = "bitcast ";
            }
            else if (srcKind == LowLevelType.TypeKind.Integer && dstKind == LowLevelType.TypeKind.Integer) {
                Integer si = st as Integer;
                Integer di = targetType as Integer;

                // LLVM doesn't care about signed/unsigned, so we can pass this through
                if (si.Bits == di.Bits) {
                    result = new llValue(targetType, src.ToString());
                    return true;
                }

                op = si.Bits > di.Bits ? "trunc " : "zext ";
            }
            else {
                return false;
            }

            string var_name = string.Format("%{0}", var_count++);
            body.Append(var_name)
                .Append(" = ")
                .Append(op)
                .AppendType(src.Type)
                .Append(' ')
                .Append(src)
                .Append(" to ")
                .AppendType(targetType)
                .AppendLine();

            result = new llValue(targetType, var_name);
            return true;
        }

        public llValue Constant<T>(T cnst, LowLevelType type)
        {
            if (type == LowLevelType.Float64) {
                return new llValue(type, string.Format("{0:e}", cnst));
            }
            return new llValue(type, cnst.ToString());
        }

        public void Comment(string v)
        {
            throw new NotImplementedException();
        }



#if false

        protected override void EmitDefinitionStart(string functionName, LowLevelType returnType, IEnumerable<FunctionParameter> prms)
        {
            builder
                .Append("define ")
                .AppendType(returnType)
                .Append(" @")
                .Append(functionName)
                .Append('(');

            var it = prms.GetEnumerator();
            bool first = true;
            while (it.MoveNext()) {
                Scalar val = AddParameter(it.Current.Expr, it.Current.Type);
                if (!first) {
                    builder.Append(", ");
                }

                builder.AppendType(it.Current.Type).Append(' ');
                if (it.Current.Type.Kind == LowLevelType.TypeKind.Pointer) {
                    builder.Append("nocapture ");
                    if (!it.Current.IsOutput) {
                        builder.Append("readonly ");
                    }
                }
                builder.Append(val);

                first = false;
            }
            builder.Append(')').AppendLine().AppendLine("{");
        }

        protected override void EmitDefinitionEnd()
        {
            if (!returnIssued) {
                builder.AppendLine("ret void");
            }
            builder.AppendLine("}");
        }

        public override Scalar AddParameter(Term parameter, LowLevelType type)
        {
            Scalar Scalar = new Scalar(type, "%" + parameter.Op.ToString());
            parameters.Add(parameter.Op, Scalar);
            return Scalar;
        }

        IEnumerable<Scalar> CastArguments(ForeignFunction func, IEnumerable<Scalar> args)
        {
            try {
                return func.ArgTypes.Zip(args, (ex, ac) => Cast(ac, ex));
            }
            catch {
                string expected = func.ArgTypes.Aggregate(string.Empty, (u, v) => u + ", " + v);
                string actual = args.Select(x => x.Type).Aggregate(string.Empty, (u, v) => u + ", " + v);
                throw new Exception(string.Format(
                    "Type mismatch: {0}\nexpects{1}\ngot    {2}",
                    func.Name, expected, actual));
            }
        }

        

       

        protected override Scalar Constant<T>(T Scalar, LowLevelType type)
        {
            if (type == LowLevelType.Float64) {
                return new Scalar(type, string.Format("{0:e}", Scalar));
            }
            return new Scalar(type, Scalar.ToString());
        }

        protected override Scalar Binary(BinaryOp op, Scalar Scalar1, Scalar Scalar2)
        {
            
        }

        protected override Scalar Offset(Scalar _base, Scalar offset)
        {
            if (!_base.Type.IsPointerType) {
                throw new ArgumentException("Expected a pointer.", "_base");
            }

            string var_name = string.Format("%{0}", var_count++);
            builder
                .Append(var_name)
                .Append(" = getelementptr ")
                .AppendType(_base.Type)
                .Append(' ')
                .Append(_base)
                .Append(", ")
                .AppendType(offset.Type)
                .Append(' ')
                .Append(offset)
                .AppendLine();
            return new Scalar(_base.Type, var_name);
        }

        int malloc_counter = 0;

        protected override Scalar AllocateByteArray(LowLevelType type, Scalar size)
        {
            
        }

        protected override void FreeArray(Scalar ptr)
        {
            CallVoidFunction("free_leak", Cast(ptr, i8ptr));
            //CallVoidFunction("free", Cast(ptr, i8ptr));
        }

        static readonly LowLevelType i8ptr = LowLevelType.Integer(8, true).Pointer();

        protected override void Copy(Scalar dst, Scalar src, Scalar count)
        {
            LowLevelType dstT = dst.Type;
            if (!dstT.IsPointerType) {
                throw new ArgumentException("Destination must be a pointer.", "dst");
            }

            LowLevelType srcT = src.Type;
            if (!srcT.IsPointerType) {
                Store(dst, src);
                return;
            }

            Scalar sizeInBytes = Binary(BinaryOp.Mul, count, Constant(srcT.ElementType.SizeInBytes, LowLevelType.NativeInteger));
            CallVoidFunction("memcpy", Cast(dst, i8ptr), Cast(src, i8ptr), sizeInBytes);
        }

        protected override void Store(Scalar dst, Scalar src)
        {
            LowLevelType dstT = dst.Type;
            if (!dstT.IsPointerType) {
                throw new ArgumentException("Destination must be a pointer.", "dst");
            }
            src = Cast(src, dstT.ElementType);

            builder
                .Append("store ")
                .AppendType(src.Type)
                .Append(' ')
                .Append(src)
                .Append(", ")
                .AppendType(dstT)
                .Append(' ')
                .Append(dst)
                .AppendLine();
        }

        protected override Scalar Load(Scalar src)
        {
            if (!src.Type.IsPointerType) {
                throw new ArgumentException("Source must be a pointer.", "src");
            }

            string var_name = string.Format("%{0}", var_count++);
            builder
                .Append(var_name)
                .Append(" = load ")
                .AppendType(src.Type)
                .Append(' ')
                .Append(src)
                .AppendLine();

            return new Scalar(src.Type.ElementType, var_name);
        }

        protected override Scalar Cast(Scalar src, LowLevelType targetType)
        {
            if (TryCast(src, targetType, out Scalar result)) {
                return result;
            }
            throw new InvalidCastException();
        }

        bool TryCast(Scalar src, LowLevelType targetType, out Scalar result)
        {
            result = null;

            if (src.Type == targetType) {
                result = src;
                return true;
            }


            LowLevelType st = src.Type;
            var srcKind = st.Kind;
            var dstKind = targetType.Kind;

            string op = string.Empty;

            if (srcKind == LowLevelType.TypeKind.Pointer && dstKind == LowLevelType.TypeKind.Pointer) {
                op = "bitcast ";
            }
            else if (srcKind == LowLevelType.TypeKind.Integer && dstKind == LowLevelType.TypeKind.Integer) {
                Integer si = st as Integer;
                Integer di = targetType as Integer;

                // LLVM doesn't care about signed/unsigned, so we can pass this through
                if (si.Bits == di.Bits) {
                    result = new Scalar(targetType, src.ToString());
                    return true;
                }

                op = si.Bits > di.Bits ? "trunc " : "zext ";
            }
            else {
                return false;
            }

            string var_name = string.Format("%{0}", var_count++);
            builder
                .Append(var_name)
                .Append(" = ")
                .Append(op)
                .AppendType(src.Type)
                .Append(' ')
                .Append(src)
                .Append(" to ")
                .AppendType(targetType)
                .AppendLine();

            result = new Scalar(targetType, var_name);
            return true;
        }

        protected override Scalar DeclareLocal(LowLevelType type)
        {
            string var_name = string.Format("%{0}", var_count++);
            builder
                .Append(var_name)
                .Append(" = ")
                .Append("alloca ")
                .AppendType(type)
                .AppendLine();

            return new Scalar(type.Pointer(), var_name);
        }

        protected override void StartBasicBlock(string name)
        {
            builder.Append(name).Append(':').AppendLine();
        }

        protected override void ConditionalJump(Scalar condition, string thenBranch, string elseBranch)
        {
            builder
                .Append("br i1 ")
                .Append(condition)
                .Append(", label %")
                .Append(thenBranch)
                .Append(", label %")
                .Append(elseBranch)
                .AppendLine();
        }

        protected override void Jump(string target)
        {
            builder
                .Append("br label %")
                .Append(target)
                .AppendLine();

        }

        protected override Scalar EmitStruct(IEnumerable<Scalar> fields)
        {
            Struct type = new Struct(fields.Select(x => x.Type));
            Scalar val = new Scalar(type, "undef");

            int i = 0;
            foreach (Scalar f in fields) {
                string var_name = string.Format("%{0}", var_count++);
                builder
                    .Append(var_name)
                    .Append(" = insertvalue ")
                    .AppendType(type)
                    .Append(' ')
                    .Append(val)
                    .Append(", ")
                    .AppendType(f.Type)
                    .Append(' ')
                    .Append(f)
                    .Append(", ")
                    .Append(i++)
                    .AppendLine();
                val = new Scalar(type, var_name);
            }
            return val;
        }

        protected override Scalar ExtractField(int index, Scalar arg)
        {
            Struct st = arg.Type as Struct;
            if (ReferenceEquals(st, null)) {
                throw new ArgumentException("Can only extract fields from structs.", "arg");
            }

            string var_name = string.Format("%{0}", var_count++);
            builder
                .Append(var_name)
                .Append(" = extractvalue ")
                .AppendType(st)
                .Append(' ')
                .Append(arg)
                .Append(", ")
                .Append(index)
                .AppendLine();
            return new Scalar(st[index], var_name);
        }

        protected override void Comment(string cmt)
        {
            builder
                .Append("; ")
                .Append(cmt)
                .AppendLine();
        }

        public override string ToString()
        {
            return declarations.ToString() + builder.ToString();
        }
    }
#endif
    }
}
