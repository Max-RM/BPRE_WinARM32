public class Vcc : ElectricalElement
{
    public float Potential { get; set; }
    public float Resistance { get; set; }

    public Vcc(float potential, float resistance)
    {
        Potential = potential;
        Resistance = resistance;
    }
}