using System;
using System.Collections.Generic;

public class CircuitSimulator
{
    private class CircuitEquationSolver
    {
        private int m_equationCount;
        private int m_variableCount;
        private int m_additionalVariableCount;
        private DisjointSet m_disjointSet;
        private int[] m_variableIndexes;
        private float[][] m_augmentedMatrix;
        private VariableValue[] m_variables;
        public VariableValue GetVariable(int index)
        {
            int num = m_variableIndexes[index];
            if (num == -1)
            {
                return new VariableValue(0f, isConstant: true, isSolved: true);
            }

            return m_variables[num];
        }

        public VariableValue GetAdditionalVariable(int index)
        {
            return m_variables[m_variableCount + index];
        }

        public CircuitEquationSolver()
        {
            m_variableIndexes = Array.Empty<int>();
            m_augmentedMatrix = Array.Empty<float[]>();
            m_variables = Array.Empty<VariableValue>();
        }

        public void Solve(List<Node> nodes, Graph<Branch> nodeGraph, int additionalVariableCount)
        {
            Initialize(nodes, nodeGraph, additionalVariableCount);
            CreateEquations(nodes, nodeGraph);
            SolveEquations();
        }

        public void Clear()
        {
            Array.Clear(m_variableIndexes, 0, m_variableIndexes.Length);
            Array.Clear(m_variables, 0, m_variables.Length);
            float[][] augmentedMatrix = m_augmentedMatrix;
            foreach (float[] array in augmentedMatrix)
            {
                Array.Clear(array, 0, array.Length);
            }
        }

        private void Initialize(List<Node> nodes, Graph<Branch> nodeGraph, int additionalVariableCount)
        {
            int count = nodes.Count;
            m_disjointSet = new DisjointSet(count + 1);
            foreach (Node node in nodes)
            {
                foreach (Graph<Branch>.Edge edge in nodeGraph.GetEdges(node.Index))
                {
                    Branch value = edge.Value;
                    if (value.IsShortCircuit)
                    {
                        switch (value.Type)
                        {
                            case BranchType.Common:
                                m_disjointSet.Union(node.Index, edge.To);
                                break;
                            case BranchType.Grounded:
                                m_disjointSet.Union(node.Index, count);
                                break;
                        }
                    }
                }
            }

            if (m_variableIndexes.Length < count + 1)
            {
                m_variableIndexes = new int[count + 1];
            }

            m_disjointSet.GetComponentIndexes(m_variableIndexes, out var componentCount);
            int num = m_variableIndexes[count];
            for (int i = 0; i < count; i++)
            {
                int num2 = m_variableIndexes[i];
                if (num2 > num)
                {
                    m_variableIndexes[i] = num2 - 1;
                }
                else if (num2 == num)
                {
                    m_variableIndexes[i] = -1;
                }
            }

            m_equationCount = count;
            m_variableCount = componentCount - 1;
            m_additionalVariableCount = additionalVariableCount;
        }

        private void CreateEquations(List<Node> nodes, Graph<Branch> nodeGraph)
        {
            float[][] array = m_augmentedMatrix;
            int equationCount = m_equationCount;
            int num = m_variableCount + m_additionalVariableCount;
            if (equationCount > array.Length || (array.Length != 0 && num + 1 > array[0].Length))
            {
                array = new float[equationCount][];
                for (int i = 0; i < equationCount; i++)
                {
                    array[i] = new float[num + 1];
                }

                m_augmentedMatrix = array;
            }

            foreach (Node node in nodes)
            {
                float[] array2 = array[node.Index];
                foreach (Graph<Branch>.Edge edge in nodeGraph.GetEdges(node.Index))
                {
                    Branch value = edge.Value;
                    bool num2 = value.StartElement == node.ElementIndex;
                    int num3 = value.AdditionalVariableIndex;
                    if (num3 != -1)
                    {
                        num3 += m_variableCount;
                    }

                    int num4 = m_variableIndexes[node.Index];
                    int num5 = ((edge.To != -1) ? m_variableIndexes[edge.To] : (-1));
                    float num6 = 1f;
                    float num7 = value.InvR;
                    if (!num2)
                    {
                        (num4, num5) = (num5, num4);
                        num6 = 0f - num6;
                        num7 = 0f - num7;
                    }

                    BranchType type = value.Type;
                    if (type != BranchType.Common && type != BranchType.Grounded)
                    {
                        continue;
                    }

                    if (num3 != -1)
                    {
                        array2[num3] += num6;
                        continue;
                    }

                    if (num4 != -1)
                    {
                        array2[num4] += num7;
                    }

                    if (num5 != -1)
                    {
                        array2[num5] -= num7;
                    }

                    array2[num] -= value.U * num7;
                }
            }
        }

        private void SolveEquations()
        {
            float[][] augmentedMatrix = m_augmentedMatrix;
            int equationCount = m_equationCount;
            int num = m_variableCount + m_additionalVariableCount;
            float num2 = 1E-05f;
            if (num > m_variables.Length)
            {
                m_variables = new VariableValue[num];
            }

            int num3 = 0;
            int i = 0;
            while (num3 < equationCount)
            {
                int num4 = num3;
                float num5 = augmentedMatrix[num3][i];
                num5 = ((num5 >= 0f) ? num5 : (0f - num5));
                for (; i < num; i++)
                {
                    for (int j = num3 + 1; j < equationCount; j++)
                    {
                        float num6 = augmentedMatrix[j][i];
                        num6 = ((num6 >= 0f) ? num6 : (0f - num6));
                        if (num6 > num5)
                        {
                            num4 = j;
                            num5 = num6;
                        }
                    }

                    if (!(0f - num2 <= num5) || !(num5 <= num2))
                    {
                        break;
                    }
                }

                if (i == num)
                {
                    break;
                }

                if (num4 != num3)
                {
                    (augmentedMatrix[num3], augmentedMatrix[num4]) = (augmentedMatrix[num4], augmentedMatrix[num3]);
                }

                float num7 = augmentedMatrix[num3][i];
                for (int k = num3 + 1; k < equationCount; k++)
                {
                    float num8 = augmentedMatrix[k][i] / num7;
                    augmentedMatrix[k][i] = 0f;
                    for (int l = i + 1; l < num + 1; l++)
                    {
                        augmentedMatrix[k][l] -= augmentedMatrix[num3][l] * num8;
                    }
                }

                num3++;
                i++;
            }

            for (int num9 = equationCount - 1; num9 >= 0; num9--)
            {
                int num10 = -1;
                float num11 = 0f;
                bool flag = true;
                float num12 = augmentedMatrix[num9][num];
                for (int m = num9; m < num; m++)
                {
                    float num13 = augmentedMatrix[num9][m];
                    if (!(0f - num2 <= num13) || !(num13 <= num2))
                    {
                        ref VariableValue reference = ref m_variables[m];
                        if (reference.IsSolved)
                        {
                            num12 -= num13 * reference.Value;
                            flag &= reference.IsConstant;
                        }
                        else if (num10 != -1)
                        {
                            reference.IsConstant = m >= m_variableCount;
                            reference.IsSolved = true;
                            flag &= reference.IsConstant;
                        }
                        else
                        {
                            num10 = m;
                            num11 = num13;
                        }
                    }
                }

                if (num10 != -1)
                {
                    m_variables[num10] = new VariableValue(num12 / num11, flag, isSolved: true);
                }
            }

            for (int n = 0; n < num; n++)
            {
                if (!m_variables[n].IsSolved)
                {
                    m_variables[n] = new VariableValue(0f, isConstant: false, isSolved: true);
                }
            }
        }
    }

    private struct Node
    {
        public ElectricalElement Element;
        public int Index;
        public float Potential;
        public bool IsGrounded;
        public bool[] Visited;
        public static Node Empty { get; } = new Node(null, -1);
        public bool IsEmpty => Index == -1;
        public int ElementIndex => Element.ElementIndex;

        public Node(ElectricalElement element, int index)
        {
            Element = element;
            Index = index;
            Potential = 0f;
            IsGrounded = false;
            Visited = null;
        }
    }

    private struct Branch
    {
        public BranchType Type;
        public float U;
        public float R;
        public float InvR;
        public int StartElement;
        public int StartElectrode;
        public int EndElement;
        public int EndElectrode;
        public int AdditionalVariableIndex;
        public bool IsShortCircuit => R == 0f;

        public Branch(int startElement, int startElectrode, int endElement, int endElectrode) : this(BranchType.Unknown, 0f, 0f, 0f, startElement, startElectrode, endElement, endElectrode)
        {
        }

        public Branch(BranchType type, float u, float r, float invR, int startElement, int startElectrode, int endElement, int endElectrode)
        {
            Type = type;
            U = u;
            R = r;
            InvR = invR;
            StartElement = startElement;
            StartElectrode = startElectrode;
            EndElement = endElement;
            EndElectrode = endElectrode;
            AdditionalVariableIndex = -1;
        }

        public void Reverse()
        {
            U = 0f - U;
            int startElectrode = StartElectrode;
            EndElectrode = StartElectrode;
            StartElectrode = startElectrode;
        }
    }

    private struct VariableValue
    {
        public float Value;
        public bool IsConstant;
        public bool IsSolved;
        public VariableValue(float value, bool isConstant, bool isSolved)
        {
            Value = value;
            IsConstant = isConstant;
            IsSolved = isSolved;
        }
    }

    public struct SimulationResult
    {
        public ElectricalElement Element;
        public int Electrode;
        public float U;
        public float I;
        public bool IsGrounded;
        public float DeltaTime;
        public SimulationResult(ElectricalElement element, int electrode, float u, float i, bool isGrounded, float deltaTime)
        {
            Element = element;
            Electrode = electrode;
            U = u;
            I = i;
            IsGrounded = isGrounded;
            DeltaTime = deltaTime;
        }
    }

    private enum BranchType
    {
        Unknown = 0,
        Common = 1,
        Cyclic = 2,
        Grounded = 3,
        Floating = 4
    }

    private List<Node> m_nodes;
    private Dictionary<ElectricalElement, Node> m_nodeTable;
    private Graph<Branch> m_nodeGraph;
    private CircuitEquationSolver m_equationSolver;
    public float DeltaTime { get; }

    public CircuitSimulator(float deltaTime)
    {
        DeltaTime = deltaTime;
        m_nodes = new List<Node>();
        m_nodeTable = new Dictionary<ElectricalElement, Node>();
        m_equationSolver = new CircuitEquationSolver();
    }

    public void Clear()
    {
        m_nodes.Clear();
        m_nodeTable.Clear();
        m_equationSolver.Clear();
    }

    public void Initialize(List<ElectricalElement> elements)
    {
        foreach (ElectricalElement element in elements)
        {
            element.Initialize();
        }
    }

    public void Simulate(List<ElectricalElement> elements)
    {
        int count = elements.Count;
        DisjointSet disjointSet = new DisjointSet(count);
        for (int i = 0; i < count; i++)
        {
            elements[i].ElementIndex = i;
        }

        foreach (ElectricalElement element in elements)
        {
            if (element.Electrodes == null)
            {
                continue;
            }

            foreach (ElectricalElement.Electrode electrode in element.Electrodes)
            {
                if (electrode.IsConnected)
                {
                    disjointSet.Union(element.ElementIndex, electrode.Element.ElementIndex);
                }
            }
        }

        int num = 0;
        int[] size;
        int componentCount;
        int[] array = disjointSet.ToComponents(out size, out componentCount);
        List<ElectricalElement> list = new List<ElectricalElement>();
        for (int j = 0; j < componentCount; j++)
        {
            for (int k = 0; k < size[j]; k++)
            {
                ElectricalElement electricalElement = elements[array[num + k]];
                electricalElement.CircuitIndex = j;
                list.Add(electricalElement);
            }

            num += size[j];
            SimulateCircuit(list);
            Clear();
            list.Clear();
        }

        foreach (ElectricalElement element2 in elements)
        {
            element2.Update();
        }
    }

    private void SimulateCircuit(List<ElectricalElement> elements)
    {
        int num = 0;
        foreach (ElectricalElement element in elements)
        {
            if (element.IsNode())
            {
                Node node = new Node(element, num);
                node.Visited = new bool[element.Electrodes.Count];
                m_nodes.Add(node);
                m_nodeTable.Add(element, node);
                num++;
            }
        }

        if (num == 0)
        {
            ScanSingleBranch(elements);
            return;
        }

        m_nodeGraph = new Graph<Branch>(num);
        int num2 = 0;
        foreach (Node node2 in m_nodes)
        {
            List<ElectricalElement.Electrode> electrodes = node2.Element.Electrodes;
            for (int i = 0; i < electrodes.Count; i++)
            {
                if (electrodes[i].IsConnected && !node2.Visited[i])
                {
                    ScanBranch(node2, i, out Node end, out Branch branch);
                    if (((branch.Type == BranchType.Common) || (branch.Type == BranchType.Grounded)) && branch.IsShortCircuit)
                    {
                        branch.AdditionalVariableIndex = num2;
                        num2++;
                    }

                    if (end.Index == -1 || branch.Type == BranchType.Cyclic)
                    {
                        m_nodeGraph.AddDirectedEdge(node2.Index, end.Index, branch);
                    }
                    else
                    {
                        m_nodeGraph.AddUndirectedEdge(node2.Index, end.Index, branch);
                    }
                }
            }
        }

        m_equationSolver.Solve(m_nodes, m_nodeGraph, num2);
        ProcessResults();
    }

    private void ScanSingleBranch(List<ElectricalElement> elements)
    {
        if (elements.Count <= 1)
        {
            return;
        }

        ElectricalElement electricalElement = elements[0];
        foreach (ElectricalElement element in elements)
        {
            if (element.GetConnectedElectrodeCount() <= 1)
            {
                electricalElement = element;
            }
        }

        Node start = new Node(electricalElement, -1);
        int anotherConnectedElectrode = electricalElement.GetAnotherConnectedElectrode(-1);
        ScanBranch(start, anotherConnectedElectrode, out Node _, out Branch branch);
        bool flag = ((electricalElement is Ground) || (electricalElement is Vcc));
        bool flag2 = branch.Type == BranchType.Grounded;
        start.IsGrounded = flag || flag2;
        if (!flag && flag2)
        {
            start.Potential = 0f - branch.U;
        }

        if (electricalElement.GetConnectedElectrodeCount() <= 1 && !flag)
        {
            branch.Type = BranchType.Floating;
        }

        ProcessBranchResults(start, Node.Empty, branch);
    }

    private void ScanBranch(Node start, int startElectrode, out Node end, out Branch branch)
    {
        end = Node.Empty;
        branch = new Branch(start.ElementIndex, startElectrode, -1, -1);
        ElectricalElement electricalElement = start.Element;
        ElectricalElement electricalElement2 = null;
        int index = startElectrode;
        if (!start.IsEmpty)
        {
            start.Visited[startElectrode] = true;
        }

        int num = 0;
        while (true)
        {
            if (num != 0 && electricalElement == start.Element)
            {
                branch.Type = BranchType.Cyclic;
                break;
            }

            ScanElement(electricalElement, electricalElement2, ref branch);
            int connectedElectrodeCount = electricalElement.GetConnectedElectrodeCount();
            if (connectedElectrodeCount == 0 || (connectedElectrodeCount == 1 && electricalElement2 != null))
            {
                if (((electricalElement is Ground) || (electricalElement is Vcc)))
                {
                    branch.Type = BranchType.Grounded;
                }
                else
                {
                    branch.Type = BranchType.Floating;
                }

                break;
            }

            if (num != 0 && connectedElectrodeCount >= 3)
            {
                branch.Type = BranchType.Common;
                break;
            }

            ElectricalElement element = electricalElement.Electrodes[index].Element;
            index = element.GetAnotherConnectedElectrode(element.GetElectrodeIndex(electricalElement));
            electricalElement2 = electricalElement;
            electricalElement = element;
            if (num++ > 10000)
            {
                throw new OverflowException("Infinite loops");
            }
        }

        branch.InvR = 1f / branch.R;
        if (electricalElement != null)
        {
            int electrodeIndex = electricalElement.GetElectrodeIndex(electricalElement2);
            branch.EndElement = electricalElement.ElementIndex;
            branch.EndElectrode = electrodeIndex;
            if (electricalElement.IsNode())
            {
                end = m_nodeTable[electricalElement];
                end.Visited[electrodeIndex] = true;
            }
        }
    }

    private void ScanElement(ElectricalElement element, ElectricalElement last, ref Branch branch)
    {
        ScanElement(element, last, out var U, out var R);
        branch.U += U;
        branch.R += R;
    }

    private void ScanElement(ElectricalElement element, ElectricalElement last, out float U, out float R)
    {
        U = 0f;
        R = 0f;
        if ((element is Resistor resistor))
        {
            R = resistor.Resistance;
        }
        else if ((element is PowerSource powerSource))
        {
            bool flag = ((last != null) ? (last == powerSource.Cathode.Element) : (powerSource.Electrodes[0].Element == powerSource.Anode.Element));
            U = powerSource.Electromotance * (flag ? 1f : (-1f));
            R = powerSource.Resistance;
        }
        else if ((element is Capacitor capacitor))
        {
            bool flag2 = ((last != null) ? (last == capacitor.Anode.Element) : (capacitor.Electrodes[0].Element == capacitor.Cathode.Element));
            U = capacitor.Charge / capacitor.Capacitance * (flag2 ? 1f : (-1f));
            R = capacitor.Resistance;
        }
        else if ((element is Ground ground))
        {
            R = ground.Resistance;
        }
        else if ((element is Vcc vcc))
        {
            bool flag3 = last == null;
            U = vcc.Potential * (flag3 ? 1f : (-1f));
            R = vcc.Resistance;
        }
    }

    private void ProcessResults()
    {
        for (int i = 0; i < m_nodes.Count; i++)
        {
            Node value = m_nodes[i];
            VariableValue variable = m_equationSolver.GetVariable(i);
            value.Potential = variable.Value;
            value.IsGrounded = variable.IsConstant;
            m_nodes[i] = value;
        }

        foreach (Node node in m_nodes)
        {
            foreach (Graph<Branch>.Edge edge in m_nodeGraph.GetEdges(node.Index))
            {
                Branch value2 = edge.Value;
                if (value2.StartElement == node.ElementIndex)
                {
                    Node end = ((edge.To != -1) ? m_nodes[edge.To] : Node.Empty);
                    ProcessBranchResults(node, end, value2);
                }
            }
        }
    }

    private void ProcessBranchResults(Node start, Node end, Branch branch)
    {
        float num = 0f;
        float potential = start.Potential;
        bool isGrounded = start.IsGrounded;
        int additionalVariableIndex = branch.AdditionalVariableIndex;
        num = ((branch.Type == BranchType.Common) ? ((additionalVariableIndex == -1) ? ((potential - end.Potential + branch.U) * branch.InvR) : m_equationSolver.GetAdditionalVariable(additionalVariableIndex).Value) : (branch.Type == BranchType.Cyclic) ? (branch.IsShortCircuit ? 0f : (branch.U * branch.InvR)) : (branch.Type == BranchType.Grounded) ? ((additionalVariableIndex == -1) ? ((potential + branch.U) * branch.InvR) : m_equationSolver.GetAdditionalVariable(additionalVariableIndex).Value) : (branch.Type == BranchType.Floating) ? 0f : num);
        ElectricalElement element = start.Element;
        ElectricalElement last = null;
        int startElectrode = branch.StartElectrode;
        int num2 = 0;
        SimulationResult result = new SimulationResult(element, startElectrode, potential, 0f - num, isGrounded, DeltaTime);
        ScanElement(element, last, out var U, out var R);
        result.Element = element;
        result.U += U - num * R;
        UpdateElement(element, result);
        if (element.GetConnectedElectrodeCount() == 0)
        {
            return;
        }

        last = element;
        element = element.Electrodes[startElectrode].Element;
        startElectrode = element.GetElectrodeIndex(last);
        while (true)
        {
            result.Element = element;
            result.Electrode = startElectrode;
            result.I = num;
            UpdateElement(element, result);
            ScanElement(element, last, out var U2, out var R2);
            result.U += U2 - num * R2;
            switch (element.GetConnectedElectrodeCount())
            {
                case 1:
                    if (last != null)
                    {
                        return;
                    }

                    goto default;
                default:
                    if (element.ElementIndex == branch.EndElement)
                    {
                        return;
                    }

                    break;
                case 0:
                    return;
            }

            startElectrode = (result.Electrode = element.GetAnotherConnectedElectrode(startElectrode));
            result.I = 0f - num;
            UpdateElement(element, result);
            ElectricalElement element2 = element.Electrodes[startElectrode].Element;
            startElectrode = element2.GetElectrodeIndex(element);
            last = element;
            element = element2;
            if (num2++ > 10000)
            {
                throw new OverflowException("Infinite loops");
            }
        }
    }

    private void UpdateElement(ElectricalElement element, SimulationResult result)
    {
        element.UpdateElectrode(result);
    }
}