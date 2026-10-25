public class Capacitor : ElectricalElement
{
    public const int AnodeType = 0;
    public const int CathodeType = 1;
    private float m_I;
    private float m_deltaTime;
    public float Capacitance { get; set; }
    public float Charge { get; set; }
    public float Resistance { get; set; }
    public Electrode Anode => GetElectrodeByType(0);
    public Electrode Cathode => GetElectrodeByType(1);

    public Capacitor(float capacitance, float resistance) : this(capacitance, 0f, resistance)
    {
    }

    public Capacitor(float capacitance, float charge, float resistance)
    {
        Capacitance = capacitance;
        Charge = charge;
        Resistance = resistance;
        m_I = float.NaN;
    }

    public override void UpdateElectrode(CircuitSimulator.SimulationResult result)
    {
        bool flag = base.Electrodes[result.Electrode].Type == 0;
        m_I = (flag ? result.I : (0f - result.I));
        m_deltaTime = result.DeltaTime;
    }

    public override void Update()
    {
        if (!float.IsNaN(m_I))
        {
            Charge += m_I * m_deltaTime;
            m_I = float.NaN;
        }
    }
}