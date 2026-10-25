using System.Collections.Generic;

public class Graph
{
    public struct Edge
    {
        public int To;
        public Edge(int to)
        {
            To = to;
        }
    }

    private List<Edge>[] m_graph;
    public int Count { get; }

    public Graph(int count)
    {
        Count = count;
        m_graph = new List<Edge>[count];
        for (int i = 0; i < count; i++)
        {
            m_graph[i] = new List<Edge>();
        }
    }

    public void AddDirectedEdge(int x, int y)
    {
        m_graph[x].Add(new Edge(y));
    }

    public void AddUndirectedEdge(int x, int y)
    {
        AddDirectedEdge(x, y);
        AddDirectedEdge(y, x);
    }

    public bool Contains(int x, int y)
    {
        foreach (Edge item in m_graph[x])
        {
            if (item.To == y)
            {
                return true;
            }
        }

        return false;
    }

    public List<Edge> GetEdges(int x)
    {
        return m_graph[x];
    }
}

public class Graph<T>
{
    public struct Edge
    {
        public int To;
        public T Value;
        public Edge(int to, T value)
        {
            To = to;
            Value = value;
        }
    }

    private List<Edge>[] m_graph;
    public int Count { get; }

    public Graph(int count)
    {
        Count = count;
        m_graph = new List<Edge>[count];
        for (int i = 0; i < count; i++)
        {
            m_graph[i] = new List<Edge>();
        }
    }

    public void AddDirectedEdge(int x, int y, T value)
    {
        m_graph[x].Add(new Edge(y, value));
    }

    public void AddUndirectedEdge(int x, int y, T value)
    {
        AddDirectedEdge(x, y, value);
        AddDirectedEdge(y, x, value);
    }

    public bool Contains(int x, int y)
    {
        foreach (Edge item in m_graph[x])
        {
            if (item.To == y)
            {
                return true;
            }
        }

        return false;
    }

    public List<Edge> GetEdges(int x)
    {
        return m_graph[x];
    }
}