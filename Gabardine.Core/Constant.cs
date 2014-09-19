/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
namespace Gabardine
{
    public interface ConstantVisitor<TRet>
    {
        TRet VisitConstant<T>(T value);
    }

    public interface ConstantVisitor<TRet, TAux>
    {
        TRet VisitConstant<T>(T value, TAux aux);
    }

    public interface IConstant<out T>
    {
        T Value { get; }
    }

    public class Constant<T> : Operator, IConstant<T>
    {
        readonly T value;

        public Constant(T value)
        : base(0, OperatorKind.Function, value.ToString())
        {
            this.value = value;
        }

        public T Value { get { return value; } }

        public override bool IsConstant { get { return true; } }

        public override bool Equals(Operator other)
        {
            Constant<T> cnst = other as Constant<T>;
            if (ReferenceEquals(cnst, null)) { return false; }
            return cnst.Value.Equals(this.Value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override TRet ConstantVisit<TRet>(ConstantVisitor<TRet> visitor)
        {
            return visitor.VisitConstant(Value);
        }

        public override TRet ConstantVisit<TRet, TAux>(ConstantVisitor<TRet, TAux> visitor, TAux auxData)
        {
            return visitor.VisitConstant(Value, auxData);
        }
    }
}