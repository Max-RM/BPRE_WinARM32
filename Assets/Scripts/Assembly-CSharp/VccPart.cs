using System.Collections.Generic;
using UnityEngine;

public class VccPart : ElectricalPart
{
    public GameObject m_inactiveSprite;
    public GameObject m_activeSprite;
    public Vcc m_vcc;
    public bool m_enabled;
    public override IEnumerable<ElectricalElement> ElectricalElements => m_vcc.ToEnumerable();

    public override void Awake()
    {
        base.Awake();
        m_inactiveSprite = base.transform.Find("InactiveSprite").gameObject;
        m_activeSprite = base.transform.Find("ActiveSprite").gameObject;
    }

    public override bool IsEnabled()
    {
        return m_enabled;
    }

    public override bool IsTriggerable()
    {
        return !base.HasGeneratorRef;
    }

    public override IEnumerable<UIPartTriggerButtonInfo> GetTriggerButtonInfo()
    {
        yield return new UIPartTriggerButtonInfo(UIPartButtonType.Trigger, 0, base.Type, 3, base.ConnectedComponent, consistent: false);
    }

    protected override void OnTouch()
    {
        m_enabled = !m_enabled;
        m_activeSprite.SetActive(m_enabled);
        m_inactiveSprite.SetActive(!m_enabled);
        m_vcc.Potential = (m_enabled ? 5f : 0f);
        m_vcc.Resistance = (m_enabled ? 0.05f : 0f);
    }

    public override void CreateElectricalElements()
    {
        m_vcc = new Vcc(0f, 0f);
    }

    protected override BitDirection GetConnectionDirection()
    {
        return BitDirection.Right.Rotate((int)m_gridRotation);
    }
}