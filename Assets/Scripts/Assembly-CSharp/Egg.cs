using System.Collections.Generic;
using UnityEngine;

public class Egg : BasePart
{
    private bool m_enabled;
    private bool m_isSpecialEgg;
    private List<Collider> m_colliders;
    private Dictionary<Rigidbody, bool> m_rigidBodyData;
    public override bool CanBeEnclosed()
    {
        return true;
    }

    public override void Initialize()
    {
        base.Initialize();
        int num = customPartIndex;
        m_isSpecialEgg = (((num == 2) || (num == 3)) || (num == 4));
    }

    protected override void OnTouch()
    {
        if (INSettings.GetBool(INFeature.SpecialEggs) && m_isSpecialEgg)
        {
            SetEnabled(!m_enabled);
        }
    }

    public override void SetEnabled(bool enabled)
    {
        if (!INSettings.GetBool(INFeature.SpecialEggs) || !m_isSpecialEgg || m_enabled == enabled)
        {
            return;
        }

        m_enabled = enabled;
        int num = customPartIndex;
        if (num == 0)
        {
            if (m_enabled)
            {
                m_rigidBodyData = new Dictionary<Rigidbody, bool>();
                foreach (BasePart part in base.contraption.Parts)
                {
                    if (part.ConnectedComponent != base.ConnectedComponent)
                    {
                        continue;
                    }

                    foreach (Rigidbody rigidbody in part.GetRigidbodies())
                    {
                        m_rigidBodyData.Add(rigidbody, rigidbody.useGravity);
                        rigidbody.useGravity = false;
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<Rigidbody, bool> rigidBodyDatum in m_rigidBodyData)
                {
                    Rigidbody key = rigidBodyDatum.Key;
                    if (key != null)
                    {
                        key.useGravity = rigidBodyDatum.Value;
                    }
                }

                m_rigidBodyData = null;
            }
        }

        switch (num)
        {
            case 3:
            {
                if (!m_enabled)
                {
                    foreach (Collider collider2 in m_colliders)
                    {
                        if (collider2 != null)
                        {
                            collider2.enabled = true;
                        }
                    }

                    break;
                }

                m_colliders = new List<Collider>();
                Collider[] components = INContraption.Instance.GetComponents<Collider>();
                foreach (Collider collider in components)
                {
                    if (collider.attachedRigidbody != null)
                    {
                        BasePart component = collider.attachedRigidbody.GetComponent<BasePart>();
                        if (component != null && component.ConnectedComponent == base.ConnectedComponent)
                        {
                            collider.enabled = false;
                            m_colliders.Add(collider);
                        }
                    }
                }

                break;
            }

            case 4:
                if (!m_enabled)
                {
                    foreach (Collider collider3 in m_colliders)
                    {
                        if (collider3 != null)
                        {
                            collider3.enabled = true;
                        }
                    }

                    break;
                }

                m_colliders = new List<Collider>();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (!INSettings.GetBool(INFeature.SpecialEggs) || !m_isSpecialEgg || !base.contraption || !base.contraption.IsRunning || !m_enabled || customPartIndex != 4)
        {
            return;
        }

        Vector3 position = base.transform.position;
        foreach (Collider collider2 in m_colliders)
        {
            if (collider2 != null)
            {
                Vector3 position2 = collider2.transform.position;
                float num = position2.x - position.x;
                float num2 = position2.y - position.y;
                if (num * num + num2 * num2 >= 64f)
                {
                    collider2.enabled = true;
                }
            }
        }

        m_colliders.Clear();
        Collider[] components = INContraption.Instance.GetComponents<Collider>();
        foreach (Collider collider in components)
        {
            Vector3 position3 = collider.transform.position;
            float num3 = position3.x - position.x;
            float num4 = position3.y - position.y;
            if (num3 * num3 + num4 * num4 < 64f)
            {
                collider.enabled = false;
                m_colliders.Add(collider);
            }
        }
    }

    public override void PostInitialize()
    {
        if (INSettings.GetBool(INFeature.SpecialEggs) && m_isSpecialEgg)
        {
            int num = customPartIndex;
            if ((((num == 2) || (num == 3)) || (num == 4)))
            {
                base.contraption.ChangeOneShotPartAmount(m_partType, EffectDirection(), 1);
                OnTouch();
            }
        }
    }

    public override bool IsEnabled()
    {
        if (INSettings.GetBool(INFeature.SpecialEggs))
        {
            return m_enabled;
        }

        return false;
    }
}