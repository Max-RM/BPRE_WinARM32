using UnityEngine;

public class MovableLight : BasePart
{
    public float m_maxLength;
    [Range(0.0001f, 1)]
    public float m_rateOfGrowth;
    public bool activated;
    private float m_OutLength;
    public float m_deltaX;
    private GameObject lightHandle;
    private BoxCollider childCollider;
    private Rigidbody childRigidbody;
    private ConfigurableJoint configurableJoint;
    public void Start()
    {
        lightHandle = base.transform.Find("Handle").gameObject;
        childCollider = lightHandle.GetComponent<BoxCollider>();
        childRigidbody = lightHandle.GetComponent<Rigidbody>();
        childRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        childRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Physics.IgnoreCollision(base.collider, childCollider);
        if ((bool)base.enclosedInto)
        {
            Physics.IgnoreCollision(base.collider, base.enclosedInto.collider);
            Physics.IgnoreCollision(childCollider, base.enclosedInto.collider);
        }

        childCollider.size = new Vector3(1f, 0.5f, 1f);
        configurableJoint = gameObject.GetComponent<ConfigurableJoint>();
    }

    public override void EnsureRigidbody()
    {
        base.EnsureRigidbody();
    //configurableJoint.connectedBody = lightHandle.gameObject.GetComponent<Rigidbody>();
    //configurableJoint.linearLimitSpring = new SoftJointLimitSpring { spring = 0 };
    }

    public override bool IsTriggerable()
    {
        return true;
    }

    public override bool IsEnabled()
    {
        return activated;
    }

    public override bool HasOnOffToggle()
    {
        return true;
    }

    public override bool CanBeEnclosed()
    {
        return true;
    }

    protected override void OnTouch()
    {
        Singleton<AudioManager>.Instance.SpawnOneShotEffect(WPFMonoBehaviour.gameData.commonAudioCollection.toggleLamp, base.transform.position);
        SetEnabled(!activated);
    }

    public void Update()
    {
        if (base.HasGeneratorRef || GameTime.IsPaused())
            return;
        if ((bool)base.enclosedInto)
        {
            base.collider.enabled = false;
        }
        else
        {
            base.collider.enabled = true;
        }

        childCollider.transform.localScale = new Vector3(m_OutLength, 1f, 1f);
        //configurableJoint.targetPosition = new Vector3(m_OutLength / 2f + 0.5f + m_deltaX, 0f, 0.05f);
        childCollider.transform.localPosition = new Vector3(m_OutLength / 2f + 0.5f + m_deltaX, 0f, 0.05f);
        lightHandle.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
        if (activated)
        {
            m_OutLength = ((m_OutLength >= m_maxLength) ? m_maxLength : (m_OutLength + m_rateOfGrowth));
        }
        else if (!activated)
        {
            m_OutLength = ((m_OutLength <= 0f) ? 0f : (m_OutLength - m_rateOfGrowth));
        }
    }

    // public void Update()
    // {
    // 	childCollider.transform.localScale = new Vector3(m_OutLength, 1f, 1f);
    // 	childCollider.transform.localPosition = new Vector3(m_OutLength / 2f + 0.5f + m_deltaX, 0f, 0.05f);
    // 	lightHandle.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
    // }
    public override void SetEnabled(bool enabled)
    {
        activated = enabled;
    }
}