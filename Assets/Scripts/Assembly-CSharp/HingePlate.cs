using System.Collections.Generic;
using UnityEngine;

public class HingePlate : BasePart
{
    public struct ConnectionData
    {
        public BasePart Part;
        public Rigidbody Rigidbody;
        public int Angle;
        public ConnectionData(BasePart part, Rigidbody rigidbody, int angle)
        {
            Part = part;
            Rigidbody = rigidbody;
            Angle = angle;
        }
    }

    public enum HingePlateType
    {
        Common = 0,
        Track = 1
    }

    public HingePlateType m_type;
    private int m_componentIndex;
    private List<Joint> m_joints;
    public static (int, int)[] Directions { get; } = new (int, int)[8]
    {
        (1, 0),
        (1, 1),
        (0, 1),
        (-1, 1),
        (-1, 0),
        (-1, -1),
        (0, -1),
        (1, -1)
    };
    public GameObject LeftPlate { get; private set; }
    public GameObject RightPlate { get; private set; }
    public Rigidbody LeftPlateRigidbody { get; private set; }
    public Rigidbody RightPlateRigidbody { get; private set; }
    public List<ConnectionData> LeftParts { get; private set; }
    public List<ConnectionData> RightParts { get; private set; }

    public new (int, int) Direction
    {
        get
        {
            int rotation = GetRotation();
            if (((rotation == 0) || (rotation == 1)))
            {
                return Directions[rotation * 2];
            }

            return Directions[(rotation - 2) * 2];
        }
    }

    public override Vector3 Position => (LeftPlate.transform.position + RightPlate.transform.position) * 0.5f;

    public override IEnumerable<Rigidbody> GetRigidbodies()
    {
        yield return LeftPlateRigidbody;
        yield return RightPlateRigidbody;
    }

    public static void InitializeStatic()
    {
        List<HingePlate> list = new List<HingePlate>();
        foreach (BasePart part in Contraption.Instance.Parts)
        {
            if ((part is HingePlate item))
            {
                list.Add(item);
            }
        }

        int count = list.Count;
        DisjointSet disjointSet = new DisjointSet(count);
        Dictionary<BasePart, int> dictionary = new Dictionary<BasePart, int>(count);
        for (int i = 0; i < count; i++)
        {
            dictionary[list[i]] = i;
        }

        for (int j = 0; j < count; j++)
        {
            HingePlate hingePlate = list[j];
            foreach (ConnectionData leftPart in hingePlate.LeftParts)
            {
                if (dictionary.TryGetValue(leftPart.Part, out var value))
                {
                    disjointSet.Union(j, value);
                }
            }

            foreach (ConnectionData rightPart in hingePlate.RightParts)
            {
                if (dictionary.TryGetValue(rightPart.Part, out var value2))
                {
                    disjointSet.Union(j, value2);
                }
            }
        }

        int componentCount;
        int[] componentIndexes = disjointSet.GetComponentIndexes(out componentCount);
        for (int k = 0; k < count; k++)
        {
            int componentIndex = componentIndexes[k];
            list[k].m_componentIndex = componentIndex;
        }

        List<HingePlate>[] array = new List<HingePlate>[componentCount];
        for (int l = 0; l < componentCount; l++)
        {
            array[l] = new List<HingePlate>();
        }

        foreach (HingePlate item2 in list)
        {
            HingePlate hingePlate2 = item2 as HingePlate;
            array[hingePlate2.m_componentIndex].Add(hingePlate2);
        }

        List<Collider> list2 = new List<Collider>();
        for (int m = 0; m < componentCount; m++)
        {
            foreach (HingePlate item3 in array[m])
            {
                if (item3.m_enclosedInto is Frame)
                {
                    list2.Add(item3.m_enclosedInto.GetComponent<Collider>());
                }
            }

            foreach (HingePlate item4 in array[m])
            {
                Collider component = item4.LeftPlate.GetComponent<Collider>();
                Collider component2 = item4.RightPlate.GetComponent<Collider>();
                foreach (Collider item5 in list2)
                {
                    Physics.IgnoreCollision(component, item5);
                    Physics.IgnoreCollision(component2, item5);
                }
            }
        }
    }

    public IEnumerable<BasePart> GetConnectedParts()
    {
        foreach (ConnectionData item in LeftParts)
        {
            yield return item.Part;
        }

        foreach (ConnectionData item2 in RightParts)
        {
            yield return item2.Part;
        }
    }

    public override void Awake()
    {
        base.Awake();
        LeftPlate = base.transform.Find("Plate1").gameObject;
        RightPlate = base.transform.Find("Plate2").gameObject;
        PhysicMaterial material = new PhysicMaterial
        {
            dynamicFriction = 0.7f,
            staticFriction = 0.7f,
            frictionCombine = PhysicMaterialCombine.Average
        };
        LeftPlate.GetComponent<Collider>().material = material;
        RightPlate.GetComponent<Collider>().material = material;
    }

    public override bool CanBeEnclosed()
    {
        return true;
    }

    public override void EnsureRigidbody()
    {
        base.EnsureRigidbody();
        EnsureRigidbodyPlate();
        base.rigidbody.isKinematic = true;
    }

    public override void PrePlaced()
    {
        base.PrePlaced();
        m_autoAlign = (AutoAlignType)(-1);
    }

    public override void Initialize()
    {
        LeftPlateRigidbody = LeftPlate.GetComponent<Rigidbody>();
        RightPlateRigidbody = RightPlate.GetComponent<Rigidbody>();
        Create();
    }

    public override void SetRotation(GridRotation rotation)
    {
        SetRotation((int)rotation);
    }

    public override void SetRotation(int rotation)
    {
        int num = (int)(m_gridRotation = (GridRotation)(rotation % 6));
        float z = LeftPlate.transform.localPosition.z;
        if (((num == 0) || (num == 1)))
        {
            LeftPlate.transform.localPosition = new Vector3(-0.25f, 0f, z);
            RightPlate.transform.localPosition = new Vector3(0.25f, 0f, z);
            base.transform.localRotation = Quaternion.AngleAxis(90 * num, Vector3.forward);
        }
        else
        {
            LeftPlate.transform.localPosition = new Vector3(-0.25f, -0.5f, z);
            RightPlate.transform.localPosition = new Vector3(0.25f, -0.5f, z);
            base.transform.localRotation = Quaternion.AngleAxis(90 * (num - 2), Vector3.forward);
        }
    }

    private void EnsureRigidbodyPlate()
    {
        EnsurePlateRigidbody(LeftPlate);
        EnsurePlateRigidbody(RightPlate);
    }

    private void EnsurePlateRigidbody(GameObject gameObject)
    {
        Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = gameObject.AddComponent<Rigidbody>();
        }

        rigidbody.constraints = (RigidbodyConstraints)56;
        rigidbody.mass = 1f;
        rigidbody.drag = 0.2f;
        rigidbody.angularDrag = 0.05f;
        rigidbody.useGravity = true;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private bool ValidatePart(ref BasePart part)
    {
        if (part == null)
        {
            return false;
        }

        if (part.m_enclosedPart != null && part.m_enclosedPart is HingePlate)
        {
            part = part.m_enclosedPart;
        }

        return true;
    }

    private bool CanConnectToLeft(BasePart part, int x, int y, out bool flip, out int angle)
    {
        flip = false;
        angle = 0;
        HingePlate hingePlate = part as HingePlate;
        int rotation = GetRotation();
        if ((object)hingePlate == null)
        {
            (int, int) direction = Direction;
            PartType partType = part.m_partType;
            if (m_type == HingePlateType.Common && (((partType == PartType.WoodenFrame) || (partType == PartType.MetalFrame)) || (partType == PartType.Kicker)) && x == -direction.Item1)
            {
                return y == -direction.Item2;
            }

            return false;
        }

        int rotation2 = hingePlate.GetRotation();
        switch (rotation)
        {
            case 0:
                if (x == -1 && y == 0)
                {
                    return rotation2 == 0;
                }

                return false;
            case 1:
                if (x == 0 && y == -1)
                {
                    return rotation2 == 1;
                }

                return false;
            default:
            {
                rotation -= 2;
                rotation2 -= 2;
                int num = (rotation2 - rotation + 4) % 4;
                (int, int) tuple = Directions[(4 + rotation * 2) % 8];
                (int, int) tuple2 = Directions[(5 + rotation * 2) % 8];
                (int, int) tuple3 = Directions[(6 + rotation * 2) % 8];
                flip = (x == tuple.Item1 && y == tuple.Item2 && num == 1) || (x == tuple2.Item1 && y == tuple2.Item2 && num == 2) || (x == tuple3.Item1 && y == tuple3.Item2 && num == 3);
                angle = (flip ? (-90) : 90) * ((num > 2) ? (num - 4) : num);
                if ((x != tuple.Item1 || y != tuple.Item2 || (num != 1 && num != 0)) && (x != tuple2.Item1 || y != tuple2.Item2 || (num != 2 && num != 1)))
                {
                    if (x == tuple3.Item1 && y == tuple3.Item2)
                    {
                        return num == 3;
                    }

                    return false;
                }

                return true;
            }
        }
    }

    private bool CanConnectToRight(BasePart part, int x, int y, out bool flip, out int angle)
    {
        flip = false;
        angle = 0;
        HingePlate hingePlate = part as HingePlate;
        int rotation = GetRotation();
        if ((object)hingePlate == null)
        {
            (int, int) direction = Direction;
            if (m_type == HingePlateType.Common && ((part is Frame) || (part is Kicker)) && x == direction.Item1)
            {
                return y == direction.Item2;
            }

            return false;
        }

        int rotation2 = hingePlate.GetRotation();
        switch (rotation)
        {
            case 0:
                if (x == 1 && y == 0)
                {
                    return rotation2 == 0;
                }

                return false;
            case 1:
                if (x == 0 && y == 1)
                {
                    return rotation2 == 1;
                }

                return false;
            default:
            {
                rotation -= 2;
                rotation2 -= 2;
                int num = (rotation2 - rotation + 4) % 4;
                (int, int) tuple = Directions[(6 + rotation * 2) % 8];
                (int, int) tuple2 = Directions[(7 + rotation * 2) % 8];
                (int, int) tuple3 = Directions[rotation * 2 % 8];
                flip = (x == tuple.Item1 && y == tuple.Item2 && num == 1) || (x == tuple2.Item1 && y == tuple2.Item2 && num == 2) || (x == tuple3.Item1 && y == tuple3.Item2 && num == 3);
                angle = (flip ? 90 : (-90)) * (num - 2);
                if ((x != tuple.Item1 || y != tuple.Item2 || num != 1) && (x != tuple2.Item1 || y != tuple2.Item2 || (num != 2 && num != 3)))
                {
                    if (x == tuple3.Item1 && y == tuple3.Item2)
                    {
                        if (num != 3)
                        {
                            return num == 0;
                        }

                        return true;
                    }

                    return false;
                }

                return true;
            }
        }
    }

    public bool CanConnectTo(BasePart part)
    {
        int coordX = m_coordX;
        int coordY = m_coordY;
        if (ValidatePart(ref part))
        {
            if (CanConnectToLeft(part, part.m_coordX - coordX, part.m_coordY - coordY, out var flip, out var angle))
            {
                return true;
            }

            if (CanConnectToRight(part, part.m_coordX - coordX, part.m_coordY - coordY, out flip, out angle))
            {
                return true;
            }
        }

        return false;
    }

    public void Create()
    {
        int coordX = m_coordX;
        int coordY = m_coordY;
        GameObject plate = LeftPlate;
        GameObject plate2 = RightPlate;
        Contraption contraption = base.contraption;
        LeftParts = new List<ConnectionData>();
        RightParts = new List<ConnectionData>();
        m_joints = new List<Joint>();
        for (int i = 0; i < 8; i++)
        {
            (int, int) tuple = Directions[i];
            BasePart part = contraption.FindPartAt(coordX + tuple.Item1, coordY + tuple.Item2, this);
            if (ValidatePart(ref part))
            {
                if (CanConnectToLeft(part, tuple.Item1, tuple.Item2, out var flip, out var angle))
                {
                    Rigidbody component = ((!((part is HingePlate hingePlate))) ? part.gameObject : (flip ? hingePlate.LeftPlate : hingePlate.RightPlate)).GetComponent<Rigidbody>();
                    LeftParts.Add(new ConnectionData(part, component, angle));
                }

                if (CanConnectToRight(part, tuple.Item1, tuple.Item2, out flip, out angle))
                {
                    Rigidbody component2 = ((!((part is HingePlate hingePlate2))) ? part.gameObject : (flip ? hingePlate2.RightPlate : hingePlate2.LeftPlate)).GetComponent<Rigidbody>();
                    RightParts.Add(new ConnectionData(part, component2, angle));
                }
            }
        }

        CreateJoint(plate, plate2, JointType.FixedJoint, 0, new Vector3(0.25f, 0f));
        CreateJoint(plate2, plate, JointType.FixedJoint, 0, new Vector3(-0.25f, 0f));
        for (int j = 0; j < 2; j++)
        {
            GameObject lhs = ((j == 0) ? plate : plate2);
            List<ConnectionData> obj = ((j == 0) ? LeftParts : RightParts);
            Vector3 anchor = new Vector3((j == 0) ? (-0.25f) : 0.25f, 0f);
            foreach (ConnectionData item2 in obj)
            {
                JointType type = ((!INSettings.GetBool(INFeature.StableHingePlate)) ? JointType.HingeJoint : JointType.FixedJoint);
                Joint item = CreateJoint(lhs, item2.Rigidbody.gameObject, type, item2.Angle, anchor);
                m_joints.Add(item);
            }
        }
    }

    private Joint CreateJoint(GameObject lhs, GameObject rhs, JointType type, int angle, Vector3 anchor)
    {
        Rigidbody component = rhs.GetComponent<Rigidbody>();
        float breakForce = (base.contraption.HasSuperGlue ? float.PositiveInfinity : (base.contraption.GetJointConnectionStrength(m_jointConnectionStrength) * INSettings.GetFloat(INFeature.ConnectionStrength) * 3f));
        switch (type)
        {
            case JointType.FixedJoint:
            {
                FixedJoint fixedJoint = lhs.AddComponent<FixedJoint>();
                fixedJoint.connectedBody = component;
                fixedJoint.anchor = anchor;
                fixedJoint.axis = Vector3.forward;
                fixedJoint.breakForce = breakForce;
                fixedJoint.breakTorque = float.PositiveInfinity;
                fixedJoint.enablePreprocessing = false;
                return fixedJoint;
            }

            case JointType.HingeJoint:
            {
                angle %= 180;
                HingeJoint hingeJoint = lhs.AddComponent<HingeJoint>();
                hingeJoint.connectedBody = component;
                hingeJoint.anchor = anchor;
                hingeJoint.axis = Vector3.forward;
                hingeJoint.breakForce = breakForce;
                hingeJoint.breakTorque = float.PositiveInfinity;
                hingeJoint.enablePreprocessing = false;
                hingeJoint.useLimits = true;
                hingeJoint.limits = new JointLimits
                {
                    min = (float)angle - 15f,
                    max = (float)angle + 15f
                };
                return hingeJoint;
            }

            default:
                return null;
        }
    }

    private void FixedUpdate()
    {
        if (!base.contraption || !base.contraption.IsRunning)
        {
            return;
        }

        foreach (HingeJoint joint in m_joints)
        {
            if (joint != null)
            {
                Rigidbody component = joint.GetComponent<Rigidbody>();
                Rigidbody connectedBody = joint.connectedBody;
                float mass = component.mass;
                float mass2 = connectedBody.mass;
                Vector3 velocity = component.velocity;
                Vector3 velocity2 = connectedBody.velocity;
                float num = velocity2.x - velocity.x;
                float num2 = velocity2.y - velocity.y;
                float num3 = 0.2f * mass * mass2 / (mass + mass2);
                float num4 = num3 * Mathf.Sqrt(num * num + num2 * num2);
                num3 = ((num4 < 10f) ? num3 : (10f / num4));
                component.AddForce(new Vector3(num3 * num, num3 * num2), ForceMode.Impulse);
                connectedBody.AddForce(new Vector3((0f - num3) * num, (0f - num3) * num2), ForceMode.Impulse);
            }
        }
    }
}