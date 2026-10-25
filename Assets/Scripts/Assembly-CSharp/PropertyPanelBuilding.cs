using System.Collections.Generic;
using UnityEngine;

public class PropertyPanelBuilding : PropertyPanel
{
    private string m_text;
    private Camera m_camera;
    public static PropertyPanelBuilding Instance { get; private set; }

    public static PropertyPanelBuilding Create()
    {
        PropertyPanelBuilding propertyPanelBuilding = (Instance = new PropertyPanelBuilding());
        propertyPanelBuilding.Initialize();
        return propertyPanelBuilding;
    }

    protected override void Initialize()
    {
        base.Initialize();
        m_status = StatusCode.Building;
    }

    public override void Start()
    {
        CreateText("PropertyTextBuilding", new Vector2(38f, -216f));
        m_camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    public override void FixedUpdate()
    {
        string text = ((INUnity.Language == SystemLanguage.Chinese) ? "无" : "None");
        Contraption instance = Contraption.Instance;
        BasePart basePart = FindTargetPart();
        string text2 = ((basePart != null) ? basePart.transform.position.Vector2ToString(m_format) : text);
        string text3 = ((basePart != null) ? new Vector2(basePart.m_coordX + basePart.offsetX, basePart.m_coordY + basePart.offsetY).ToString() : text);
        string text4 = INUnity.Language != SystemLanguage.Chinese ? this.m_versionText + "\n" + FormatHeading2("Camera Target Properties") + "\n" + this.m_prefix + "Position " + text2 + "\n" + this.m_prefix + "Building Position " + text3 + "\n\n" + FormatHeading2("Camera Properties") + "\n" + this.m_prefix + "Size " + this.m_camera.orthographicSize.ToString(this.m_format) + "\n" + this.m_prefix + "Position " + this.m_camera.transform.position.Vector2ToString(this.m_format) + "\n\n" + FormatHeading2("Contraption Properties") + "\n" + this.m_prefix + "Part Count " + instance.Parts.Count + "\n" : this.m_versionText + "\n" + FormatHeading2("目标部件属性") + "\n" + this.m_prefix + "位置\u3000 " + text2 + "\n" + this.m_prefix + "建造位置 " + text3 + "\n\n" + FormatHeading2("视野属性") + "\n" + this.m_prefix + "大小\u3000 " + this.m_camera.orthographicSize.ToString(this.m_format) + "\n" + this.m_prefix + "位置\u3000 " + this.m_camera.transform.position.Vector2ToString(this.m_format) + "\n\n" + FormatHeading2("载具属性") + "\n" + this.m_prefix + "部件数 " + instance.Parts.Count + "\n";
        m_text = text4;
    }

    public override void Update()
    {
        LevelManager.GameState gameState = WPFMonoBehaviour.levelManager.gameState;
        RectTransform component = m_textMesh.GetComponent<RectTransform>();
        if (!GameRules.ShowPropertyPanel)
        {
            m_textMesh.text = string.Empty;
            return;
        }

        switch (gameState)
        {
            case LevelManager.GameState.Building:
                m_textMesh.text = m_text;
                component.anchoredPosition = new Vector2(38f, -216f);
                break;
            case LevelManager.GameState.PausedWhileBuilding:
                m_textMesh.text = m_text;
                component.anchoredPosition = new Vector2(38f, -324f);
                break;
            default:
                m_textMesh.text = string.Empty;
                break;
        }
    }

    private BasePart FindTargetPart()
    {
        BasePart.PartType partType = ((SortedPartType)INSettings.GetInt(INFeature.CameraTargetPartType)).ToPartType();
        List<BasePart> parts = Contraption.Instance.Parts;
        for (int num = parts.Count - 1; num >= 0; num--)
        {
            BasePart basePart = parts[num];
            if (basePart != null && basePart.m_partType == partType)
            {
                return basePart;
            }
        }

        return null;
    }
}