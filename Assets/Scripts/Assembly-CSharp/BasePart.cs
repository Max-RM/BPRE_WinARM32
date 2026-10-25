using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

public class BasePart : WPFMonoBehaviour
{
    public enum JointType
    {
        FixedJoint = 0,
        HingeJoint = 1
    }

    public enum PartTier
    {
        Regular = 0,
        Common = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    public enum PartType
    {
        Unknown = 0,
        Balloon = 1,
        Balloons2 = 2,
        Balloons3 = 3,
        Fan = 4,
        WoodenFrame = 5,
        Bellows = 6,
        CartWheel = 7,
        Basket = 8,
        Sandbag = 9,
        Pig = 10,
        Sandbag2 = 11,
        Sandbag3 = 12,
        Propeller = 13,
        Wings = 14,
        Tailplane = 15,
        Engine = 16,
        Rocket = 17,
        MetalFrame = 18,
        SmallWheel = 19,
        MetalWing = 20,
        MetalTail = 21,
        Rotor = 22,
        MotorWheel = 23,
        TNT = 24,
        EngineSmall = 25,
        EngineBig = 26,
        NormalWheel = 27,
        Spring = 28,
        Umbrella = 29,
        Rope = 30,
        CokeBottle = 31,
        KingPig = 32,
        RedRocket = 33,
        SodaBottle = 34,
        PoweredUmbrella = 35,
        Egg = 36,
        JetEngine = 37,
        ObsoleteWheel = 38,
        SpringBoxingGlove = 39,
        StickyWheel = 40,
        GrapplingHook = 41,
        Pumpkin = 42,
        Kicker = 43,
        Gearbox = 44,
        GoldenPig = 45,
        PointLight = 46,
        SpotLight = 47,
        TimeBomb = 48,
        ElectricalPart = 49,
        NeuralPart = 50,
        MAX = 51
    }

    public float GetMaxHp(BasePart basePart) //this is still WIP
    {
        return ((basePart.m_partType == PartType.Balloon) ? 13f : (basePart.m_partType == PartType.Balloons2) ? 26f : (basePart.m_partType == PartType.Balloons3) ? 39f : (basePart.m_partType == PartType.Fan) ? 4f : (basePart.m_partType == PartType.WoodenFrame) ? 5f : (basePart.m_partType == PartType.Bellows) ? 6f : (basePart.m_partType == PartType.CartWheel) ? 7f : (basePart.m_partType == PartType.Basket) ? 8f : (basePart.m_partType == PartType.Sandbag) ? 9f : (basePart.m_partType == PartType.Pig) ? 3e+38f : (basePart.m_partType == PartType.Sandbag2) ? 11f : (basePart.m_partType == PartType.Sandbag3) ? 12f : (basePart.m_partType == PartType.Propeller) ? 13f : (basePart.m_partType == PartType.Wings) ? 14f : (basePart.m_partType == PartType.Tailplane) ? 15f : (basePart.m_partType == PartType.Engine) ? 16f : (basePart.m_partType == PartType.Rocket) ? 17f : (basePart.m_partType == PartType.MetalFrame) ? 18f : (basePart.m_partType == PartType.SmallWheel) ? 19f : (basePart.m_partType == PartType.MetalWing) ? 20f : (basePart.m_partType == PartType.MetalTail) ? 21f : (basePart.m_partType == PartType.Rotor) ? 22f : (basePart.m_partType == PartType.MotorWheel) ? 23f : (basePart.m_partType == PartType.TNT) ? 24f : (basePart.m_partType == PartType.EngineSmall) ? 25f : (basePart.m_partType == PartType.EngineBig) ? 26f : (basePart.m_partType == PartType.NormalWheel) ? 27f : (basePart.m_partType == PartType.Spring) ? 28f : (basePart.m_partType == PartType.Umbrella) ? 29f : (basePart.m_partType == PartType.Rope) ? 30f : (basePart.m_partType == PartType.CokeBottle) ? 31f : (basePart.m_partType == PartType.KingPig) ? 3e+38f : (basePart.m_partType == PartType.RedRocket) ? 33f : (basePart.m_partType == PartType.SodaBottle) ? 34f : (basePart.m_partType == PartType.PoweredUmbrella) ? 35f : (basePart.m_partType == PartType.Egg) ? 3e+38f : (basePart.m_partType == PartType.JetEngine) ? 37f : (basePart.m_partType == PartType.ObsoleteWheel) ? 38f : (basePart.m_partType == PartType.SpringBoxingGlove) ? 39f : (basePart.m_partType == PartType.StickyWheel) ? 40f : (basePart.m_partType == PartType.GrapplingHook) ? 41f : (basePart.m_partType == PartType.Pumpkin) ? float.PositiveInfinity : (basePart.m_partType == PartType.Kicker) ? 2000f : (basePart.m_partType == PartType.Gearbox) ? 44f : (basePart.m_partType == PartType.GoldenPig) ? 45f : (basePart.m_partType == PartType.PointLight) ? 46f : (basePart.m_partType == PartType.SpotLight) ? 47f : (basePart.m_partType == PartType.TimeBomb) ? 48f : (basePart.m_partType == PartType.ElectricalPart) ? 2000f : 51f);
    }

    public enum AutoAlignType
    {
        None = 0,
        Rotate = 1,
        FlipVertically = 2
    }

    /// <summary>
    /// An alternate expression for BasePart.GridRotation but directional.
    /// DON'T USE (BasePart.Direction)Integer !
    /// </summary>
    public enum Direction
    {
        Right = 0,
        Up = 1,
        Left = 2,
        Down = 3,
        UpRight = 4,
        UpLeft = 5,
        DownLeft = 6,
        DownRight = 7
    }

    /// <summary>
    /// Mathematically Counterclockwise, [0..3]=>0 to 270 deg
    /// DON'T USE (BasePart.GridRotation)Integer !
    /// <example>
    /// Deg_0 = 0,
    /// Deg_90 = 1,
    /// Deg_180 = 2,
    /// Deg_270 = 3,
    /// Deg_45 = 4,
    /// Deg_135 = 5,
    /// Deg_225 = 6,
    /// Deg_315 = 7,
    /// Deg_Max = 8
    /// </example>
    /// </summary>
    public enum GridRotation
    {
        Deg_0 = 0,
        Deg_90 = 1,
        Deg_180 = 2,
        Deg_270 = 3,
        Deg_45 = 4,
        Deg_135 = 5,
        Deg_225 = 6,
        Deg_315 = 7,
        Deg_Max = 8
    }

    public enum JointConnectionType
    {
        None = 0,
        Source = 1,
        Target = 2
    }

    public enum JointConnectionDirection
    {
        Any = 0,
        Right = 1,
        Up = 2,
        Left = 3,
        Down = 4,
        LeftAndRight = 5,
        UpAndDown = 6,
        None = 7
    }

    public enum JointConnectionStrength
    {
        Weak = 0,
        Normal = 1,
        High = 2,
        Extreme = 3,
        HighlyExtreme = 4
    }

    private static float m_lastTimeUsedCollisionParticles;
    protected static int m_groundLayer = -1;
    protected static int m_iceGroundLayer = -1;
    protected bool m_isOnGround;
    [SerializeField]
    private bool m_jointPreprocessing;
    public bool m_eightWay;
    public int m_coordX;
    public int m_coordY;
    public float m_mass = 1f;
    public float m_interactiveRadius = 0.5f;
    public float m_breakVelocity;
    public float m_powerConsumption;
    public float m_enginePower;
    public float m_ZOffset;
    public int customPartIndex;
    public bool craftable = true;
    public bool lootCrateReward = true;
    public List<string> tags;
    private float m_lastTimeTouchedGround;
    private bool m_isOnIce;
    private AudioSource m_slidingSound;
    [SerializeField]
    private AudioManager.AudioMaterial audioMaterial;
    public JointType m_jointType;
    public PartTier m_partTier;
    public PartType m_partType;
    public AutoAlignType m_autoAlign;
    public bool m_flipped;
    public GridRotation m_gridRotation;
    public int m_gridXmin;
    public int m_gridXmax;
    public int m_gridYmin;
    public int m_gridYmax;
    [Range(-0.499999f, 0.499999f)]
    public float offsetX = 0f;
    [Range(-0.499999f, 0.499999f)]
    public float offsetY = 0f;
    public bool m_static;
    public JointConnectionStrength m_jointConnectionStrength;
    public JointConnectionType m_jointConnectionType = JointConnectionType.Target;
    public JointConnectionDirection m_jointConnectionDirection;
    public JointConnectionDirection m_customJointConnectionDirection = JointConnectionDirection.None;
    private bool m_broken;
    public BasePart m_enclosedPart;
    public BasePart m_enclosedInto;
    public Sprite m_constructionIconSprite;
    protected bool m_valid;
    protected SpriteManager m_spriteManager;
    public Vector3 lateSpeed;
    public float m_hp = 1000f;
    public float m_maxHp;
    public float m_minDamage = 31.4f; //do not change without purpose
    public void Hurt(float damage)
    {
        if (GameRules.PartHPStatus == 0)
            return;
        if (damage < m_minDamage)
        {
            return;
        }

        m_hp -= damage;
        if (m_hp <= 0f)
        {
            LowHpDestruction(damage);
        }
    }

    public virtual void LowHpDestruction(float passDamage)
    {
        switch (GameRules.PartHPStatus)
        {
            case 0:
                return;
            case 1:
                contraption.RemovePart(this);
                Destroy(gameObject);
                return;
        }

        //if (Math.Abs(m_minDamage +2f) < 0.001f)return;
        //Destroy(gameObject);
        m_minDamage = -2f;
        Joint[] joints = this.gameObject.GetComponents<Joint>();
        foreach (Joint V in joints)
        {
            if (V)
                Destroy(V);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.5f);
        foreach (Collider _collider in colliders)
        {
            BasePart basePart = _collider.GetComponent<BasePart>();
            if (basePart)
            {
                Physics.IgnoreCollision(_collider, collider);
            //this thing makes stackoverflow --> basePart.Hurt(passDamage/(4f+64f*Vector3.SqrMagnitude(_collider.transform.position-transform.position)));//hidden recursion, use big denominator to avoid stackoverflow
            }
        }

        gameObject.isStatic = true;
        contraption.RemovePart(this);
    }

    public bool VisibleOnPartListBeforeUnlocking
    {
        get
        {
            if (CustomizationManager.IsPartUnlocked(this))
                return true;
            return craftable && lootCrateReward;
        }
    }

    public bool JointPreprocessing => m_jointPreprocessing;
    public virtual Vector3 Position => base.transform.position;
    public AudioManager.AudioMaterial AudioMaterial => audioMaterial;
    public Contraption contraption { get; set; }
    public int ConnectedComponent { get; set; } = -1;

    public BasePart enclosedPart
    {
        get => m_enclosedPart;
        set
        {
            m_enclosedPart = value;
            if ((bool)value)
            {
                value.enclosedInto = this;
            }
        }
    }

    public BasePart enclosedInto { get => m_enclosedInto; set => m_enclosedInto = value; }
    public Vector3 WindVelocity { get; set; }
    public bool valid { get => m_valid; set => m_valid = value; }
    public PartType Type { get => m_partType; set => m_partType = value; }
    public PartTier Tier { get => m_partTier; set => m_partTier = value; }
    public int Index { get => customPartIndex; set => customPartIndex = value; }
    public int CoordX { get => m_coordX; set => m_coordX = value; }
    public int CoordY { get => m_coordY; set => m_coordY = value; }
    public GridRotation Rotation { get => m_gridRotation; set => m_gridRotation = value; }
    public bool Flipped { get => m_flipped; set => m_flipped = value; }

    public PartTypeInfo TypeInfo
    {
        get => new PartTypeInfo(m_partType, customPartIndex);
        set
        {
            PartTypeInfo partTypeInfo = value;
            (m_partType, customPartIndex) = partTypeInfo;
        }
    }

    public int StrictConnectedComponent { get; set; }
    public int GeneralConnectedComponent { get; set; }
    public int GeneratorRefCount { get; set; }
    public int GenerationLevel { get; set; }
    public int GenerationIndex { get; set; }
    public float Temperature { get; set; }

    public bool HasGeneratorRef
    {
        get
        {
            if (INSettings.GetBool(INFeature.PartGenerator))
            {
                return GeneratorRefCount > 0;
            }

            return false;
        }
    }

    public virtual JointConnectionStrength GetJointConnectionStrength()
    {
        return m_jointConnectionStrength;
    }

    public static PartType BaseType(PartType type)
    {
        switch (type)
        {
            case PartType.Balloons2:
            case PartType.Balloons3:
                return PartType.Balloon;
            case PartType.Sandbag2:
            case PartType.Sandbag3:
                return PartType.Sandbag;
            default:
                return type;
        }
    }

    public virtual void Awake()
    {
        ConnectedComponent = -1;
        StrictConnectedComponent = -1;
        GeneralConnectedComponent = -1;
        m_valid = true;
        m_spriteManager = GetComponent<SpriteManager>();
        m_groundLayer = LayerMask.NameToLayer("Ground");
        m_iceGroundLayer = LayerMask.NameToLayer("IceGround");
    }

    public virtual void Initialize()
    {
    }

    public virtual void InitializeEngine()
    {
    }

    public virtual bool CanBeEnabled()
    {
        return false;
    }

    public virtual bool HasOnOffToggle()
    {
        return false;
    }

    public virtual bool IsEnabled()
    {
        return false;
    }

    public virtual void SetEnabled(bool enabled)
    {
    }

    public virtual bool IsPowered()
    {
        return m_powerConsumption > 0f;
    }

    public bool IsEngine()
    {
        return m_enginePower > 0f;
    }

    public virtual Direction EffectDirection()
    {
        return Direction.Right;
    }

    public virtual GridRotation AutoAlignRotation(JointConnectionDirection target)
    {
        if (m_jointConnectionDirection == JointConnectionDirection.None)
        {
            return RotationTo(m_customJointConnectionDirection, target);
        }

        return RotationTo(m_jointConnectionDirection, target);
    }

    public bool HasTag(string tag)
    {
        if (tags != null)
        {
            return tags.Contains(tag);
        }

        return false;
    }

    public bool IsFlipped()
    {
        return m_flipped;
    }

    public virtual void SetFlipped(bool flipped)
    {
        m_flipped = flipped;
        if (m_flipped)
        {
            base.transform.localRotation = Quaternion.AngleAxis(180f, Vector3.up);
        }
        else
        {
            base.transform.localRotation = Quaternion.identity;
        }

        OnFlipped();
    }

    public static Direction ConvertDirection(JointConnectionDirection direction)
    {
        return (Direction)(direction - 1);
    }

    public static bool IsDirection(JointConnectionDirection direction)
    {
        if (direction != JointConnectionDirection.Left && direction != JointConnectionDirection.Right && direction != JointConnectionDirection.Up)
        {
            return direction == JointConnectionDirection.Down;
        }

        return true;
    }

    public JointConnectionDirection GetJointConnectionDirection()
    {
        if (INSettings.GetBool(INFeature.NewTimeBombConnection) && m_partType == PartType.TimeBomb)
        {
            return GlobalJointConnectionDirection((!(m_enclosedInto == null)) ? JointConnectionDirection.UpAndDown : JointConnectionDirection.Any);
        }

        return GlobalJointConnectionDirection(m_jointConnectionDirection);
    }

    public JointConnectionDirection GetCustomJointConnectionDirection()
    {
        return GlobalJointConnectionDirection(m_customJointConnectionDirection);
    }

    public JointConnectionDirection GlobalJointConnectionDirection(JointConnectionDirection localDirection)
    {
        if (((localDirection == JointConnectionDirection.Any) || (localDirection == JointConnectionDirection.None)))
        {
            return localDirection;
        }

        JointConnectionDirection jointConnectionDirection = localDirection;
        switch (localDirection)
        {
            case JointConnectionDirection.LeftAndRight:
                if (((m_gridRotation == GridRotation.Deg_90) || (m_gridRotation == GridRotation.Deg_270)))
                {
                    jointConnectionDirection = JointConnectionDirection.UpAndDown;
                }

                break;
            case JointConnectionDirection.UpAndDown:
                if (((m_gridRotation == GridRotation.Deg_90) || (m_gridRotation == GridRotation.Deg_270)))
                {
                    jointConnectionDirection = JointConnectionDirection.LeftAndRight;
                }

                break;
            default:
                jointConnectionDirection = (JointConnectionDirection)(((int)(localDirection - 1) + (int)m_gridRotation) % 4 + 1);
                break;
        }

        if (m_flipped)
        {
            switch (jointConnectionDirection)
            {
                case JointConnectionDirection.Left:
                    return JointConnectionDirection.Right;
                case JointConnectionDirection.Right:
                    return JointConnectionDirection.Left;
                case JointConnectionDirection.Up:
                    return JointConnectionDirection.Down;
                case JointConnectionDirection.Down:
                    return JointConnectionDirection.Up;
            }
        }

        return jointConnectionDirection;
    }

    /// <summary>
    /// Inverse any given <c>BasePart.Direction</c>
    /// </summary>
    /// <param name = "direction">Given direction</param>
    /// <returns>(BasePart.Direction)Inverse direction of given direction</returns>
    public static Direction InverseDirection(Direction direction)
    {
        return ((direction == Direction.Right) ? Direction.Left : (direction == Direction.Up) ? Direction.Down : (direction == Direction.Left) ? Direction.Right : (direction == Direction.Down) ? Direction.Up : Direction.Right);
    }

    /// <summary>
    /// Extension method to <c>BasePart</c>
    /// [Check this function to add more connection directions]
    /// </summary>
    /// <param name = "direction">The connection direction that will be checked</param>
    /// <returns>(bool)Can this part connect to given direction</returns>
    /// <seealso cref = "JointConnectionDirection"/>
    /// <seealso cref = "Direction"/>
    /// <seealso cref = "GridRotation"/>
    public bool CanConnectTo(Direction direction)
    {
        switch (GetJointConnectionDirection())
        {
            case JointConnectionDirection.Any:
                return true;
            case JointConnectionDirection.Right:
                return direction == Direction.Right;
            case JointConnectionDirection.Up:
                return direction == Direction.Up;
            case JointConnectionDirection.Left:
                return direction == Direction.Left;
            case JointConnectionDirection.Down:
                return direction == Direction.Down;
            case JointConnectionDirection.LeftAndRight:
                if (direction != Direction.Left)
                {
                    return direction == Direction.Right;
                }

                return true;
            case JointConnectionDirection.UpAndDown:
                if (direction != Direction.Up)
                {
                    return direction == Direction.Down;
                }

                return true;
            case JointConnectionDirection.None:
                return false;
            default:
                return false;
        }
    }

    public bool CanCustomConnectTo(Direction direction)
    {
        switch (GetCustomJointConnectionDirection())
        {
            case JointConnectionDirection.Any:
                return true;
            case JointConnectionDirection.Right:
                return direction == Direction.Right;
            case JointConnectionDirection.Up:
                return direction == Direction.Up;
            case JointConnectionDirection.Left:
                return direction == Direction.Left;
            case JointConnectionDirection.Down:
                return direction == Direction.Down;
            case JointConnectionDirection.LeftAndRight:
                if (direction != Direction.Left)
                {
                    return direction == Direction.Right;
                }

                return true;
            case JointConnectionDirection.UpAndDown:
                if (direction != Direction.Up)
                {
                    return direction == Direction.Down;
                }

                return true;
            case JointConnectionDirection.None:
                return false;
            default:
                return false;
        }
    }

    [BurstCompile]
    public static Direction Rotate(Direction direction, GridRotation rotation)
    {
        return (Direction)(((int)direction + (int)rotation) % 4);
    }

    public static Direction RotateWithEightDirections(Direction direction, GridRotation rotation)
    {
        return (Direction)(((int)direction + (int)rotation) % 8);
    }

    public static GridRotation RotationTo(JointConnectionDirection source, JointConnectionDirection target)
    {
        if (source == JointConnectionDirection.Any || target == JointConnectionDirection.Any || source == JointConnectionDirection.None || target == JointConnectionDirection.None)
        {
            return GridRotation.Deg_0;
        }

        switch (source)
        {
            case JointConnectionDirection.LeftAndRight:
                if ((((target == JointConnectionDirection.UpAndDown) || (target == JointConnectionDirection.Up)) || (target == JointConnectionDirection.Down)))
                {
                    return GridRotation.Deg_90;
                }

                return GridRotation.Deg_0;
            default:
                return (GridRotation)((target - source + 4) % 4);
            case JointConnectionDirection.UpAndDown:
                if ((((target == JointConnectionDirection.LeftAndRight) || (target == JointConnectionDirection.Left)) || (target == JointConnectionDirection.Right)))
                {
                    return GridRotation.Deg_90;
                }

                return GridRotation.Deg_0;
        }
    }

    public float GetRotationAngle(GridRotation rotation)
    {
        return ((rotation == GridRotation.Deg_0) ? 0f : (rotation == GridRotation.Deg_90) ? 90f : (rotation == GridRotation.Deg_180) ? 180f : (rotation == GridRotation.Deg_270) ? 270f : (rotation == GridRotation.Deg_45) ? 45f : (rotation == GridRotation.Deg_135) ? 135f : (rotation == GridRotation.Deg_225) ? 225f : (rotation == GridRotation.Deg_315) ? 315f : 0f);
    }

    public virtual void SetRotation(GridRotation rotation)
    {
        m_gridRotation = rotation;
        base.transform.localRotation = Quaternion.AngleAxis(GetRotationAngle(rotation), Vector3.forward);
    }

    public void RotateClockwise()
    {
        switch (m_gridRotation)
        {
            case GridRotation.Deg_0:
                if (!m_eightWay)
                {
                    SetRotation(GridRotation.Deg_270);
                }
                else
                {
                    SetRotation(GridRotation.Deg_315);
                }

                break;
            case GridRotation.Deg_90:
                if (!m_eightWay)
                {
                    SetRotation(GridRotation.Deg_0);
                }
                else
                {
                    SetRotation(GridRotation.Deg_45);
                }

                break;
            case GridRotation.Deg_180:
                if (!m_eightWay)
                {
                    SetRotation(GridRotation.Deg_90);
                }
                else
                {
                    SetRotation(GridRotation.Deg_135);
                }

                break;
            case GridRotation.Deg_270:
                if (!m_eightWay)
                {
                    SetRotation(GridRotation.Deg_180);
                }
                else
                {
                    SetRotation(GridRotation.Deg_225);
                }

                break;
            case GridRotation.Deg_45:
                SetRotation(GridRotation.Deg_0);
                break;
            case GridRotation.Deg_135:
                SetRotation(GridRotation.Deg_90);
                break;
            case GridRotation.Deg_225:
                SetRotation(GridRotation.Deg_180);
                break;
            case GridRotation.Deg_315:
                SetRotation(GridRotation.Deg_270);
                break;
        }
    }

    public virtual bool IsInInteractiveRadius(Vector3 position)
    {
        Vector3 a = base.transform.position - base.rigidbody.velocity * Time.deltaTime;
        a.z = 0f;
        return Vector3.Distance(a, position) <= m_interactiveRadius;
    }

    public void ProcessTouch()
    {
        if ((!WPFMonoBehaviour.levelManager || WPFMonoBehaviour.levelManager.gameState == LevelManager.GameState.Running) && !HasGeneratorRef)
        {
            OnTouch();
            OnTouch(hasPosition: false, Vector3.zero);
        }
    }

    public void ProcessTouch(Vector3 touchPosition)
    {
        if (!WPFMonoBehaviour.levelManager || WPFMonoBehaviour.levelManager.gameState == LevelManager.GameState.Running)
        {
            OnTouch();
            OnTouch(hasPosition: true, touchPosition);
        }
    }

    protected virtual void OnTouch()
    {
    }

    protected virtual void OnTouch(bool hasPosition, Vector3 touchPosition)
    {
    }

    public virtual void OnCollisionEnter(Collision c)
    {
        var target = c.collider.gameObject.GetComponent<BasePart>();
        if (target && target.rigidbody != null)
        {
            target.Hurt((0.1f * (UnityEngine.Mathf.Abs(lateSpeed.x - target.lateSpeed.x) + UnityEngine.Mathf.Abs(lateSpeed.y - target.lateSpeed.y)) * ((UnityEngine.Mathf.Abs(lateSpeed.x - target.lateSpeed.x) + UnityEngine.Mathf.Abs(lateSpeed.y - target.lateSpeed.y)) * rigidbody.mass / 2)));
        }

        //from homewool^^}
        Collider collider = c.collider;
        GameObject gameObject = collider.gameObject;
        int layer = gameObject.layer;
        BasePart component = collider.GetComponent<BasePart>();
        if (layer == m_groundLayer || gameObject.CompareTag("Ground"))
        {
            if (contraption.IsConnectedToPig(this, base.collider))
            {
                m_lastTimeTouchedGround = Time.time;
                contraption.SetTouchingGround(touching: true);
            }

            m_isOnGround = true;
        }

        if ((bool)component && component.ConnectedComponent == ConnectedComponent)
        {
            return;
        }

        m_isOnIce = gameObject.CompareTag("IceSurface");
        PlayCollisionAudio(this, c);
        if (!(Time.time - m_lastTimeUsedCollisionParticles > 0.25f))
        {
            return;
        }

        int num = c.contacts.Length;
        for (int i = 0; i < num; i++)
        {
            ContactPoint contactPoint = c.contacts[i];
            if (contactPoint.otherCollider.CompareTag("Untagged") && (bool)base.rigidbody)
            {
                m_lastTimeUsedCollisionParticles = Time.time;
                WPFMonoBehaviour.effectManager.CreateParticles(WPFMonoBehaviour.gameData.m_dustParticles, base.transform.position - Vector3.forward);
                float num2 = Vector3.Dot(base.rigidbody.GetPointVelocity(contactPoint.point), contactPoint.normal);
                if (m_breakVelocity > 0f && num2 > m_breakVelocity && !m_broken)
                {
                    OnBreak();
                    m_broken = true;
                }

                break;
            }
        }
    }

    public virtual void OnCollisionStay(Collision c)
    {
        int layer = c.collider.gameObject.layer;
        if (layer == m_groundLayer || layer == m_iceGroundLayer)
        {
            m_lastTimeTouchedGround = Time.time;
            contraption.SetGroundTouchTime(this);
        }
    }

    public virtual void OnCollisionExit(Collision c)
    {
        GameObject _gameObject = c.gameObject;
        int layer = _gameObject.layer;
        if (layer == m_iceGroundLayer)
        {
            m_isOnIce = false;
        }

        if (layer == m_groundLayer || _gameObject.CompareTag("Ground"))
        {
            m_isOnGround = false;
        }
    }

    protected void LateUpdate()
    {
        if ((bool)rigidbody)
        {
            lateSpeed = rigidbody.velocity;
        }

        UpdateSoundEffect();
    }

    public void PlayCollisionAudio(BasePart collisionPart, Collision collisionData)
    {
        if (collisionData.relativeVelocity.magnitude < 2.5f)
        {
            return;
        }

        AudioSource[] array = null;
        AudioSource[] array2 = null;
        switch (collisionPart.AudioMaterial)
        {
            default:
                array = null;
                array2 = null;
                break;
            case AudioManager.AudioMaterial.Wood:
                array = WPFMonoBehaviour.gameData.commonAudioCollection.collisionWoodHit;
                array2 = WPFMonoBehaviour.gameData.commonAudioCollection.collisionWoodDamage;
                break;
            case AudioManager.AudioMaterial.Metal:
                array = WPFMonoBehaviour.gameData.commonAudioCollection.collisionMetalHit;
                array2 = WPFMonoBehaviour.gameData.commonAudioCollection.collisionMetalDamage;
                break;
        }

        float num = 1f;
        if (this is GoldenPig)
        {
            array = WPFMonoBehaviour.gameData.commonAudioCollection.goldenPigHit;
            array2 = array;
            num = 1.5f;
        }

        if (array == null || array2 == null)
        {
            return;
        }

        float num2 = 0f;
        Vector3 soundPosition = Vector3.zero;
        foreach (ContactPoint collisionDatum in collisionData)
        {
            float num3 = Vector3.Dot(collisionData.relativeVelocity, collisionDatum.normal);
            if (num3 > num2)
            {
                num2 = num3 * num;
                soundPosition = collisionDatum.point;
            }
        }

        if (num2 > 8f)
        {
            Singleton<AudioManager>.Instance.SpawnOneShotEffect(array2, soundPosition);
        }
        else if (num2 > 2.5f)
        {
            AudioSource audioSource = Singleton<AudioManager>.Instance.SpawnOneShotEffect(array, soundPosition);
            if ((bool)audioSource)
            {
                audioSource.volume = (num2 - 2f) / 8f;
            }
        }
    }

    public void PlayBreakAudio(BasePart breakingPart)
    {
        AudioSource[] array = ((breakingPart.AudioMaterial == AudioManager.AudioMaterial.Wood) ? WPFMonoBehaviour.gameData.commonAudioCollection.collisionWoodDestroy : (breakingPart.AudioMaterial == AudioManager.AudioMaterial.Metal) ? WPFMonoBehaviour.gameData.commonAudioCollection.collisionMetalBreak : null);
        if (array != null)
        {
            Singleton<AudioManager>.Instance.SpawnOneShotEffect(array, breakingPart.transform.position);
        }
    }

    public virtual void PostInitialize()
    {
        AddShineEffect();
    }

    [Obsolete("Obsolete")]
    public virtual void PrePlaced()
    {
        if (!INSettings.GetBool(INFeature.DisableAlienPartParticles))
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == "Alien_part")
                {
                    GameObject obj = UnityEngine.Object.Instantiate(WPFMonoBehaviour.gameData.m_alienPartParticles, base.transform, true);
                    obj.transform.localPosition = Vector3.back * 0.1f + new Vector3(offsetX, offsetY);
                    obj.transform.localRotation = Quaternion.identity;
                    obj.GetComponent<ParticleSystem>().startDelay = UnityEngine.Random.Range(0f, 2f);
                }
            }
        }

        if (WPFMonoBehaviour.levelManager != null && WPFMonoBehaviour.levelManager.CurrentGameMode is CakeRaceMode && Singleton<CakeRaceKingsFavorite>.Instance.CurrentFavorite.m_partType == m_partType && Singleton<CakeRaceKingsFavorite>.Instance.CurrentFavorite.m_partTier == m_partTier)
        {
            GameObject obj2 = UnityEngine.Object.Instantiate(WPFMonoBehaviour.gameData.m_heartParticles, base.transform, true);
            obj2.transform.localPosition = Vector3.back + new Vector3(offsetX, offsetY);
            obj2.transform.localRotation = Quaternion.identity;
        }
    }

    private void AddShineEffect()
    {
        if (GameRules.PerformanceMode)
        {
            return;
        }

        if (WPFMonoBehaviour.levelManager != null && HasTag("Gold"))
        {
            SpriteShineEffect.AddOneTimeShine(base.gameObject);
        }
    }

    public static Vector3 GetDirectionVector(Direction direction)
    {
        return ((direction == Direction.Right) ? Vector3.right : (direction == Direction.Up) ? Vector3.up : (direction == Direction.Left) ? -Vector3.right : (direction == Direction.Down) ? -Vector3.up : Vector3.up);
    }

    public virtual bool IsIntegralPart()
    {
        return true;
    }

    public virtual bool CanEncloseParts()
    {
        return false;
    }

    public virtual void MoveOnGrid(BasePart part, int dx, int dy)
    {
        part.contraption.SetPartPos(part.m_coordX + dx, part.m_coordY + dy, part);
    }

    public virtual bool CanBeEnclosed()
    {
        if (INSettings.GetBool(INFeature.EnclosableParts))
        {
            if (INSettings.GetBool(INFeature.WoodenBox) && m_partType == PartType.WoodenFrame && customPartIndex == 10)
            {
                return true;
            }

            if (INSettings.GetBool(INFeature.MetalBox) && m_partType == PartType.MetalFrame && customPartIndex == 130)
            {
                return true;
            }

            if (m_partType != PartType.WoodenFrame)
            {
                return m_partType != PartType.MetalFrame;
            }

            return false;
        }

        return false;
    }

    public virtual bool IsPartOfChassis()
    {
        return false;
    }

    public virtual JointConnectionType GetJointConnectionType()
    {
        return m_jointConnectionType;
    }

    public virtual bool ValidatePart()
    {
        return true;
    }

    /// <summary>
    /// Add and Ensure RigidBody to this BasePart
    /// </summary>
    public virtual void EnsureRigidbody()
    {
        if (base.rigidbody == null)
        {
            base.rigidbody = base.gameObject.AddComponent<Rigidbody>();
        }

        base.rigidbody.constraints = (RigidbodyConstraints)56;
        base.rigidbody.mass = m_mass;
        base.rigidbody.drag = 0.2f;
        base.rigidbody.angularDrag = 0.05f;
        base.rigidbody.useGravity = true;
        base.rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        base.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        if (base.gameObject.layer == LayerMask.NameToLayer("Default") || base.gameObject.layer == LayerMask.NameToLayer("Contraption"))
        {
            base.gameObject.layer = LayerMask.NameToLayer("Contraption");
            for (int i = 0; i < base.transform.childCount; i++)
            {
                base.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("Contraption");
            }
        }

        if (enclosedPart is ExplodingGrapplingHook)
        {
            base.rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
    }

    public virtual Joint CustomConnectToPart(BasePart part)
    {
        return null;
    }

    public virtual void OnChangeConnections()
    {
        if (m_jointConnectionDirection != 0 && m_jointConnectionDirection != JointConnectionDirection.None && !contraption.CanConnectTo(this, GetJointConnectionDirection()))
        {
            contraption.AutoAlign(this);
        }

        ChangeVisualConnections();
    }

    public virtual void ChangeVisualConnections()
    {
    }

    public void OnEnclosedPartDetached()
    {
    }

    public virtual void OnDetach()
    {
    }

    protected virtual void OnJointBreak(float breakForce)
    {
        HandleJointBreak();
    }

    protected virtual void OnFlipped()
    {
    }

    public virtual void OnPartPlaced()
    {
    }

    public void HandleJointBreak(bool playEffects = true)
    {
        CheckForBrokenPartsAchievement();
        contraption.UpdateConnectedComponents();
        if (playEffects)
        {
            PlayBreakAudio(this);
            Vector3 position = base.transform.position;
            Vector3 normalized = base.rigidbody.velocity.normalized;
            GameObject sprite = ((m_partType != PartType.WoodenFrame && m_partType != PartType.Pig) ? WPFMonoBehaviour.gameData.m_snapSprite : WPFMonoBehaviour.gameData.m_krakSprite);
            WPFMonoBehaviour.effectManager.ShowBreakEffect(sprite, position - 2f * normalized + new Vector3(0f, 0f, -10f), Quaternion.AngleAxis(UnityEngine.Random.Range(-30f, 30f), Vector3.forward));
        }
    }

    public virtual void OnBreak()
    {
    }

    protected virtual void UpdateSoundEffect()
    {
        if (!contraption || !contraption.IsRunning)
        {
            return;
        }

        float num = Mathf.Abs(base.rigidbody.velocity.magnitude);
        float num2 = 1f;
        float num3 = 10f;
        if (m_isOnIce && num > num2)
        {
            if (!m_slidingSound)
            {
                SpawnSlidingSound();
            }

            if ((bool)m_slidingSound)
            {
                m_slidingSound.volume = 0.25f * (Mathf.Clamp(num, num2, num3) / num3);
            }
        }
        else if ((bool)m_slidingSound)
        {
            if (Time.time - m_lastTimeTouchedGround < 0.4f)
            {
                m_slidingSound.volume = 0.25f * Mathf.Clamp01(1f - (Time.time - m_lastTimeTouchedGround) / 0.3f);
            }
            else
            {
                m_slidingSound.volume = 0f;
            }
        }
    }

    private void SpawnSlidingSound()
    {
        if ((bool)m_slidingSound)
        {
            m_slidingSound.Stop();
        }

        m_slidingSound = Singleton<AudioManager>.Instance.SpawnOneShotEffect(WPFMonoBehaviour.gameData.commonAudioCollection.partSlideOnIceLoop, base.transform);
        if ((bool)m_slidingSound)
        {
            m_slidingSound.volume = 0f;
        }
    }

    public void CheckForBrokenPartsAchievement()
    {
        if (GameRules.PerformanceMode)
        {
            return;
        }

        if (!Singleton<SocialGameManager>.IsInstantiated() || !Singleton<GameManager>.Instance.IsInGame())
        {
            return;
        }

        int brokenParts = GameProgress.GetInt("Broken_Parts") + 1;
        GameProgress.SetInt("Broken_Parts", brokenParts);
        ((Action<List<string>>)delegate (List<string> achievements)
        {
            foreach (string achievement in achievements)
            {
                if (Singleton<SocialGameManager>.Instance.TryReportAchievementProgress(achievement, 100.0, (int limit) => brokenParts >= limit))
                {
                    break;
                }
            }
        })(new List<string> { "grp.VETERAN_WRECKER", "grp.QUALIFIED_WRECKER", "grp.JUNIOR_WRECKER" });
    }

    public string GetAnalyticsName()
    {
        return base.name;
    }

    public virtual IEnumerable<Rigidbody> GetRigidbodies()
    {
        yield return base.rigidbody;
    }

    public virtual IEnumerable<Collider> GetColliders()
    {
        yield return base.collider;
    }

    public virtual IEnumerable<Renderer> GetRenderers()
    {
        yield return base.renderer;
    }

    public virtual bool IsCustomRotated()
    {
        return m_autoAlign == (AutoAlignType)(-1);
    }

    public virtual int GetRotation()
    {
        return (int)m_gridRotation;
    }

    public virtual void SetRotation(int rotation)
    {
    }

    public virtual void CreateCustomJoints()
    {
    }

    public virtual void OnLightEnter(EntityLightCollision collision)
    {
    }

    public virtual bool IsTriggerable()
    {
        if (!HasGeneratorRef)
        {
            if (!CanBeEnabled())
            {
                return contraption.HasOneShotParts(m_partType, EffectDirection());
            }

            return true;
        }

        return false;
    }

    public virtual IEnumerable<UIPartTriggerButtonInfo> GetTriggerButtonInfo()
    {
        yield return new UIPartTriggerButtonInfo(UIPartButtonType.Trigger, 0, m_partType, (int)EffectDirection(), ConnectedComponent, HasOnOffToggle());
    }

    public virtual IEnumerable<UIPartSliderButtonInfo> GetSliderButtonInfo()
    {
        yield break;
    }

    public virtual void OnButtonTriggered(UIPartTriggerButton button)
    {
        ProcessTouch();
    }

    public virtual void OnSliderButtonTriggered(UIPartSliderButton button)
    {
    }

    public static Direction Rotate4(Direction direction, int times)
    {
        return (Direction)((int)(direction + times) % 4);
    }

    public static Direction Rotate8(Direction direction, int times)
    {
        int num = (int)direction;
        num = ((num < 4) ? (num * 2) : ((num - 4) * 2 + 1));
        num = (num + times) % 8;
        return (Direction)((num % 2 == 0) ? (num / 2) : (num / 2 + 4));
    }

    public static int GetIntegralAngle(GridRotation rotation)
    {
        return ((rotation == GridRotation.Deg_0) ? 0 : (rotation == GridRotation.Deg_45) ? 45 : (rotation == GridRotation.Deg_90) ? 90 : (rotation == GridRotation.Deg_135) ? 135 : (rotation == GridRotation.Deg_180) ? 180 : (rotation == GridRotation.Deg_225) ? 225 : (rotation == GridRotation.Deg_270) ? 270 : (rotation == GridRotation.Deg_315) ? 315 : (rotation == GridRotation.Deg_Max) ? 360 : throw new ArgumentException("rotation"));
    }

    public static (int, int) GetDirection(GridRotation rotation)
    {
        return ((rotation == GridRotation.Deg_0) ? (1, 0) : (rotation == GridRotation.Deg_45) ? (1, 1) : (rotation == GridRotation.Deg_90) ? (0, 1) : (rotation == GridRotation.Deg_135) ? (-1, 1) : (rotation == GridRotation.Deg_180) ? (-1, 0) : (rotation == GridRotation.Deg_225) ? (-1, -1) : (rotation == GridRotation.Deg_270) ? (0, -1) : (rotation == GridRotation.Deg_315) ? (1, -1) : (rotation == GridRotation.Deg_Max) ? (1, 0) : throw new ArgumentException("rotation"));
    }

    public static void GetDirection(GridRotation rotation, out int x, out int y)
    {
        (x, y) = GetDirection(rotation);
    }
}