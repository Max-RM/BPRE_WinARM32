using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;

public class INContraption : MonoBehaviour
{
    private bool m_initialized;
    private bool m_enabled;
    private float m_startTime;
    private INBehaviour.StatusCode m_status;
    private ContraptionExtensionData m_extensionData;
    private Dictionary<Rigidbody, INPhysicMaterial> m_materials;
    private Dictionary<Rigidbody, INBounds> m_bounds;
    private List<INBehaviour> m_behaviours;
    private Dictionary<Rigidbody, (float, float)> m_rigidbodyDragTable;
    public static INContraption Instance { get; private set; }

    public ContraptionExtensionData ExtensionData
    {
        get
        {
            if (m_extensionData == null)
            {
                m_extensionData = new ContraptionExtensionData();
            }

            return m_extensionData;
        }
    }

    public bool Enabled
    {
        get => m_enabled;
        private set
        {
            m_enabled = value;
            base.enabled = value;
        }
    }

    public bool IsRunning { get; set; }

    public static INContraption Create(Contraption contraption)
    {
        GameObject obj = contraption.gameObject;
        INContraption component = obj.GetComponent<INContraption>();
        if (component != null)
        {
            UnityEngine.Object.Destroy(component);
        }

        component = obj.AddComponent<INContraption>();
        component.m_enabled = WPFMonoBehaviour.levelManager != null;
        Instance = component;
        return component;
    }

    public void AddBehaviour(INBehaviour behaviour)
    {
        m_behaviours.Add(behaviour);
    }

    private IEnumerable<INBehaviour> GetBehaviours()
    {
        if (m_behaviours == null)
        {
            yield break;
        }

        foreach (INBehaviour behaviour in m_behaviours.Where(behaviour => (behaviour.Status & m_status) != 0))
        {
            yield return behaviour;
        }
    }

    public void OnInterfaceEnabled()
    {
        if (!IsRunning)
        {
            PropertyPanelBuilding.Instance?.OnDisable();
        }
        else
        {
            PropertyPanelRunning.Instance?.OnDisable();
        }

        bool active = false;
        IngameCamera ingameCamera = WPFMonoBehaviour.ingameCamera;
        LevelManager levelManager = WPFMonoBehaviour.levelManager;
        if (ingameCamera != null)
        {
            ingameCamera.enabled = active;
        }

        if (levelManager != null)
        {
            levelManager.InGameGUI.gameObject.SetActive(active);
        }
    }

    public void OnInterfaceDisabled()
    {
        if (!IsRunning)
        {
            PropertyPanelBuilding.Instance?.OnEnable();
        }
        else
        {
            PropertyPanelRunning.Instance?.OnEnable();
        }

        bool active = true;
        IngameCamera ingameCamera = WPFMonoBehaviour.ingameCamera;
        LevelManager levelManager = WPFMonoBehaviour.levelManager;
        if (ingameCamera != null)
        {
            ingameCamera.enabled = active;
        }

        if (levelManager != null)
        {
            levelManager.InGameGUI.gameObject.SetActive(active);
        }
    }

    public void Initialize()
    {
        ContraptionExtensions.Initialize();
        if (!m_enabled)
        {
            return;
        }

        m_initialized = true;
        m_status = ((!IsRunning) ? INBehaviour.StatusCode.Building : INBehaviour.StatusCode.Running);
        m_behaviours = new List<INBehaviour>();
        if (INSettings.GetBool(INFeature.ColoredFrame))
        {
            PartManager.Create<ColoredFrameManager>();
        }

        if (!IsRunning)
        {
            if (INSettings.GetBool(INFeature.PropertyPanel))
            {
                PropertyPanelBuilding.Create();
            }

            return;
        }

        Contraption.Instance.ConnectedComponentsChanged += SetConnectedParts;
        m_startTime = Time.time;
        if (INSettings.GetBool(INFeature.UIPartButtonSystem) && UIPartButtonList.Enabled)
        {
            UIPartButtonList.RuntimeWrapper.Create();
        }

        if (INSettings.GetBool(INFeature.PropertyPanel))
        {
            PropertyPanelRunning.Create();
        }

        if (INSettings.GetBool(INFeature.WaterSystem))
        {
            WaterSystem.Create();
        }

        if (INSettings.GetBool(INFeature.SeparatedFrame))
        {
            PartManager.Create<SeparatedFrameManager>();
        }

        if (INSettings.GetBool(INFeature.BracketFrame))
        {
            PartManager.Create<BracketFrameManager>();
        }

        if (INSettings.GetBool(INFeature.FrameJoint))
        {
            PartManager.Create<FrameJointManager>();
        }

        if (INSettings.GetBool(INFeature.FuelSystem))
        {
            PartManager.Create<FuelSystem>();
        }

        if (INSettings.GetBool(INFeature.PartGenerator))
        {
            PartManager.Create<PartGeneratorManager>();
        }

        if (INSettings.GetBool(INFeature.FixedPumpkin))
        {
            PartManager.Create<FixedPumpkinManager>();
        }

        if (INSettings.GetBool(INFeature.MarkerSeparator))
        {
            PartManager.Create<MarkerManager>();
        }

        if (INSettings.GetBool(INFeature.LightSystem))
        {
            PartManager.Create<EntityLightManager>();
        }

        if (INSettings.GetBool(INFeature.DecelerationLight))
        {
            PartManager.Create<DecelerationLightManager>();
        }

        if (INSettings.GetBool(INFeature.AutoControlLight))
        {
            PartManager.Create<AutoControlLightManager>();
        }

        if (INSettings.GetBool(INFeature.ElectricalSystem))
        {
            PartManager.Create<ElectricalSystem>();
        }
    }

    public void PostInitialize()
    {
        if (m_enabled)
        {
            m_materials = new Dictionary<Rigidbody, INPhysicMaterial>();
            m_bounds = new Dictionary<Rigidbody, INBounds>();
            Rigidbody[] components = GetComponents<Rigidbody>();
            foreach (Rigidbody rigidbody in components)
            {
                GetMaterial(rigidbody);
                GetBounds(rigidbody);
            }

            if (INSettings.GetBool(INFeature.HingePlate))
            {
                HingePlate.InitializeStatic();
            }
        }
    }

    private void Start()
    {
        if (!m_enabled)
        {
            return;
        }

        if (!m_initialized)
        {
            Initialize();
        }

        StartSelf();
        foreach (INBehaviour behaviour in GetBehaviours())
        {
            behaviour.Start();
        }
    }

    private void StartSelf()
    {
        GameObject[] array = GameObject.FindGameObjectsWithTag("Ground");
        foreach (GameObject gameObject in array)
        {
            if (gameObject.name == "GroundCollider")
            {
                gameObject.layer = LayerMask.NameToLayer("Ground");
            }
        }
    }

    private void OnEnable()
    {
        if (!m_enabled)
        {
            return;
        }

        foreach (INBehaviour behaviour in GetBehaviours())
        {
            behaviour.OnEnable();
        }
    }

    private void OnDisable()
    {
        if (!m_enabled)
        {
            return;
        }

        foreach (INBehaviour behaviour in GetBehaviours())
        {
            behaviour.OnDisable();
        }
    }

    private void FixedUpdate() //most expensive stuff
    {
        if (!m_enabled)
        {
            return;
        }

        FixedUpdateSelf();
        foreach (INBehaviour behaviour in GetBehaviours())
        {
            behaviour.FixedUpdate();
        }
    }

    private void FixedUpdateSelf()
    {
        if (!IsRunning)
        {
            return;
        }

        if (INSettings.GetBool(INFeature.NoDrag))
        {
            m_rigidbodyDragTable = m_rigidbodyDragTable ?? new Dictionary<Rigidbody, (float, float)>();
            Rigidbody[] components = GetComponents<Rigidbody>();
            foreach (Rigidbody key in components)
            {
                m_rigidbodyDragTable.TryAdd(key, (key.drag, key.angularDrag));
                key.drag = 0f;
                key.angularDrag = 0f;
            //set everything to 0 drag
            }
        }
        else
        {
            if (m_rigidbodyDragTable == null || (!(m_rigidbodyDragTable.Count > 0)))
            {
                return;
            }

            foreach (KeyValuePair<Rigidbody, (float, float)> pair in m_rigidbodyDragTable)
            {
                Rigidbody key = pair.Key;
                (float, float) value = pair.Value;
                if (key == null)
                    continue;
                key.drag = value.Item1;
                key.angularDrag = value.Item2;
            }

            m_rigidbodyDragTable.Clear();
        }
    }

    private void Update()
    {
        if (!m_enabled)
        {
            return;
        }

        foreach (INBehaviour behaviour in GetBehaviours())
        {
            behaviour.Update();
        }
    }

    private void LateUpdate()
    {
        if (!m_enabled)
        {
            return;
        }

        foreach (INBehaviour behaviour in GetBehaviours())
        {
            behaviour.LateUpdate();
        }
    }

    private void OnDestroy()
    {
        if (!m_enabled)
        {
            return;
        }

        if (Instance == this)
        {
            Instance = null;
        }

        foreach (INBehaviour behaviour in GetBehaviours())
        {
            behaviour.OnDestroy();
        }
    }

    public float GetTime()
    {
        return Time.time - m_startTime;
    }

    private void SetConnectedParts()
    {
        int connectedComponentCount = Contraption.Instance.ConnectedComponentCount;
        List<BasePart> parts = Contraption.Instance.Parts;
        int[] array = new int[connectedComponentCount];
        foreach (BasePart item in parts)
        {
            array[item.ConnectedComponent]++;
        }

        List<BasePart>[] array2 = new List<BasePart>[connectedComponentCount];
        for (int i = 0; i < connectedComponentCount; i++)
        {
            array2[i] = new List<BasePart>(array[i]);
        }

        foreach (BasePart item2 in parts)
        {
            array2[item2.ConnectedComponent].Add(item2);
        }

        ExtensionData.ConnectedParts = array2;
    }

    public new T[] GetComponents<T>()
        where T : Component
    {
        return GetComponents<T>(Contraption.Instance);
    }

    private T[] GetComponents<T>(Component component)
        where T : Component
    {
        Dictionary<Type, ContraptionExtensionData.ComponentsData> components = ExtensionData.Components;
        float fixedTime = Time.fixedTime;
        float fixedDeltaTime = Time.fixedDeltaTime;
        Component[] components2;
        if (components.TryGetValue(typeof(T), out ContraptionExtensionData.ComponentsData value))
        {
            if (value.Time < fixedTime)
            {
                T[] componentsInChildren = component.GetComponentsInChildren<T>();
                ContraptionExtensionData.ComponentsData componentsData = value;
                components2 = componentsInChildren;
                componentsData.Components = components2;
                value.Time = fixedTime + fixedDeltaTime * 0.5f;
            }

            return (T[])value.Components;
        }

        T[] componentsInChildren2 = component.GetComponentsInChildren<T>();
        Type typeFromHandle = typeof(T);
        components2 = componentsInChildren2;
        components.Add(typeFromHandle, new ContraptionExtensionData.ComponentsData(components2, fixedTime + fixedDeltaTime * 0.5f));
        return componentsInChildren2;
    }

    public static INPhysicMaterial GetMaterial(Rigidbody rigidbody)
    {
        if (Instance.m_materials.TryGetValue(rigidbody, out INPhysicMaterial value))
        {
            return value;
        }

        Collider[] componentsInChildren = rigidbody.GetComponentsInChildren<Collider>();
        foreach (Collider collider in componentsInChildren)
        {
            INPhysicMaterial other = new INPhysicMaterial(collider.material);
            value.AddMaterial(other);
        }

        return Instance.m_materials[rigidbody] = value;
    }

    public static INBounds GetBounds(Rigidbody rigidbody)
    {
        if (Instance.m_bounds.TryGetValue(rigidbody, out INBounds value))
        {
            return value;
        }

        Collider[] componentsInChildren = rigidbody.GetComponentsInChildren<Collider>();
        float num = 0f;
        float num2 = 0f;
        float num3 = 0f;
        float num4 = 0f;
        value.Type = 0;
        Collider[] array = componentsInChildren;
        foreach (Collider collider in array)
        {
            if (collider.enabled && !collider.isTrigger)
            {
                Vector2 vector = default(Vector2);
                Vector2 vector2 = default(Vector2);
                if ((collider is BoxCollider boxCollider))
                {
                    vector = boxCollider.center;
                    vector2 = new Vector2(boxCollider.size.x * 0.5f, boxCollider.size.y * 0.5f);
                }
                else if ((collider is CapsuleCollider capsuleCollider))
                {
                    vector = capsuleCollider.center;
                    vector2 = new Vector2(capsuleCollider.radius, capsuleCollider.height * 0.5f);
                }
                else if ((collider is SphereCollider sphereCollider))
                {
                    vector = sphereCollider.center;
                    vector2 = new Vector2(sphereCollider.radius, sphereCollider.radius);
                    value.Type = 1;
                }

                Transform parent = collider.transform;
                while (parent != null && parent != rigidbody.transform)
                {
                    Vector3 localPosition = parent.localPosition;
                    Vector3 vector3 = parent.localRotation * Vector3.right;
                    float num5 = vector3.x * vector.x - vector3.y * vector.y;
                    float num6 = vector3.x * vector.y + vector3.y * vector.x;
                    vector.x = localPosition.x + num5;
                    vector.y = localPosition.y + num6;
                    parent = parent.parent;
                }

                if (vector.x - vector2.x < num)
                {
                    num = vector.x - vector2.x;
                }

                if (vector.x + vector2.x > num2)
                {
                    num2 = vector.x + vector2.x;
                }

                if (vector.y - vector2.y < num3)
                {
                    num3 = vector.y - vector2.y;
                }

                if (vector.y + vector2.y > num4)
                {
                    num4 = vector.y + vector2.y;
                }
            }
        }

        value.X = (num + num2) * 0.5f;
        value.Y = (num3 + num4) * 0.5f;
        value.A = (num2 - num) * 0.5f;
        value.B = (num4 - num3) * 0.5f;
        value.R = Mathf.Sqrt(value.A * value.A + value.B * value.B);
        if (value.Type == 1)
        {
            value.R = ((value.A > value.B) ? value.A : value.B);
            value.A = value.R;
            value.B = value.R;
        }

        return Instance.m_bounds[rigidbody] = value;
    }

    public static BasePart SetRuntimePartInternal(Vector3 position, Vector2Int coord, BasePart.GridRotation gridRotation, bool flipped, BasePart.PartType partType, int customIndex)
    {
        Contraption contraptionRunning = WPFMonoBehaviour.levelManager.ContraptionRunning;
        BasePart basePart = UnityEngine.Object.Instantiate(WPFMonoBehaviour.gameData.GetCustomPart(partType, customIndex), contraptionRunning.transform, true);
        basePart.transform.position = position;
        basePart.CoordX = coord.x;
        basePart.CoordY = coord.y;
        basePart.SetRotation(gridRotation);
        if (flipped)
        {
            basePart.SetFlipped(flipped);
        }

        basePart.contraption = contraptionRunning;
        basePart.gameObject.SetActive(value: true);
        basePart.enabled = true;
        basePart.PrePlaced();
        basePart.ConnectedComponent = -1;
        basePart.gameObject.tag = "Contraption";
        for (int i = 0; i < basePart.transform.childCount; i++)
        {
            basePart.transform.GetChild(i).gameObject.tag = "Contraption";
        }

        basePart.ChangeVisualConnections();
        basePart.EnsureRigidbody();
        basePart.rigidbody.position = position;
        contraptionRunning.AddRuntimePart(basePart);
        basePart.Initialize();
        basePart.PostInitialize();
        contraptionRunning.UpdateConnectedComponents();
        return basePart;
    }
}