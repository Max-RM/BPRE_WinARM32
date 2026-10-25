using System;

public class Variant : IEquatable<Variant>
{
    private ValueVariant m_value;
    private RefVariant m_reference;
    public bool IsValue { get; }
    public bool IsRef => !IsValue;

    public ref readonly ValueVariant Value
    {
        get
        {
            if (IsValue)
            {
                return ref m_value;
            }

            throw new InvalidOperationException();
        }
    }

    public RefVariant Reference
    {
        get
        {
            if (!IsValue)
            {
                return m_reference;
            }

            throw new InvalidOperationException();
        }
    }

    public Variant(ValueVariant value)
    {
        IsValue = true;
        m_value = value;
    }

    public Variant(RefVariant reference)
    {
        IsValue = false;
        m_reference = reference;
    }

    public object ToObject()
    {
        if (IsValue)
        {
            return m_value.ToObject();
        }

        return m_reference.ToObject();
    }

    public override string ToString()
    {
        if (IsValue)
        {
            return m_value.ToString();
        }

        return m_reference.ToString();
    }

    public bool Equals(Variant other)
    {
        return this == other;
    }

    public static bool operator ==(Variant left, Variant right)
    {
        if (left.IsValue && right.IsValue)
        {
            return left.m_value == right.m_value;
        }

        if (!left.IsValue && !right.IsValue)
        {
            return left.m_reference == right.m_reference;
        }

        return false;
    }

    public static bool operator !=(Variant left, Variant right)
    {
        return !(left == right);
    }
}