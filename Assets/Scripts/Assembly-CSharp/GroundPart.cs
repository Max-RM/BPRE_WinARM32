using System.Collections.Generic;

public class GroundPart : ElectricalPart
{
    private Ground m_ground;
    public override IEnumerable<ElectricalElement> ElectricalElements => m_ground.ToEnumerable();

    protected override BitDirection GetConnectionDirection()
    {
        return BitDirection.Up.Rotate((int)m_gridRotation);
    }

    public override void CreateElectricalElements()
    {
        m_ground = new Ground(0f);
    }
}