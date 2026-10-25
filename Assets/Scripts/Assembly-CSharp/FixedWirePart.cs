using System;
using System.Collections.Generic;
using UnityEngine;

public class FixedWirePart : WirePartBase
{
    private GameObject m_wireObjectX;
    private GameObject m_wireObjectY;
    private Wire m_wireX;
    private Wire m_wireY;
    private bool m_invalidX;
    private bool m_invalidY;
    private float m_maxIX;
    private float m_maxIY;
    public override IEnumerable<ElectricalElement> ElectricalElements
    {
        get
        {
            if (m_wireX != null)
            {
                yield return m_wireX;
            }

            if (m_wireY != null)
            {
                yield return m_wireY;
            }
        }
    }

    public override void Awake()
    {
        base.Awake();
        m_autoAlign = (AutoAlignType)(-1);
        m_wireObjectX = base.transform.Find("WireX").gameObject;
        m_wireObjectY = base.transform.Find("WireY").gameObject;
    }

    public override void SetRotation(GridRotation rotation)
    {
        SetRotation((int)rotation);
    }

    public override void SetRotation(int rotation)
    {
        int num = (int)(m_gridRotation = (GridRotation)(rotation % 7));
        int num2;
        float angle;
        if (num <= 1)
        {
            num2 = 0;
            angle = (float)num * 90f;
        }
        else if (num <= 5)
        {
            num2 = 1;
            angle = (float)(num - 2) * 90f;
        }
        else
        {
            num2 = 0;
            angle = 0f;
        }

        INSerializedSprite component = m_wireObjectX.GetComponent<INSerializedSprite>();
        component.SpriteName = "Wire2_Sprite_" + (num2 + 1);
        component.UpdateMesh();
        m_wireObjectX.SetActive(value: true);
        m_wireObjectY.SetActive(num == 6);
        m_wireObjectX.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public override bool IsElectromagnetic()
    {
        return customPartIndex == 3;
    }

    public override void CreateElectricalElements()
    {
        GetRotation();
        if (m_wireObjectX.activeSelf)
        {
            m_wireX = new Wire();
            m_wireX.ElementUpdatedEvent += OnElementUpdatedX;
        }

        if (m_wireObjectY.activeSelf)
        {
            m_wireY = new Wire();
            m_wireY.ElementUpdatedEvent += OnElementUpdatedY;
        }
    }

    protected override BitDirection GetConnectionDirection()
    {
        int rotation = GetRotation();
        if (rotation <= 1)
        {
            return BitDirection.LeftAndRight.Rotate(rotation);
        }

        if (rotation <= 5)
        {
            return ((BitDirection)9).Rotate(rotation - 2);
        }

        return BitDirection.Any;
    }

    public override ElectricalElement GetElectricalElementByDirection(BitDirection direction)
    {
        if (GetRotation() <= 5)
        {
            return m_wireX;
        }

        if ((direction & BitDirection.LeftAndRight) != 0)
        {
            return m_wireX;
        }

        return m_wireY;
    }

    public override void PreUpdateElements()
    {
        m_maxIX = 0f;
        m_maxIY = 0f;
    }

    private void OnElementUpdatedX(CircuitSimulator.SimulationResult result)
    {
        OnElementUpdatedBase(result);
        m_maxIX = Math.Max(Math.Abs(result.I), m_maxIX);
        float u = result.U;
        u = ((u >= 0f) ? u : 0f);
        u /= u + 1f;
        u = u * 0.7f + 0.3f;
        if (!result.IsGrounded)
        {
            u = 0.6f;
        }

        m_wireObjectX.GetComponent<MeshRenderer>().material.color = new Color(u, u, u, 1f);
    }

    private void OnElementUpdatedY(CircuitSimulator.SimulationResult result)
    {
        OnElementUpdatedBase(result);
        m_maxIY = Math.Max(Math.Abs(result.I), m_maxIY);
        float u = result.U;
        u = ((u >= 0f) ? u : 0f);
        u /= u + 1f;
        u = u * 0.7f + 0.3f;
        if (!result.IsGrounded)
        {
            u = 0.6f;
        }

        m_wireObjectY.GetComponent<MeshRenderer>().material.color = new Color(u, u, u, 1f);
    }

    public override void PostUpdateElements()
    {
        if (m_maxIX > 50000f && !m_invalidX)
        {
            m_invalidX = true;
            ToGray(m_wireObjectX, gray: true);
            RemoveConnections((ConnectionData connection) => (connection.Direction & BitDirection.LeftAndRight) != 0);
        }

        if (m_maxIY > 50000f && !m_invalidY)
        {
            m_invalidY = true;
            ToGray(m_wireObjectY, gray: true);
            RemoveConnections((ConnectionData connection) => (connection.Direction & BitDirection.UpAndDown) != 0);
        }
    }
}