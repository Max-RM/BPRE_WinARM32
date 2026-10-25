using System.Collections.Generic;
using UnityEngine;

public class FrameJointManager : PartManager
{
    private bool m_needsUpdate;
    private List<BasePart> m_cacheParts;
    public static FrameJointManager Instance { get; private set; }
    public int JointCount { get; private set; }

    protected override void Initialize()
    {
        base.Initialize();
        m_status = StatusCode.Running;
        Instance = this;
    }

    public override void Start()
    {
        AddFrameParts(Contraption.Instance.Parts);
    }

    public override void FixedUpdate()
    {
        if (!m_needsUpdate)
            return;
        AddFrameJoints(m_cacheParts);
        m_needsUpdate = false;
        m_cacheParts = null;
    }

    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddFrameParts(List<BasePart> parts)
    {
        m_needsUpdate = true;
        if (m_cacheParts == null)
        {
            m_cacheParts = parts;
        }
        else
        {
            m_cacheParts.AddRange(parts);
        }
    }

    private void AddFrameJoints(List<BasePart> parts)
    {
        List<BasePart> list = new List<BasePart>();
        foreach (BasePart part in parts)
        {
            if (part.IsBracketFrame())
            {
                list.Add(part);
            }
        }

        int count = list.Count;
        DisjointSet disjointSet = new DisjointSet(count);
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                BasePart basePart = list[i];
                BasePart basePart2 = list[j];
                int num = basePart2.m_coordX - basePart.m_coordX;
                int num2 = basePart2.m_coordY - basePart.m_coordY;
                if (num * num + num2 * num2 == 1)
                {
                    disjointSet.Union(i, j);
                }
            }
        }

        int componentCount;
        int[] componentIndexes = disjointSet.GetComponentIndexes(out componentCount);
        Dictionary<BasePart, int> dictionary = new Dictionary<BasePart, int>();
        for (int k = 0; k < count; k++)
        {
            int value = componentIndexes[k];
            dictionary[list[k]] = value;
        }

        List<(BasePart, byte)> list2 = new List<(BasePart, byte)>();
        float breakForce = (Contraption.Instance.HasSuperGlue ? float.PositiveInfinity : (WPFMonoBehaviour.gameData.m_jointConnectionStrengthHigh * INSettings.GetFloat(INFeature.ConnectionStrength)));
        foreach (BasePart part2 in parts)
        {
            byte b = 0;
            BasePart enclosedPart = part2.m_enclosedPart;
            bool flag = enclosedPart != null && enclosedPart.m_partType == BasePart.PartType.SpringBoxingGlove && enclosedPart.customPartIndex == 4;
            b = (((part2.m_partType == BasePart.PartType.MetalFrame) && (flag)) ? (byte)(b | 1u) : ((part2.m_partType == BasePart.PartType.WoodenFrame) && (flag)) ? (byte)(b | 2u) : b);
            if (part2.IsLightFrame())
            {
                b = (byte)(b | 4u);
            }

            if (part2.IsBracketFrame())
            {
                b = (byte)(b | 8u);
            }

            if (b != 0)
            {
                list2.Add((part2, b));
            }
        }

        for (int l = 0; l < list2.Count; l++)
        {
            for (int m = l + 1; m < list2.Count; m++)
            {
                (BasePart basePart, byte item3) = list2[l];
                (BasePart basePart2, byte b) = list2[m];
                byte num3 = (byte)(item3 & b);
                byte b2 = (byte)(num3 & 1u);
                byte b3 = (byte)(num3 & 2u);
                byte b4 = (byte)(num3 & 8u);
                if (num3 == 0 || ((b4 > 0) ? (dictionary[basePart] != dictionary[basePart2]) : (basePart.StrictConnectedComponent != basePart2.StrictConnectedComponent)))
                    continue;
                Vector3 position = basePart.transform.position;
                Vector3 position2 = basePart2.transform.position;
                float dx = position.x - position2.x;
                float dy = position.y - position2.y;
                float num6 = b2 > 0 ? 32f : b3 > 0 ? 16f : 8f;
                if (!(dx * dx + dy * dy < num6 * num6))
                    continue;
                FixedJoint fixedJoint = basePart.gameObject.AddComponent<FixedJoint>();
                fixedJoint.connectedBody = basePart2.rigidbody;
                fixedJoint.breakForce = breakForce;
                FixedJoint fixedJoint2 = basePart2.gameObject.AddComponent<FixedJoint>();
                fixedJoint2.connectedBody = basePart.rigidbody;
                fixedJoint2.breakForce = breakForce;
                JointCount += 2;
            }
        }
    }
}