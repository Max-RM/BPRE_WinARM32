using System;

public class INBehaviour
{
    [Flags]
    public enum StatusCode
    {
        None = 0,
        Building = 1,
        Running = 2
    }

    protected StatusCode m_status;
    public StatusCode Status => m_status;

    public virtual void Awake()
    {
    }

    public virtual void Start()
    {
    }

    public virtual void FixedUpdate()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void LateUpdate()
    {
    }

    public virtual void OnEnable()
    {
    }

    public virtual void OnDisable()
    {
    }

    public virtual void OnDestroy()
    {
    }
}