using System;
using UnityEngine;

public struct PartTypeInfo : IEquatable<PartTypeInfo>
{
    public BasePart.PartType PartType;
    public int PartIndex;
    public PartTypeInfo(GameObject gameObject) : this(gameObject.GetComponent<BasePart>())
    {
    }

    public PartTypeInfo(BasePart part)
    {
        PartType = part.Type;
        PartIndex = part.Index;
    }

    public PartTypeInfo(BasePart.PartType partType, int partIndex)
    {
        PartType = partType;
        PartIndex = partIndex;
    }

    public override bool Equals(object other)
    {
        if ((other is PartTypeInfo partTypeInfo))
        {
            return this == partTypeInfo;
        }

        return false;
    }

    public bool Equals(PartTypeInfo other)
    {
        return this == other;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PartType, PartIndex);
    }

    public void Deconstruct(out BasePart.PartType partType, out int partIndex)
    {
        partType = PartType;
        partIndex = PartIndex;
    }

    public static bool operator ==(PartTypeInfo left, PartTypeInfo right)
    {
        if (left.PartType == right.PartType)
        {
            return left.PartIndex == right.PartIndex;
        }

        return false;
    }

    public static bool operator !=(PartTypeInfo left, PartTypeInfo right)
    {
        return !(left == right);
    }
}