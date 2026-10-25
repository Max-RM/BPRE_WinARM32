using System;

public class DisjointSet
{
    private int[] m_parent;
    private int[] m_size;
    public int Count { get; }

    public DisjointSet(int count)
    {
        Count = count;
        if (count == 0)
        {
            m_parent = Array.Empty<int>();
            m_size = Array.Empty<int>();
            return;
        }

        m_parent = new int[count];
        m_size = new int[count];
        for (int i = 0; i < count; i++)
        {
            MakeSet(i);
        }
    }

    public void MakeSet(int x)
    {
        m_parent[x] = x;
        m_size[x] = 1;
    }

    public int FindSet(int x)
    {
        int num = m_parent[x];
        if (x != num)
        {
            return m_parent[x] = FindSet(num);
        }

        return num;
    }

    public int FindSet(int x, out int size)
    {
        int num = FindSet(x);
        size = m_size[num];
        return num;
    }

    public void Union(int x, int y)
    {
        int num = FindSet(x);
        int num2 = FindSet(y);
        if (num != num2)
        {
            if (m_size[num] > m_size[num2])
            {
                m_size[num] += m_size[num2];
                m_parent[num2] = num;
            }
            else
            {
                m_size[num2] += m_size[num];
                m_parent[num] = num2;
            }
        }
    }

    public void Clear()
    {
        for (int i = 0; i < Count; i++)
        {
            MakeSet(i);
        }
    }
}