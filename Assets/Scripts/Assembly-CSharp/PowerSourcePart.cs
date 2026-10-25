using System.Collections.Generic;

public class PowerSourcePart : ElectricalPart
{
    private PowerSource m_powerSource;
    public override IEnumerable<ElectricalElement> ElectricalElements => m_powerSource.ToEnumerable();

    public override void CreateElectricalElements()
    {
        int num = customPartIndex - 12;
        float electromotance = ((num == 0) ? 1f : (num == 1) ? 5f : (num == 2) ? 50f : 0f);
        m_powerSource = new PowerSource(electromotance, 0.05f);
    }

    public override void ConnectElectricalElements()
    {
        foreach (ConnectionData connection in m_connections)
        {
            ElectricalElement element = connection.Element1;
            ElectricalElement element2 = connection.Element2;
            int type = ((connection.Direction != BitDirection.Right.Rotate((int)m_gridRotation)) ? 1 : 0);
            element.AddConnectedElement(element2, type);
        }
    }

    protected override BitDirection GetConnectionDirection()
    {
        return BitDirection.LeftAndRight.Rotate((int)m_gridRotation);
    }
}