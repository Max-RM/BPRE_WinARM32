using System;

/// <summary>
/// Data Container for UIPartButton, inherits System.ValueType
/// </summary>
public struct UIPartButtonInfo : IEquatable<UIPartButtonInfo>
{
    public UIPartButtonType ButtonType;
    public int ButtonIndex;
    public BasePart.PartType PartType;
    public int PartIndex;
    public int ComponentIndex;
    public int ComponentRank;
    public UIPartButtonInfo(UIPartButtonType buttonType, int buttonIndex, BasePart.PartType partType, int partIndex, int componentIndex) : this(buttonType, buttonIndex, partType, partIndex, componentIndex, -1)
    {
    }

    public UIPartButtonInfo(UIPartButtonType buttonType, int buttonIndex, BasePart.PartType partType, int partIndex, int componentIndex, int componentRank)
    {
        ButtonType = buttonType;
        ButtonIndex = buttonIndex;
        PartType = partType;
        PartIndex = partIndex;
        ComponentIndex = componentIndex;
        ComponentRank = componentRank;
    }

    //No need to look below, just some boring equal methods
    public bool Equals(UIPartButtonInfo other)
    {
        return this == other;
    }

    public override bool Equals(object other)
    {
        if ((other is UIPartButtonInfo other2))
        {
            return Equals(other2);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ButtonType, ButtonIndex, PartType, PartIndex, ComponentRank);
    }

    public static bool operator ==(UIPartButtonInfo left, UIPartButtonInfo right)
    {
        if (left.ButtonType == right.ButtonType && left.ButtonIndex == right.ButtonIndex && left.PartType == right.PartType && left.PartIndex == right.PartIndex)
        {
            return left.ComponentRank == right.ComponentRank;
        }

        return false;
    }

    public static bool operator !=(UIPartButtonInfo left, UIPartButtonInfo right)
    {
        return !(left == right);
    }
}