using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Oscillator : VccPart
{
    private int _oscillatorStatus = -1;
    private int frameElapsed = 0;
    public int framePeriod;
    public override void Awake()
    {
        base.Awake();
        _oscillatorStatus = -1;
        frameElapsed = 0;
    }

    protected override void OnTouch()
    {
        m_enabled = !m_enabled;
        m_activeSprite.SetActive(m_enabled);
        m_inactiveSprite.SetActive(!m_enabled);
        m_vcc.Potential = (m_enabled ? 5f : 0f);
        m_vcc.Resistance = (m_enabled ? 0.05f : 0f);
    }

    protected void FixedUpdate()
    {
        if (!m_enabled)
            return;
        if (frameElapsed < framePeriod)
        {
            frameElapsed++;
            return;
        }
        else
        {
            _oscillatorStatus = -_oscillatorStatus;
            m_vcc.Potential = (_oscillatorStatus == 1 ? 5f : 0f);
            m_vcc.Resistance = (_oscillatorStatus == 1 ? 0.05f : 0f);
            frameElapsed = 0;
        }
    }
}