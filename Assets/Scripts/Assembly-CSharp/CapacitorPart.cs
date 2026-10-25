using System;
using System.Collections.Generic;

public class CapacitorPart : ElectricalPart
{
    private Capacitor m_capacitor;
    private float m_maxU;
    public override IEnumerable<ElectricalElement> ElectricalElements => m_capacitor.ToEnumerable();

    public override void CreateElectricalElements()
    {
        int num = customPartIndex - 15;
        float capacitance = 0f;
        float maxU = 0f;
        switch (num)
        {
            case 0:
                capacitance = 0.1f;
                maxU = 5f;
                break;
            case 1:
                capacitance = 1f;
                maxU = 50f;
                break;
            case 2:
                capacitance = 20f;
                maxU = 500f;
                break;
        }

        m_maxU = maxU;
        m_capacitor = new Capacitor(capacitance, 0.01f);
    }

    protected override BitDirection GetConnectionDirection()
    {
        return BitDirection.LeftAndRight.Rotate((int)m_gridRotation);
    }

    public override void SetRotation(GridRotation rotation)
    {
        int rotation2 = (int)rotation % 2;
        base.SetRotation((GridRotation)rotation2);
    }

    public override void PostUpdateElements()
    {
        float value = m_capacitor.Charge / m_capacitor.Capacitance;
        if (Math.Abs(value) > m_maxU)
        {
            SetInvalid(invalid: true);
            RemoveAllConnections();
            m_capacitor.Charge = (float)Math.Sign(value) * m_maxU * m_capacitor.Capacitance;
        }
    }
}