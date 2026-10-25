public struct UIPartTriggerButtonInfo
{
    public UIPartButtonInfo Value;
    public bool Consistent;
    public UIPartTriggerButtonInfo(UIPartButtonInfo value, bool consistent)
    {
        Value = value;
        Consistent = consistent;
    }

    public UIPartTriggerButtonInfo(UIPartButtonType buttonType, int buttonIndex, BasePart.PartType partType, int partIndex, int componentIndex, bool consistent)
    {
        Value = new UIPartButtonInfo(buttonType, buttonIndex, partType, partIndex, componentIndex);
        Consistent = consistent;
    }
}