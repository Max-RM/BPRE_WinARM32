using UnityEngine;

public class LightTrigger : MonoBehaviour
{
    private Transform cachedTransform;
    public PointLightSource LightSource { get; private set; }

    public void Init(PointLightSource pls)
    {
        this.LightSource = pls;
    }

    private void Start()
    {
        cachedTransform = base.transform;
    }

    private void Update()
    {
        cachedTransform.position += Vector3.zero;
    }

    private void OnTriggerEnter(Collider c)
    {
        if (!(LightSource == null) && LightSource.lightType == PointLightMask.LightType.PointLight && LightSource.canLitObjects && LightSource.isEnabled)
        {
            c.SendMessageUpwards("Lit", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void OnTriggerStay(Collider c)
    {
        if (LightSource == null)
        {
            return;
        }

        if (LightSource.lightType == PointLightMask.LightType.PointLight && LightSource.canLitObjects && LightSource.isEnabled)
        {
            c.SendMessage("Lit", SendMessageOptions.DontRequireReceiver);
        }
        else if (LightSource.lightType == PointLightMask.LightType.BeamLight && LightSource.canLitObjects && LightSource.isEnabled)
        {
            float beamAngle = LightSource.beamAngle;
            Vector3 vector = Vector3.up * c.transform.position.y + Vector3.right * c.transform.position.x;
            Vector3 vector2 = Vector3.up * base.transform.position.y + Vector3.right * base.transform.position.x;
            float num = Vector3.Angle(vector - vector2, LightSource.transform.up);
            if (Vector3.Distance(vector, vector2) <= LightSource.baseLightSize + LightSource.borderWidth || num < beamAngle * 0.5f)
            {
                c.SendMessageUpwards("Lit", SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public void Lit()
    {
        if (LightSource != null && !LightSource.isEnabled && LightSource.canBeLit)
        {
            LightSource.isEnabled = true;
        }
    }
}