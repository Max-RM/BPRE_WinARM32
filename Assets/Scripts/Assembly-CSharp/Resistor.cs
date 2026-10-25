public class Resistor : ElectricalElement
{
    public float Resistance { get; set; }

    public Resistor(float resistance)
    {
        Resistance = resistance;
    }
}