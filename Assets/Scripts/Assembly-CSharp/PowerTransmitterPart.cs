using System;
using System.Collections.Generic;

public class PowerTransmitterPart : ElectricalPart
{
    public enum PowerTransmitterType
    {
        Sender = 0,
        Receiver = 1
    }

    private ElectricalElement m_element;
    private PowerTransmitterPart m_connectedPart;
    public bool IsSender => TransmitterType == PowerTransmitterType.Sender;
    public bool IsReceiver => TransmitterType == PowerTransmitterType.Receiver;
    public PowerTransmitterType TransmitterType => (PowerTransmitterType)(customPartIndex - 40);
    public override IEnumerable<ElectricalElement> ElectricalElements => m_element.ToEnumerable();

    public override void Awake()
    {
        base.Awake();
    }

    public override void CreateElectricalElements()
    {
        m_element = ((TransmitterType == PowerTransmitterType.Sender) ? new Wire() : (TransmitterType == PowerTransmitterType.Receiver) ? new Resistor(0f) : m_element);
    }

    protected override BitDirection GetConnectionDirection()
    {
        return ((TransmitterType == PowerTransmitterType.Sender) ? BitDirection.Down.Rotate((int)m_gridRotation) : (TransmitterType == PowerTransmitterType.Receiver) ? BitDirection.Up.Rotate((int)m_gridRotation) : BitDirection.None);
    }

    public void Connect(PowerTransmitterPart other, float distance)
    {
        if (m_connectedPart != other)
        {
            if (m_connectedPart != null)
            {
                CircuitFactory.Disconnect(m_element, m_connectedPart.m_element);
            }

            if (other != null)
            {
                CircuitFactory.Connect(m_element, other.m_element);
            }

            m_connectedPart = other;
        }

        Resistor resistor = (Resistor)m_element;
        if (other == null)
        {
            resistor.Resistance = 0f;
            return;
        }

        distance = Math.Max(distance - 1f, 0f);
        resistor.Resistance = 0.2f * distance * distance + 0.1f;
    }
}