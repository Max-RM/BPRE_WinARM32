using System;
using UnityEngine;

public class GlobalEntitySceneDependency : ScriptableObject
{
    public EntitySceneGuid cacheGUID;
}

[Serializable]
public struct EntitySceneGuid
{
    public UInt4 Value;
}

[Serializable]
public struct UInt4
{
    public uint x;
    public uint y;
    public uint z;
    public uint w;
}
