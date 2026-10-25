using System;
using System.Collections.Generic;

public abstract class ElectricalElement
{
    public struct Electrode
    {
        public ElectricalElement Element;
        public int Index;
        public int Type;
        public bool IsConnected;
        private int m_connectionStack;
        public static Electrode Empty { get; } = new Electrode(null, -1, -1);
        public bool IsEmpty => Index == -1;

        public Electrode(ElectricalElement element, int index, int type) : this(element, index, type, isConnected: true)
        {
        }

        public Electrode(ElectricalElement element, int index, int type, bool isConnected)
        {
            Element = element;
            Index = index;
            Type = type;
            IsConnected = isConnected;
            m_connectionStack = 0;
        }

        public void SetConnected(bool connected)
        {
            m_connectionStack += ((!connected) ? 1 : (-1));
            if (m_connectionStack < 0)
            {
                m_connectionStack = 0;
            }

            IsConnected = m_connectionStack == 0;
        }
    }

    public const int DefaultElectrodeType = -1;
    public int ElementIndex { get; set; }
    public int CircuitIndex { get; set; }
    public List<Electrode> Electrodes { get; }

    public IEnumerable<Electrode> ConnectedElectrodes
    {
        get
        {
            foreach (Electrode electrode in Electrodes)
            {
                if (electrode.IsConnected)
                {
                    yield return electrode;
                }
            }
        }
    }

    public event Action<CircuitSimulator.SimulationResult> ElementUpdatedEvent;
    public ElectricalElement()
    {
        Electrodes = new List<Electrode>();
    }

    public virtual void Initialize()
    {
    }

    public virtual void Update()
    {
    }

    public int GetConnectedElectrodeCount()
    {
        int num = 0;
        foreach (Electrode electrode in Electrodes)
        {
            if (electrode.IsConnected)
            {
                num++;
            }
        }

        return num;
    }

    public Electrode GetElectrodeByIndex(int index)
    {
        return Electrodes[index];
    }

    public Electrode GetConnectedElectrodeByIndex(int index)
    {
        Electrode result = Electrodes[index];
        if (!result.IsConnected)
        {
            return Electrode.Empty;
        }

        return result;
    }

    public Electrode GetElectrodeByType(int type)
    {
        foreach (Electrode electrode in Electrodes)
        {
            if (electrode.Type == type)
            {
                return electrode;
            }
        }

        return Electrode.Empty;
    }

    public Electrode GetConnectedElectrodeByType(int type)
    {
        foreach (Electrode electrode in Electrodes)
        {
            if (electrode.Type == type && electrode.IsConnected)
            {
                return electrode;
            }
        }

        return Electrode.Empty;
    }

    public Electrode GetAnotherElectrode(Electrode electrode)
    {
        return GetAnotherElectrode(electrode.Element);
    }

    public Electrode GetAnotherElectrode(ElectricalElement element)
    {
        for (int i = 0; i < Electrodes.Count; i++)
        {
            Electrode result = Electrodes[i];
            if (result.Element != element)
            {
                return result;
            }
        }

        return Electrode.Empty;
    }

    public Electrode GetAnotherConnectedElectrode(ElectricalElement element)
    {
        for (int i = 0; i < Electrodes.Count; i++)
        {
            Electrode result = Electrodes[i];
            if (result.Element != element && result.IsConnected)
            {
                return result;
            }
        }

        return Electrode.Empty;
    }

    public int GetAnotherElectrode(int electrode)
    {
        for (int i = 0; i < Electrodes.Count; i++)
        {
            if (i != electrode)
            {
                return i;
            }
        }

        return -1;
    }

    public int GetAnotherConnectedElectrode(int electrode)
    {
        for (int i = 0; i < Electrodes.Count; i++)
        {
            if (i != electrode && Electrodes[i].IsConnected)
            {
                return i;
            }
        }

        return -1;
    }

    public int GetElectrodeIndex(Electrode electrode)
    {
        return GetElectrodeIndex(electrode.Element);
    }

    public int GetElectrodeIndex(ElectricalElement element)
    {
        for (int i = 0; i < Electrodes.Count; i++)
        {
            if (Electrodes[i].Element == element)
            {
                return i;
            }
        }

        return -1;
    }

    public virtual bool IsNode()
    {
        return false;
    }

    public virtual void UpdateElectrode(CircuitSimulator.SimulationResult result)
    {
        this.ElementUpdatedEvent?.Invoke(result);
    }

    public void AddConnectedElement(ElectricalElement element)
    {
        AddConnectedElement(element, -1);
    }

    public void AddConnectedElement(ElectricalElement element, int type)
    {
        Electrode item = new Electrode(element, Electrodes.Count, type);
        Electrodes.Add(item);
    }

    public void RemoveConnectedElement(ElectricalElement element)
    {
        int num = -1;
        for (int i = 0; i < Electrodes.Count; i++)
        {
            if (Electrodes[i].Element == element)
            {
                num = i;
                break;
            }
        }

        if (num != -1)
        {
            Electrodes.RemoveAt(num);
        }
    }
}