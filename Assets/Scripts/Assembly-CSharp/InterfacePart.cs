using System;
using System.Collections.Generic;

public class InterfacePart : WirePartBase
{
    private struct InterfaceConnectionData : IEquatable<InterfaceConnectionData>
    {
        public ElectricalElement Element;
        public BitDirection Direction;
        public InterfaceConnectionData(ElectricalElement element, BitDirection direction)
        {
            Element = element;
            Direction = direction;
        }

        public bool Equals(InterfaceConnectionData other)
        {
            if (Element == other.Element)
            {
                return Direction == other.Direction;
            }

            return false;
        }
    }

    private List<InterfaceConnectionData> m_dynamicConnections;
    private List<InterfaceConnectionData> m_newDynamicConnections;
    public Wire Wire { get; private set; }
    public override IEnumerable<ElectricalElement> ElectricalElements => Wire.ToEnumerable();

    public override void Awake()
    {
        base.Awake();
        m_autoAlign = AutoAlignType.None;
        m_dynamicConnections = new List<InterfaceConnectionData>();
        m_newDynamicConnections = new List<InterfaceConnectionData>();
    }

    protected override BitDirection GetConnectionDirection()
    {
        return BitDirection.Any;
    }

    public override bool IsElectromagnetic()
    {
        return customPartIndex == 39;
    }

    public override void CreateElectricalElements()
    {
        Wire wire = new Wire();
        wire.ElementUpdatedEvent += OnElementUpdated;
        Wire = wire;
    }

    public void AddConnection(ElectricalElement element, BitDirection direction)
    {
        m_newDynamicConnections.Add(new InterfaceConnectionData(element, direction));
    }

    public bool IsDynamicallyConnected(ElectricalElement element)
    {
        foreach (InterfaceConnectionData dynamicConnection in m_dynamicConnections)
        {
            if (dynamicConnection.Element == element)
            {
                return true;
            }
        }

        return false;
    }

    public void UpdateConnections()
    {
        foreach (InterfaceConnectionData dynamicConnection in m_dynamicConnections)
        {
            ElectricalElement element = dynamicConnection.Element;
            if (element != null && !m_newDynamicConnections.Contains(dynamicConnection) && !IsConnected(element))
            {
                CircuitFactory.Disconnect(Wire, element);
            }
        }

        foreach (InterfaceConnectionData newDynamicConnection in m_newDynamicConnections)
        {
            ElectricalElement element2 = newDynamicConnection.Element;
            if (!CircuitFactory.IsConnected(Wire, element2))
            {
                CircuitFactory.Connect(Wire, element2);
            }
        }

        m_dynamicConnections = new List<InterfaceConnectionData>(m_newDynamicConnections);
        m_newDynamicConnections.Clear();
    }

    private void OnElementUpdated(CircuitSimulator.SimulationResult result)
    {
        m_maxI = Math.Max(Math.Abs(result.I), m_maxI);
        if (!IsElectromagnetic() || result.Electrode == -1 || result.Electrode >= result.Element.Electrodes.Count)
        {
            return;
        }

        ElectricalElement element = result.Element.Electrodes[result.Electrode].Element;
        int count = m_connections.Count;
        int count2 = m_dynamicConnections.Count;
        for (int i = 0; i < count + count2; i++)
        {
            ElectricalElement obj = ((i < count) ? m_connections[i].Element1 : Wire);
            ElectricalElement electricalElement = ((i < count) ? m_connections[i].Element2 : m_dynamicConnections[i - count].Element);
            BitDirection bitDirection = ((i < count) ? m_connections[i].Direction : m_dynamicConnections[i - count].Direction);
            if (obj == result.Element && electricalElement == element)
            {
                int num = 0;
                int num2 = (int)bitDirection;
                while (num2 > 1)
                {
                    num2 >>= 1;
                    num++;
                }

                m_I[num] += result.I;
                break;
            }
        }
    }
}