using System;
using System.Collections.Generic;
using UnityEngine;

namespace System
{
    public static class HashCode
    {
        public static int Combine<T1, T2>(T1 value1, T2 value2)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (value1 == null ? 0 : value1.GetHashCode());
                hash = hash * 31 + (value2 == null ? 0 : value2.GetHashCode());
                return hash;
            }
        }

        public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
        {
            unchecked
            {
                int hash = Combine(value1, value2);
                hash = hash * 31 + (value3 == null ? 0 : value3.GetHashCode());
                hash = hash * 31 + (value4 == null ? 0 : value4.GetHashCode());
                hash = hash * 31 + (value5 == null ? 0 : value5.GetHashCode());
                return hash;
            }
        }
    }
}

public static class Unity2018DictionaryExtensions
{
    public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary.ContainsKey(key))
        {
            return false;
        }

        dictionary.Add(key, value);
        return true;
    }
}

namespace Unity.Burst
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public sealed class BurstCompileAttribute : Attribute
    {
        public bool CompileSynchronously { get; set; }
    }
}

namespace Unity.Entities
{
}

namespace Unity.Entities.UniversalDelegates
{
}

namespace Unity.Properties
{
}

namespace Unity.Mathematics
{
    public struct float3
    {
        public float x;
        public float y;
        public float z;

        public float3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static float3 operator +(float3 a, float3 b)
        {
            return new float3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static implicit operator Vector3(float3 value)
        {
            return new Vector3(value.x, value.y, value.z);
        }
    }

    public static class math
    {
        public const float E = 2.718281828f;

        public static float floor(float value)
        {
            return Mathf.Floor(value);
        }

        public static float sqrt(float value)
        {
            return Mathf.Sqrt(value);
        }

        public static double sqrt(double value)
        {
            return Math.Sqrt(value);
        }

        public static float pow(float value, float power)
        {
            return Mathf.Pow(value, power);
        }

        public static double pow(double value, double power)
        {
            return Math.Pow(value, power);
        }

        public static float log(float value)
        {
            return Mathf.Log(value);
        }

        public static double log(double value)
        {
            return Math.Log(value);
        }

        public static float3 log(float3 value)
        {
            return new float3(Mathf.Log(value.x), Mathf.Log(value.y), Mathf.Log(value.z));
        }

        public static double cos(double value)
        {
            return Math.Cos(value);
        }

        public static double sin(double value)
        {
            return Math.Sin(value);
        }

        public static float3 abs(Vector3 value)
        {
            return new float3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }
    }
}
