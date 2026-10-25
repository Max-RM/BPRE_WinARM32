public class TemperatureSystem : INBehaviour
{
    public static void Create()
    {
        new TemperatureSystem().Initialize();
    }

    private void Initialize()
    {
        INContraption.Instance.AddBehaviour(this);
        m_status = StatusCode.Running;
    }

    public override void FixedUpdate()
    {
        foreach (BasePart part in Contraption.Instance.Parts)
        {
            part.Temperature *= 0.01f;
        }
    }
}