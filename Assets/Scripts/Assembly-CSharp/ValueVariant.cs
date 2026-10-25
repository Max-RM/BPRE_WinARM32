using System;
using System.Globalization;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 8)]
public readonly struct ValueVariant : IEquatable<ValueVariant>
{
    [FieldOffset(4)]
    private readonly bool m_booleanValue;
    [FieldOffset(4)]
    private readonly byte m_byteValue;
    [FieldOffset(4)]
    private readonly short m_int16Value;
    [FieldOffset(4)]
    private readonly int m_int32Value;
    [FieldOffset(4)]
    private readonly float m_singleValue;
    [field: FieldOffset(0)]
    public TypeCode Type { get; }

    public ValueVariant(bool value)
    {
        this = default(ValueVariant);
        Type = TypeCode.Boolean;
        m_booleanValue = value;
    }

    public ValueVariant(byte value)
    {
        this = default(ValueVariant);
        Type = TypeCode.Byte;
        m_byteValue = value;
    }

    public ValueVariant(short value)
    {
        this = default(ValueVariant);
        Type = TypeCode.Int16;
        m_int16Value = value;
    }

    public ValueVariant(int value)
    {
        this = default(ValueVariant);
        Type = TypeCode.Int32;
        m_int32Value = value;
    }

    public ValueVariant(float value)
    {
        this = default(ValueVariant);
        Type = TypeCode.Single;
        m_singleValue = value;
    }

    public ValueVariant(TypeCode type, IConvertible value)
    {
        this = default(ValueVariant);
        Type = type;
        IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
        switch (type)
        {
            case TypeCode.Boolean:
                m_booleanValue = value.ToBoolean(invariantCulture);
                break;
            case TypeCode.Byte:
                m_byteValue = value.ToByte(invariantCulture);
                break;
            case TypeCode.Int16:
                m_int16Value = value.ToInt16(invariantCulture);
                break;
            case TypeCode.Int32:
                m_int32Value = value.ToInt32(invariantCulture);
                break;
            case TypeCode.Single:
                m_singleValue = value.ToSingle(invariantCulture);
                break;
            default:
                throw new ArgumentException("Invalid Type: " + type);
        }
    }

    public bool ToBoolean()
    {
        return m_booleanValue;
    }

    public byte ToByte()
    {
        return m_byteValue;
    }

    public short ToInt16()
    {
        return m_int16Value;
    }

    public int ToInt32()
    {
        return m_int32Value;
    }

    public float ToSingle()
    {
        return m_singleValue;
    }

    public object ToObject()
    {
        TypeCode type = Type;
        return ((type == TypeCode.Boolean) ? (object)m_booleanValue : (type == TypeCode.Byte) ? (object)m_byteValue : (type == TypeCode.Int16) ? (object)m_int16Value : (type == TypeCode.Int32) ? (object)m_int32Value : (type == TypeCode.Single) ? (object)m_singleValue : throw new ArgumentException("Invalid Type: " + type));
    }

    public override string ToString()
    {
        return ToString(CultureInfo.InvariantCulture);
    }

    public string ToString(IFormatProvider provider)
    {
        TypeCode type = Type;
        return ((type == TypeCode.Boolean) ? m_booleanValue.ToString(provider) : (type == TypeCode.Byte) ? m_byteValue.ToString(provider) : (type == TypeCode.Int16) ? m_int16Value.ToString(provider) : (type == TypeCode.Int32) ? m_int32Value.ToString(provider) : (type == TypeCode.Single) ? m_singleValue.ToString(provider) : throw new ArgumentException("Invalid Type: " + type));
    }

    public static ValueVariant Parse(TypeCode type, string str)
    {
        return Parse(type, str, CultureInfo.InvariantCulture);
    }

    public static ValueVariant Parse(TypeCode type, string str, IFormatProvider provider)
    {
        return ((type == TypeCode.Boolean) ? new ValueVariant(bool.Parse(str)) : (type == TypeCode.Byte) ? new ValueVariant(byte.Parse(str, provider)) : (type == TypeCode.Int16) ? new ValueVariant(short.Parse(str, provider)) : (type == TypeCode.Int32) ? new ValueVariant(int.Parse(str, provider)) : (type == TypeCode.Single) ? new ValueVariant(float.Parse(str, provider)) : throw new ArgumentException("Invalid Type: " + type));
    }

    public static bool TryParse(TypeCode type, string str, out ValueVariant result)
    {
        return TryParse(type, str, CultureInfo.InvariantCulture, out result);
    }

    public static bool TryParse(TypeCode type, string str, IFormatProvider provider, out ValueVariant result)
    {
        bool result2 = false;
        result = default(ValueVariant);
        switch (type)
        {
            case TypeCode.Boolean:
            {
                if (bool.TryParse(str, out var result4))
                {
                    result2 = true;
                    result = new ValueVariant(result4);
                }

                break;
            }

            case TypeCode.Byte:
            {
                if (byte.TryParse(str, NumberStyles.Integer, provider, out var result6))
                {
                    result2 = true;
                    result = new ValueVariant(result6);
                }

                break;
            }

            case TypeCode.Int16:
            {
                if (short.TryParse(str, NumberStyles.Integer, provider, out var result7))
                {
                    result2 = true;
                    result = new ValueVariant(result7);
                }

                break;
            }

            case TypeCode.Int32:
            {
                if (int.TryParse(str, NumberStyles.Integer, provider, out var result5))
                {
                    result2 = true;
                    result = new ValueVariant(result5);
                }

                break;
            }

            case TypeCode.Single:
            {
                if (float.TryParse(str, NumberStyles.Float, provider, out var result3))
                {
                    result2 = true;
                    result = new ValueVariant(result3);
                }

                break;
            }
        }

        return result2;
    }

    public bool Equals(ValueVariant other)
    {
        return this == other;
    }

    public static bool operator ==(ValueVariant left, ValueVariant right)
    {
        if (left.Type != right.Type)
        {
            return false;
        }

        TypeCode type = left.Type;
        return ((type == TypeCode.Boolean) ? left.m_booleanValue == right.m_booleanValue : (type == TypeCode.Byte) ? left.m_byteValue == right.m_byteValue : (type == TypeCode.Int16) ? left.m_int16Value == right.m_int16Value : (type == TypeCode.Int32) ? left.m_int32Value == right.m_int32Value : (type == TypeCode.Single) ? left.m_singleValue == right.m_singleValue : throw new ArgumentException("Invalid Type: " + type));
    }

    public static bool operator !=(ValueVariant left, ValueVariant right)
    {
        return !(left == right);
    }

    public static explicit operator bool (ValueVariant value)
    {
        value.CheckType(TypeCode.Boolean);
        return value.m_booleanValue;
    }

    public static explicit operator byte (ValueVariant value)
    {
        value.CheckType(TypeCode.Byte);
        return value.m_byteValue;
    }

    public static explicit operator short (ValueVariant value)
    {
        value.CheckType(TypeCode.Int16);
        return value.m_int16Value;
    }

    public static explicit operator int (ValueVariant value)
    {
        value.CheckType(TypeCode.Int32);
        return value.m_int32Value;
    }

    public static explicit operator float (ValueVariant value)
    {
        value.CheckType(TypeCode.Single);
        return value.m_singleValue;
    }

    private void CheckType(TypeCode type)
    {
        if (Type != type)
        {
            throw new InvalidOperationException();
        }
    }
}