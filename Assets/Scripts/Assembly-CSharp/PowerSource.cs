public class PowerSource : ElectricalElement
{
    public const int AnodeType = 0;
    public const int CathodeType = 1;
    public float Electromotance { get; set; }
    public float Resistance { get; set; }
    public Electrode Anode => GetElectrodeByType(0);
    public Electrode Cathode => GetElectrodeByType(1);

    public PowerSource(float electromotance, float resistance)
    {
        Electromotance = electromotance;
        Resistance = resistance;
    }
}