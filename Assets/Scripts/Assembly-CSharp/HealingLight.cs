using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingLight : PointLight
{
    public float m_HealingRadius;
    public float m_HealingAmount;
    public new void LateUpdate()
    {
        if (this.HasGeneratorRef || !base.activated || GameTime.IsPaused() || m_hp <= 0)
            return;
        Collider[] colliders = Physics.OverlapSphere(base.transform.position, m_HealingRadius);
        foreach (Collider _collider in colliders)
        {
            BasePart part = _collider.GetComponent<BasePart>();
            if ((bool)part)
            {
                float updatedHp = part.m_hp + m_HealingAmount;
                if (updatedHp <= 0f)
                {
                    LowHpDestruction(0f);
                    return;
                }

                Heal(updatedHp, part);
            }
        }
    }

    private static void Heal(float updatedHp, BasePart part)
    {
        if (updatedHp >= part.m_maxHp)
        {
            part.m_hp = part.m_maxHp;
        }
        else if (updatedHp < part.m_maxHp)
        {
            part.m_hp = updatedHp;
        }
    }
}