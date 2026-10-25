using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ElectricalPart : BasePart
{
    public enum LogicLevel
    {
        Invalid = 0,
        Low = 1,
        High = 2
    }

    protected struct ConnectionData
    {
        public ElectricalPart Part1;
        public ElectricalPart Part2;
        public ElectricalElement Element1;
        public ElectricalElement Element2;
        public Joint Joint1;
        public Joint Joint2;
        public BitDirection Direction;
        public bool IsInvalid()
        {
            if (!(Part1 == null) && !(Part2 == null))
            {
                if (Joint1 == null)
                {
                    return Joint2 == null;
                }

                return false;
            }

            return true;
        }

        public ConnectionData(ElectricalPart part1, ElectricalPart part2, BitDirection direction) : this(part1, part2, null, null, null, null, direction)
        {
        }

        public ConnectionData(ElectricalPart part1, ElectricalPart part2, ElectricalElement element1, ElectricalElement element2, Joint joint1, Joint joint2, BitDirection direction)
        {
            Part1 = part1;
            Part2 = part2;
            Element1 = element1;
            Element2 = element2;
            Joint1 = joint1;
            Joint2 = joint2;
            Direction = direction;
        }
    }

    protected bool m_invalid;
    protected List<ConnectionData> m_connections;
    public bool Invalid => m_invalid;
    public virtual IEnumerable<ElectricalElement> ElectricalElements { get; }

    public override void Awake()
    {
        m_connections = new List<ConnectionData>();
        m_ZOffset = 0.01f;
        m_jointConnectionDirection = ((this is WirePart || this is FixedWirePart || this is PointChargePart) ? JointConnectionDirection.None : JointConnectionDirection.Any);
    }

    public override void CreateCustomJoints()
    {
        BasePart[] array = FindConnectedPartArray();
        if (array == null)
        {
            return;
        }

        for (int i = 0; i < array.Length; i++)
        {
            BasePart basePart = array[i];
            if (!(basePart == null))
            {
                base.contraption.AddFixedJoint(this, basePart);
                if ((basePart is ElectricalPart part))
                {
                    BitDirection direction = (BitDirection)(1 << i);
                    ConnectionData item = new ConnectionData(this, part, direction);
                    m_connections.Add(item);
                }
            }
        }
    }

    public override void LowHpDestruction(float passDamage)
    {
        void ColorToGray(bool gray)
        {
            Shader shader = INUnity.LoadShader(gray ? "PreAlpha_Unlit_ColorTransparent_Geometry_Gray" : "PreAlpha_Unlit_ColorTransparent_Geometry");
            MeshRenderer[] componentsInChildren = this.gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer t in componentsInChildren)
            {
                t.material.shader = shader;
            }
        }

        switch (GameRules.PartHPStatus)
        {
            case 0:
                return;
            case 1:
                ColorToGray(true);
                SetInvalid(true);
                collider.enabled = false;
                gameObject.SetActive(false);
                return;
        }

        m_minDamage = -1f;
        Joint[] joints = this.gameObject.GetComponents<Joint>();
        foreach (Joint V in joints)
        {
            if (V)
                Destroy(V);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, passDamage * 0.04f + 0.5f);
        foreach (Collider _collider in colliders)
        {
            BasePart basePart = _collider.GetComponent<BasePart>();
            if (basePart)
            {
                Physics.IgnoreCollision(_collider, collider);
            }
        }

        ColorToGray(true);
        SetInvalid(true);
    }

    protected virtual BitDirection GetConnectionDirection()
    {
        return BitDirection.None;
    }

    protected virtual List<BasePart> FindConnectedParts()
    {
        ElectricalPart electricalPart = FindConnectedPart(1, 0, BitDirection.Right);
        ElectricalPart electricalPart2 = FindConnectedPart(0, 1, BitDirection.Up);
        ElectricalPart electricalPart3 = FindConnectedPart(-1, 0, BitDirection.Left);
        ElectricalPart electricalPart4 = FindConnectedPart(0, -1, BitDirection.Down);
        List<BasePart> list = new List<BasePart>();
        if ((object)electricalPart != null)
        {
            list.Add(electricalPart);
        }

        if ((object)electricalPart3 != null)
        {
            list.Add(electricalPart3);
        }

        if ((object)electricalPart2 != null)
        {
            list.Add(electricalPart2);
        }

        if ((object)electricalPart4 != null)
        {
            list.Add(electricalPart4);
        }

        return list;
    }

    protected virtual BasePart[] FindConnectedPartArray()
    {
        ElectricalPart electricalPart = FindConnectedPart(1, 0, BitDirection.Right);
        ElectricalPart electricalPart2 = FindConnectedPart(0, 1, BitDirection.Up);
        ElectricalPart electricalPart3 = FindConnectedPart(-1, 0, BitDirection.Left);
        ElectricalPart electricalPart4 = FindConnectedPart(0, -1, BitDirection.Down);
        BasePart[] array = new BasePart[4];
        if ((object)electricalPart != null)
        {
            array[0] = electricalPart;
        }

        if ((object)electricalPart2 != null)
        {
            array[1] = electricalPart2;
        }

        if ((object)electricalPart3 != null)
        {
            array[2] = electricalPart3;
        }

        if ((object)electricalPart4 != null)
        {
            array[3] = electricalPart4;
        }

        return array;
    }

    protected ElectricalPart FindConnectedPart(int x, int y, BitDirection direction)
    {
        BasePart basePart = base.contraption.FindPartAt(m_coordX + x, m_coordY + y, this);
        if (basePart != null)
        {
            if (basePart.m_enclosedPart != null)
            {
                basePart = basePart.m_enclosedPart;
            }

            if (CanConnectTo(basePart, direction))
            {
                return (ElectricalPart)basePart;
            }
        }

        return null;
    }

    public bool CanConnectTo(BasePart part, BitDirection direction)
    {
        if ((part is ElectricalPart electricalPart) && (GetConnectionDirection() & electricalPart.GetConnectionDirection().Reverse() & direction) != 0)
        {
            return true;
        }

        return false;
    }

    public bool IsConnected(ElectricalElement element)
    {
        foreach (ConnectionData connection in m_connections)
        {
            if (connection.Element2 == element)
            {
                return true;
            }
        }

        return false;
    }

    public virtual ElectricalElement GetElectricalElementByDirection(BitDirection direction)
    {
        return ElectricalElements.First();
    }

    public virtual void CreateElectricalElements()
    {
    }

    public virtual void ConnectElectricalElements()
    {
        if (m_connections == null)
        {
            return;
        }

        foreach (ConnectionData connection in m_connections)
        {
            ElectricalElement element = connection.Element1;
            ElectricalElement element2 = connection.Element2;
            element.AddConnectedElement(element2);
        }
    }

    public virtual void InitializeElectricalElements()
    {
    }

    public virtual void PreUpdateElements()
    {
    }

    public virtual void PostUpdateElements()
    {
    }

    public void InitializeConnections()
    {
        for (int i = 0; i < m_connections.Count; i++)
        {
            ConnectionData value = m_connections[i];
            ElectricalPart part = value.Part1;
            ElectricalPart part2 = value.Part2;
            Rigidbody rigidbody = part.rigidbody;
            Rigidbody rigidbody2 = part2.rigidbody;
            BitDirection direction = value.Direction;
            value.Element1 = part.GetElectricalElementByDirection(direction);
            value.Element2 = part2.GetElectricalElementByDirection(direction.Reverse());
            value.Joint1 = rigidbody.FindSpecifiedJoint(rigidbody2);
            value.Joint2 = rigidbody2.FindSpecifiedJoint(rigidbody);
            m_connections[i] = value;
        }
    }

    public void DisableAllConnections()
    {
        foreach (ElectricalElement electricalElement in ElectricalElements)
        {
            List<ElectricalElement.Electrode> electrodes = electricalElement.Electrodes;
            for (int i = 0; i < electrodes.Count; i++)
            {
                ElectricalElement.Electrode value = electrodes[i];
                value.SetConnected(connected: false);
                electrodes[i] = value;
            }
        }
    }

    public void RemoveAllConnections()
    {
        foreach (ConnectionData connection in m_connections)
        {
            CircuitFactory.Disconnect(connection.Element1, connection.Element2);
        }

        m_connections.Clear();
    }

    public void RemoveInvalidConnections()
    {
        foreach (ConnectionData connection in m_connections)
        {
            if (connection.IsInvalid())
            {
                CircuitFactory.Disconnect(connection.Element1, connection.Element2);
            }
        }

        m_connections.RemoveAll((ConnectionData connection) => connection.IsInvalid());
    }

    protected void RemoveConnections(Predicate<ConnectionData> match)
    {
        foreach (ConnectionData connection in m_connections)
        {
            if (match(connection))
            {
                CircuitFactory.Disconnect(connection.Element1, connection.Element2);
            }
        }

        m_connections.RemoveAll(match);
    }

    private void OnDestroy()
    {
        RemoveAllConnections();
    }

    protected void SetInvalid(bool invalid)
    {
        if (m_invalid != invalid)
        {
            m_invalid = invalid;
            ToGray(base.gameObject, invalid);
        }
    }

    protected void ToGray(GameObject gameObject, bool gray)
    {
        Shader shader = INUnity.LoadShader(gray ? "PreAlpha_Unlit_ColorTransparent_Geometry_Gray" : "PreAlpha_Unlit_ColorTransparent_Geometry");
        MeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < componentsInChildren.Length; i++)
        {
            componentsInChildren[i].material.shader = shader;
        }
    }

    public static LogicLevel GetLogicLevel(float voltage)
    {
        if (-10f <= voltage && voltage <= 2.5f)
        {
            return LogicLevel.Low;
        }

        if (2.5f < voltage && voltage <= 10f)
        {
            return LogicLevel.High;
        }

        return LogicLevel.Invalid;
    }
}