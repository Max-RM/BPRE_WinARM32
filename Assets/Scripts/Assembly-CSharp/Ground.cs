public class Ground : ElectricalElement
{
    public float Resistance { get; set; }

    public Ground(float resistance)
    {
        Resistance = resistance;
    }
}