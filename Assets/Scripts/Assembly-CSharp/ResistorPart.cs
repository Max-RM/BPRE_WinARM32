using System.Collections.Generic;

public class ResistorPart : ElectricalPart
{
    private bool m_variable;
    private Resistor m_resistor;
    public int CurrentResistorType => customPartIndex - 6;
    public override IEnumerable<ElectricalElement> ElectricalElements => m_resistor.ToEnumerable();

    public override bool IsTriggerable()
    {
        if (!base.HasGeneratorRef)
        {
            return m_variable;
        }

        return false;
    }

    public override IEnumerable<UIPartTriggerButtonInfo> GetTriggerButtonInfo()
    {
        yield break;
    }

    public override IEnumerable<UIPartSliderButtonInfo> GetSliderButtonInfo()
    {
        if (m_variable)
        {
            yield return new UIPartSliderButtonInfo(UIPartButtonType.Slider, 0, base.Type, 2, base.ConnectedComponent, new UIPartSliderButton.Range((m_resistor == null) ? 1f : m_resistor.Resistance, 1f, 0.1f, 10f, 0.1f, 0.01f));
        }
    }

    public override void OnSliderButtonTriggered(UIPartSliderButton button)
    {
        if (m_variable)
        {
            m_resistor.Resistance = button.Value;
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        m_variable = CurrentResistorType == 5;
    }

    public override void CreateElectricalElements()
    {
        float resistance = ((CurrentResistorType == 0) ? 0.01f : (CurrentResistorType == 1) ? 0.1f : (CurrentResistorType == 2) ? 1f : (CurrentResistorType == 3) ? 10f : (CurrentResistorType == 4) ? 100f : (CurrentResistorType == 5) ? 1f : 0f);
        m_resistor = new Resistor(resistance);
    }

    protected override BitDirection GetConnectionDirection()
    {
        return BitDirection.LeftAndRight.Rotate((int)m_gridRotation);
    }

    public override void SetRotation(GridRotation rotation)
    {
        int rotation2 = (int)rotation % 2;
        base.SetRotation((GridRotation)rotation2);
    }
}