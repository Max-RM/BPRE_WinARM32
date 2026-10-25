using UnityEngine;

public static class CircuitFactory
{
    public static void Connect(ElectricalElement left, ElectricalElement right)
    {
        left.AddConnectedElement(right);
        right.AddConnectedElement(left);
    }

    public static void Disconnect(ElectricalElement left, ElectricalElement right)
    {
        /*Debug.Log("Left:");
		Debug.Log(left);
		Debug.Log("Right:");
		Debug.Log(right);*/
        left.RemoveConnectedElement(right);
        right.RemoveConnectedElement(left);
    }

    public static bool IsConnected(ElectricalElement left, ElectricalElement right)
    {
        if (left.Electrodes.Count > right.Electrodes.Count)
        {
            (left, right) = (right, left);
        }

        foreach (ElectricalElement.Electrode electrode in left.Electrodes)
        {
            if (electrode.Element == right)
            {
                return true;
            }
        }

        return false;
    }
}