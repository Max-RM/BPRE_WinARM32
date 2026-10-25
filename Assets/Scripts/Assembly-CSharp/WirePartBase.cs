using System;

public class WirePartBase : ElectricalPart
{
    protected float m_maxI;
    protected float[] m_I;
    protected const float MaxWireI = 50000f;
    public float[] I => m_I;

    public override void Awake()
    {
        base.Awake();
        m_I = new float[4];
    }

    public virtual bool IsElectromagnetic()
    {
        return false;
    }

    public override void PreUpdateElements()
    {
        m_maxI = 0f;
        for (int i = 0; i < 4; i++)
        {
            m_I[i] = 0f;
        }
    }

    protected void OnElementUpdatedBase(CircuitSimulator.SimulationResult result)
    {
        if (result.Electrode == -1)
        {
            return;
        }

        m_maxI = Math.Max(Math.Abs(result.I), m_maxI);
        if (!IsElectromagnetic())
        {
            return;
        }

        ElectricalElement element = result.Element.Electrodes[result.Electrode].Element;
        foreach (ConnectionData connection in m_connections)
        {
            ElectricalElement element2 = connection.Element1;
            ElectricalElement element3 = connection.Element2;
            if (element2 == result.Element && element3 == element)
            {
                int num = 0;
                int num2 = (int)connection.Direction;
                while (num2 > 1)
                {
                    num2 >>= 1;
                    num++;
                }

                m_I[num] = result.I;
            }
        }
    }

    public override void PostUpdateElements()
    {
        if (m_maxI > 50000f)
        {
            SetInvalid(invalid: true);
            RemoveAllConnections();
        }
    }
}