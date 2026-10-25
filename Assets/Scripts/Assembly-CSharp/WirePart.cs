using System;
using System.Collections.Generic;
using UnityEngine;

public class WirePart : WirePartBase
{
    [SerializeField]
    private int m_autoRotationIndex;
    private GameObject m_sprite;
    private Wire m_wire;
    private static Dictionary<(byte, byte, byte, byte), int> s_rotationMap;
    public override IEnumerable<ElectricalElement> ElectricalElements => m_wire.ToEnumerable();

    static WirePart()
    {
        s_rotationMap = new Dictionary<(byte, byte, byte, byte), int>
        {
            [(0, 0, 0, 0)] = 10,
            [(1, 0, 0, 0)] = 0,
            [(0, 1, 0, 0)] = 1,
            [(0, 0, 1, 0)] = 0,
            [(0, 0, 0, 1)] = 1,
            [(1, 0, 1, 0)] = 0,
            [(0, 1, 0, 1)] = 1,
            [(1, 0, 0, 1)] = 2,
            [(1, 1, 0, 0)] = 3,
            [(0, 1, 1, 0)] = 4,
            [(0, 0, 1, 1)] = 5,
            [(1, 1, 1, 0)] = 6,
            [(0, 1, 1, 1)] = 7,
            [(1, 0, 1, 1)] = 8,
            [(1, 1, 0, 1)] = 9,
            [(1, 1, 1, 1)] = 10
        };
    }

    public override void Awake()
    {
        base.Awake();
        m_autoAlign = (AutoAlignType)(-1);
        m_sprite = base.transform.Find("WireSprite").gameObject;
    }

    public override void ChangeVisualConnections()
    {
        int autoRotationIndex = GetAutoRotationIndex();
        SetAutoRotationIndex(autoRotationIndex);
    }

    public override void SetRotation(GridRotation rotation)
    {
    }

    private int GetAutoRotationIndex()
    {
        _ = m_coordX;
        _ = m_coordY;
        ElectricalPart electricalPart = FindConnectedPart(1, 0, BitDirection.Right);
        ElectricalPart electricalPart2 = FindConnectedPart(0, 1, BitDirection.Up);
        ElectricalPart electricalPart3 = FindConnectedPart(-1, 0, BitDirection.Left);
        ElectricalPart electricalPart4 = FindConnectedPart(0, -1, BitDirection.Down);
        return s_rotationMap[(Convert.ToByte((object)electricalPart != null), Convert.ToByte((object)electricalPart2 != null), Convert.ToByte((object)electricalPart3 != null), Convert.ToByte((object)electricalPart4 != null))];
    }

    private void SetAutoRotationIndex(int rotationIndex)
    {
        if (rotationIndex == m_autoRotationIndex)
        {
            return;
        }

        float angle = ((rotationIndex <= 1) ? ((float)rotationIndex * 90f) : ((rotationIndex <= 5) ? ((float)(rotationIndex - 2) * 90f) : ((rotationIndex > 9) ? 0f : ((float)(rotationIndex - 6) * 90f))));
        m_sprite.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        int num = customPartIndex;
        int num2 = ((m_autoRotationIndex > 1) ? ((m_autoRotationIndex <= 5) ? 1 : ((m_autoRotationIndex <= 9) ? 2 : 3)) : 0);
        int num3 = ((rotationIndex > 1) ? ((rotationIndex <= 5) ? 1 : ((rotationIndex <= 9) ? 2 : 3)) : 0);
        if (num2 != num3 || m_autoRotationIndex == -1)
        {
            string spriteName = "Wire" + (num + 1) + "_Sprite_" + (num3 + 1);
            INSerializedSprite component = m_sprite.GetComponent<INSerializedSprite>();
            component.SpriteName = spriteName;
            component.UpdateMesh();
            BoxCollider component2 = GetComponent<BoxCollider>();
            switch (num3)
            {
                case 1:
                    component2.center = new Vector3(0.15f, -0.15f, 0f);
                    component2.size = new Vector3(0.7f, 0.7f, 1f);
                    break;
                case 2:
                    component2.center = new Vector3(0f, 0.1f, 0f);
                    component2.size = new Vector3(1f, 0.8f, 1f);
                    break;
                case 3:
                    component2.center = new Vector3(0f, 0f, 0f);
                    component2.size = new Vector3(1f, 1f, 1f);
                    break;
            }
        }

        m_autoRotationIndex = rotationIndex;
    }

    protected override BitDirection GetConnectionDirection()
    {
        return BitDirection.Any;
    }

    public override bool IsElectromagnetic()
    {
        return customPartIndex == 2;
    }

    public override void CreateElectricalElements()
    {
        Wire wire = new Wire();
        wire.ElementUpdatedEvent += OnElementUpdated;
        m_wire = wire;
    }

    private void OnElementUpdated(CircuitSimulator.SimulationResult result)
    {
        OnElementUpdatedBase(result);
        float u = result.U;
        u = ((u >= 0f) ? u : 0f);
        u /= u + 1f;
        u = u * 0.7f + 0.3f;
        if (!result.IsGrounded)
        {
            u = 0.6f;
        }

        m_sprite.GetComponent<MeshRenderer>().material.color = new Color(u, u, u, 1f);
    }
}