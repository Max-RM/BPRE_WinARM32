using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeuralNetworkManager : PartManager
{
    private List<NeuralPart> NeuralParts;
    public override void FixedUpdate()
    {
        foreach (BasePart part in Contraption.Instance.Parts)
        {
            switch (part)
            {
                case NeuralPart neuralPart:
                    NeuralParts.Add(neuralPart);
                    break;
            }
        }
    }
}