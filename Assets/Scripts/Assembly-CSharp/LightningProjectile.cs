using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightningProjectile : WPFMonoBehaviour
{
    public Action OnExplosion;
    [SerializeField]
    private float m_explosionImpulse;
    [SerializeField]
    private float m_explosionRadius;
    [SerializeField]
    private float m_force;
    [SerializeField]
    private float m_ttl;
    [SerializeField]
    private GameObject m_smokeCloud;
    private LightningProjectile m_projectilePrefab;
    private bool m_triggered;
    private Renderer m_renderer;
    private Vector3 m_forceDirection;
    private int m_explosionCount;
    public int m_remainingIterations;
    private float m_sleepTime;
    private void Start()
    {
        m_triggered = false;
        m_explosionCount = 1;
        m_remainingIterations = 5;
        m_sleepTime = 0;
        m_projectilePrefab = this;
        m_renderer = GetComponentInChildren<Renderer>();
        EventManager.Connect<UIEvent>(OnUIEvent);
        m_forceDirection = base.transform.parent.TransformDirection(Vector3.right);
        base.rigidbody.AddForceAtPosition(m_forceDirection * m_force * INSettings.GetFloat(INFeature.GunProjectileSpeed), Vector3.zero, ForceMode.Impulse);
        StartCoroutine(TTL(m_ttl * INSettings.GetFloat(INFeature.GunProjectileExplosionTime)));
    }

    private void OnDestroy()
    {
        EventManager.Disconnect<UIEvent>(OnUIEvent);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (m_remainingIterations <= 0)
        {
            UnityEngine.GameObject.Destroy(this);
            EventManager.Disconnect<UIEvent>(OnUIEvent);
            return;
        }
        else
        {
            if (m_remainingIterations > 0)
            {
                SummonCopy(0, m_force, m_explosionImpulse, m_explosionRadius / 2f, m_ttl / 2f);
            }

            m_remainingIterations = 0;
            //Thread.Sleep((int)m_sleepTime);
            Explode();
            UnityEngine.GameObject.Destroy(this);
            EventManager.Disconnect<UIEvent>(OnUIEvent);
        }

        UnityEngine.GameObject.Destroy(this);
        EventManager.Disconnect<UIEvent>(OnUIEvent);
    }

    public void Explode()
    {
        if (m_triggered)
        {
            return;
        }

        m_explosionCount--;
        if (m_explosionCount <= 0)
        {
            m_triggered = true;
        }

        Collider[] array = Physics.OverlapSphere(base.transform.position, m_explosionRadius * INSettings.GetFloat(INFeature.GunProjectileExplosionRadius));
        foreach (Collider collider in array)
        {
            GameObject gameObject = FindParentWithRigidBody(collider.gameObject);
            if (gameObject != null)
            {
                int num = CountChildColliders(gameObject, 0);
                AddExplosionForce(gameObject, INSettings.GetFloat(INFeature.GunProjectileExplosionForce) / (float)num);
            }

            TNT component = collider.GetComponent<TNT>();
            if ((bool)component && !component.HasGeneratorRef)
            {
                component.Explode();
            }

            BasePart basePart = collider.GetComponent<BasePart>();
            if ((bool)basePart)
            {
                basePart.Hurt(this.m_explosionImpulse * 1.5714f / (Vector3.Distance(this.transform.position, basePart.transform.position) * Vector3.Distance(this.transform.position, basePart.transform.position) + 0.04f) + 5f);
            }
        }

        WPFMonoBehaviour.effectManager.CreateParticles(m_smokeCloud, base.transform.position - Vector3.forward * 12f, force: true);
        Singleton<AudioManager>.Instance.SpawnOneShotEffect(WPFMonoBehaviour.gameData.commonAudioCollection.tntExplosion, base.transform.position);
        //StartCoroutine(ShineLight());
        if (OnExplosion != null)
        {
            OnExplosion();
            OnExplosion = null;
        }
    }

    private int CountChildColliders(GameObject obj, int count)
    {
        if ((bool)obj.GetComponent<Collider>())
        {
            count++;
        }

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            count = CountChildColliders(obj.transform.GetChild(i).gameObject, count);
        }

        return count;
    }

    private GameObject FindParentWithRigidBody(GameObject obj)
    {
        if ((bool)obj.GetComponent<Rigidbody>())
        {
            return obj;
        }

        if ((bool)obj.transform.parent)
        {
            return FindParentWithRigidBody(obj.transform.parent.gameObject);
        }

        return null;
    }

    private void SummonCopy(int rem, float force, float impulse, float radius, float ttl)
    {
        LightningProjectile currentProjectile;
        Singleton<AudioManager>.Instance.SpawnOneShotEffect(WPFMonoBehaviour.gameData.commonAudioCollection.alienLaserFire, base.transform);
        currentProjectile = UnityEngine.Object.Instantiate(this).GetComponent<LightningProjectile>();
        currentProjectile.rigidbody.velocity = this.rigidbody.velocity * Random.Range(0.8f, 1.2f);
        currentProjectile.transform.parent = base.transform;
        currentProjectile.transform.localPosition = Vector3.forward * 0.1f + Random.insideUnitSphere;
        currentProjectile.transform.rotation = base.transform.rotation;
        currentProjectile.m_remainingIterations = rem;
        currentProjectile.m_force = force;
        currentProjectile.m_explosionImpulse = impulse;
        currentProjectile.m_explosionRadius = radius;
        currentProjectile.m_ttl = ttl;
        currentProjectile.m_sleepTime = m_sleepTime;
        if (INSettings.GetBool(INFeature.InertialGunProjectile))
        {
            Vector3 right = base.transform.right;
            currentProjectile.transform.position = base.rigidbody.position + right + Random.insideUnitSphere;
            currentProjectile.rigidbody.position = base.rigidbody.position + right;
        }

        currentProjectile.rigidbody.drag = INSettings.GetFloat(INFeature.GunProjectileDrag);
        Physics.IgnoreCollision(currentProjectile.GetComponentInChildren<Collider>(), this.GetComponentInChildren<Collider>());
        currentProjectile.OnExplosion = (Action)Delegate.Combine(currentProjectile.OnExplosion, (Action)delegate
        {
        });
        currentProjectile.transform.localPosition += currentProjectile.transform.position.normalized;
    //currentProjectile.Explode();
    }

    private void AddExplosionForce(GameObject target, float forceFactor)
    {
        if (target is LightningProjectile)
            return;
        Vector3 vector = target.transform.position - base.transform.position;
        float f = Mathf.Max(vector.magnitude, 1f);
        float num = forceFactor * m_explosionImpulse / Mathf.Pow(f, 1.5f);
        Rigidbody component = target.GetComponent<Rigidbody>();
        if (component.mass < 0.1f)
        {
            num *= component.mass;
        }
        else if (component.mass < 0.4f)
        {
            num *= component.mass / 0.4f;
        }

        component.AddForce(num * vector.normalized, ForceMode.Impulse);
    }

    private IEnumerator ShineLight()
    {
        PointLightSource pls = GetComponentInChildren<PointLightSource>();
        if ((bool)pls)
        {
            if (m_renderer != null)
            {
                m_renderer.enabled = false;
            }

            pls.onLightTurnOff = (Action)Delegate.Combine(pls.onLightTurnOff, (Action)delegate
            {
                base.gameObject.SetActive(value: false);
            });
            pls.isEnabled = true;
            yield return new WaitForSeconds(pls.turnOnCurve[pls.turnOnCurve.length - 1].time);
            pls.isEnabled = false;
        }

        if (m_explosionCount <= 0)
        {
            UnityEngine.Object.Destroy(base.gameObject);
        }
    }

    private IEnumerator TTL(float ttl)
    {
        float current = 0f;
        float coolingTime = INSettings.GetFloat(INFeature.GunProjectileCoolingTime);
        while (current < ttl)
        {
            if (OnExplosion != null && current >= coolingTime)
            {
                OnExplosion();
                OnExplosion = null;
            }

            current += Time.deltaTime;
            yield return null;
        }

        while (!m_triggered)
        {
            Explode();
        }
    }

    private void OnUIEvent(UIEvent data)
    {
        if (data.type == UIEvent.Type.Building)
        {
            UnityEngine.Object.Destroy(base.gameObject);
        }
    }
}