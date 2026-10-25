public class Switch : ElectricalElement
{
    public bool IsClosed { get; private set; }

    public Switch()
    {
        IsClosed = false;
    }

    public override void Initialize()
    {
        SetClosedInternal(IsClosed);
    }

    public void SetClosed(bool closed)
    {
        if (IsClosed != closed)
        {
            IsClosed = closed;
            SetClosedInternal(closed);
        }
    }

    private void SetClosedInternal(bool closed)
    {
        for (int i = 0; i < base.Electrodes.Count; i++)
        {
            Electrode value = base.Electrodes[i];
            value.SetConnected(closed);
            base.Electrodes[i] = value;
            ElectricalElement element = value.Element;
            int electrodeIndex = element.GetElectrodeIndex(this);
            Electrode value2 = element.Electrodes[electrodeIndex];
            value2.SetConnected(closed);
            element.Electrodes[electrodeIndex] = value2;
        }
    }
}