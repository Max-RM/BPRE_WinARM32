using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class e2dTerrain : MonoBehaviour
{
    public List<e2dCurveNode> TerrainCurve = new List<e2dCurveNode>();
    public Rect TerrainBoundary = new Rect(0f, 0f, 0f, 0f);
    public Texture FillTexture;
    public float FillTextureTileWidth = e2dConstants.INIT_FILL_TEXTURE_WIDTH;
    public float FillTextureTileHeight = e2dConstants.INIT_FILL_TEXTURE_HEIGHT;
    public float FillTextureTileOffsetX = e2dConstants.INIT_FILL_TEXTURE_OFFSET_X;
    public float FillTextureTileOffsetY = e2dConstants.INIT_FILL_TEXTURE_OFFSET_Y;
    public bool CurveClosed = e2dConstants.INIT_CURVE_CLOSED;
    public bool NoCollider = e2dConstants.INIT_NO_COLLIDER;
    public List<e2dCurveTexture> CurveTextures = new List<e2dCurveTexture>();
    public bool PlasticEdges = true;
    public bool AllowRebuildMaterial = true;
    [NonSerialized]
    public UnityEngine.Object EditorReference;
    public bool IsEditable
    {
        get
        {
            if (TerrainCurve != null)
            {
                return TerrainCurve.Count >= 2;
            }

            return false;
        }
    }

    public e2dTerrainBoundary Boundary { get; private set; }
    public e2dTerrainCurveMesh CurveMesh { get; private set; }
    public e2dTerrainFillMesh FillMesh { get; private set; }
    public e2dTerrainColliderMesh ColliderMesh { get; private set; }
    public bool CurveIntercrossing { get; private set; }

    private void OnEnable()
    {
        EditorReference = null;
        Boundary = new e2dTerrainBoundary(this);
        FillMesh = new e2dTerrainFillMesh(this);
        CurveMesh = new e2dTerrainCurveMesh(this);
        ColliderMesh = new e2dTerrainColliderMesh(this);
        if (!FillMesh.IsMeshValid())
        {
            FixCurve();
            FixBoundary();
            RebuildAllMaterials();
            RebuildAllMeshes();
        }
        else
        {
            CurveMesh.UpdateControlTextures(forceRecreate: true);
        }
    }

    private void OnDisable()
    {
        CurveMesh.DestroyTemporaryAssets();
    }

    public void Reset()
    {
        TerrainCurve.Clear();
        TerrainBoundary = new Rect(0f, 0f, 0f, 0f);
        FillMesh.DestroyMesh();
        CurveMesh.DestroyMesh();
        ColliderMesh.DestroyMesh();
    }

    public int GetMaxNodesCount()
    {
        return 8192;
    }

    public void AddPointOnCurve(int beforeWhichIndex, Vector2 toAdd)
    {
        e2dCurveNode e2dCurveNode2 = new e2dCurveNode(toAdd);
        if (beforeWhichIndex > 0)
        {
            e2dCurveNode2.texture = TerrainCurve[beforeWhichIndex - 1].texture;
        }
        else if (beforeWhichIndex < TerrainCurve.Count)
        {
            e2dCurveNode2.texture = TerrainCurve[beforeWhichIndex].texture;
        }

        TerrainCurve.Insert(beforeWhichIndex, e2dCurveNode2);
        CurveMesh.UpdateControlTextures();
    }

    public void RemovePointOnCurve(int index, bool moveTheRest)
    {
        Vector2 vector = Vector2.zero;
        if (index < TerrainCurve.Count - 1)
        {
            vector = TerrainCurve[index + 1].position - TerrainCurve[index].position;
        }

        TerrainCurve.RemoveAt(index);
        if (moveTheRest)
        {
            for (int i = index; i < TerrainCurve.Count; i++)
            {
                TerrainCurve[i].position = TerrainCurve[i].position - vector;
            }
        }

        CurveMesh.UpdateControlTextures();
    }

    public void AddCurvePoints(Vector2[] points, int firstToReplace, int lastToReplace)
    {
        if (TerrainCurve.Count + points.Length - (lastToReplace - firstToReplace + 1) <= GetMaxNodesCount())
        {
            TerrainCurve.RemoveRange(firstToReplace, lastToReplace - firstToReplace + 1);
            TerrainCurve.Capacity = TerrainCurve.Count + points.Length;
            int num = firstToReplace;
            foreach (Vector2 position in points)
            {
                TerrainCurve.Insert(num++, new e2dCurveNode(position));
            }

            CurveMesh.UpdateControlTextures();
        }
    }

    public void AlignPointsOnCurve(int index, int referenceIndex, bool horizontally)
    {
        if (index >= 0 && index <= TerrainCurve.Count - 1 && referenceIndex >= 0 && referenceIndex <= TerrainCurve.Count - 1)
        {
            if (horizontally)
            {
                TerrainCurve[index].position.y = TerrainCurve[referenceIndex].position.y;
            }
            else
            {
                TerrainCurve[index].position.x = TerrainCurve[referenceIndex].position.x;
            }
        }
    }

    public void FixCurve()
    {
        for (int i = 0; i < TerrainCurve.Count; i++)
        {
            if (float.IsNaN(TerrainCurve[i].position.x))
            {
                TerrainCurve[i].position.x = 0f;
            }

            if (float.IsNaN(TerrainCurve[i].position.y))
            {
                TerrainCurve[i].position.y = 0f;
            }
        }

        if (TerrainCurve.Count >= 3 && !CurveClosed && TerrainCurve[TerrainCurve.Count - 1] == TerrainCurve[0])
        {
            Vector2 vector = TerrainCurve[TerrainCurve.Count - 1].position - TerrainCurve[TerrainCurve.Count - 2].position;
            TerrainCurve[TerrainCurve.Count - 1].position -= 0.5f * vector;
        }

        if (TerrainCurve.Count >= 3 && CurveClosed)
        {
            TerrainCurve[TerrainCurve.Count - 1].Copy(TerrainCurve[0]);
        }

        if (!e2dConstants.CHECK_CURVE_INTERCROSSING)
        {
            return;
        }

        CurveIntercrossing = false;
        int num = TerrainCurve.Count;
        if (CurveClosed)
        {
            num--;
        }

        for (int j = 3; j < num; j++)
        {
            if (IntersectsCurve(0, j - 2, TerrainCurve[j - 1].position, TerrainCurve[j].position))
            {
                CurveIntercrossing = true;
            }
        }
    }

    public void FixBoundary()
    {
        Boundary.FixBoundary();
    }

    public bool IntersectsCurve(int startIndex, int endIndex, Vector2 a, Vector2 b)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            if (e2dUtils.SegmentsIntersect(a, b, TerrainCurve[i].position, TerrainCurve[i + 1].position))
            {
                return true;
            }
        }

        return false;
    }

    public void RebuildAllMeshes()
    {
        FillMesh.RebuildMesh();
        CurveMesh.RebuildMesh();
        if (NoCollider)
        {
            ColliderMesh.ResetMesh();
        }
        else
        {
            ColliderMesh.RebuildMesh();
        }
    }

    public void RebuildAllMaterials()
    {
        FillMesh.RebuildMaterial();
        CurveMesh.UpdateControlTextures(forceRecreate: true);
        CurveMesh.RebuildMaterial();
    }
}