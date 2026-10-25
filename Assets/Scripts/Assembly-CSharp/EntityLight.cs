using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using UnityEngine;

public class EntityLight : MonoBehaviour
{
    NativeArray<float> physicFloats = new NativeArray<float>(length: 4, Allocator.Persistent);
    NativeArray<Vector2> deltaVectors = new NativeArray<Vector2>(length: 4, Allocator.Persistent);
    public void OnDestroy()
    {
        physicFloats.Dispose();
        deltaVectors.Dispose();
    }

    public struct LightData
    {
        public Vector2 Position0;
        public Vector2 Position1;
        public Vector2 Direction0;
        public Vector2 Direction1;
        public void Set(EntityLight light)
        {
            Vector3 position = light.Transform.position;
            Quaternion rotation = light.Transform.rotation;
            Position0 = Position1;
            Direction0 = Direction1;
            Position1.x = position.x;
            Position1.y = position.y;
            Direction1.x = 1f - (rotation.y * rotation.y * 2f + rotation.z * rotation.z * 2f);
            Direction1.y = rotation.x * rotation.y * 2f + rotation.w * rotation.z * 2f;
        }

        public void SetPosition(Vector2 position, Vector2 prePosition)
        {
            Position0 = prePosition;
            Position1 = position;
        }
    }

    [SerializeField]
    private int m_type;
    [SerializeField]
    private float m_angle;
    [SerializeField]
    private float m_halfWidth;
    [SerializeField]
    private float m_length;
    private bool m_enabled;
    private bool m_colored;
    private bool m_ignoreCollision;
    public float m_electricity;
    public float m_coefficient;
    private Color m_color;
    private INPhysicMaterial m_physicMaterial;
    private MeshRenderer m_meshRenderer;
    public MeshFilter m_meshFilter;
    private Collider m_collider;
    private EntityLightManager m_manager;
    private LightData m_data;
    public ref LightData Data => ref m_data;
    public int Sides { get; private set; }

    public bool Enabled
    {
        get => m_enabled;
        set
        {
            m_enabled = value;
            if (IsLightPillar)
            {
                m_collider.enabled = value;
            }
        }
    }

    public int Type { get => m_type; set => m_type = value; }
    public float Length { get => m_length; set => m_length = value; }
    public float HalfWidth { get => m_halfWidth; set => m_halfWidth = value; }
    public float Angle { get => m_angle; set => m_angle = value; }
    public float Cos { get; private set; }

    public bool IsLightPillar
    {
        get
        {
            if (m_type != 0)
            {
                return m_type == 1;
            }

            return true;
        }
    }

    public bool IsLightShield
    {
        get
        {
            if (m_type != 2)
            {
                return m_type == 4;
            }

            return true;
        }
    }

    public bool IsLightBox => m_type == 3;
    public int Index { get; set; }
    public GameObject Light => Transform.gameObject;
    public Transform Transform { get; private set; }
    public BasePart Part { get; private set; }
    public int ComponentIndex => Part.ConnectedComponent;

    private void Awake()
    {
        Part = GetComponent<BasePart>();
        Transform = base.transform.Find("INLight");
        if (Transform == null)
        {
            GameObject _gameObject = new GameObject("INLight");
            Transform = _gameObject.transform;
            Transform.parent = base.transform;
            _gameObject.AddComponent<MeshRenderer>();
            _gameObject.AddComponent<MeshFilter>();
        }

        Transform.localPosition = new Vector3(0f, 0.5f, -0.5f);
        Transform.localRotation = new Quaternion(0f, 0f, 0.70710677f, 0.70710677f);
        m_meshRenderer = Transform.GetComponent<MeshRenderer>();
        m_meshRenderer.sharedMaterial = new Material(INUnity.CustomTransparentShader);
        m_meshRenderer.material.color = Color.clear;
        m_meshFilter = Transform.GetComponent<MeshFilter>();
    }

    private void Start()
    {
        m_manager = EntityLightManager.Instance;
        Transform.gameObject.layer = LayerMask.NameToLayer("Ground");
        if (((m_type == 0) || (m_type == 1)))
        {
            m_meshFilter.sharedMesh = MeshExtensions.CreateRectMesh(m_length, m_halfWidth);
        }
        else
        {
            Cos = Mathf.Cos(m_angle * (UnityEngine.Mathf.PI / 360f));
            m_meshFilter.sharedMesh = MeshExtensions.CreateCircleMesh(m_length, m_halfWidth, m_angle, 150);
        }

        if (Contraption.Instance.IsRunning)
        {
            CreateCollider();
            InitializeColor();
            InitializePhysicMaterial();
            BasePart enclosedInto = Part.m_enclosedInto;
            if (enclosedInto != null && enclosedInto.IsTransparentFrame())
            {
                m_ignoreCollision = true;
            }
        }
    }

    private void CreateCollider()
    {
        if (IsLightPillar)
        {
            m_collider = Transform.GetComponent<BoxCollider>();
            if (m_collider == null)
            {
                m_collider = Transform.gameObject.AddComponent<BoxCollider>();
            }

            BoxCollider obj = m_collider as BoxCollider;
            obj.size = new Vector3(m_length, m_halfWidth * 2f, 1f);
            obj.center = new Vector3(m_length * 0.5f, 0f, 0.5f);
            obj.isTrigger = true;
        }
    }

    private void InitializePhysicMaterial()
    {
        float bounciness = 0f;
        PhysicMaterialCombine bounceMode = PhysicMaterialCombine.Average;
        float friction = 0.7f;
        PhysicMaterialCombine frictionMode = PhysicMaterialCombine.Average;
        BasePart enclosedInto = Part.m_enclosedInto;
        if (enclosedInto != null && (enclosedInto.IsAlienMetalFrame() || enclosedInto.IsColoredrame()))
        {
            friction = 0f;
            frictionMode = PhysicMaterialCombine.Minimum;
        }

        m_physicMaterial = new INPhysicMaterial(bounciness, bounceMode, friction, frictionMode);
    }

    private void InitializeColor()
    {
        Color color = default(Color);
        bool flag = !Contraption.Instance.HasTurboCharge;
        BasePart enclosedInto = Part.m_enclosedInto;
        if (INSettings.GetBool(INFeature.ColoredFrame) && enclosedInto != null && (enclosedInto is ColoredFrame coloredFrame))
        {
            m_colored = true;
            color = coloredFrame.Color;
            color.a *= (flag ? 0.5f : 0.7f);
        }
        else
        {
            m_colored = false;
            if (flag)
            {
                float a = 0.5f;
                switch (m_type)
                {
                    case 0:
                        color = new Color(0.5f, 0.75f, 1f, a);
                        break;
                    case 1:
                        color = new Color(0.5f, 0.65f, 1f, a);
                        break;
                    case 2:
                    case 4:
                        color = new Color(0.5f, 0.7f, 1f, a);
                        break;
                    case 3:
                        color = new Color(0.5f, 0.6f, 1f, a);
                        break;
                }
            }
            else
            {
                float a2 = 0.7f;
                switch (m_type)
                {
                    case 0:
                        color = new Color(0.55f, 0.5f, 1f, a2);
                        break;
                    case 1:
                        color = new Color(0.65f, 0.5f, 1f, a2);
                        break;
                    case 2:
                    case 4:
                        color = new Color(0.6f, 0.5f, 1f, a2);
                        break;
                    case 3:
                        color = new Color(0.7f, 0.5f, 1f, a2);
                        break;
                }
            }
        }

        m_color = color;
    }

    public void UpdateSelf()
    {
        if (m_type != 0 && m_type != 1)
        {
            return;
        }

        Sides = 0;
        if (Contraption.Instance.ConnectedToGearbox(Part))
        {
            Gearbox gearbox = Contraption.Instance.GetGearbox(Part);
            if (gearbox.m_partTier != 0)
            {
                BasePart.GridRotation gridRotation = Part.m_gridRotation;
                bool flag = gearbox.IsEnabled() ^ ((((gridRotation == BasePart.GridRotation.Deg_0) || (gridRotation == BasePart.GridRotation.Deg_45)) || (gridRotation == BasePart.GridRotation.Deg_90)) || (gridRotation == BasePart.GridRotation.Deg_135));
                Sides = (flag ? 1 : 2);
            }
        }
    }

    private float GetPowerConsumption(float velocity, float mass)
    {
        float num = 2f * mass / (2f + mass);
        float num2 = Math.Abs(velocity);
        return 0.75f * (0.5f * num * num2 * num2 + 8f * num * num2);
    }

    public void HandleCollision(ref EntityLightManager.CCDData data, ref EntityLightManager.TOIResult result)
    {
        BasePart component = data.Rigidbody.GetComponent<BasePart>();
        if (!m_ignoreCollision || !(component != null) || component.ConnectedComponent != Part.ConnectedComponent)
        {
            int type = Type;
            if (((type == 0) || (type == 1)))
            {
                HandlePillarCollision(ref data, ref result);
            }
            else
            {
                HandleShieldAndBoxCollision(ref data, ref result);
            }
        }
    }

    private void HandlePillarCollision(ref EntityLightManager.CCDData data, ref EntityLightManager.TOIResult result)
    {
        BasePart component = data.Rigidbody.GetComponent<BasePart>();
        if (!m_ignoreCollision || !(component != null) || component.ConnectedComponent != Part.ConnectedComponent)
        {
            INPhysicMaterial material = INContraption.GetMaterial(data.Rigidbody);
            INBounds bounds = data.Bounds;
            float timeOfImpact = result.TimeOfImpact; //data import
            float x = result.ContactNormal.x;
            float y = result.ContactNormal.y;
            float x2 = result.RelativeVelocity.x;
            float y2 = result.RelativeVelocity.y;
            Vector2 value = Vector.InvTransform(data.Direction1, m_data.Direction1);
            Vector2 vector = Vector.Transform(value, result.ContactPoint);
            float num3 = y2 / data.DeltaTime;
            float mass = data.Rigidbody.mass;
            float electricity = -GetPowerConsumption(num3, mass);
            float x3 = vector.x;
            float y3 = vector.y;
            const float num = 0.01f;
            PillarCollision pillarCollision = new PillarCollision()
            {
                timeOfImpact = result.TimeOfImpact,
                x = result.ContactNormal.x,
                y = result.ContactNormal.y,
                x2 = result.RelativeVelocity.x,
                y2 = result.RelativeVelocity.y,
                value = Vector.InvTransform(data.Direction1, m_data.Direction1),
                vector = Vector.Transform(value, result.ContactPoint),
                boundsIsCircle = bounds.IsCircle,
                boundsR = bounds.R,
                determinator = (Math.Abs(value.x) < num || Math.Abs(value.x) < num) && bounds.IsRect,
                zAngularVelocity = data.Rigidbody.angularVelocity.z,
                deltaTime = data.DeltaTime,
                mass = data.Rigidbody.mass,
                electricity = -GetPowerConsumption(num3, mass),
                bounce = m_physicMaterial.CombineBounce(material),
                friction = m_physicMaterial.CombineFriction(material),
                ContactSeparation = result.ContactSeparation,
                Vector2s = deltaVectors,
                numbers = physicFloats
            };
            JobHandle jobHandle = pillarCollision.Schedule(8, 128);
            jobHandle.Complete();
            data.Position0 = data.Position0 * (1f - timeOfImpact) + data.Position1 * timeOfImpact;
            data.Position1 += deltaVectors[0];
            data.DeltaTime *= 1f - timeOfImpact;
            data.Time += (1f - data.Time) * timeOfImpact;
            m_manager.AddImpulse(this, new EntityLightManager.ImpulseData(this, data.Rigidbody, data.Position0, deltaVectors[0], deltaVectors[1], physicFloats[0] + physicFloats[1], electricity));
            if (component != null)
            {
                Vector2 contactPoint = data.Position1 + new Vector2(x * y3, y * y3);
                SendLightEvent(data.Rigidbody, component, new EntityLightCollision(contactPoint, deltaVectors[0], deltaVectors[1]));
            }
        }
    }

    [BurstCompile]
    private struct PillarCollision : IJobParallelFor
    {
        [ReadOnly]
        public float timeOfImpact;
        [ReadOnly]
        public float x;
        [ReadOnly]
        public float y;
        [ReadOnly]
        public float x2;
        [ReadOnly]
        public float y2;
        [ReadOnly]
        public Vector2 value;
        [ReadOnly]
        public Vector2 vector;
        [ReadOnly]
        public bool boundsIsCircle;
        [ReadOnly]
        public float boundsR;
        [ReadOnly]
        public bool determinator;
        [ReadOnly]
        public float zAngularVelocity;
        [ReadOnly]
        public float deltaTime;
        [ReadOnly]
        public float mass;
        [ReadOnly]
        public float electricity;
        [ReadOnly]
        public float bounce;
        [ReadOnly]
        public float friction;
        [ReadOnly]
        public float ContactSeparation;
        //out
        [WriteOnly]
        public NativeArray<float> numbers;
        [WriteOnly]
        public NativeArray<Vector2> Vector2s;
        public void Execute(int ohhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh)
        {
            if (boundsIsCircle)
            {
                vector = new Vector2(0f, 0f - boundsR);
            }

            float x3 = vector.x;
            float y3 = vector.y;
            float xVelocity = x2 / deltaTime;
            float yVelocity = y2 / deltaTime;
            float num4 = xVelocity - zAngularVelocity * y3;
            float num5 = yVelocity + zAngularVelocity * x3;
            float num6 = bounce;
            float num7 = friction;
            float num8 = ((num4 > 0f) ? (-1f) : 1f) * num7 * ((num5 > 0f) ? num5 : (0f - num5));
            if ((num4 + num8 > 0f) ^ (num4 > 0f))
            {
                num8 = 0f - num4;
            }

            float num9 = (0f - (1f - timeOfImpact)) * y2 * (num6 + 1f) - ContactSeparation;
            float num10 = (0f - yVelocity) * (num6 + 1f);
            Vector2s[0] = new Vector2(x * num9, y * num9); //originally vector2
            Vector2s[1] = new Vector2(x * num10 + y * num8, y * num10 - x * num8); //originally deltaVelocity
            numbers[0] = (0f - x3) * ((!determinator) ? num5 : (2f * zAngularVelocity * x3)) * (num6 + 1f);
            numbers[1] = (0f - num8) * y3;
        }
    }

    private void HandleShieldAndBoxCollision(ref EntityLightManager.CCDData data, ref EntityLightManager.TOIResult result)
    {
        BasePart component = data.Rigidbody.GetComponent<BasePart>();
        if (!m_ignoreCollision || !(component != null) || component.ConnectedComponent != Part.ConnectedComponent)
        {
            INPhysicMaterial material = INContraption.GetMaterial(data.Rigidbody);
            LightData data2 = m_data;
            Vector2 vector = data.Position0 - data2.Position0;
            Vector2 vector2 = data.Position1 - data2.Position1;
            Vector2 vector3 = Vector.Transform(result.ContactNormal, data.Direction1);
            Vector.Transform(result.ContactPoint, data.Direction1);
            float timeOfImpact = result.TimeOfImpact;
            float num = Vector.Dot(vector2 - vector, vector3);
            float num4 = num / data.DeltaTime;
            float electricity = -GetPowerConsumption(num4, data.Rigidbody.mass);
            //job section end
            float mass;
            ShieldAndBoxCollision shieldAndBoxCollision = new ShieldAndBoxCollision()
            {
                timeOfImpact = result.TimeOfImpact,
                deltaTime = data.DeltaTime,
                vector = data.Position0 - data2.Position0,
                vector2 = data.Position1 - data2.Position1,
                ContactNormal = result.ContactNormal,
                ContactPoint = result.ContactPoint,
                Direction1 = data.Direction1,
                boundsIsCircle = data.Bounds.IsCircle,
                boundsR = data.Bounds.R,
                determinator = result.ContactCount == 2,
                zAngularVelocity = data.Rigidbody.angularVelocity.z,
                bounce = m_physicMaterial.CombineBounce(material),
                ContactSeparation = result.ContactSeparation,
                type = m_type,
                length = m_length,
                halfWidth = m_halfWidth,
                physicFloats = physicFloats,
                deltaVectors = deltaVectors
            };
            JobHandle jobHandle = shieldAndBoxCollision.Schedule(8, 128);
            jobHandle.Complete();
            data.Position0 = data.Position0 * (1f - timeOfImpact) + data.Position1 * timeOfImpact + deltaVectors[0];
            data.Position1 += deltaVectors[1];
            data.DeltaTime *= 1f - timeOfImpact;
            data.Time += (1f - data.Time) * timeOfImpact;
            m_manager.AddImpulse(this, new EntityLightManager.ImpulseData(this, data.Rigidbody, data.Position0, deltaVectors[1], deltaVectors[2], physicFloats[0], electricity));
            SendLightEvent(data.Rigidbody, new EntityLightCollision(data.Position1, deltaVectors[1], deltaVectors[2]));
        }
    }

    [BurstCompile]
    private struct ShieldAndBoxCollision : IJobParallelFor
    {
        [ReadOnly]
        public float timeOfImpact;
        [ReadOnly]
        public float deltaTime;
        [ReadOnly]
        public Vector2 vector;
        [ReadOnly]
        public Vector2 vector2;
        [ReadOnly]
        public Vector2 ContactNormal;
        [ReadOnly]
        public Vector2 ContactPoint;
        [ReadOnly]
        public Vector2 Direction1;
        [ReadOnly]
        public bool boundsIsCircle;
        [ReadOnly]
        public float boundsR;
        [ReadOnly]
        public bool determinator;
        [ReadOnly]
        public float zAngularVelocity;
        [ReadOnly]
        public float bounce;
        [ReadOnly]
        public float ContactSeparation;
        [ReadOnly]
        public int type;
        [ReadOnly]
        public float length;
        [ReadOnly]
        public float halfWidth;
        //out
        [WriteOnly]
        public NativeArray<float> physicFloats;
        [WriteOnly]
        public NativeArray<Vector2> deltaVectors;
        public void Execute(int index)
        {
            Vector2 vector3 = Vector.Transform(ContactNormal, Direction1);
            float num = Vector.Dot(vector2 - vector, vector3);
            float num2 = 0f - Vector.Cross(vector2 - vector, vector3);
            float num3 = num * (1f - timeOfImpact);
            Vector2 vector4 = Vector.Transform(Direction1, ContactPoint);
            if (boundsIsCircle)
            {
                vector4 = new Vector2(0f, 0f - boundsR);
            }

            float x = vector4.x;
            float y = vector4.y;
            _ = num2 / deltaTime;
            float num4 = num / deltaTime;
            float num5 = num4 + zAngularVelocity * x;
            float num7 = -ContactSeparation;
            Vector2 vector5 = num7 * vector3;
            float num8 = (0f - num3) * (bounce + 1f) + num7;
            float num9 = (0f - num4) * (bounce + 1f);
            if (type == 3)
            {
                float num10 = 0.5f * num2 * num2 / (length - halfWidth);
                num8 += (1f - timeOfImpact) * num10;
                num9 += num10 / deltaTime;
            }

            Vector2 vector6 = vector3 * num8;
            Vector2 deltaVelocity = vector3 * num9;
            float torque = (0f - x) * ((!determinator) ? num5 : (2f * zAngularVelocity * x)) * (bounce + 1f);
            deltaVectors[0] = vector5;
            deltaVectors[1] = vector6;
            deltaVectors[2] = deltaVelocity;
            physicFloats[0] = torque;
        }
    }

    private void SendLightEvent(Rigidbody rigidbody, EntityLightCollision collision)
    {
        SendLightEvent(rigidbody, rigidbody.GetComponent<BasePart>(), collision);
    }

    private void SendLightEvent(Rigidbody rigidbody, BasePart part, EntityLightCollision collision)
    {
        if (part != null)
        {
            part.OnLightEnter(collision);
        }
    }

    private void Update()
    {
        if (!Contraption.Instance.IsRunning)
        {
            if (m_type == 4)
            {
                m_meshRenderer.material.color = Color.clear;
                return;
            }

            if (Part.m_enclosedInto != null)
            {
                m_meshRenderer.material.color = new Color(1f, 1f, 1f, 0.1f);
                return;
            }

            Contraption instance = Contraption.Instance;
            int num = 1;
            int num2 = 0;
            for (int i = 0; i < 4; i++)
            {
                BasePart part = instance.FindPartAt(Part.m_coordX + num, Part.m_coordY + num2);
                if (instance.CanConnectTo(Part, part, (BasePart.Direction)i))
                {
                    m_meshRenderer.material.color = new Color(1f, 1f, 1f, 0.1f);
                    return;
                }

                int num3 = num;
                num = -num2;
                num2 = num3;
            }

            m_meshRenderer.material.color = Color.clear;
        }
        else if (!m_enabled)
        {
            m_meshRenderer.material.color = Color.clear;
        }
        else
        {
            Color color = m_color;
            Color a = (m_colored ? m_color : new Color(1f, 0.25f, 0.25f));
            a.a = 0.1f;
            if (m_type == 4)
            {
                color.a /= m_coefficient;
                a.a /= m_coefficient;
            }

            if (!m_manager.ConsumePower)
            {
                m_meshRenderer.material.color = color;
                return;
            }

            float num4 = m_manager.m_electricities[Index] / m_manager.m_capacities[Index];
            m_meshRenderer.material.color = Color.Lerp(a, color, (num4 > 0f) ? Mathf.Sqrt(num4) : 0f);
        }
    }

    public static bool RaycastWithLights(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
    {
        RaycastHit[] array = Physics.RaycastAll(origin, direction, maxDistance, layerMask);
        int num = -1;
        float num2 = float.PositiveInfinity;
        for (int i = 0; i < array.Length; i++)
        {
            RaycastHit raycastHit = array[i];
            if (raycastHit.distance < num2 && CheckRaycast(raycastHit))
            {
                num2 = raycastHit.distance;
                num = i;
            }
        }

        if (num == -1)
        {
            hitInfo = default(RaycastHit);
            return false;
        }

        hitInfo = array[num];
        return true;
    }

    public static bool CheckRaycast(RaycastHit raycastHit)
    {
        Rigidbody rigidbody = raycastHit.rigidbody;
        if (rigidbody == null)
        {
            return true;
        }

        EntityLight component = rigidbody.GetComponent<EntityLight>();
        if (component == null || !component.IsLightPillar || !Contraption.Instance.ConnectedToGearbox(component.Part))
        {
            return true;
        }

        Gearbox gearbox = Contraption.Instance.GetGearbox(component.Part);
        if (gearbox.m_partTier == BasePart.PartTier.Regular)
        {
            return true;
        }

        Vector2 direction = component.m_data.Direction1;
        Vector2 position = component.m_data.Position1;
        Vector3 point = raycastHit.point;
        bool num = direction.x * (point.y - position.y) - direction.y * (point.x - position.x) > 0f;
        BasePart.GridRotation gridRotation = component.Part.m_gridRotation;
        return num ^ gearbox.IsEnabled() ^ ((((gridRotation == BasePart.GridRotation.Deg_0) || (gridRotation == BasePart.GridRotation.Deg_45)) || (gridRotation == BasePart.GridRotation.Deg_90)) || (gridRotation == BasePart.GridRotation.Deg_135));
    }
}