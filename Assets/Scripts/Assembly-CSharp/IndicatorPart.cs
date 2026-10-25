using System.Collections.Generic;
using UnityEngine;

public class IndicatorPart : ElectricalPart
{
    private enum IndicatorType
    {
        Ammeter = 0,
        Voltmeter = 1
    }

    [SerializeField]
    private IndicatorType m_type;
    private Resistor m_indicator;
    private float m_I;
    private float m_U1;
    private float m_U2;
    private GameObject m_symbol;
    private TextMesh m_text;
    public override IEnumerable<ElectricalElement> ElectricalElements => m_indicator.ToEnumerable();

    public override void Awake()
    {
        base.Awake();
        m_symbol = base.transform.Find("Symbol").gameObject;
        m_text = base.transform.Find("Text").GetComponent<TextMesh>();
        m_I = float.NaN;
        m_U1 = float.NaN;
        m_U2 = float.NaN;
    }

    public override void CreateElectricalElements()
    {
        Resistor resistor = ((m_type == IndicatorType.Ammeter) ? new Resistor(0f) : (m_type == IndicatorType.Voltmeter) ? new Resistor(10000f) : null);
        resistor.ElementUpdatedEvent += OnElementUpdated;
        m_indicator = resistor;
    }

    public override void ConnectElectricalElements()
    {
        foreach (ConnectionData connection in m_connections)
        {
            ElectricalElement element = connection.Element1;
            ElectricalElement element2 = connection.Element2;
            int type = ((connection.Direction != BitDirection.Left.Rotate((int)m_gridRotation)) ? 1 : 0);
            element.AddConnectedElement(element2, type);
        }
    }

    private void OnElementUpdated(CircuitSimulator.SimulationResult result)
    {
        if (result.Electrode < m_indicator.Electrodes.Count)
        {
            if (m_indicator.Electrodes[result.Electrode].Type == 0)
            {
                m_I = result.I;
                m_U1 = result.U;
            }
            else
            {
                m_I = 0f - result.I;
                m_U2 = result.U;
            }
        }
    }

    protected override BitDirection GetConnectionDirection()
    {
        return BitDirection.LeftAndRight.Rotate((int)m_gridRotation);
    }

    public override void SetRotation(GridRotation rotation)
    {
        base.SetRotation(rotation);
        m_symbol.transform.rotation = Quaternion.identity;
        m_text.transform.rotation = Quaternion.identity;
    }

    public override void PostUpdateElements()
    {
        float num = ((m_type == IndicatorType.Ammeter) ? m_I : (m_type == IndicatorType.Voltmeter) ? m_U1 - m_U2 : 0f);
        if (float.IsNaN(num))
        {
            num = 0f;
        }

        m_I = float.NaN;
        m_U1 = float.NaN;
        m_U2 = float.NaN;
        bool flag = num >= 0f;
        num = (flag ? num : (0f - num));
        string text = ((num >= 1000f) ? "999" : ((!(num >= 100f)) ? num.ToString("00.0") : num.ToString("000")));
        m_text.text = (flag ? string.Empty : "-") + text;
    }
}