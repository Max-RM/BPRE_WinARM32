using System;
using System.Collections.Generic;
using UnityEngine;

public class IntegratedCircuitPart : ElectricalPart
{
    public enum ICType
    {
        BUF = 0,
        NOT = 1,
        OR1 = 2,
        OR2 = 3,
        NOR1 = 4,
        NOR2 = 5,
        AND1 = 6,
        AND2 = 7,
        NAND1 = 8,
        NAND2 = 9,
        NMOS = 10,
        PMOS = 11,
        OPAMP1 = 12,
        OPAMP2 = 13,
        DELAY1 = 14,
        DELAY2 = 15
    }

    private Wire m_input1;
    private Wire m_input2;
    private Vcc m_output;
    private Switch m_switch;
    private float m_U1;
    private float m_U2;
    private int m_delay;
    private LogicLevel m_level1;
    private LogicLevel m_level2;
    private Queue<bool> m_delayQueue;
    private bool m_canBeFlipped;
    public override IEnumerable<ElectricalElement> ElectricalElements
    {
        get
        {
            if (m_input1 != null)
            {
                yield return m_input1;
            }

            if (m_input2 != null)
            {
                yield return m_input2;
            }

            if (m_output != null)
            {
                yield return m_output;
            }

            if (m_switch != null)
            {
                yield return m_switch;
            }
        }
    }

    public ICType GetICType()
    {
        return (ICType)(customPartIndex - 22);
    }

    public override void Awake()
    {
        base.Awake();
        int num = (int)(GetICType() + 1);
        INSerializedSprite component = GetComponent<INSerializedSprite>();
        component.SpriteName = "IntegratedCircuit" + num + "_Sprite";
        component.UpdateMesh();
        switch (GetICType())
        {
            case ICType.OR2:
            case ICType.NOR2:
            case ICType.AND2:
            case ICType.NAND2:
            case ICType.OPAMP1:
            case ICType.OPAMP2:
                m_canBeFlipped = true;
                m_autoAlign = (AutoAlignType)(-1);
                break;
            case ICType.DELAY1:
                m_delay = 5;
                m_delayQueue = new Queue<bool>(m_delay - 1);
                break;
            case ICType.DELAY2:
                m_delay = 25;
                m_delayQueue = new Queue<bool>(m_delay - 1);
                break;
            case ICType.NOR1:
            case ICType.AND1:
            case ICType.NAND1:
            case ICType.NMOS:
            case ICType.PMOS:
                break;
        }
    }

    public override void SetRotation(GridRotation rotation)
    {
        if (m_canBeFlipped)
        {
            SetRotation(GetRotation(rotation, m_flipped));
        }
        else
        {
            base.SetRotation(rotation);
        }
    }

    public override void SetFlipped(bool flipped)
    {
        if (m_canBeFlipped)
        {
            SetRotation(GetRotation(m_gridRotation, flipped));
        }
        else
        {
            base.SetFlipped(flipped);
        }
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
        base.transform.localRotation = Quaternion.Euler(num3, num4, num5);
    }

    public override void CreateElectricalElements()
    {
        if (GetInput1Direction() != 0)
        {
            m_input1 = new Wire();
            m_input1.ElementUpdatedEvent += OnInput1Updated;
        }

        if (GetInput2Direction() != 0)
        {
            m_input2 = new Wire();
            m_input2.ElementUpdatedEvent += OnInput2Updated;
        }

        if (GetOutputDirection() != 0)
        {
            m_output = new Vcc(0f, 0f);
        }

        if (GetSwitchDirection() != 0)
        {
            m_switch = new Switch();
        }
    }

    public override ElectricalElement GetElectricalElementByDirection(BitDirection direction)
    {
        direction = direction.Rotate(0 - (int)m_gridRotation);
        if (direction == GetInput1Direction())
        {
            return m_input1;
        }

        if (direction == GetInput2Direction())
        {
            return m_input2;
        }

        if (direction == GetOutputDirection())
        {
            return m_output;
        }

        if ((direction & GetSwitchDirection()) != 0)
        {
            return m_switch;
        }

        return null;
    }

    protected override BitDirection GetConnectionDirection()
    {
        return (GetInput1Direction() | GetInput2Direction() | GetOutputDirection() | GetSwitchDirection()).Rotate((int)m_gridRotation);
    }

    private BitDirection GetInput1Direction()
    {
        switch (GetICType())
        {
            case ICType.BUF:
            case ICType.NOT:
                return BitDirection.Left;
            case ICType.OR1:
            case ICType.NOR1:
            case ICType.AND1:
            case ICType.NAND1:
                return BitDirection.Up;
            case ICType.OR2:
            case ICType.NOR2:
            case ICType.AND2:
            case ICType.NAND2:
                return BitDirection.Left;
            case ICType.NMOS:
            case ICType.PMOS:
                return BitDirection.Left;
            case ICType.OPAMP1:
            case ICType.OPAMP2:
                if (m_flipped)
                {
                    return BitDirection.Down;
                }

                return BitDirection.Up;
            case ICType.DELAY1:
            case ICType.DELAY2:
                return BitDirection.Left;
            default:
                return BitDirection.None;
        }
    }

    private BitDirection GetInput2Direction()
    {
        switch (GetICType())
        {
            case ICType.OR1:
            case ICType.NOR1:
            case ICType.AND1:
            case ICType.NAND1:
                return BitDirection.Down;
            case ICType.OR2:
            case ICType.NOR2:
            case ICType.AND2:
            case ICType.NAND2:
                if (m_flipped)
                {
                    return BitDirection.Down;
                }

                return BitDirection.Up;
            case ICType.OPAMP1:
            case ICType.OPAMP2:
                if (m_flipped)
                {
                    return BitDirection.Up;
                }

                return BitDirection.Down;
            default:
                return BitDirection.None;
        }
    }

    private BitDirection GetOutputDirection()
    {
        ICType iCType = GetICType();
        if ((uint)(iCType - 10) <= 1u)
        {
            return BitDirection.None;
        }

        return BitDirection.Right;
    }

    private BitDirection GetSwitchDirection()
    {
        ICType iCType = GetICType();
        if ((uint)(iCType - 10) <= 1u)
        {
            return BitDirection.UpAndDown;
        }

        return BitDirection.None;
    }

    private void OnInput1Updated(CircuitSimulator.SimulationResult result)
    {
        if (result.IsGrounded)
        {
            m_level1 = ElectricalPart.GetLogicLevel(result.U);
            m_U1 = result.U;
        }
    }

    private void OnInput2Updated(CircuitSimulator.SimulationResult result)
    {
        if (result.IsGrounded)
        {
            m_level2 = ElectricalPart.GetLogicLevel(result.U);
            m_U2 = result.U;
        }
    }

    public override void PreUpdateElements()
    {
        m_U1 = float.NaN;
        m_U2 = float.NaN;
        m_level1 = LogicLevel.Invalid;
        m_level2 = LogicLevel.Invalid;
    }

    public override void PostUpdateElements()
    {
        switch (GetICType())
        {
            case ICType.NMOS:
                SetInvalid(m_level1 == LogicLevel.Invalid);
                m_switch.SetClosed(m_level1 == LogicLevel.High);
                return;
            case ICType.PMOS:
                SetInvalid(m_level1 == LogicLevel.Invalid);
                m_switch.SetClosed(m_level1 == LogicLevel.Low);
                return;
            case ICType.OPAMP1:
                SetInvalid(float.IsNaN(m_U1) || float.IsNaN(m_U2));
                if (!m_invalid)
                {
                    float potential = UnityEngine.Mathf.Clamp((m_U1 - m_U2) * 1000f, -10f, 10f);
                    m_output.Potential = potential;
                }

                return;
            case ICType.OPAMP2:
                SetInvalid(float.IsNaN(m_U1) || float.IsNaN(m_U2));
                if (!m_invalid)
                {
                    float num = UnityEngine.Mathf.Clamp((m_U1 - m_U2) * 1000f, -10f, 10f);
                    m_output.Potential = m_output.Potential * 0.99f + num * 0.01f;
                }

                return;
            case ICType.DELAY1:
            case ICType.DELAY2:
            {
                SetInvalid(m_level1 == LogicLevel.Invalid);
                bool flag = false;
                if (m_delayQueue.Count == m_delay - 1)
                {
                    flag = m_delayQueue.Dequeue();
                }

                m_delayQueue.Enqueue(m_level1 == LogicLevel.High);
                m_output.Potential = (flag ? 5f : 0f);
                return;
            }
        }

        bool flag2 = false;
        if (GetInput1Direction() != 0)
        {
            flag2 |= m_level1 == LogicLevel.Invalid;
        }

        if (GetInput2Direction() != 0)
        {
            flag2 |= m_level2 == LogicLevel.Invalid;
        }

        SetInvalid(flag2);
        if (!flag2)
        {
            bool output = false;
            bool flag3 = m_level1 == LogicLevel.High;
            bool flag4 = m_level2 == LogicLevel.High;
            switch (GetICType())
            {
                case ICType.BUF:
                    output = flag3;
                    break;
                case ICType.NOT:
                    output = !flag3;
                    break;
                case ICType.OR1:
                case ICType.OR2:
                    output = flag3 || flag4;
                    break;
                case ICType.NOR1:
                case ICType.NOR2:
                    output = !(flag3 || flag4);
                    break;
                case ICType.AND1:
                case ICType.AND2:
                    output = flag3 && flag4;
                    break;
                case ICType.NAND1:
                case ICType.NAND2:
                    output = !(flag3 && flag4);
                    break;
            }

            SetOutput(output);
        }
        else
        {
            SetOutput(value: false);
        }
    }

    private void SetOutput(bool value)
    {
        if (value)
        {
            SetOutput(5f, 0.05f);
        }
        else
        {
            SetOutput(0f, 0f);
        }
    }

    private void SetOutput(float potential, float resistance)
    {
        m_output.Potential = potential;
        m_output.Resistance = resistance;
    }
}