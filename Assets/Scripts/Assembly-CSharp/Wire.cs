public class Wire : ElectricalElement
{
    public override bool IsNode()
    {
        return GetConnectedElectrodeCount() >= 3;
    }
}