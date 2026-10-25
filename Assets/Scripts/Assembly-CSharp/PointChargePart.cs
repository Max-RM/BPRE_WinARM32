using UnityEngine;

public class PointChargePart : ElectricalPart
{
    private MeshRenderer m_renderer;
    public bool IsPositive => m_gridRotation == GridRotation.Deg_0;

    public float Charge
    {
        get
        {
            if (!IsPositive)
            {
                return -20f;
            }

            return 20f;
        }
    }

    public override void Awake()
    {
        base.Awake();
        m_renderer = GetComponent<MeshRenderer>();
    }

    public override void SetRotation(GridRotation rotation)
    {
        bool flag = (m_gridRotation = (GridRotation)((int)rotation % 2)) == GridRotation.Deg_0;
        INSerializedSprite component = GetComponent<INSerializedSprite>();
        component.SpriteName = (flag ? "PointCharge1_Sprite" : "PointCharge2_Sprite");
        component.UpdateMesh();
    }
}