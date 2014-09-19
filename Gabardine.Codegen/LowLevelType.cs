/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace Gabardine.Codegen
{
    public abstract class LowLevelType : IEquatable<LowLevelType>
    {
        public enum TypeKind { Integer, Float64, Void, Pointer, Struct };

        public static LowLevelType Convert(Type type)
        {
            if (type == typeof(int)) {
                return new Integer(32, true);
            }

            if (type == typeof(uint)) {
                return new Integer(32, false);
            }

            if (type == typeof(double)) {
                return Float64;
            }

            throw new NotImplementedException();
        }

        public abstract Type ToManagedType();

        static readonly Void _void = new Void();
        public static LowLevelType Void { get { return _void; } }

        static readonly Float64 _double = new Float64();
        public static LowLevelType Float64 { get { return _double; } }

        public static LowLevelType Integer(int bits, bool signed = false)
        {
            return new Codegen.Integer(bits, signed);
        }

        static readonly Integer nativeInt = new Codegen.Integer(IntPtr.Size * 8);
        public static LowLevelType NativeInteger { get { return nativeInt; } }

        public LowLevelType Pointer()
        {
            return new Pointer(this);
        }

        public static bool operator ==(LowLevelType left, LowLevelType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LowLevelType left, LowLevelType right)
        {
            return !left.Equals(right);
        }

        public abstract bool Equals(LowLevelType other);

        public abstract override int GetHashCode();

        public override bool Equals(object obj)
        {
            LowLevelType other = obj as LowLevelType;
            if (ReferenceEquals(other, null)) {
                return false;
            }
            return other.Equals(this);
        }

        public abstract TypeKind Kind { get; }

        public virtual bool IsPointerType { get { return false; } }
        public virtual LowLevelType ElementType { get { throw new InvalidOperationException("Not a pointer type."); } }
        public abstract int SizeInBytes { get; }
    }

    public class Integer : LowLevelType
    {
        readonly int bits;
        readonly bool signed;

        public Integer(int bits, bool signed = false)
        {
            this.bits = bits;
            this.signed = signed;
        }

        public int Bits { get { return bits; } }
        public bool Signed { get { return signed; } }
        public override int SizeInBytes
        {
            get
            {
                if (bits % 8 != 0) {
                    throw new InvalidOperationException("Irregular integer.");
                }
                return bits / 8;
            }
        }

        public override bool Equals(LowLevelType other)
        {
            Integer iOther = other as Integer;
            if (ReferenceEquals(iOther, null)) {
                return false;
            }

            return iOther.Bits == this.Bits && iOther.Signed == this.Signed;
        }

        public override int GetHashCode()
        {
            return Hash.MakeHash(Bits, Signed ? 0x2D98AA4B : 0x309AC3B3);
        }

        public override string ToString()
        {
            return string.Format("{0}i{1}", Signed ? "" : "u", Bits);
        }

        public override Type ToManagedType()
        {
            switch(Bits) {
                case 16:
                    return typeof(System.Int16);
                case 32:
                    return typeof(System.Int32);
                case 64:
                    return typeof(System.Int64);
                default:
                    throw new InvalidCastException(string.Format("Cannot translate a {0}-bit integer into a managed type.", Bits));
            }
        }

        public override TypeKind Kind { get { return TypeKind.Integer; } }
    }



    public class Pointer : LowLevelType
    {
        readonly LowLevelType elementType;

        public Pointer(LowLevelType elementType)
        {
            this.elementType = elementType;
        }

        public override bool IsPointerType { get { return true; } }
        public override LowLevelType ElementType { get { return elementType; } }
        public override int SizeInBytes { get { return IntPtr.Size; } }

        public override bool Equals(LowLevelType other)
        {
            Pointer ptr = other as Pointer;
            if (ReferenceEquals(ptr, null)) {
                return false;
            }
            return ptr.elementType == elementType;
        }

        public override int GetHashCode()
        {
            return Hash.MakeHash(elementType.GetHashCode(), 0x6BEFF93C);
        }

        public override string ToString()
        {
            return elementType.ToString() + "*";
        }

        public override Type ToManagedType()
        {
            Type elem = ElementType.ToManagedType();
            return elem.MakePointerType();
        }

        public override TypeKind Kind { get { return TypeKind.Pointer; } }

    }

    class Float64 : LowLevelType
    {

        public override int SizeInBytes { get { return 8; } }

        public override bool Equals(LowLevelType other)
        {
            return other is Float64;
        }

        public override int GetHashCode()
        {
            return unchecked((int)0xD854E23D);
        }

        public override string ToString()
        {
            return "double";
        }

        public override Type ToManagedType()
        {
            return typeof(double);
        }

        public override TypeKind Kind { get { return TypeKind.Float64; } }

    }

    class Void : LowLevelType
    {
        public override int SizeInBytes { get { return 0; } }

        public override bool Equals(LowLevelType other)
        {
            return other is Void;
        }

        public override int GetHashCode()
        {
            return 0x78E99A3A;
        }

        public override string ToString()
        {
            return "void";
        }

        public override Type ToManagedType()
        {
            return typeof(void);
        }

        public override TypeKind Kind { get { return TypeKind.Void; } }
    }

    public class Struct : LowLevelType, IEnumerable<LowLevelType>
    {
        readonly LowLevelType[] fields;

        public Struct(IEnumerable<LowLevelType> fields)
        {
            this.fields = fields.ToArray();
        }

        public LowLevelType this[int index] { get { return fields[index]; } }
        public int Length { get { return fields.Length; } }

        public override TypeKind Kind { get { return TypeKind.Struct; } }

        public override int SizeInBytes { get { return fields.Select(x => x.SizeInBytes).Sum(); } }

        public override bool Equals(LowLevelType other)
        {
            Struct s = other as Struct;
            if (ReferenceEquals(s, null)) {
                return false;
            }

            if (s.Length != Length) {
                return false;
            }

            for (int i = 0; i < fields.Length; ++i) {
                if (s[i] != this[i]) {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return Hash.MakeHash(fields.Select(x => x.GetHashCode()));
        }

        public override string ToString()
        {
            string str = "{";
            for (int i = 0; i < Length; ++i) {
                if (i > 0) {
                    str += ", ";
                }
                str += this[i].ToString();
            }
            str += "}";
            return str;
        }

        public IEnumerator<LowLevelType> GetEnumerator()
        {
            foreach(LowLevelType t in fields) { yield return t; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return fields.GetEnumerator();
        }

        public override Type ToManagedType()
        {
            throw new NotImplementedException();
        }
    }
}
