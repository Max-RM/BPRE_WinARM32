using System;
using System.Collections.Generic;
using UnityEngine;

public class INRuntimeGameData : Singleton<INRuntimeGameData>
{
    [SerializeField]
    private GameObject m_partContainer;
    [SerializeField]
    private GameObject m_partIconContainer;
    private GameData m_gameData;
    private PartListData m_partListData;
    private PartListBuilder m_partListBuilder;
    public static bool IsInitialized
    {
        get
        {
            if (Singleton<INRuntimeGameData>.instance != null)
            {
                return Singleton<INRuntimeGameData>.instance.m_gameData != null;
            }

            return false;
        }
    }

    public GameData GameData => m_gameData;
    public GameObject PartContainer => m_partContainer;
    public GameObject PartIconContainer => m_partIconContainer;

    private void Awake()
    {
        SetAsPersistant();
        m_partListData = INUnity.LoadScriptableObject<PartListData>("PartListData");
        m_partListBuilder = new PartListBuilder(m_partListData);
        m_gameData = CreateGameData();
        InitializeSettings();
    }

    private void InitializeSettings()
    {
        InitializePart(INFeature.WoodenBox, SetWoodenBox);
        InitializePart(INFeature.MetalBox, SetMetalBox);
        InitializePart(INFeature.BracketFrame, SetBracketFrame);
        InitializePart(INFeature.ColoredFrame, SetColoredFrame);
        InitializePart(INFeature.OffRoadWheel, SetOffRoadWheel);
        InitializePart(INFeature.FuelSystem, SetFuelSystem);
        InitializePart(INFeature.HingePlate, SetHingePlate);
        InitializePart(INFeature.MultipartGenerator, SetMultiPartGenerator);
        InitializePart(INFeature.AutoGun, SetAutoGun);
        InitializePart(INFeature.DecelerationLight, SetDecelerationLight);
        InitializePart(INFeature.AutoControlLight, SetAutoControlLight);
        InitializePart(INFeature.ElectricalSystem, SetElectricalSystem);
        InitializePart(INFeature.AlienExtras, SetAlienExtras);
        InitializePart(INFeature.NeuralPart, SetNeuralPart);
    }

    private void InitializePart(INFeature feature, Action action)
    {
        INSettings.AddListener(feature, action);
        if (INSettings.GetBool(feature))
        {
            action();
        }
    }

    private void SetColoredFrame()
    {
        if (INSettings.GetBool(INFeature.ColoredFrame))
        {
            List<BasePart> partList = m_gameData.GetCustomPart(BasePart.PartType.MetalFrame).PartList;
            BasePart part = m_partListBuilder.GetPart(new PartTypeInfo(BasePart.PartType.MetalFrame, 12));
            float num = INSettings.GetFloat(INFeature.ColoredFrameAlpha);
            float num2 = INSettings.GetFloat(INFeature.ColoredFrameForegroundAlpha);
            float num3 = INSettings.GetFloat(INFeature.ColoredFrameBackgroundAlpha);
            Shader shader = INUnity.LoadShader("PreAlpha_Unlit_ColorTransparent_Geometry");
            for (int i = 0; i < 120; i++)
            {
                int num4 = i - 118;
                int num5 = i / 36;
                int num6 = i % 18;
                string text = (i + 13).ToString();
                BasePart basePart = CreatePartAndSetParent(part);
                basePart.customPartIndex = i + 12;
                basePart.m_partTier = ((num5 == 2) ? BasePart.PartTier.Epic : (num5 == 1) ? BasePart.PartTier.Rare : (num5 == 0) ? BasePart.PartTier.Common : BasePart.PartTier.Legendary);
                if (num4 >= 0)
                {
                    text = (num4 + 133).ToString();
                    basePart.customPartIndex = num4 + 132;
                    basePart.m_partTier = BasePart.PartTier.Regular;
                }

                basePart.gameObject.name = "Part_MetalFrame_" + text + "_SET";
                Color color;
                if (num5 < 3)
                {
                    float s = ((i / 18 % 2 == 0) ? 0.7f : 0.4f);
                    color = Color.HSVToRGB((float)num6 / 18f, s, ((num5 == 1) ? 0.6f : (num5 == 0) ? 0.9f : 0.3f));
                }
                else
                {
                    color = Color.Lerp(Color.white, Color.black, (float)num6 * 0.1f);
                }

                if (num4 >= 0)
                {
                    color = Color.white;
                    num *= ((num4 == 0) ? 0.5f : 0f);
                    num2 *= 0.5f;
                    num3 *= 0.5f;
                }

                (basePart as ColoredFrame).Color = color;
                basePart.GetComponent<MeshRenderer>().material.shader = shader;
                Sprite constructionIconSprite = basePart.m_constructionIconSprite;
                constructionIconSprite.name = "Icon_MetalFrame_" + text;
                MeshRenderer component = constructionIconSprite.GetComponent<MeshRenderer>();
                MeshRenderer component2 = constructionIconSprite.transform.Find("Background").GetComponent<MeshRenderer>();
                component.material.name = "IngameAtlas3_MetalFrame_" + text;
                component.material.shader = shader;
                component.material.color = new Color(color.r, color.g, color.b, num);
                component2.material.name = "IngameAtlas3_MetalFrame_Background_" + text;
                component2.material.color = new Color(color.r, color.g, color.b, num3);
                basePart.m_constructionIconSprite = constructionIconSprite;
                partList.Add(basePart);
            }
        }
        else
        {
            for (int j = 12; j <= 129; j++)
            {
                RemoveCustomPart(BasePart.PartType.MetalFrame, j);
            }

            RemoveCustomPart(BasePart.PartType.MetalFrame, 132);
            RemoveCustomPart(BasePart.PartType.MetalFrame, 133);
        }
    }

    private void SetWoodenBox()
    {
        if (INSettings.GetBool(INFeature.WoodenBox))
        {
            AddCustomPart(CreatePartAndSetParent(BasePart.PartType.WoodenFrame, 10));
        }
        else
        {
            RemoveCustomPart(BasePart.PartType.WoodenFrame, 10);
        }
    }

    private void SetMetalBox()
    {
        if (INSettings.GetBool(INFeature.MetalBox))
        {
            AddCustomPart(CreatePartAndSetParent(BasePart.PartType.MetalFrame, 130));
        }
        else
        {
            RemoveCustomPart(BasePart.PartType.MetalFrame, 130);
        }
    }

    private void SetBracketFrame()
    {
        if (INSettings.GetBool(INFeature.BracketFrame))
        {
            AddCustomPart(CreatePartAndSetParent(BasePart.PartType.MetalFrame, 131));
        }
        else
        {
            RemoveCustomPart(BasePart.PartType.MetalFrame, 131);
        }
    }

    private void SetOffRoadWheel()
    {
        if (INSettings.GetBool(INFeature.OffRoadWheel))
        {
            AddCustomPart(CreatePartAndSetParent(BasePart.PartType.MotorWheel, 7));
        }
        else
        {
            RemoveCustomPart(BasePart.PartType.MotorWheel, 7);
        }
    }

    private void SetHingePlate()
    {
        if (INSettings.GetBool(INFeature.HingePlate))
        {
            AddCustomPart(CreatePartAndSetParent(BasePart.PartType.Rope, 4));
            AddCustomPart(CreatePartAndSetParent(BasePart.PartType.Rope, 5));
            AddCustomPart(CreatePartAndSetParent(BasePart.PartType.Rope, 6));
            AddCustomPart(CreatePartAndSetParent(BasePart.PartType.Rope, 7));
        }
        else
        {
            RemoveCustomPart(BasePart.PartType.Rope, 4);
            RemoveCustomPart(BasePart.PartType.Rope, 5);
            RemoveCustomPart(BasePart.PartType.Rope, 6);
            RemoveCustomPart(BasePart.PartType.Rope, 7);
        }
    }

    private void SetAutoGun()
    {
        if (INSettings.GetBool(INFeature.AutoGun))
        {
            AddCustomPart(CreatePartAndSetParent(BasePart.PartType.GrapplingHook, 6));
        }
        else
        {
            RemoveCustomPart(BasePart.PartType.GrapplingHook, 6);
        }
    }

    private void SetMultiPartGenerator()
    {
        if (INSettings.GetBool(INFeature.MultipartGenerator))
        {
            for (int i = 0; i < 3; i++)
            {
                BasePart basePart = CreatePartAndSetParent(BasePart.PartType.GrapplingHook, 8);
                if (i != 0)
                {
                    basePart.gameObject.name = $"Part_GrapplingHook_{i + 9}_SET";
                    basePart.customPartIndex = i + 8;
                    basePart.m_constructionIconSprite.gameObject.name = $"Icon_GrapplingHook_{i + 9}";
                    INSerializedSprite component = basePart.GetComponent<INSerializedSprite>();
                    component.SpriteName = $"MultiPartGenerator{i + 1}_Sprite";
                    component.UpdateMesh();
                    INSerializedSprite componentInChildren = basePart.m_constructionIconSprite.GetComponentInChildren<INSerializedSprite>();
                    componentInChildren.SpriteName = $"MultiPartGenerator{i + 1}_IconSprite";
                    componentInChildren.UpdateMesh();
                }

                basePart.m_partTier = BasePart.PartTier.Common;
                AddCustomPart(basePart);
            }
        }
        else
        {
            for (int j = 0; j < 3; j++)
            {
                RemoveCustomPart(BasePart.PartType.GrapplingHook, j + 8);
            }
        }
    }

    private void SetDecelerationLight()
    {
        if (INSettings.GetBool(INFeature.DecelerationLight))
        {
            AddCustomPart(CreatePartAndSetParent(BasePart.PartType.PointLight, 5));
        }
        else
        {
            RemoveCustomPart(BasePart.PartType.PointLight, 5);
        }
    }

    private void SetAutoControlLight()
    {
        if (INSettings.GetBool(INFeature.AutoControlLight))
        {
            AddCustomPart(CreatePartAndSetParent(BasePart.PartType.PointLight, 6));
        }
        else
        {
            RemoveCustomPart(BasePart.PartType.PointLight, 6);
        }
    }

    private void SetFuelSystem()
    {
        if (!INSettings.GetBool(INFeature.FuelSystem))
        {
            return;
        }

        foreach (BasePart part in m_partListBuilder.GetParts(BasePart.PartType.JetEngine))
        {
            BasePart basePart = CreatePartAndSetParent(part);
            if (basePart.customPartIndex == 0)
            {
                AddPart(basePart);
            }
            else
            {
                AddCustomPart(basePart);
            }
        }
    }

    private void SetElectricalSystem()
    {
        if (!INSettings.GetBool(INFeature.ElectricalSystem))
        {
            return;
        }

        foreach (BasePart part in m_partListBuilder.GetParts(BasePart.PartType.ElectricalPart))
        {
            BasePart basePart = CreatePartAndSetParent(part);
            if (basePart.customPartIndex == 0)
            {
                AddPart(basePart);
            }
            else
            {
                AddCustomPart(basePart);
            }
        }

        foreach (PartListBuilder.PartRangeValue partRange in m_partListBuilder.GetPartRanges(BasePart.PartType.ElectricalPart))
        {
            foreach (BasePart item in m_partListBuilder.CreatePartRange(partRange))
            {
                SetParent(item);
                if (item.customPartIndex == 0)
                {
                    AddPart(item);
                }
                else
                {
                    AddCustomPart(item);
                }
            }
        }
    }

    private void SetAlienExtras()
    { /*
		if (INSettings.GetBool(INFeature.AlienExtras))
		{
			AddCustomPart(CreatePartAndSetParent(BasePart.PartType.WoodenFrame, 11));
			AddCustomPart(CreatePartAndSetParent(BasePart.PartType.Spring, 4));
			AddCustomPart(CreatePartAndSetParent(BasePart.PartType.GrapplingHook, 11));
			AddCustomPart(CreatePartAndSetParent(BasePart.PartType.TNT, 6));
			AddCustomPart(CreatePartAndSetParent(BasePart.PartType.GrapplingHook, 12));
			AddCustomPart(CreatePartAndSetParent(BasePart.PartType.Propeller, 10));
			AddCustomPart(CreatePartAndSetParent(BasePart.PartType.WoodenFrame, 12));
			AddCustomPart(CreatePartAndSetParent(BasePart.PartType.Engine, 7));
			AddCustomPart(CreatePartAndSetParent(BasePart.PartType.EngineBig, 7));
			AddCustomPart(CreatePartAndSetParent(BasePart.PartType.StickyWheel, 4));
		}
		else
		{
			RemoveCustomPart(BasePart.PartType.WoodenFrame, 11);
			RemoveCustomPart(BasePart.PartType.Spring, 4);
			RemoveCustomPart(BasePart.PartType.GrapplingHook, 11);
			RemoveCustomPart(BasePart.PartType.TNT, 7);
			RemoveCustomPart(BasePart.PartType.GrapplingHook, 12);
			RemoveCustomPart(BasePart.PartType.Propeller, 10);
			RemoveCustomPart(BasePart.PartType.WoodenFrame, 12);
			RemoveCustomPart(BasePart.PartType.Engine, 7);
			RemoveCustomPart(BasePart.PartType.EngineBig, 7);
			RemoveCustomPart(BasePart.PartType.StickyWheel, 4);
		}*/
    }

    private void SetNeuralPart()
    {
        if (!INSettings.GetBool(INFeature.NeuralPart))
        {
            return;
        }

        foreach (BasePart part in m_partListBuilder.GetParts(BasePart.PartType.NeuralPart))
        {
            BasePart basePart = CreatePartAndSetParent(part);
            if (basePart.customPartIndex == 0)
            {
                AddPart(basePart);
            }
            else
            {
                AddCustomPart(basePart);
            }
        }

        foreach (PartListBuilder.PartRangeValue partRange in m_partListBuilder.GetPartRanges(BasePart.PartType.NeuralPart))
        {
            foreach (BasePart item in m_partListBuilder.CreatePartRange(partRange))
            {
                SetParent(item);
                if (item.customPartIndex == 0)
                {
                    AddPart(item);
                }
                else
                {
                    AddCustomPart(item);
                }
            }
        }
    }

    private GameData CreateGameData()
    {
        GameData gameData = UnityEngine.Object.Instantiate(Singleton<GameManager>.Instance.gameData);
        for (int i = 0; i < gameData.m_parts.Count; i++)
        {
            BasePart component = gameData.m_parts[i].GetComponent<BasePart>();
            gameData.m_parts[i] = CreatePartAndSetParent(component).gameObject;
        }

        foreach (CustomPartInfo customPart in gameData.m_customParts)
        {
            List<BasePart> partList = customPart.PartList;
            for (int j = 0; j < partList.Count; j++)
            {
                partList[j] = CreatePartAndSetParent(partList[j]);
            }
        }

        return gameData;
    }

    private BasePart CreatePartAndSetParent(BasePart.PartType partType, int partIndex)
    {
        return CreatePartAndSetParent(new PartTypeInfo(partType, partIndex));
    }

    private BasePart CreatePartAndSetParent(PartTypeInfo info)
    {
        return CreatePartAndSetParent(m_partListBuilder.GetPart(info));
    }

    private BasePart CreatePartAndSetParent(BasePart part)
    {
        BasePart basePart = m_partListBuilder.CreatePart(part);
        SetParent(basePart);
        return basePart;
    }

    private void SetParent(BasePart part)
    {
        part.transform.parent = m_partContainer.transform;
        if (part.m_constructionIconSprite != null)
        {
            part.m_constructionIconSprite.transform.parent = m_partIconContainer.transform;
        }
    }

    private void AddPart(BasePart newPart)
    {
        m_gameData.m_parts.Add(newPart.gameObject);
    }

    private void AddCustomPart(BasePart newPart)
    {
        CustomPartInfo customPartInfo = m_gameData.GetCustomPart(newPart.m_partType);
        if (customPartInfo == null)
        {
            customPartInfo = new CustomPartInfo(newPart.m_partType, new List<BasePart>());
            m_gameData.m_customParts.Add(customPartInfo);
        }

        customPartInfo.PartList.Add(newPart);
    }

    private void ReplaceCustomPart(BasePart newPart)
    {
        List<BasePart> partList = m_gameData.GetCustomPart(newPart.m_partType).PartList;
        for (int i = 0; i < partList.Count; i++)
        {
            BasePart basePart = partList[i];
            if (basePart.customPartIndex == newPart.customPartIndex)
            {
                partList[i] = newPart;
                if (basePart.m_constructionIconSprite != null)
                {
                    UnityEngine.Object.Destroy(basePart.m_constructionIconSprite.gameObject);
                }

                UnityEngine.Object.Destroy(basePart.gameObject);
                break;
            }
        }
    }

    private void RemoveCustomPart(BasePart.PartType type, int customIndex)
    {
        List<BasePart> partList = m_gameData.GetCustomPart(type).PartList;
        for (int i = 0; i < partList.Count; i++)
        {
            BasePart basePart = partList[i];
            if (basePart.customPartIndex == customIndex)
            {
                partList.RemoveAt(i);
                if (basePart.m_constructionIconSprite != null)
                {
                    UnityEngine.Object.Destroy(basePart.m_constructionIconSprite.gameObject);
                }

                UnityEngine.Object.Destroy(basePart.gameObject);
                break;
            }
        }
    }

    public BasePart GetCustomPart(BasePart.PartType type, int customIndex)
    {
        if (customIndex <= 0)
        {
            GameObject part = m_gameData.GetPart(type);
            if (!(part != null))
            {
                return null;
            }

            return part.GetComponent<BasePart>();
        }

        CustomPartInfo customPart = m_gameData.GetCustomPart(type);
        if (customPart != null)
        {
            foreach (BasePart part2 in customPart.PartList)
            {
                if (part2.customPartIndex == customIndex)
                {
                    return part2;
                }
            }
        }

        return null;
    }

    public int GetCustomPartIndex(BasePart.PartType type, string partName)
    {
        GameObject part = m_gameData.GetPart(type);
        if (part != null && part.name.Equals(partName))
        {
            return 0;
        }

        if (m_gameData.GetCustomPart(type) != null)
        {
            foreach (BasePart part2 in m_gameData.GetCustomPart(type).PartList)
            {
                if (part2.name.Equals(partName))
                {
                    return part2.customPartIndex;
                }
            }
        }

        return -1;
    }
}