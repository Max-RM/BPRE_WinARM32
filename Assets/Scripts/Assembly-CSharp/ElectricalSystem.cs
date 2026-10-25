using System;
using System.Collections.Generic;
using UnityEngine;

public class ElectricalSystem : PartManager
{
    private List<ElectricalPart> m_electricalParts;
    private List<ElectricalElement> m_electricalElements;
    private List<InterfacePart> m_interfaces;
    private CircuitSimulator m_circuitSimulator;
    public static ElectricalSystem Instance { get; private set; }

    protected override void Initialize()
    {
        base.Initialize();
        m_status = StatusCode.Running;
        Instance = this;
    }

    public override void Start()
    {
        m_electricalParts = new List<ElectricalPart>();
        m_electricalElements = new List<ElectricalElement>();
        m_interfaces = new List<InterfacePart>();
        m_circuitSimulator = new CircuitSimulator(Time.fixedDeltaTime);
        InitializeElectricalParts(Contraption.Instance.Parts);
    }

    public void InitializeElectricalParts(List<BasePart> parts)
    {
        GetElectricalParts(parts, m_electricalParts);
        foreach (ElectricalPart electricalPart in m_electricalParts)
        {
            electricalPart.CreateElectricalElements();
        }

        GetElectricalElements(m_electricalParts, m_electricalElements);
        foreach (ElectricalPart electricalPart2 in m_electricalParts)
        {
            electricalPart2.InitializeConnections();
        }

        foreach (ElectricalPart electricalPart3 in m_electricalParts)
        {
            electricalPart3.ConnectElectricalElements();
        }

        foreach (ElectricalPart electricalPart4 in m_electricalParts)
        {
            electricalPart4.InitializeElectricalElements();
        }

        m_circuitSimulator.Initialize(m_electricalElements);
        m_electricalParts.Clear();
        m_electricalElements.Clear();
    }

    private void GetElectricalParts(List<BasePart> parts, List<ElectricalPart> electricalParts)
    {
        CheckEmptyList(electricalParts, "electricalParts");
        foreach (BasePart part in parts)
        {
            if ((part is ElectricalPart item))
            {
                electricalParts.Add(item);
            }
        }
    }

    private void GetElectricalElements(List<ElectricalPart> electricalParts, List<ElectricalElement> elements)
    {
        CheckEmptyList(elements, "elements");
        foreach (ElectricalPart electricalPart in electricalParts)
        {
            if (electricalPart.ElectricalElements == null)
            {
                continue;
            }

            foreach (ElectricalElement electricalElement in electricalPart.ElectricalElements)
            {
                if (electricalElement != null)
                {
                    elements.Add(electricalElement);
                }
            }
        }
    }

    private List<T> SelectElectricalParts<T>()
        where T : ElectricalPart
    {
        List<T> list = new List<T>();
        SelectElectricalParts(list);
        return list;
    }

    private void SelectElectricalParts<T>(List<T> list)
        where T : ElectricalPart
    {
        foreach (ElectricalPart electricalPart in m_electricalParts)
        {
            if ((electricalPart is T item))
            {
                list.Add(item);
            }
        }
    }

    public void ResetLists()
    {
        m_electricalParts = new List<ElectricalPart>();
        m_electricalElements = new List<ElectricalElement>();
        m_interfaces = new List<InterfacePart>();
    }

    public override void FixedUpdate()
    {
        List<BasePart> parts = Contraption.Instance.Parts;
        //ResetLists();
        GetElectricalParts(parts, m_electricalParts);
        GetElectricalElements(m_electricalParts, m_electricalElements);
        if (m_electricalParts.Count == 0)
        {
            return;
        }

        foreach (ElectricalPart electricalPart in m_electricalParts)
        {
            electricalPart.RemoveInvalidConnections();
        }

        UpdateInterfaces();
        UpdatePowerTransmitters();
        foreach (ElectricalPart electricalPart2 in m_electricalParts)
        {
            electricalPart2.PreUpdateElements();
        }

        m_circuitSimulator.Simulate(m_electricalElements);
        foreach (ElectricalPart electricalPart3 in m_electricalParts)
        {
            electricalPart3.PostUpdateElements();
        }

        UpdatePointCharges();
        UpdateMagneticFieldGenerators();
        m_electricalParts.Clear();
        m_electricalElements.Clear();
    }

    private void UpdateInterfaces()
    {
        foreach (ElectricalPart electricalPart in m_electricalParts)
        {
            if ((electricalPart is InterfacePart item))
            {
                m_interfaces.Add(item);
            }
        }

        foreach (InterfacePart @interface in m_interfaces)
        {
            @interface.UpdateConnections();
        }

        m_interfaces.Clear();
    }

    private void UpdatePowerTransmitters()
    {
        List<PowerTransmitterPart> senders = new List<PowerTransmitterPart>();
        List<PowerTransmitterPart> receivers = new List<PowerTransmitterPart>();
        foreach (ElectricalPart electricalPart in m_electricalParts)
        {
            if ((electricalPart is PowerTransmitterPart powerTransmitterPart))
            {
                if (powerTransmitterPart.IsSender)
                {
                    senders.Add(powerTransmitterPart);
                }
                else
                {
                    receivers.Add(powerTransmitterPart);
                }
            }
        }

        if (!GameRules.ChanneledRadio)
        {
            foreach (PowerTransmitterPart receiver in receivers)
            {
                Vector3 position = receiver.transform.position;
                float num = 256f;
                PowerTransmitterPart other = null;
                foreach (PowerTransmitterPart sender in senders)
                {
                    Vector3 position2 = sender.transform.position;
                    float num2 = Vector.DistanceSquared(position, position2);
                    if (!(num2 < num))
                        continue;
                    num = num2;
                    other = sender;
                }

                receiver.Connect(other, Mathf.Sqrt(num));
            }
        }
        else
        {
            foreach (PowerTransmitterPart receiver in receivers)
            {
                Vector3 position = receiver.transform.position;
                float num = 256f;
                foreach (PowerTransmitterPart sender in senders)
                {
                    Vector3 position2 = sender.transform.position;
                    float num2 = Vector.DistanceSquared(position, position2);
                    if (!(num2 < num))
                        continue;
                    num = num2;
                    if (IsOnSameChannel(receiver, sender))
                        receiver.Connect(sender, Mathf.Sqrt(num));
                }
            }
        }

        return;
        bool IsOnSameChannel(PowerTransmitterPart t, PowerTransmitterPart other)
        {
            if (!GameRules.ChanneledRadio)
                return true;
            return (((t.enclosedInto == null) && ((other.enclosedInto == null))) ? true : ((!(t.enclosedInto == null)) && ((other.enclosedInto == null))) ? false : ((t.enclosedInto == null) && ((!(other.enclosedInto == null)))) ? false : ((!(t.enclosedInto == null)) && (t.enclosedInto.m_partType is BasePart.PartType.WoodenFrame || other.enclosedInto.m_partType is BasePart.PartType.WoodenFrame)) ? true : ((t.enclosedInto is ColoredFrame coloredFrame) && ((other.enclosedInto is ColoredFrame otherEnclosedInto))) ? coloredFrame.customPartIndex == otherEnclosedInto.customPartIndex : false);
        }
    }

    private void UpdatePointCharges()
    {
        List<PointChargePart> list = SelectElectricalParts<PointChargePart>();
        List<ElectricFieldGenerator> list2 = SelectElectricalParts<ElectricFieldGenerator>();
        List<MagneticFieldGenerator> list3 = SelectElectricalParts<MagneticFieldGenerator>();
        for (int i = 0; i < list.Count; i++)
        {
            PointChargePart pointChargePart = list[i];
            Vector3 position = pointChargePart.transform.position;
            for (int j = i + 1; j < list.Count; j++)
            {
                PointChargePart pointChargePart2 = list[j];
                Vector3 position2 = pointChargePart2.transform.position;
                float num = position2.x - position.x;
                float num2 = position2.y - position.y;
                float num3 = num * num + num2 * num2;
                if (num3 < 4096f)
                {
                    float num4 = Mathf.Sqrt(num3);
                    num4 = ((num4 > 1f) ? num4 : 1f);
                    float num5 = 1.5f * pointChargePart.Charge * pointChargePart2.Charge / (num4 * num4 * num4);
                    pointChargePart.rigidbody.AddForce(new Vector3((0f - num5) * num, (0f - num5) * num2));
                    pointChargePart2.rigidbody.AddForce(new Vector3(num5 * num, num5 * num2));
                }
            }
        }

        foreach (ElectricFieldGenerator item in list2)
        {
            if (!item.IsEnabled())
            {
                continue;
            }

            Vector3 up = item.transform.up;
            Vector3 position3 = item.transform.position;
            foreach (PointChargePart item2 in list)
            {
                Vector3 position4 = item2.transform.position;
                float num6 = Vector.DistanceSquared(position3, position4);
                if (!(num6 > 64f))
                {
                    float num7 = item.GetElectricFieldIntensityByDistance(Mathf.Sqrt(num6)) * item2.Charge;
                    item2.rigidbody.AddForce(new Vector3(num7 * up.x, num7 * up.y));
                    item.rigidbody.AddForce(new Vector3((0f - num7) * up.x, (0f - num7) * up.y));
                }
            }
        }

        foreach (MagneticFieldGenerator item3 in list3)
        {
            if (!item3.IsEnabled())
            {
                continue;
            }

            Vector3 position5 = item3.transform.position;
            Vector3 velocity = item3.rigidbody.velocity;
            _ = item3.transform.right;
            foreach (PointChargePart item4 in list)
            {
                Vector3 velocity2 = item4.rigidbody.velocity;
                float num8 = velocity2.x - velocity.x;
                float num9 = velocity2.y - velocity.y;
                float distance = Vector.Distance(position5, item4.transform.position);
                float num10 = item3.GetMagneticFluxDensityByDistance(distance) * item4.Charge;
                item4.rigidbody.AddForce(new Vector3((0f - num10) * num9, num10 * num8));
                item3.rigidbody.AddForce(new Vector3(num10 * num9, (0f - num10) * num8));
            }
        }
    }

    private void UpdateMagneticFieldGenerators()
    {
        List<WirePartBase> list = new List<WirePartBase>();
        foreach (ElectricalPart electricalPart in m_electricalParts)
        {
            if (!((electricalPart is WirePartBase wirePartBase)) || !wirePartBase.IsElectromagnetic())
            {
                continue;
            }

            bool flag = false;
            float[] i = wirePartBase.I;
            for (int j = 0; j < i.Length; j++)
            {
                if (i[j] > 0f)
                {
                    flag = true;
                    break;
                }
            }

            if (flag)
            {
                list.Add(wirePartBase);
            }
        }

        foreach (ElectricalPart electricalPart2 in m_electricalParts)
        {
            if (!((electricalPart2 is MagneticFieldGenerator magneticFieldGenerator)) || !magneticFieldGenerator.IsEnabled())
            {
                continue;
            }

            Vector3 position = magneticFieldGenerator.transform.position;
            foreach (WirePartBase item in list)
            {
                Vector3 position2 = item.transform.position;
                float num = Vector.DistanceSquared(position, position2);
                if (!(num > 64f))
                {
                    Vector3 right = item.transform.right;
                    Vector3 vector = new Vector2(0f - right.y, right.x);
                    float[] i2 = item.I;
                    Vector3 vector2 = 0.5f * (i2[0] - i2[2]) * vector + 0.5f * (i2[1] - i2[3]) * right;
                    vector2 *= magneticFieldGenerator.GetMagneticFluxDensityByDistance(Mathf.Sqrt(num));
                    item.rigidbody.AddForce(vector2);
                    magneticFieldGenerator.rigidbody.AddForce(-vector2);
                }
            }
        }
    }

    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void CheckEmptyList<T>(List<T> list, string name)
    {
        if (list == null)
        {
            throw new ArgumentNullException(name);
        }

        if (list.Count > 0)
        {
            throw new ArgumentException("List is not empty.", name);
        }
    }
}