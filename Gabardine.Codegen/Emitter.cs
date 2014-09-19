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
using System.Diagnostics.Debug;
using System.Linq;

namespace Gabardine.Codegen
{
    public enum BinaryOp { Add, Sub, Mul, Div, LT, GT };

    public interface IEmitter<V> where V : Value
    {
        void FinalizeFunction(string functionName);

        V AddInputParameter(string name, LowLevelType type);
        void Return(V arg);
        void Copy(V dst, V src, V count);
        void Store(V dst, V src);
        void CallVoidFunction(string name, IEnumerable<V> args);
        void FreeArray(V buf);
        void StartBasicBlock(string v);
        void ConditionalJump(V cond, string thenBranch, string elseBranch);
        void Jump(string v);
        void Comment(string v);
        V Binary(BinaryOp op, V left, V right);
        V Offset(V _base, V offset);
        V CallFunction(string name, IEnumerable<V> args);
        V AllocateByteArray(LowLevelType type, V size);
        V Cast(V src, LowLevelType type);
        V DeclareLocal(LowLevelType lowLevelType);
        V Load(V ptr);
        V EmitStruct(IEnumerable<V> fields);
        V ExtractField(int index, V arg);
        V Constant<T>(T cnst, LowLevelType type);
    }

    public static class Emission
    {
        public static void EmitFunction<V>(IEmitter<V> emitter, RewriteSystem rw, string functionName, IEnumerable<Operator> parameterOrder, Term expr)
            where V : Value
        {
            // Run soundness check
            if (!CheckModel(rw, expr)) {
                return;
            }

            // Lower the output expression(s)
            expr = BaseRewriter.Before.RewriteUnordered(expr);
            expr = rw.RewriteUnordered(expr);
            expr = BaseRewriter.After.RewriteUnordered(expr);

            // Break into statements and run analysis
            var program = ConsUtils.UnconsMany(expr);
            program = Analysis.Full(program);

            // Define the input parameters
            var parameters = new Dictionary<Operator, V>();
            foreach (Operator p in parameterOrder) {
                LowLevelType type = GetType(rw, p.CreateTerm());
                V value = emitter.AddInputParameter(p.ToString(), type);
                parameters.Add(p, value);
            }

            // Emit the statements
            Emission<V> em = new Emission<V>(emitter, parameters);
            foreach (Term step in program) {
                em.EmitStatement(step);
            }

            // Close
            emitter.FinalizeFunction(functionName);
        }

        static LowLevelType GetType(RewriteSystem rw, Term term)
        {
            Term typeTest = rw.RewriteUnordered(Special.Typeof[term]);
            Constant<LowLevelType> cnst = typeTest.Op as Constant<LowLevelType>;
            if (ReferenceEquals(cnst, null)) {
                throw new InvalidCastException(string.Format("Cannot get a low level type for {0}: type deduced is {1}.", term, typeTest));
            }
            return cnst.Value;
        }

        #region Model checking

        static void ReportErrorDetail(Term expr)
        {
            Terminal.Stderr.PushFormat(TerminalFormat.LightPurple);
            Terminal.Stderr.SendLine("Expression {0} is not sound.", expr);
            Terminal.Stderr.PopFormat();
        }

        static void ReportInconclusive(Term expr)
        {
            Terminal.Stderr.PushFormat(TerminalFormat.LightPurple);
            Terminal.Stderr.SendLine("Unable to reduce {0} without further information.", expr);
            Terminal.Stderr.PopFormat();
        }

        static bool CheckModel(RewriteSystem rw, Term expr)
        {
            ModelChecker modelChecker = new ModelChecker(rw);
            Term result = modelChecker.CheckAndIngest(expr);
            switch (Special.GetKind(result.Op)) {
                case Special.Kind.False:
                    ReportErrorDetail(expr);
                    return false;
                case Special.Kind.True:
                    return true;
                default:
                    break;
            }
            ReportInconclusive(result);
            return false;
        }

        #endregion
    }

    class Emission<V> : ConstantVisitor<V>, ConstantVisitor<V, LowLevelType>
        where V : Value
    {
        readonly IEmitter<V> emitter;
        readonly Dictionary<Operator, V> parameters;
        readonly Dictionary<string, V> temporaries = new Dictionary<string, V>();

        public Emission(IEmitter<V> emitter, Dictionary<Operator, V> parameters)
        {
            this.emitter = emitter;
            this.parameters = parameters;
        }

        public void EmitStatement(Term statement)
        {
            switch (Statements.GetKind(statement.Op)) {
                case Statements.Kind.Return:
                    V arg = EmitArgument(statement[0]);
                    //CallVoidFunction("dump_leaks");
                    emitter.Return(arg);
                    break;

                case Statements.Kind.TmpAssign:
                    V value = EmitInstruction(statement[1]);
                    string name = ExtractConstant<string>(statement[0]);
                    temporaries.Add(name, value);
                    break;

                case Statements.Kind.Copy:
                    {
                        V dst = EmitArgument(statement[0]);
                        V src = EmitArgument(statement[1]);
                        V count = EmitArgument(statement[2]);
                        emitter.Copy(dst, src, count);
                        break;
                    }

                case Statements.Kind.Store:
                    {
                        V dst = EmitArgument(statement[0]);
                        V src = EmitArgument(statement[1]);
                        emitter.Store(dst, src);
                        break;
                    }

                case Statements.Kind.VoidCall:
                    string fname = ExtractConstant<string>(statement[0]);
                    var args = ConsUtils.UnconsMany(statement[1]).Select(x => EmitArgument(x));
                    emitter.CallVoidFunction(fname, args);
                    break;

                case Statements.Kind.Free:
                    var buf = EmitArgument(statement[0]);
                    emitter.FreeArray(buf);
                    break;

                case Statements.Kind.BasicBlock:
                    emitter.StartBasicBlock(ExtractConstant<string>(statement[0]));
                    break;

                case Statements.Kind.If:
                    V cond = EmitArgument(statement[0]);
                    emitter.ConditionalJump(cond, ExtractConstant<string>(statement[1]), ExtractConstant<string>(statement[2]));
                    break;

                case Statements.Kind.Goto:
                    emitter.Jump(ExtractConstant<string>(statement[0]));
                    break;

                case Statements.Kind.Bind:
                case Statements.Kind.Unbind:
                    Assert(false, "Bind and unbind should have been eliminated during analysis.");
                    break;

                case Statements.Kind.Comment:
                    emitter.Comment(ExtractConstant<string>(statement[0]));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
        
        V EmitInstruction(Term instr)
        {
            switch (Instructions.GetKind(instr.Op)) {
                case Instructions.Kind.Binary:
                    V left = EmitArgument(instr[1]);
                    V right = EmitArgument(instr[2]);
                    BinaryOp op = ExtractConstant<BinaryOp>(instr[0]);
                    return emitter.Binary(op, left, right);

                case Instructions.Kind.Offset:
                    V _base = EmitArgument(instr[0]);
                    V offset = EmitArgument(instr[1]);
                    return emitter.Offset(_base, offset);

                case Instructions.Kind.Call:
                    string name = ExtractConstant<string>(instr[0]);
                    var args = ConsUtils.UnconsMany(instr[1]).Select(x => EmitArgument(x));
                    return emitter.CallFunction(name, args);

                case Instructions.Kind.Alloc:
                    {
                        LowLevelType type = ExtractType(instr[0]);
                        var size = EmitArgument(instr[1]);
                        return emitter.AllocateByteArray(type, size);
                    }

                case Instructions.Kind.Cast:
                    {
                        var src = EmitArgument(instr[0]);
                        LowLevelType type = ExtractConstant<LowLevelType>(instr[1]);
                        return emitter.Cast(src, type);
                    }

                case Instructions.Kind.Local:
                    return emitter.DeclareLocal(ExtractType(instr[0]));

                case Instructions.Kind.Load:
                    {
                        var ptr = EmitArgument(instr[0]);
                        return emitter.Load(ptr);
                    }

                case Instructions.Kind.Struct:
                    {
                        var fields = ConsUtils.UnconsMany(instr[0]).Select(x => EmitArgument(x));
                        return emitter.EmitStruct(fields);
                    }

                case Instructions.Kind.Field:
                    {
                        int index = ExtractConstant<int>(instr[0]);
                        V arg = EmitArgument(instr[1]);
                        return emitter.ExtractField(index, arg);
                    }

                case Instructions.Kind.Param:
                    return GetParameter(instr[0]);

                case Instructions.Kind.GenerateFail:
                    var expr = instr[0];
                    throw new NotImplementedException(
                        string.Format("No rules were implemented to generate the expression:\n{0}", expr)
                        );

            }
            throw new NotImplementedException();
        }

        V EmitArgument(Term term)
        {
            switch (Arguments.GetKind(term.Op)) {
                case Arguments.Kind.Tmp:
                    return GetTemporary(ExtractConstant<string>(term[0]));
                case Arguments.Kind.Const:
                    return term[0].Op.ConstantVisit(this);
                case Arguments.Kind.Literal:
                    return term[0].Op.ConstantVisit(this, ExtractConstant<LowLevelType>(term[1]));
                default:
                    throw new NotImplementedException();
            }
        }       

        static T ExtractConstant<T>(Term constant)
        {
            Assert(constant.Op.IsConstant, "Expected constant");
            IConstant<T> cnst = constant.Op as IConstant<T>;
            return cnst.Value;
        }

        V Constant<T>(T cnst)
        {
            LowLevelType type = LowLevelType.Convert(typeof(T));
            return emitter.Constant(cnst, type);
        }

        V GetParameter(Term parameter)
        {
            Operator op = parameter.Op;
            if (op.Kind != OperatorKind.Parameter) {
                throw new ArgumentException("Expected a parameter.", "parameter");
            }
            if (parameters.TryGetValue(op, out V V)) {
                return V;
            }
            throw new KeyNotFoundException("Couldn't find a value for parameter: " + parameter);
        }

        LowLevelType ExtractType(Term typespec)
        {
            switch (TypeInference.GetKind(typespec.Op)) {
                case TypeInference.Kind.Static:
                    return ExtractConstant<LowLevelType>(typespec[0]);
                case TypeInference.Kind.Typeof:
                    V arg = EmitArgument(typespec[0]);
                    return arg.Type;
                default:
                    throw new NotImplementedException();
            }
        }

        public V VisitConstant<T>(T cnst)
        {
            return Constant(cnst);
        }

        public V VisitConstant<T>(T cnst, LowLevelType type)
        {
            return emitter.Constant(cnst, type);
        }

        V GetTemporary(string name)
        {
            if (temporaries.TryGetValue(name, out V val)) {
                return val;
            }
            throw new KeyNotFoundException("A temporary %" + name + " wasn't found.");
        }
    }
}
