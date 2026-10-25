using System;
using System.Collections.Generic;
using UnityEngine;

public class WaterSystem : INBehaviour
{
    private static bool m_enabled;
    private static float m_height;
    private static float m_fluctuationSpeed;
    private static GameObject m_water;
    private static Dictionary<MeshRenderer, (Color, Color)> m_rendererTable;
    public static void Create()
    {
        new WaterSystem().Initialize();
    }

    private void Initialize()
    {
        INContraption.Instance.AddBehaviour(this);
        m_status = StatusCode.Running;
        m_enabled = false;
        m_height = float.NaN;
        m_fluctuationSpeed = 0f;
        m_rendererTable = new Dictionary<MeshRenderer, (Color, Color)>();
    }

    private GameObject CreateWater()
    {
        GameObject gameObject = new GameObject("INWater");
        gameObject.transform.parent = Contraption.Instance.transform;
        LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
        Color color = new Color(0.32f, 0.56f, 0.8f, 0.5f);
        lineRenderer.material = new Material(Shader.Find("GUI/Text Shader"));
        lineRenderer.material.color = color;
        lineRenderer.SetWidth(0.5f, 0.5f);
        lineRenderer.SetColors(color, color);
        lineRenderer.positionCount = 2;
        gameObject.AddComponent<MeshFilter>().sharedMesh = INUnity.QuadMesh;
        Material sharedMaterial = new Material(INUnity.ColorTransparentShader)
        {
            color = new Color(0.28f, 0.49f, 0.7f, 0.3f)
        };
        gameObject.AddComponent<MeshRenderer>().sharedMaterial = sharedMaterial;
        return gameObject;
    }

    private void GetState(out bool enabled, out float height)
    {
        enabled = false;
        height = float.NaN;
        foreach (BasePart part in Contraption.Instance.Parts)
        {
            if (part.m_partType == BasePart.PartType.Egg && part.m_partTier == BasePart.PartTier.Rare && part.IsEnabled())
            {
                float y = part.transform.position.y;
                if (!enabled || y < height)
                {
                    enabled = true;
                    height = y;
                }
            }
        }
    }

    public override void FixedUpdate()
    {
        if (m_enabled)
        {
            AddBuoyancy();
        }
    }

    private void AddBuoyancy()
    {
        List<BasePart> list = new List<BasePart>();
        foreach (BasePart part in Contraption.Instance.Parts)
        {
            if ((part.m_enclosedInto == null || ((part.m_partType == BasePart.PartType.KingPig) || (part.m_partType == BasePart.PartType.GoldenPig))) && part.rigidbody != null && !part.rigidbody.IsFixed())
            {
                list.Add(part);
            }
        }

        int count = list.Count;
        if (count != 0)
        {
            int num = count * 4;
            int num2 = 0;
            Vector2[] array = new Vector2[num];
            List<Vector2> list2 = new List<Vector2>();
            Graph<int> graph = new Graph<int>(num);
            DisjointSet disjointSet = new DisjointSet(num);
            DisjointSet disjointSet2 = new DisjointSet(num);
            Dictionary<BasePart, int> dictionary = new Dictionary<BasePart, int>(count);
            for (int i = 0; i < count; i++)
            {
                dictionary[list[i]] = i;
            }

            for (int j = 0; j < count; j++)
            {
                BasePart basePart = list[j];
                Vector3 position = basePart.transform.position;
                Vector3 right = basePart.transform.right;
                INBounds bounds = INContraption.GetBounds(basePart.rigidbody);
                float num3 = position.x + bounds.X * right.x - bounds.Y * right.y;
                float num4 = position.y + bounds.X * right.y + bounds.Y * right.x;
                array[j * 4] = new Vector2(num3 + bounds.A * right.x - bounds.B * right.y, num4 + bounds.A * right.y + bounds.B * right.x);
                array[j * 4 + 1] = new Vector2(num3 - bounds.A * right.x - bounds.B * right.y, num4 - bounds.A * right.y + bounds.B * right.x);
                array[j * 4 + 2] = new Vector2(num3 - bounds.A * right.x + bounds.B * right.y, num4 - bounds.A * right.y - bounds.B * right.x);
                array[j * 4 + 3] = new Vector2(num3 + bounds.A * right.x + bounds.B * right.y, num4 + bounds.A * right.y - bounds.B * right.x);
            }

            for (int k = 0; k < count; k++)
            {
                BasePart enclosedPart = list[k].m_enclosedPart;
                if (!(enclosedPart != null) || !dictionary.TryGetValue(enclosedPart, out var value))
                {
                    continue;
                }

                for (int l = 0; l < 4; l++)
                {
                    for (int m = 0; m < 4; m++)
                    {
                        Vector2 vector = array[k * 4 + l];
                        Vector2 vector2 = array[value * 4 + m];
                        if ((vector.x - vector2.x) * (vector.x - vector2.x) + (vector.y - vector2.y) * (vector.y - vector2.y) < 0.01f)
                        {
                            disjointSet.Union(k * 4 + l, value * 4 + m);
                            disjointSet2.Union(k * 4 + l, value * 4 + m);
                        }
                    }
                }
            }

            float num5 = 0.05f * INSettings.GetFloat(INFeature.WatertightCoefficient);
            float num6 = 0.05f * INSettings.GetFloat(INFeature.WatertightCoefficient);
            HashSet<int> hashSet = new HashSet<int>();
            List<(float, int)>[] array2 = new List<(float, int)>[num];
            foreach (Contraption.JointConnection item4 in Contraption.Instance.JointMap)
            {
                if (!dictionary.TryGetValue(item4.partA, out var value2) || !dictionary.TryGetValue(item4.partB, out var value3) || hashSet.Contains(value2 + value3 * count))
                {
                    continue;
                }

                hashSet.Add(value2 + value3 * count);
                hashSet.Add(value3 + value2 * count);
                for (int n = 0; n < 4; n++)
                {
                    for (int num7 = 0; num7 < 4; num7++)
                    {
                        int num8 = value2 * 4 + n;
                        int num9 = value2 * 4 + ((n < 3) ? (n + 1) : 0);
                        int num10 = value3 * 4 + num7;
                        int num11 = value3 * 4 + ((num7 < 3) ? (num7 + 1) : 0);
                        Vector2 vector3 = array[num8];
                        Vector2 vector4 = array[num9];
                        Vector2 vector5 = array[num10];
                        Vector2 vector6 = array[num11];
                        float num12 = Mathf.Sqrt((vector4.x - vector3.x) * (vector4.x - vector3.x) + (vector4.y - vector3.y) * (vector4.y - vector3.y));
                        float num13 = Mathf.Sqrt((vector6.x - vector5.x) * (vector6.x - vector5.x) + (vector6.y - vector5.y) * (vector6.y - vector5.y));
                        float num14 = ((vector5.x - vector3.x) * (vector4.y - vector3.y) - (vector5.y - vector3.y) * (vector4.x - vector3.x)) / num12;
                        float num15 = ((vector6.x - vector3.x) * (vector4.y - vector3.y) - (vector6.y - vector3.y) * (vector4.x - vector3.x)) / num12;
                        float num16 = ((vector3.x - vector5.x) * (vector6.y - vector5.y) - (vector3.y - vector5.y) * (vector6.x - vector5.x)) / num13;
                        float num17 = ((vector4.x - vector5.x) * (vector6.y - vector5.y) - (vector4.y - vector5.y) * (vector6.x - vector5.x)) / num13;
                        float num18 = num14 / (num14 - num15);
                        float num19 = num16 / (num16 - num17);
                        if (Mathf.Abs(num14 - num15) < 0.01f || Mathf.Abs(num16 - num17) < 0.01f || !(num18 > 0f - num6) || !(num18 < 1f + num6) || !(num19 > 0f - num6) || !(num19 < 1f + num6))
                        {
                            continue;
                        }

                        if (num18 > 0f - num5 && num18 < num5 && num19 > 0f - num5 && num19 < num5)
                        {
                            disjointSet.Union(num8, num10);
                            disjointSet2.Union(num8, num10);
                            continue;
                        }

                        if (num18 > 0f - num5 && num18 < num5 && num19 > 1f - num5 && num19 < 1f + num5)
                        {
                            disjointSet.Union(num9, num10);
                            disjointSet2.Union(num9, num10);
                            continue;
                        }

                        if (num18 > 1f - num5 && num18 < 1f + num5 && num19 > 0f - num5 && num19 < num5)
                        {
                            disjointSet.Union(num8, num11);
                            disjointSet2.Union(num8, num11);
                            continue;
                        }

                        if (num18 > 1f - num5 && num18 < 1f + num5 && num19 > 1f - num5 && num19 < 1f + num5)
                        {
                            disjointSet.Union(num9, num11);
                            disjointSet2.Union(num9, num11);
                            continue;
                        }

                        if (array2[num8] == null)
                        {
                            array2[num8] = new List<(float, int)>();
                        }

                        if (array2[num10] == null)
                        {
                            array2[num10] = new List<(float, int)>();
                        }

                        array2[num8].Add((num19, num + num2));
                        array2[num10].Add((num18, num + num2));
                        float x = vector5.x + num18 * (vector6.x - vector5.x);
                        float y = vector5.y + num18 * (vector6.y - vector5.y);
                        disjointSet.Union(num8, num10);
                        list2.Add(new Vector2(x, y));
                        num2++;
                    }
                }
            }

            Graph<int> graph2 = new Graph<int>(num2);
            for (int num20 = 0; num20 < count; num20++)
            {
                for (int num21 = 0; num21 < 4; num21++)
                {
                    int num22 = num20 * 4 + num21;
                    int num23 = num20 * 4 + ((num21 < 3) ? (num21 + 1) : 0);
                    List<(float, int)> list3 = array2[num22];
                    if (list3 == null)
                    {
                        disjointSet.Union(num22, num23);
                        graph.AddUndirectedEdge(num22, num23, num20);
                        continue;
                    }

                    list3.Add((0f, num22));
                    list3.Add((1f, num23));
                    list3.Sort();
                    for (int num24 = 0; num24 < list3.Count - 1; num24++)
                    {
                        (float, int) tuple = list3[num24];
                        (float, int) tuple2 = list3[num24 + 1];
                        if (tuple.Item2 < num && tuple2.Item2 < num)
                        {
                            disjointSet.Union(tuple.Item2, tuple2.Item2);
                        }

                        if (tuple.Item2 < num)
                        {
                            graph.AddDirectedEdge(tuple.Item2, tuple2.Item2, num20);
                        }
                        else
                        {
                            graph2.AddDirectedEdge(tuple.Item2 - num, tuple2.Item2, num20);
                        }

                        if (tuple2.Item2 < num)
                        {
                            graph.AddDirectedEdge(tuple2.Item2, tuple.Item2, num20);
                        }
                        else
                        {
                            graph2.AddDirectedEdge(tuple2.Item2 - num, tuple.Item2, num20);
                        }
                    }
                }
            }

            int[] array3 = disjointSet2.ToArray();
            Vector2[] array4 = new Vector2[num];
            List<int>[] array5 = new List<int>[num];
            for (int num25 = 0; num25 < num; num25++)
            {
                int num26 = array3[num25];
                if (array5[num26] == null)
                {
                    array5[num26] = new List<int>();
                }

                array4[num26] += array[num25];
                array5[num26].Add(num25);
            }

            for (int num27 = 0; num27 < num; num27++)
            {
                if (array5[num27] != null)
                {
                    array4[num27] /= (float)array5[num27].Count;
                }
            }

            int[] size;
            int componentCount;
            int[] array6 = disjointSet.ToComponents(out size, out componentCount);
            List<(int, Vector2, Vector2)>[] array7 = new List<(int, Vector2, Vector2)>[componentCount];
            int num28 = 0;
            for (int num29 = 0; num29 < componentCount; num29++)
            {
                int num30 = -1;
                float num31 = float.MaxValue;
                for (int num32 = 0; num32 < size[num29]; num32++)
                {
                    int num33 = array6[num28 + num32];
                    float x2 = array[num33].x;
                    if (x2 < num31)
                    {
                        num30 = num33;
                        num31 = x2;
                    }
                }

                num28 += size[num29];
                if (num30 == -1)
                {
                    continue;
                }

                List<(int, Vector2, Vector2)> list4 = new List<(int, Vector2, Vector2)>();
                List<int> list5 = new List<int>();
                List<int> list6 = array5[array3[num30]];
                Vector2 vector7 = new Vector2(1f, 0f);
                for (int num34 = 0; num34 < num + num2; num34++)
                {
                    int num35 = -1;
                    int item = -1;
                    float num36 = float.MaxValue;
                    Vector2 vector8 = default(Vector2);
                    Vector2 item2 = default(Vector2);
                    Vector2 item3 = default(Vector2);
                    foreach (int item5 in list6)
                    {
                        foreach (Graph<int>.Edge item6 in (item5 < num) ? graph.GetEdges(item5) : graph2.GetEdges(item5 - num))
                        {
                            int to = item6.To;
                            if (!list5.Contains(to) && !list6.Contains(to))
                            {
                                Vector2 vector9 = ((item5 < num) ? array4[array3[item5]] : list2[item5 - num]);
                                Vector2 vector10 = ((to < num) ? array4[array3[to]] : list2[to - num]);
                                Vector2 vector11 = vector10 - vector9;
                                vector11 /= Mathf.Sqrt(vector11.x * vector11.x + vector11.y * vector11.y);
                                float num37 = Mathf.Atan2(vector11.y * vector7.x - vector11.x * vector7.y, vector11.x * vector7.x + vector11.y * vector7.y);
                                if (num37 < num36)
                                {
                                    num35 = to;
                                    item = item6.Value;
                                    num36 = num37;
                                    vector8 = vector11;
                                    item2 = vector9;
                                    item3 = vector10;
                                }
                            }
                        }
                    }

                    if (num35 == -1)
                    {
                        break;
                    }

                    list5 = list6;
                    list6 = ((num35 < num) ? array5[array3[num35]] : new List<int>(new int[1] { num35 }));
                    vector7 = vector8;
                    list4.Add((item, item2, item3));
                    if (list6.Contains(num30))
                    {
                        break;
                    }
                }

                array7[num29] = list4;
            }

            (Vector2, bool)[] array8 = new (Vector2, bool)[count];
            (Vector2, int)[] array9 = new (Vector2, int)[Contraption.Instance.ConnectedComponentCount];
            float @float = INSettings.GetFloat(INFeature.BuoyancyCoefficient);
            float float2 = INSettings.GetFloat(INFeature.HydraulicCoefficient);
            List<(int, Vector2, Vector2)>[] array10 = array7;
            foreach (List<(int, Vector2, Vector2)> list7 in array10)
            {
                int num39 = 0;
                int num40 = 0;
                int[] array11 = new int[list7.Count];
                List<(float, int)> list8 = new List<(float, int)>();
                for (int num41 = 0; num41 < list7.Count; num41++)
                {
                    array11[num41] = -1;
                    (int, Vector2, Vector2) tuple3 = list7[num41];
                    if (tuple3.Item2.y >= m_height && tuple3.Item3.y >= m_height)
                    {
                        num39++;
                        continue;
                    }

                    if (tuple3.Item2.y < m_height && tuple3.Item3.y < m_height)
                    {
                        num40++;
                        continue;
                    }

                    float num42 = (m_height - tuple3.Item2.y) / (tuple3.Item3.y - tuple3.Item2.y);
                    if (!float.IsNaN(num42))
                    {
                        list8.Add((tuple3.Item2.x * (1f - num42) + tuple3.Item3.x * num42, num41));
                    }
                }

                if (num40 == 0 && list8.Count == 0)
                {
                    continue;
                }

                if (num40 == list7.Count)
                {
                    for (int num43 = 0; num43 < list7.Count; num43++)
                    {
                        (int, Vector2, Vector2) tuple4 = list7[num43];
                        Vector2 vector12 = tuple4.Item3 - tuple4.Item2;
                        float num44 = m_height - (tuple4.Item2.y + tuple4.Item3.y) * 0.5f;
                        float num45 = 9.81f * num44 * @float;
                        ref (Vector2, bool) reference = ref array8[tuple4.Item1];
                        reference.Item1 += new Vector2((0f - vector12.y) * num45, vector12.x * num45);
                        reference.Item2 = true;
                    }
                }
                else
                {
                    if (list8.Count <= 0)
                    {
                        continue;
                    }

                    list8.Sort();
                    for (int num46 = 0; num46 < list8.Count; num46++)
                    {
                        array11[list8[num46].Item2] = num46;
                    }

                    int num47 = list8[0].Item2;
                    for (int num48 = 0; num48 < list7.Count; num48++)
                    {
                        (int, Vector2, Vector2) tuple5 = list7[num47];
                        bool flag = tuple5.Item3.y >= m_height;
                        if (array11[num47] >= 0)
                        {
                            if (flag)
                            {
                                tuple5.Item3 = new Vector2(list8[array11[num47]].Item1, m_height);
                            }
                            else
                            {
                                tuple5.Item2 = new Vector2(list8[array11[num47]].Item1, m_height);
                            }
                        }

                        Vector2 vector13 = tuple5.Item3 - tuple5.Item2;
                        float num49 = m_height - (tuple5.Item2.y + tuple5.Item3.y) * 0.5f;
                        float num50 = 9.81f * num49 * @float;
                        ref (Vector2, bool) reference2 = ref array8[tuple5.Item1];
                        reference2.Item1 += new Vector2((0f - vector13.y) * num50, vector13.x * num50);
                        reference2.Item2 = true;
                        if (array11[num47] >= 0 && flag)
                        {
                            if (array11[num47] == list8.Count - 1)
                            {
                                break;
                            }

                            num47 = list8[array11[num47] + 1].Item2;
                        }
                        else
                        {
                            num47 = (num47 + 1) % list7.Count;
                        }
                    }
                }
            }

            for (int num51 = 0; num51 < count; num51++)
            {
                (Vector2, bool) tuple6 = array8[num51];
                if (tuple6.Item2)
                {
                    ref (Vector2, int) reference3 = ref array9[list[num51].ConnectedComponent];
                    reference3.Item1 += tuple6.Item1;
                    reference3.Item2++;
                }
            }

            for (int num52 = 0; num52 < count; num52++)
            {
                (Vector2, bool) tuple7 = array8[num52];
                BasePart basePart2 = list[num52];
                if (tuple7.Item2 && basePart2.rigidbody.useGravity)
                {
                    (Vector2, int) tuple8 = array9[basePart2.ConnectedComponent];
                    basePart2.rigidbody.AddForce(tuple7.Item1 * float2 + tuple8.Item1 / tuple8.Item2 * (1f - float2));
                }
            }

            foreach (BasePart part2 in Contraption.Instance.Parts)
            {
                if (part2.transform.position.y < m_height)
                {
                    part2.rigidbody.AddForce(-part2.rigidbody.velocity * 0.5f);
                }
            }
        }

        Rigidbody[] components = INContraption.Instance.GetComponents<Rigidbody>();
        foreach (Rigidbody rigidbody in components)
        {
            if (rigidbody.IsFixed() || !(rigidbody.GetComponent<BasePart>() == null))
            {
                continue;
            }

            Vector3 position2 = rigidbody.transform.position;
            Vector3 right2 = rigidbody.transform.right;
            INBounds bounds2 = INContraption.GetBounds(rigidbody);
            float num53 = position2.y + bounds2.X * right2.y + bounds2.Y * right2.x;
            float num54 = ((bounds2.A > bounds2.B) ? bounds2.A : bounds2.B) * 2f;
            float num55 = m_height - (num53 - num54 * 0.5f);
            if (num55 > 0f)
            {
                float y2 = 9.81f * num54 * ((num55 > num54) ? num54 : num55) * INSettings.GetFloat(INFeature.BuoyancyCoefficient);
                Vector3 vector14 = new Vector3(0f, y2);
                if (rigidbody.useGravity)
                {
                    rigidbody.AddForce(vector14 - rigidbody.velocity * 0.5f);
                }
                else
                {
                    rigidbody.AddForce(-rigidbody.velocity * 0.5f);
                }
            }
        }
    }

    public override void Update()
    {
        GetState(out var enabled, out var height);
        if (enabled != m_enabled)
        {
            SetEnabled(enabled);
        }

        if (enabled)
        {
            SetHeight(height);
            UpdateRenderers();
        }
    }

    public override void LateUpdate()
    {
        if (m_enabled)
        {
            UpdateWaterPosition();
        }
    }

    private void UpdateWaterPosition()
    {
        Camera component = WPFMonoBehaviour.ingameCamera.GetComponent<Camera>();
        Vector3 position = component.transform.position;
        float num = component.orthographicSize * 1.1f;
        float num2 = num * (float)Screen.width / (float)Screen.height;
        float height = m_height;
        m_water.GetComponent<LineRenderer>().SetPosition(0, new Vector3(position.x - num2, height - 0.25f));
        m_water.GetComponent<LineRenderer>().SetPosition(1, new Vector3(position.x + num2, height - 0.25f));
        float num3 = position.y - num;
        float num4 = UnityEngine.Mathf.Clamp(num3, height, position.y + num);
        m_water.transform.position = new Vector3(position.x, (num3 + num4) * 0.5f);
        m_water.transform.localScale = new Vector3(num2 * 2f, num4 - num3, 1f);
    }

    private void UpdateRenderers()
    {
        MeshRenderer[] components = INContraption.Instance.GetComponents<MeshRenderer>();
        Dictionary<MeshRenderer, (Color, Color)> dictionary = new Dictionary<MeshRenderer, (Color, Color)>();
        MeshRenderer[] array = components;
        foreach (MeshRenderer meshRenderer in array)
        {
            if (meshRenderer != null && meshRenderer.name != "INWater")
            {
                float num = meshRenderer.transform.position.y - m_height;
                if (num < 0f)
                {
                    Color color = meshRenderer.material.color;
                    (Color, Color) value;
                    Color item = ((m_rendererTable.TryGetValue(meshRenderer, out value) && ColorEquals(color, value.Item2)) ? value.Item1 : color);
                    float num2 = 0.7f / (1f - num / 64f) + 0.3f;
                    Color color2 = new Color((num2 - 0.1f) * 0.8f * item.r, (num2 - 0.05f) * 0.9f * item.g, num2 * item.b, num2 * item.a);
                    dictionary.Add(meshRenderer, (item, color2));
                    meshRenderer.material.color = color2;
                }
            }
        }

        foreach (KeyValuePair<MeshRenderer, (Color, Color)> item2 in m_rendererTable)
        {
            MeshRenderer key = item2.Key;
            if (key != null && !dictionary.ContainsKey(key))
            {
                key.material.color = item2.Value.Item1;
            }
        }

        m_rendererTable = dictionary;
        bool ColorEquals(Color x, Color y)
        {
            if (x.r == y.r && x.g == y.g)
            {
                return x.b == y.b;
            }

            return false;
        }
    }

    private void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            if (m_water == null)
            {
                m_water = CreateWater();
            }

            m_water.SetActive(value: true);
            UpdateWaterPosition();
        }
        else
        {
            m_height = float.NaN;
            m_fluctuationSpeed = 0f;
            m_water.SetActive(value: false);
            foreach (KeyValuePair<MeshRenderer, (Color, Color)> item in m_rendererTable)
            {
                MeshRenderer key = item.Key;
                if (key != null)
                {
                    key.material.color = item.Value.Item1;
                }
            }

            m_rendererTable.Clear();
        }

        m_enabled = enabled;
    }

    private void SetHeight(float height)
    {
        if (float.IsNaN(m_height))
        {
            m_height = height;
            return;
        }

        float fixedDeltaTime = Time.fixedDeltaTime;
        float @float = INSettings.GetFloat(INFeature.WaterMaxFluctuationSpeed);
        float value = m_fluctuationSpeed + (-16f * m_fluctuationSpeed + 32f * (height - m_height)) * fixedDeltaTime;
        m_height += (m_fluctuationSpeed = UnityEngine.Mathf.Clamp(value, 0f - @float, @float)) * fixedDeltaTime;
    }
}