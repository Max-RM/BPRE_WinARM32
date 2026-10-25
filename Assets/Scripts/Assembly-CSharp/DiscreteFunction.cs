using System;

public struct DiscreteFunction
{
    public float Min { get; }
    public float Max { get; }
    public float Delta { get; }
    public float[] RawArray { get; }

    public DiscreteFunction(float min, float max, float delta)
    {
        Min = min;
        Max = max;
        Delta = delta;
        RawArray = new float[(int)Math.Round((max - min) / delta) + 1];
    }

    public bool IsInDomain(float x)
    {
        if (Min <= x)
        {
            return x <= Max;
        }

        return false;
    }

    public float Get(float x)
    {
        if (!IsInDomain(x))
        {
            throw new ArgumentOutOfRangeException("x");
        }

        int num = (int)Math.Round((x - Min) / Delta);
        return RawArray[num];
    }

    public void Set(float value)
    {
        for (int i = 0; i < RawArray.Length; i++)
        {
            RawArray[i] = value;
        }
    }

    public void Set(Func<float, float> f)
    {
        for (int i = 0; i < RawArray.Length; i++)
        {
            float arg = Min + Delta * (float)i;
            RawArray[i] = f(arg);
        }
    }

    public void Set(Func<float, float, float> f)
    {
        for (int i = 0; i < RawArray.Length; i++)
        {
            float arg = Min + Delta * (float)i;
            float arg2 = RawArray[i];
            RawArray[i] = f(arg, arg2);
        }
    }
}