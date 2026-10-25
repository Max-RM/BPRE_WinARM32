using System.Collections.Generic;

public class PartTrigger : ElectricalPart
{
    private Wire m_wire;
    private LogicLevel m_level;
    private LogicLevel m_newLevel;
    private BasePart[] m_connectedParts;
    public bool m_isTriple;
    public override IEnumerable<ElectricalElement> ElectricalElements => m_wire.ToEnumerable();

    public override void CreateElectricalElements()
    {
        m_wire = new Wire();
        m_wire.ElementUpdatedEvent += OnElementUpdated;
    }

    protected override BitDirection GetConnectionDirection()
    {
        return BitDirection.Left.Rotate((int)m_gridRotation);
    }

    public override void InitializeElectricalElements()
    {
        m_connectedParts = new BasePart[3];
        m_level = LogicLevel.Invalid;
        int x_coord = 1;
        int y_coord = 0;
        for (int i = 0; i < (int)m_gridRotation; i++)
        {
            int num3 = x_coord;
            x_coord = -y_coord;
            y_coord = num3;
        } //direction function, 90 deg only

        if (!m_isTriple)
        {
            BasePart basePart = base.contraption.FindPartAt(m_coordX + x_coord, m_coordY + y_coord, this);
            if (basePart != null && basePart.ConnectedComponent == base.ConnectedComponent)
            {
                basePart = ((basePart.m_enclosedPart != null) ? basePart.m_enclosedPart : basePart);
                m_connectedParts[0] = basePart;
            }
        }
        else if (m_isTriple)
        {
            for (int i = 1; i <= 3; i++)
            {
                BasePart basePart = base.contraption.FindPartAt(m_coordX + i * x_coord, m_coordY + i * y_coord, this); //yeah we're using complex i LOLOLOL
                if (basePart != null && basePart.ConnectedComponent == base.ConnectedComponent)
                {
                    basePart = ((basePart.m_enclosedPart != null) ? basePart.m_enclosedPart : basePart);
                    m_connectedParts[i - 1] = basePart;
                }
            }
        }
    }

    private void OnElementUpdated(CircuitSimulator.SimulationResult result)
    {
        if (result.IsGrounded)
        {
            m_newLevel = ElectricalPart.GetLogicLevel(result.U);
        }
    }

    public override void PreUpdateElements()
    {
        m_newLevel = LogicLevel.Invalid;
    }

    public override void PostUpdateElements()
    {
        SetInvalid(m_newLevel == LogicLevel.Invalid);
        /*if (!m_isTriple)
		{
			if (m_level != 0 && m_newLevel != 0 && m_level != m_newLevel && m_connectedParts[0] != null &&
			    m_connectedParts[0].ConnectedComponent == base.ConnectedComponent)
			{
				m_connectedParts[0].ProcessTouch();
			}
		}
		else if (m_isTriple)
		{*/
        foreach (BasePart basepart in m_connectedParts)
        {
            if (m_level != 0 && m_newLevel != 0 && m_level != m_newLevel && basepart != null && basepart.ConnectedComponent == base.ConnectedComponent)
            {
                basepart.ProcessTouch();
            }
        }

        /*}*/
        m_level = m_newLevel;
    }
}