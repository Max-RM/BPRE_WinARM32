using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class EntityLightManager : PartManager
{
    /// <summary>
    /// <code>Fixed Memory Allocator with certain usage</code>
    /// <example>nativeBooleans[0] is reserved for <see cref = "BroadPhaseDetectJob"/></example>
    /// </summary>
    private static NativeArray<bool> nativeBooleans;
    /// <summary>
    /// <code>Fixed Memory Allocator with certain usage</code>
    /// <example>nativeFloats[0] is reserved for <see cref = "LineAndCircleIntersectionJob"/>  out float result1</example>
    /// <example> nativeFloats[1] is reserved for <see cref = "LineAndCircleIntersectionJob"/>  out float result2</example>
    /// 
    /// </summary>
    private static NativeArray<float> nativeFloats;
    /// <summary>
    /// <code>Fixed Memory Allocator with certain usage</code>
    /// <example>nativeInts[0] is reserved for <see cref = "LineAndCircleIntersectionJob"/>  out int resultCount</example>
    /// </summary>
    private static NativeArray<int> nativeInts;
    public override void OnDestroy()
    {
        nativeBooleans.Dispose();
        nativeFloats.Dispose();
        nativeInts.Dispose();
    }

    public struct RigidbodyData
    {
        public Rigidbody Rigidbody;
        public Vector2 Position;
        public Vector2 Direction;
        public float DeltaTime;
        public RigidbodyData(Rigidbody rigidbody, Vector2 position, float deltaTime)
        {
            Quaternion rotation = rigidbody.rotation;
            Rigidbody = rigidbody;
            Position = position;
            Direction.x = 1f - (rotation.y * rotation.y * 2f + rotation.z * rotation.z * 2f);
            Direction.y = rotation.x * rotation.y * 2f + rotation.w * rotation.z * 2f;
            DeltaTime = deltaTime;
        }
    }

    public readonly struct TOIResult
    {
        public readonly float TimeOfImpact;
        public readonly int ContactCount;
        public readonly Vector2 ContactPoint;
        public readonly Vector2 ContactNormal;
        public readonly float ContactSeparation;
        public readonly Vector2 RelativeVelocity;
        public static TOIResult Default => default(TOIResult);

        public TOIResult(float timeOfImpact, int contactCount = 0, Vector2 contactPoint = default(Vector2), Vector2 contactNormal = default(Vector2), float contactSeparation = 0f, Vector2 relativeVelocity = default(Vector2))
        {
            TimeOfImpact = timeOfImpact;
            ContactCount = contactCount;
            ContactPoint = contactPoint;
            ContactNormal = contactNormal;
            ContactSeparation = contactSeparation;
            RelativeVelocity = relativeVelocity;
        } //fix ugly code
    }

    public struct CCDData
    {
        public Rigidbody Rigidbody;
        public Vector2 Position0;
        public Vector2 Direction0;
        public Vector2 Position1;
        public Vector2 Direction1;
        public INBounds Bounds;
        public float Time;
        public float DeltaTime;
        public CCDData(RigidbodyData data)
        {
            this = default(CCDData);
            Set(data);
        }

        public void Set(RigidbodyData data)
        {
            Rigidbody rigidbody = data.Rigidbody;
            Vector3 position = rigidbody.position;
            Quaternion rotation = rigidbody.rotation;
            INBounds bounds = INContraption.GetBounds(rigidbody);
            Rigidbody = rigidbody;
            Direction0 = data.Direction;
            Position0.x = data.Position.x + Direction0.x * bounds.X - Direction0.y * bounds.Y;
            Position0.y = data.Position.y + Direction0.x * bounds.Y + Direction0.y * bounds.X;
            Direction1.x = 1f - (rotation.y * rotation.y * 2f + rotation.z * rotation.z * 2f);
            Direction1.y = rotation.x * rotation.y * 2f + rotation.w * rotation.z * 2f;
            Position1.x = position.x + Direction1.x * bounds.X - Direction1.y * bounds.Y;
            Position1.y = position.y + Direction1.x * bounds.Y + Direction1.y * bounds.X;
            Bounds = bounds;
            Time = 0f;
            DeltaTime = data.DeltaTime;
        }
    }

    public readonly struct ImpulseData
    {
        public readonly EntityLight Light;
        public readonly Rigidbody Rigidbody;
        public readonly Vector2 Position;
        public readonly Vector2 DeltaPosition;
        public readonly Vector2 DeltaVelocity;
        public readonly float Torque;
        public readonly float Electricity;
        public ImpulseData(EntityLight light, Rigidbody rigidbody, Vector2 position, Vector2 deltaPosition, Vector2 deltaVelocity, float torque, float electricity)
        {
            Light = light;
            Rigidbody = rigidbody;
            Position = position;
            DeltaPosition = deltaPosition;
            DeltaVelocity = deltaVelocity;
            Torque = torque;
            Electricity = electricity;
        }
    }

    public float[] m_electricities;
    public float[] m_capacities;
    private int[] m_lightCounts;
    private int m_rigidbodyCount;
    private int m_componentCount;
    private int m_alienComponentCount;
    private float m_maxDetectionDistance;
    private float m_maxTorque;
    private RigidbodyData[] m_rigidbodyData;
    private List<EntityLight> m_lights;
    private List<ImpulseData>[] m_lightImpulses;
    public static EntityLightManager Instance { get; private set; }
    public bool ConsumePower { get; private set; }

    protected override void Initialize()
    {
        base.Initialize();
        m_status = StatusCode.Running;
        Instance = this;
    }

    public override void Start()
    {
        ConsumePower = !Contraption.Instance.HasTurboCharge;
        m_rigidbodyCount = 0;
        m_rigidbodyData = Array.Empty<RigidbodyData>();
        m_lights = new List<EntityLight>();
        m_lightImpulses = Array.Empty<List<ImpulseData>>();
        m_maxDetectionDistance = 40f;
        m_maxTorque = 1f;
        InitNativeArrays();
    }

    public void InitNativeArrays()
    {
        nativeBooleans = new NativeArray<bool>(length: 8, allocator: Allocator.Persistent);
        nativeFloats = new NativeArray<float>(length: 8, allocator: Allocator.Persistent);
        nativeInts = new NativeArray<int>(length: 8, allocator: Allocator.Persistent);
    }

    public override void FixedUpdate()
    {
        m_lights.Clear();
        foreach (BasePart part in Contraption.Instance.Parts)
        {
            switch (part)
            {
                case PointLight pointLight when pointLight.EntityLight != null:
                    m_lights.Add(pointLight.EntityLight);
                    break;
                case SpotLight spotLight when spotLight.EntityLight != null:
                    m_lights.Add(spotLight.EntityLight);
                    break;
            }
        }

        _ = m_lights.Count;
        List<EntityLight> lights = m_lights;
        UpdateLights(lights);
        UpdateAlienLights(lights);
        foreach (EntityLight item in lights)
        {
            if (item.Type != 4)
            {
                item.Data.Set(item);
            }

            item.UpdateSelf();
        }

        CCDData data = default(CCDData);
        bool[] array = new bool[m_componentCount];
        List<EntityLight> list = new List<EntityLight>();
        for (int i = 0; i < m_rigidbodyCount; i++)
        {
            RigidbodyData data2 = m_rigidbodyData[i];
            if (data2.Rigidbody == null || data2.Rigidbody.IsFixed())
            {
                continue;
            }

            data.Set(data2);
            list.Clear();
            Array.Clear(array, 0, array.Length);
            foreach (EntityLight item2 in lights)
            {
                if (!item2.Enabled)
                {
                    continue;
                }

                if (item2.Type == 4)
                {
                    if (array[item2.Index / 5])
                    {
                        continue;
                    }

                    array[item2.Index / 5] = true;
                }

                if (BroadPhaseDetect(item2, ref data))
                {
                    list.Add(item2);
                }
            }

            int num = 4;
            int count = list.Count;
            int num2 = -1;
            for (int j = 0; j < num; j++)
            {
                int num3 = -1;
                TOIResult result = TOIResult.Default;
                for (int k = 0; k < count; k++)
                {
                    if (k != num2 && ComputeTimeOfImpact(list[k], ref data, out TOIResult result2) && (num3 == -1 || result2.TimeOfImpact < result.TimeOfImpact))
                    {
                        num3 = k;
                        result = result2;
                    }
                }

                if (num3 == -1)
                {
                    break;
                }

                num2 = num3;
                list[num3].HandleCollision(ref data, ref result);
            }
        }

        for (int l = 0; l < m_lightImpulses.Length; l++)
        {
            float num4 = 0f;
            List<ImpulseData> list2 = m_lightImpulses[l];
            foreach (ImpulseData item3 in list2)
            {
                num4 -= item3.Electricity;
            }

            float val = Math.Min(num4, m_electricities[l]);
            float num5 = ((num4 > 0f && ConsumePower) ? (Math.Max(val, 0f) / num4) : 1f);
            foreach (ImpulseData item4 in list2)
            {
                EntityLight light = item4.Light;
                Rigidbody rigidbody = item4.Rigidbody;
                float num6 = item4.Electricity * num5 * num5;
                float num7 = num5 * Defend(item4.Light.Data.Position1, item4.Position, num6, m_electricities[light.Index]);
                if (light.Type == 4)
                {
                    foreach (EntityLight item5 in lights)
                    {
                        if (item5.Index == light.Index)
                        {
                            item5.m_electricity += (ConsumePower ? (num6 / light.m_coefficient) : 0f);
                        }
                    }
                }
                else
                {
                    light.m_electricity += (ConsumePower ? num6 : 0f);
                }

                Vector3 position = rigidbody.position + (Vector3)(item4.DeltaPosition * num7);
                Vector3 force = item4.DeltaVelocity * num7;
                rigidbody.position = position;
                rigidbody.AddForce(force, ForceMode.VelocityChange);
                float torque = item4.Torque;
                torque = UnityEngine.Mathf.Clamp(torque, 0f - m_maxTorque, m_maxTorque);
                rigidbody.AddTorque(new Vector3(0f, 0f, torque), ForceMode.VelocityChange);
            }
        }

        Rigidbody[] components = INContraption.Instance.GetComponents<Rigidbody>();
        m_rigidbodyCount = components.Length;
        if (m_rigidbodyData.Length < m_rigidbodyCount)
        {
            m_rigidbodyData = new RigidbodyData[m_rigidbodyCount];
        }

        for (int m = 0; m < m_rigidbodyCount; m++)
        {
            Rigidbody rigidbody2 = components[m];
            m_rigidbodyData[m] = new RigidbodyData(rigidbody2, rigidbody2.position, Time.fixedDeltaTime);
        }
    }

    private bool BroadPhaseDetect(EntityLight light, ref CCDData data)
    {
        EntityLight.LightData data2 = light.Data;
        BroadPhaseDetectJob broadPhaseDetectJob = new BroadPhaseDetectJob
        {
            type = light.Type,
            dataPosition0x = data.Position0.x,
            dataPosition0y = data.Position0.y,
            dataPosition1x = data.Position1.x,
            dataPosition1y = data.Position1.y,
            data2Position0x = data2.Position0.x,
            data2Position0y = data2.Position0.y,
            data2Position1x = data2.Position1.x,
            data2Position1y = data2.Position1.y,
            data2Direction0x = data2.Direction0.x,
            data2Direction0y = data2.Direction0.y,
            data2Direction1x = data2.Direction1.x,
            data2Direction1y = data2.Direction1.y,
            maxDetectionDistance = m_maxDetectionDistance,
            Length = light.Length,
            HalfWidth = light.HalfWidth,
            NativeBooleans = nativeBooleans
        };
        JobHandle jobHandle = broadPhaseDetectJob.Schedule(8, 8);
        jobHandle.Complete();
        return nativeBooleans[0];
    }

    [BurstCompile]
    private struct BroadPhaseDetectJob : IJobParallelFor
    {
        [ReadOnly]
        public int type;
        [ReadOnly]
        public float dataPosition0x;
        [ReadOnly]
        public float dataPosition0y;
        [ReadOnly]
        public float dataPosition1x;
        [ReadOnly]
        public float dataPosition1y;
        [ReadOnly]
        public float data2Position0x;
        [ReadOnly]
        public float data2Position0y;
        [ReadOnly]
        public float data2Position1x;
        [ReadOnly]
        public float data2Position1y;
        [ReadOnly]
        public float data2Direction0x;
        [ReadOnly]
        public float data2Direction0y;
        [ReadOnly]
        public float data2Direction1x;
        [ReadOnly]
        public float data2Direction1y;
        [ReadOnly]
        public float maxDetectionDistance;
        [ReadOnly]
        public float Length;
        [ReadOnly]
        public float HalfWidth;
        [WriteOnly]
        public NativeArray<bool> NativeBooleans;
        public void Execute(int index)
        {
            if (((type == 0) || (type == 1)))
            {
                float valueX = dataPosition0x - data2Position0x;
                float valueY = dataPosition0y - data2Position0y;
                float valueX2 = dataPosition1x - data2Position1x;
                float valueY2 = dataPosition1y - data2Position1y;
                float x = data2Direction0x;
                float y = data2Direction0y;
                float x2 = data2Direction1x;
                float y2 = data2Direction1y;
                Vector.InvTransform(valueX, valueY, x, y, out var resultX, out var resultY);
                Vector.InvTransform(valueX2, valueY2, x2, y2, out var resultX2, out var resultY2);
                float length = Length;
                float halfWidth = HalfWidth;
                if ((resultX < 0f - maxDetectionDistance || resultX > length + maxDetectionDistance) && (resultX2 < 0f - maxDetectionDistance || resultX2 > length + maxDetectionDistance))
                {
                    NativeBooleans[0] = false;
                }

                NativeBooleans[0] = !(Math.Abs(resultY) > halfWidth + maxDetectionDistance) || !(Math.Abs(resultY2) > halfWidth + maxDetectionDistance);
            }

            float num = dataPosition0x - data2Position0x;
            float num2 = dataPosition1x - data2Position1x;
            float num3 = dataPosition1y - data2Position1y;
            float num4 = num * num + num * num;
            float num5 = num2 * num2 + num3 * num3;
            float num6 = Length + maxDetectionDistance;
            float num7 = num6 * num6;
            NativeBooleans[0] = !(num4 > num7) || !(num5 > num7);
        }
    }

    private bool ComputeTimeOfImpact(EntityLight light, ref CCDData data, out TOIResult result)
    {
        switch (light.Type)
        {
            case 0:
            case 1:
                return ComputeTimeOfImpactRect(light, ref data, out result);
            case 2:
            case 4:
                return ComputeTimeOfImpactOuterCircle(light, ref data, out result);
            default:
                return ComputeTimeOfImpactInnerCircle(light, ref data, out result);
        }
    }

    private bool ComputeTimeOfImpactRect(EntityLight light, ref CCDData data, out TOIResult result)
    {
        result = TOIResult.Default;
        EntityLight.LightData data2 = light.Data;
        float length = light.Length;
        float valueX = data.Position0.x - data2.Position0.x;
        float valueY = data.Position0.y - data2.Position0.y;
        float valueX2 = data.Position1.x - data2.Position1.x;
        float valueY2 = data.Position1.y - data2.Position1.y;
        float x = data2.Direction0.x;
        float y = data2.Direction0.y;
        float x2 = data2.Direction1.x;
        float y2 = data2.Direction1.y;
        Vector.InvTransform(valueX, valueY, x, y, out var resultX, out var resultY);
        Vector.InvTransform(valueX2, valueY2, x2, y2, out var resultX2, out var resultY2);
        if ((!(0f < resultX) || !(resultX < length)) && (!(0f < resultX2) || !(resultX2 < length)))
        {
            return false;
        }

        int sides = light.Sides;
        float halfWidth = light.HalfWidth;
        INBounds bounds = data.Bounds;
        Vector.InvTransform(data.Direction1, data2.Direction1, out var resultX3, out var resultY3);
        float halfProjection = bounds.GetHalfProjection(0f - resultY3, resultX3);
        float num = resultY - halfProjection - halfWidth;
        float num2 = resultY2 - halfProjection - halfWidth;
        float num3 = resultY + halfProjection + halfWidth;
        float num4 = resultY2 + halfProjection + halfWidth;
        bool flag = num > -0.25f && num2 < 0f && num2 < num;
        bool flag2 = num3 < 0.25f && num4 > 0f && num4 > num3;
        if (sides != 0)
        {
            flag = flag && (sides & 1) == 0;
            flag2 = flag2 && (sides & 2) == 0;
        }

        if (!flag && !flag2)
        {
            return false;
        }

        int num5 = (flag ? 1 : (-1));
        float num6 = (flag ? num : (0f - num3));
        float num7 = (flag ? num2 : (0f - num4));
        float num8 = (0f - num6) / (num7 - num6);
        float contactSeparation = 0f;
        if (Math.Abs(num7 - num6) < 1E-05f || num8 < 0f)
        {
            num8 = 0f;
            contactSeparation = num7;
        }

        bounds.GetLocalContactPoint(resultX3, resultY3, out var pointX, out var pointY);
        Vector2 contactPoint = new Vector2(pointX, pointY);
        Vector2 contactNormal = new Vector2(-num5 * y2, num5 * x2);
        Vector2 relativeVelocity = new Vector2(num5 * (resultX2 - resultX), num5 * (resultY2 - resultY));
        result = new TOIResult(num8, 1, contactPoint, contactNormal, contactSeparation, relativeVelocity);
        return true;
    }

    private bool ComputeTimeOfImpactOuterCircle(EntityLight light, ref CCDData data, out TOIResult result)
    {
        result = TOIResult.Default;
        EntityLight.LightData data2 = light.Data;
        float time = data.Time;
        Vector2 vector = (1f - time) * data2.Position0 + time * data2.Position1 - data.Position0;
        Vector2 vector2 = data2.Position1 - data.Position1;
        if (0f - Vector.Dot(vector, data2.Direction0) <= light.Cos * Vector.Length(vector) && 0f - Vector.Dot(vector2, data2.Direction1) <= light.Cos * Vector.Length(vector2))
        {
            return false;
        }

        float x3 = data.Direction1.x;
        float y = data.Direction1.y;
        float num = x3 * vector.x + y * vector.y;
        float num2 = x3 * vector.y - y * vector.x;
        float num3 = x3 * vector2.x + y * vector2.y;
        float num4 = x3 * vector2.y - y * vector2.x;
        float num5 = num3 - num;
        float num6 = num4 - num2;
        if (Vector.LengthSquared(num5, num6) < 1E-10f)
        {
            return false;
        }

        float num7 = light.Length + light.HalfWidth;
        float num8 = num7 - 0.5f;
        float num9 = num7 * num7;
        float num10 = num8 * num8;
        INBounds bounds = data.Bounds;
        int resultCount = 0;
        float result2 = 0f;
        float result3 = 0f;
        int resultCount2 = 0;
        float result4 = 0f;
        float result5 = 0f;
        if (bounds.IsCircle)
        {
            LineAndCircleIntersection(num, num2, num5, num6, 0f, 0f, num7 + bounds.R, out resultCount, out result2, out result3);
            LineAndCircleIntersection(num, num2, num5, num6, 0f, 0f, num8 + bounds.R, out resultCount2, out result4, out result5);
        }
        else if (Math.Abs(num5) < 1E-05f)
        {
            float num11 = Math.Max(Math.Abs(num) - bounds.A, 0f);
            int num12 = ((num6 > 0f) ? 1 : (-1));
            if (num7 > num11)
            {
                float num13 = num12 * ((float)Math.Sqrt(num9 - num11 * num11) + bounds.B);
                resultCount = 2;
                result2 = (0f - num13 - num2) / num6;
                result3 = (num13 - num2) / num6;
            }

            if (num8 > num11)
            {
                float num14 = num12 * ((float)Math.Sqrt(num10 - num11 * num11) + bounds.B);
                resultCount2 = 2;
                result4 = (0f - num14 - num2) / num6;
                result5 = (num14 - num2) / num6;
            }
        }
        else if (Math.Abs(num6) < 1E-05f)
        {
            float num15 = Math.Max(Math.Abs(num2) - bounds.B, 0f);
            int num16 = ((num5 > 0f) ? 1 : (-1));
            if (num7 > num15)
            {
                float num17 = num16 * ((float)Math.Sqrt(num9 - num15 * num15) + bounds.A);
                resultCount = 2;
                result2 = (0f - num17 - num) / num5;
                result3 = (num17 - num) / num5;
            }

            if (num8 > num15)
            {
                float num18 = num16 * ((float)Math.Sqrt(num10 - num15 * num15) + bounds.A);
                resultCount2 = 2;
                result4 = (0f - num18 - num) / num5;
                result5 = (num18 - num) / num5;
            }
        }
        else
        {
            float item = (0f - bounds.A - num) / num5;
            float item2 = (bounds.A - num) / num5;
            float item3 = (0f - bounds.B - num2) / num6;
            float item4 = (bounds.B - num2) / num6;
            SortFour((item, 0), (item2, 1), (item3, 2), (item4, 3), out (float, int) b, out (float, int) b2, out (float, int) b3, out (float, int) b4);
            int num19 = -1;
            int num20 = -1;
            float num21 = float.NegativeInfinity;
            for (int i = 0; i < 5; i++)
            {
                (float, int) tuple = ((i >= 2) ? ((i == 2) ? b3 : b4) : ((i == 0) ? b : b2));
                float num22;
                if (i < 4)
                {
                    (num22, _) = tuple;
                }
                else
                {
                    num22 = float.PositiveInfinity;
                }

                float num23 = num22;
                int num24 = ((num5 > 0f) ? num19 : (-num19));
                int num25 = ((num6 > 0f) ? num20 : (-num20));
                float num26 = num24 * num5;
                float num27 = num24 * num - bounds.A;
                float num28 = num25 * num6;
                float num29 = num25 * num2 - bounds.B;
                if (num24 != 0 && num25 != 0)
                {
                    QuadraticFunction quadraticFunction = new QuadraticFunction(num26 * num26 + num28 * num28, 2f * (num26 * num27 + num28 * num29), num27 * num27 + num29 * num29);
                    QuadraticFunction.IntersectResult intersections = quadraticFunction.GetIntersections(num9);
                    QuadraticFunction.IntersectResult intersections2 = quadraticFunction.GetIntersections(num10);
                    for (int j = 0; j < intersections.Count; j++)
                    {
                        float num30 = intersections[j];
                        if (num21 <= num30 && num30 < num23)
                        {
                            SetResult(num30, ref resultCount, ref result2, ref result3);
                        }
                    }

                    for (int k = 0; k < intersections2.Count; k++)
                    {
                        float num31 = intersections2[k];
                        if (num21 <= num31 && num31 < num23)
                        {
                            SetResult(num31, ref resultCount2, ref result4, ref result5);
                        }
                    }
                }
                else if (num24 != 0 && num25 == 0)
                {
                    float num32 = (num7 - num27) / num26;
                    float num33 = (num8 - num27) / num26;
                    if (num21 <= num32 && num32 < num23)
                    {
                        SetResult(num32, ref resultCount, ref result2, ref result3);
                    }

                    if (num21 <= num33 && num33 < num23)
                    {
                        SetResult(num33, ref resultCount2, ref result4, ref result5);
                    }
                }
                else if (num24 == 0 && num25 != 0)
                {
                    float num34 = (num7 - num29) / num28;
                    float num35 = (num8 - num29) / num28;
                    if (num21 <= num34 && num34 < num23)
                    {
                        SetResult(num34, ref resultCount, ref result2, ref result3);
                    }

                    if (num21 <= num35 && num35 < num23)
                    {
                        SetResult(num35, ref resultCount2, ref result4, ref result5);
                    }
                }

                num21 = num23;
                if (tuple.Item2 < 2)
                {
                    num19++;
                }
                else
                {
                    num20++;
                }
            }
        }

        if (resultCount == 2 && ((resultCount2 == 2) ? result4 : result3) >= 0f && result2 <= 1f)
        {
            float num36 = ((result2 > 0f) ? result2 : 0f);
            float num37 = num + num5 * num36;
            float num38 = num2 + num6 * num36;
            Vector2 vector3 = new Vector2(num37, num38);
            Vector2 vector4 = default(Vector2);
            Vector2 vector5 = default(Vector2);
            float num39 = 0f;
            if (bounds.IsCircle)
            {
                num39 = Vector.Length(vector3);
                vector4 = bounds.R / num39 * vector3;
                vector5 = -1f / num39 * vector3;
                num39 -= bounds.R;
            }
            else
            {
                int num40 = ((num37 < 0f - bounds.A) ? (-1) : ((!(num37 < bounds.A)) ? 1 : 0));
                int num41 = ((num38 < 0f - bounds.B) ? (-1) : ((!(num38 < bounds.B)) ? 1 : 0));
                if (num40 == 0 && num41 == 0)
                {
                    return false;
                }

                if (num40 != 0 && num41 != 0)
                {
                    vector4 = new Vector2(num40 * bounds.A, num41 * bounds.B);
                    vector5 = vector4 - vector3;
                    num39 = Vector.Length(vector5);
                    vector5 /= num39;
                }
                else if (num40 == 0 && num41 != 0)
                {
                    vector4 = new Vector2(num37, num41 * bounds.B);
                    vector5 = new Vector2(0f, -num41);
                    num39 = Math.Abs(num38) - bounds.B;
                }
                else if (num40 != 0 && num41 == 0)
                {
                    vector4 = new Vector2(num40 * bounds.A, num38);
                    vector5 = new Vector2(-num40, 0f);
                    num39 = Math.Abs(num37) - bounds.A;
                }
            }

            result = new TOIResult(num36, 1, vector4, vector5, num39 - num7);
            return true;
        }

        return false;
        void SetResult(float value, ref int count, ref float x1, ref float x2)
        {
            if (count == 0)
            {
                count = 1;
                x1 = value;
            }
            else if (count == 1 && Math.Abs(value - x1) > 1E-05f)
            {
                count = 2;
                if (value >= x1)
                {
                    x2 = value;
                }
                else
                {
                    x2 = x1;
                    x1 = value;
                }
            }
        }
    }

    private bool ComputeTimeOfImpactInnerCircle(EntityLight light, ref CCDData data, out TOIResult result)
    {
        result = TOIResult.Default;
        EntityLight.LightData data2 = light.Data;
        float time = data.Time;
        Vector2 vector = (1f - time) * data2.Position0 + time * data2.Position1 - data.Position0;
        Vector2 vector2 = data2.Position1 - data.Position1;
        if (0f - Vector.Dot(vector, data2.Direction0) <= light.Cos * Vector.Length(vector) && 0f - Vector.Dot(vector2, data2.Direction1) <= light.Cos * Vector.Length(vector2))
        {
            return false;
        }

        float x3 = data.Direction1.x;
        float y = data.Direction1.y;
        float num = x3 * vector.x + y * vector.y;
        float num2 = x3 * vector.y - y * vector.x;
        float num3 = x3 * vector2.x + y * vector2.y;
        float num4 = x3 * vector2.y - y * vector2.x;
        float num5 = num3 - num;
        float num6 = num4 - num2;
        if (Vector.LengthSquared(num5, num6) < 1E-10f)
        {
            return false;
        }

        float num7 = light.Length - light.HalfWidth;
        float num8 = num7 + 0.5f;
        float num9 = num7 * num7;
        float num10 = num8 * num8;
        INBounds bounds = data.Bounds;
        int resultCount = 0;
        float result2 = 0f;
        float result3 = 0f;
        int resultCount2 = 0;
        float result4 = 0f;
        float result5 = 0f;
        if (bounds.IsCircle)
        {
            LineAndCircleIntersection(num, num2, num5, num6, 0f, 0f, num7 - bounds.R, out resultCount, out result2, out result3);
            LineAndCircleIntersection(num, num2, num5, num6, 0f, 0f, num8 - bounds.R, out resultCount2, out result4, out result5);
        }
        else if (Math.Abs(num5) < 1E-05f)
        {
            float num11 = Math.Abs(num) + bounds.A;
            int num12 = ((num6 > 0f) ? 1 : (-1));
            if (num7 > num11)
            {
                float num13 = num12 * ((float)Math.Sqrt(num9 - num11 * num11) - bounds.B);
                resultCount = 2;
                result2 = (0f - num13 - num2) / num6;
                result3 = (num13 - num2) / num6;
            }

            if (num8 > num11)
            {
                float num14 = num12 * ((float)Math.Sqrt(num10 - num11 * num11) - bounds.B);
                resultCount2 = 2;
                result4 = (0f - num14 - num2) / num6;
                result5 = (num14 - num2) / num6;
            }
        }
        else if (Math.Abs(num6) < 1E-05f)
        {
            float num15 = Math.Abs(num2) + bounds.B;
            int num16 = ((num5 > 0f) ? 1 : (-1));
            if (num7 > num15)
            {
                float num17 = num16 * ((float)Math.Sqrt(num9 - num15 * num15) - bounds.A);
                resultCount = 2;
                result2 = (0f - num17 - num) / num5;
                result3 = (num17 - num) / num5;
            }

            if (num8 > num15)
            {
                float num18 = num16 * ((float)Math.Sqrt(num10 - num15 * num15) - bounds.A);
                resultCount2 = 2;
                result4 = (0f - num18 - num) / num5;
                result5 = (num18 - num) / num5;
            }
        }
        else
        {
            float item = (0f - num) / num5;
            float item2 = (0f - num2) / num6;
            (float, int) tuple = (item, 0);
            (float, int) tuple2 = (item2, 1);
            if (tuple.CompareTo(tuple2) > 0)
            {
                (tuple, tuple2) = (tuple2, tuple); //swap
            }

            int num19 = -1;
            int num20 = -1;
            float num21 = float.NegativeInfinity;
            for (int i = 0; i < 3; i++)
            {
                (float, int) tuple4 = ((i == 0) ? tuple : tuple2);
                float num22;
                if (i < 2)
                {
                    (num22, _) = tuple4;
                }
                else
                {
                    num22 = float.PositiveInfinity;
                }

                float num23 = num22;
                int num24 = ((num5 > 0f) ? num19 : (-num19));
                int num25 = ((num6 > 0f) ? num20 : (-num20));
                float num26 = num24 * num5;
                float num27 = num24 * num + bounds.A;
                float num28 = num25 * num6;
                float num29 = num25 * num2 + bounds.B;
                QuadraticFunction quadraticFunction = new QuadraticFunction(num26 * num26 + num28 * num28, 2f * (num26 * num27 + num28 * num29), num27 * num27 + num29 * num29);
                QuadraticFunction.IntersectResult intersections = quadraticFunction.GetIntersections(num9);
                QuadraticFunction.IntersectResult intersections2 = quadraticFunction.GetIntersections(num10);
                for (int j = 0; j < intersections.Count; j++)
                {
                    float num30 = intersections[j];
                    if (num21 <= num30 && num30 < num23)
                    {
                        SetResult(num30, ref resultCount, ref result2, ref result3);
                    }
                }

                for (int k = 0; k < intersections2.Count; k++)
                {
                    float num31 = intersections2[k];
                    if (num21 <= num31 && num31 < num23)
                    {
                        SetResult(num31, ref resultCount2, ref result4, ref result5);
                    }
                }

                num21 = num23;
                if (tuple4.Item2 == 0)
                {
                    num19 += 2;
                }
                else
                {
                    num20 += 2;
                }
            }
        }

        if (resultCount2 == 2 && result5 >= 0f && ((resultCount == 2) ? result3 : result4) <= 1f)
        {
            float num32 = ((resultCount == 2 && result3 > 0f) ? result3 : 0f);
            float num33 = num + num5 * num32;
            float num34 = num2 + num6 * num32;
            int contactCount = 1;
            Vector2 vector3 = new Vector2(num33, num34);
            Vector2 vector4;
            Vector2 vector5;
            float num35;
            if (bounds.IsCircle)
            {
                num35 = Vector.Length(vector3);
                vector4 = (0f - bounds.R) / num35 * vector3;
                vector5 = 1f / num35 * vector3;
                num35 += bounds.R;
            }
            else
            {
                int num36 = ((!(num33 < 0f)) ? 1 : (-1));
                int num37 = ((!(num34 < 0f)) ? 1 : (-1));
                num35 = Vector.Length(num33 + num36 * bounds.A, num34 + num37 * bounds.B);
                if (Math.Abs(num33) / num7 < 0.02f)
                {
                    contactCount = 2;
                    num36 = 0;
                }
                else if (Math.Abs(num34) / num7 < 0.02f)
                {
                    contactCount = 2;
                    num37 = 0;
                }

                vector4 = new Vector2(-num36 * bounds.A, -num37 * bounds.B);
                vector5 = vector3 - vector4;
                vector5 /= Vector.Length(vector5);
            }

            result = new TOIResult(num32, contactCount, vector4, vector5, num7 - num35);
            return true;
        }

        return false;
        void SetResult(float value, ref int count, ref float x1, ref float x2)
        {
            if (count == 0)
            {
                count = 1;
                x1 = value;
            }
            else if (count == 1 && Math.Abs(value - x1) > 1E-05f)
            {
                count = 2;
                if (value >= x1)
                {
                    x2 = value;
                }
                else
                {
                    x2 = x1;
                    x1 = value;
                }
            }
        }
    }

    private static void LineAndCircleIntersection(float pX, float pY, float dX, float dY, float cX, float cY, float r, out int resultCount, out float result1, out float result2)
    {
        LineAndCircleIntersectionJob lineAndCircleIntersectionJob = new LineAndCircleIntersectionJob
        {
            pX = pX,
            pY = pY,
            dX = dX,
            dY = dY,
            cX = cX,
            cY = cY,
            r = r,
            nativeInts = nativeInts,
            nativeFloats = nativeFloats
        };
        JobHandle jobHandle = lineAndCircleIntersectionJob.Schedule(8, 8);
        jobHandle.Complete();
        resultCount = nativeInts[0];
        result1 = nativeFloats[0];
        result2 = nativeFloats[1];
    }

    [BurstCompile]
    private struct LineAndCircleIntersectionJob : IJobParallelFor
    {
        [ReadOnly]
        public float pX;
        [ReadOnly]
        public float pY;
        [ReadOnly]
        public float dX;
        [ReadOnly]
        public float dY;
        [ReadOnly]
        public float cX;
        [ReadOnly]
        public float cY;
        [ReadOnly]
        public float r;
        [WriteOnly]
        public NativeArray<int> nativeInts;
        [WriteOnly]
        public NativeArray<float> nativeFloats;
        public void Execute(int index)
        {
            float num = pX - cX;
            float num2 = pY - cY;
            float num3 = dX * num + dY * num2;
            float num4 = dX * num2 - dY * num;
            float num5 = dX * dX + dY * dY;
            float num6 = r * r * num5 - num4 * num4;
            float num7 = 1E-10f;
            if (num6 < 0f - num7)
            {
                nativeInts[0] = 0;
                nativeFloats[0] = float.NaN;
                nativeFloats[1] = float.NaN;
            }
            else if (num6 < num7)
            {
                nativeInts[0] = 1;
                nativeFloats[0] = (0f - num3) / num5;
                nativeFloats[1] = (0f - num3) / num5;
            }
            else
            {
                float num8 = math.sqrt(num6);
                nativeInts[0] = 2;
                nativeFloats[0] = (0f - num3 - num8) / num5;
                nativeFloats[1] = (0f - num3 + num8) / num5;
            }
        }
    }

    private void UpdateLights(List<EntityLight> lights)
    {
        int num = 0;
        int num2 = 5;
        float num3 = 20000f * INSettings.GetFloat(INFeature.LightElectricityFactor);
        float @float = INSettings.GetFloat(INFeature.LightChargingSpeedFactor);
        float num4 = 5000f;
        List<BasePart> parts = Contraption.Instance.Parts;
        int[] array = new int[Contraption.Instance.ConnectedComponentCount];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = -1;
        }

        foreach (EntityLight light in lights)
        {
            int componentIndex = light.ComponentIndex;
            light.Enabled = light.Part.IsEnabled() && !light.Part.IsSinglePart();
            if (componentIndex != -1)
            {
                array[componentIndex] = num++;
            }
        }

        m_componentCount = num;
        foreach (EntityLight light2 in lights)
        {
            int componentIndex2 = light2.ComponentIndex;
            if (componentIndex2 != -1)
            {
                light2.Index = array[componentIndex2] * num2 + light2.Type;
            }
        }

        m_electricities = new float[num * num2];
        m_capacities = new float[num * num2];
        m_lightCounts = new int[num * num2];
        m_lightImpulses = new List<ImpulseData>[num * num2];
        float[] array2 = new float[num];
        float[] array3 = new float[num * num2];
        float[] array4 = new float[num * num2];
        for (int j = 0; j < num * num2; j++)
        {
            m_lightImpulses[j] = new List<ImpulseData>();
        }

        foreach (BasePart item in parts)
        {
            if (item is Engine)
            {
                int connectedComponent = item.ConnectedComponent;
                if (connectedComponent != -1 && array[connectedComponent] != -1)
                {
                    array2[array[connectedComponent]] += num3;
                }
            }
        }

        foreach (EntityLight light3 in lights)
        {
            m_electricities[light3.Index] += light3.m_electricity;
            m_capacities[light3.Index] += num4;
            m_lightCounts[light3.Index]++;
        }

        for (int k = 0; k < num; k++)
        {
            int num5 = 0;
            for (int l = 0; l < num2; l++)
            {
                num5 += m_lightCounts[k * num2 + l];
            }

            if (num5 == 0)
                continue;
            for (int m = 0; m < num2; m++)
            {
                m_capacities[k * num2 + m] += array2[k] * m_lightCounts[k * num2 + m] / num5;
            }
        }

        for (int n = 0; n < num * num2; n++)
        {
            float num6 = m_capacities[n];
            float num7 = m_electricities[n] + num6;
            float num8 = Math.Min(num7 + Mathf.Sqrt(num6) * @float, num6);
            m_electricities[n] = num8;
            array3[n] = (num8 - num7) / m_lightCounts[n];
        }

        for (int num9 = 0; num9 < num; num9++)
        {
            int num10 = 0;
            float num11 = 0f;
            for (int num12 = 0; num12 < num2; num12++)
            {
                num11 += m_electricities[num9 * num2 + num12];
            }

            for (int num13 = num2; num13 > 0; num13--)
            {
                int num14 = 0;
                float num15 = float.MaxValue;
                for (int num16 = 0; num16 < num2; num16++)
                {
                    if ((num10 & (1 << num16)) == 0)
                    {
                        float num17 = m_capacities[num9 * num2 + num16];
                        if (num17 < num15)
                        {
                            num14 = num16;
                            num15 = num17;
                        }
                    }
                }

                num10 |= 1 << num14;
                float num18 = ((num15 < num11 / num13) ? num15 : (num11 / num13));
                num11 -= num18;
                int num19 = m_lightCounts[num9 * num2 + num14];
                if (num19 != 0)
                {
                    array4[num9 * num2 + num14] = (num18 - num15) / num19;
                }
            }
        }

        foreach (EntityLight light4 in lights)
        {
            light4.m_electricity += array3[light4.Index];
            light4.m_electricity += (array4[light4.Index] - light4.m_electricity) * 0.04f;
            _ = m_electricities[light4.Index] / m_capacities[light4.Index];
        }
    }

    private void SortFour<T>(ref T a1, ref T a2, ref T a3, ref T a4)
        where T : IComparable<T>
    {
        SortFour(a1, a2, a3, a4, out T b, out T b2, out T b3, out T b4);
        a1 = b;
        a2 = b2;
        a3 = b3;
        a4 = b4;
    }

    private static void SortFour<T>(T a1, T a2, T a3, T a4, out T b1, out T b2, out T b3, out T b4)
        where T : IComparable<T>
    {
        T val;
        T val2;
        if (a1.CompareTo(a2) <= 0)
        {
            val = a1;
            val2 = a2;
        }
        else
        {
            val = a2;
            val2 = a1;
        }

        T val3;
        T val4;
        if (a3.CompareTo(a4) <= 0)
        {
            val3 = a3;
            val4 = a4;
        }
        else
        {
            val3 = a4;
            val4 = a3;
        }

        if (val.CompareTo(val3) <= 0)
        {
            b1 = val;
            b2 = val3;
        }
        else
        {
            b1 = val3;
            b2 = val;
        }

        if (val2.CompareTo(val4) <= 0)
        {
            b3 = val2;
            b4 = val4;
        }
        else
        {
            b3 = val4;
            b4 = val2;
        }

        if (b2.CompareTo(b3) > 0)
        {
            (b2, b3) = (b3, b2);
        }
    }

    private void UpdateAlienLights(List<EntityLight> lights)
    {
        int num = 0;
        int[] array = new int[Contraption.Instance.ConnectedComponentCount];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = -1;
        }

        foreach (EntityLight light in lights)
        {
            int connectedComponent = light.Part.ConnectedComponent;
            if (light.Type == 4 && connectedComponent != -1)
            {
                array[connectedComponent] = num++;
            }
        }

        m_alienComponentCount = num;
        List<BasePart> parts = Contraption.Instance.Parts;
        List<BasePart>[] array2 = new List<BasePart>[num];
        List<EntityLight>[] array3 = new List<EntityLight>[num];
        for (int j = 0; j < num; j++)
        {
            array2[j] = new List<BasePart>();
            array3[j] = new List<EntityLight>();
        }

        foreach (BasePart item in parts)
        {
            if (item.ConnectedComponent != -1)
            {
                int num2 = array[item.ConnectedComponent];
                if (num2 != -1)
                {
                    array2[num2].Add(item);
                }
            }
        }

        foreach (EntityLight light2 in lights)
        {
            int connectedComponent2 = light2.Part.ConnectedComponent;
            if (light2.Type == 4 && connectedComponent2 != -1)
            {
                int num3 = array[connectedComponent2];
                array3[num3].Add(light2);
            }
        }

        float fixedDeltaTime = Time.fixedDeltaTime;
        float @float = INSettings.GetFloat(INFeature.AlienLightRadiusLimitFactor);
        for (int k = 0; k < num; k++)
        {
            int num4 = 0;
            float num5 = 0f;
            Vector2 position = default(Vector2);
            Vector2 prePosition = default(Vector2);
            List<BasePart> list = array2[k];
            foreach (BasePart item2 in list)
            {
                if (!item2.HasMultipleRigidbodies())
                {
                    num4++;
                    Vector3 position2 = item2.transform.position;
                    Vector3 velocity = item2.rigidbody.velocity;
                    position = new Vector2(position.x + position2.x, position.y + position2.y);
                    prePosition = new Vector2(prePosition.x + position2.x - velocity.x * fixedDeltaTime, prePosition.y + position2.y - velocity.y * fixedDeltaTime);
                }
            }

            position /= num4;
            prePosition /= num4;
            foreach (BasePart item3 in list)
            {
                if (!item3.HasMultipleRigidbodies())
                {
                    Vector3 position3 = item3.transform.position;
                    float num6 = (position3.x - position.x) * (position3.x - position.x) + (position3.y - position.y) * (position3.y - position.y);
                    num5 = ((num6 > num5) ? num6 : num5);
                }
            }

            int num7 = 0;
            float num8 = 2f * Mathf.Sqrt(list.Count) * @float;
            float num9 = Mathf.Sqrt(num5);
            num9 = ((num9 < num8) ? num9 : num8) + 4f;
            foreach (EntityLight item4 in array3[k])
            {
                if (item4.Enabled)
                {
                    num7++;
                }

                Vector3 position4 = item4.Transform.position;
                item4.Transform.position = new Vector3(position.x, position.y, position4.z);
                item4.Length = num9;
                item4.HalfWidth = (num9 + 8f) / 64f;
                item4.m_meshFilter.sharedMesh.UpdateCircleMesh(item4.Length, item4.HalfWidth, item4.Angle, 150);
                item4.Data.SetPosition(position, prePosition);
            }

            float coefficient = ((num7 <= 1) ? 1 : num7);
            foreach (EntityLight item5 in array3[k])
            {
                item5.m_coefficient = coefficient;
            }
        }
    }

    public void AddImpulse(EntityLight light, ImpulseData impulseData)
    {
        m_lightImpulses[light.Index].Add(impulseData);
    }

    public float Defend(Vector2 from, Vector2 to, float work, float electricity)
    {
        EntityLight[] components = INContraption.Instance.GetComponents<EntityLight>();
        List<(int, float)> list = null;
        int[] array = null;
        float num = 0f;
        for (int i = 0; i < components.Length; i++)
        {
            EntityLight entityLight = components[i];
            if (!entityLight.Enabled || entityLight.Type != 4 || !(m_electricities[entityLight.Index] / m_capacities[entityLight.Index] > 0f))
            {
                continue;
            }

            Vector2 position = entityLight.Data.Position1;
            float num2 = position.x - from.x;
            float num3 = position.y - from.y;
            float num4 = position.x - to.x;
            float num5 = position.y - to.y;
            float num6 = entityLight.Length - entityLight.HalfWidth;
            float num7 = num6 * num6;
            if (num4 * num4 + num5 * num5 < num7 && num2 * num2 + num3 * num3 > num7)
            {
                float num8 = UnityEngine.Mathf.Sqrt(m_electricities[entityLight.Index]);
                if (list == null)
                {
                    list = new List<(int, float)>();
                    array = new int[m_componentCount * 5];
                }

                if (array[entityLight.Index] == 0)
                {
                    num += num8;
                }

                array[entityLight.Index]++;
                list.Add((i, num8));
            }
        }

        float num9 = 0.7f;
        if (electricity > 0f)
        {
            num9 = UnityEngine.Mathf.Clamp(0.4f * num / UnityEngine.Mathf.Sqrt(electricity), 0f, 0.7f);
        }

        if (ConsumePower && list != null && work < 0f)
        {
            float num10 = work * num9 / num;
            for (int j = 0; j < list.Count; j++)
            {
                (int, float) tuple = list[j];
                components[tuple.Item1].m_electricity += num10 * tuple.Item2;
            }
        }

        return 1f - num9;
    }
}