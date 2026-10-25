using System.Collections.Generic;
using UnityEngine;

public class SPDTSwitchPart : ElectricalPart
{
    private GameObject m_sprite;
    private SPDTSwitch m_switch;
    private bool m_closed;
    public override IEnumerable<ElectricalElement> ElectricalElements => m_switch.ToEnumerable();

    public override void Awake()
    {
        base.Awake();
        m_autoAlign = (AutoAlignType)(-1);
        m_sprite = base.transform.Find("Sprite").gameObject;
    }

    public override bool IsEnabled()
    {
        return m_closed;
    }

    public override bool IsTriggerable()
    {
        return !base.HasGeneratorRef;
    }

    public override IEnumerable<UIPartTriggerButtonInfo> GetTriggerButtonInfo()
    {
        yield return new UIPartTriggerButtonInfo(UIPartButtonType.Trigger, 0, base.Type, 1, base.ConnectedComponent, consistent: false);
    }

    protected override void OnTouch()
    {
        m_closed = !m_closed;
        m_switch.SetClosed(m_closed);
        int gridRotation = (int)m_gridRotation;
        bool num = m_flipped ^ m_closed;
        int num2 = ((num && ((gridRotation == 0) || (gridRotation == 2))) ? 180 : 0);
        int num3 = ((num && ((gridRotation == 1) || (gridRotation == 3))) ? 180 : 0);
        int num4 = 90 * gridRotation;
        m_sprite.transform.localRotation = Quaternion.Euler(num2, num3, num4);
    }

    public override void SetRotation(GridRotation rotation)
    {
        SetRotation(GetRotation(rotation, m_flipped));
    }

    public override void SetFlipped(bool flipped)
    {
        SetRotation(GetRotation(m_gridRotation, flipped));
    }

    public override int GetRotation()
    {
        return GetRotation(m_gridRotation, m_flipped);
    }

    private int GetRotation(GridRotation rotation, bool flipped)
    {
        return (int)rotation * 2 + (flipped ? 1 : 0);
    }

    public override void SetRotation(int rotation)
    {
        int num = rotation % 8;
        int num2 = num / 2;
        bool flag = (m_flipped = num % 2 == 1);
        m_gridRotation = (GridRotation)num2;
        int num3 = ((flag && ((num2 == 0) || (num2 == 2))) ? 180 : 0);
        int num4 = ((flag && ((num2 == 1) || (num2 == 3))) ? 180 : 0);
        int num5 = 90 * num2;
        m_sprite.transform.localRotation = Quaternion.Euler(num3, num4, num5);
    }

    public override void CreateElectricalElements()
    {
        m_switch = new SPDTSwitch();
    }

    public override void ConnectElectricalElements()
    {
        BitDirection direction = ((!m_flipped) ? BitDirection.Up : BitDirection.Down);
        foreach (ConnectionData connection in m_connections)
        {
            ElectricalElement element = connection.Element1;
            ElectricalElement element2 = connection.Element2;
            int type = ((connection.Direction != BitDirection.Left.Rotate((int)m_gridRotation)) ? ((connection.Direction == direction.Rotate((int)m_gridRotation)) ? 1 : 2) : 0);
            element.AddConnectedElement(element2, type);
        }
    }

    protected override BitDirection GetConnectionDirection()
    {
        return ((BitDirection)14).Rotate((int)m_gridRotation);
    }

    public override ElectricalElement GetElectricalElementByDirection(BitDirection direction)
    {
        return base.GetElectricalElementByDirection(direction);
    }
}