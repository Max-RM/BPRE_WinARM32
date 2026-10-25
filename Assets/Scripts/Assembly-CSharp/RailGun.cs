using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;

public class RailGun : BasePart
{
    private RailGunProjectile currentProjectile;
    private GameObject EnergyCore;
    [SerializeField]
    private GameObject m_projectilePrefab;
    [SerializeField]
    private ParticleSystem m_particleEffect;
    private bool m_enabled;
    private float m_shootTime;
    public float m_rapidCooldownTime = 0.5f;
    protected float CoolingTime { get; set; }
    protected float ForcedCoolingTime { get; set; }

    public override bool CanBeEnabled()
    {
        return true;
    }

    public override bool IsEnabled()
    {
        return m_enabled;
    }

    public override Direction EffectDirection()
    {
        return BasePart.RotateWithEightDirections(Direction.Right, m_gridRotation);
    }

    public override void Awake()
    {
        base.Awake();
        m_eightWay = true;
    }

    public override void ChangeVisualConnections()
    {
    }

    protected override void OnTouch()
    {
        Shoot();
    }

    public float GetVesselElectricity()
    {
        float temp = 0f;
        int count = 1;
        List<BasePart> baseParts = contraption.GetConnectedParts(this);
        foreach (BasePart basePart in baseParts)
        {
            if (basePart is Engine)
            {
                Engine engine = basePart as Engine;
                temp += engine.m_enginePower;
            }
            else if (basePart is RailGun)
            {
                count++;
            }
        }

        return temp / count;
    }

    protected void Shoot()
    {
        float e = GetVesselElectricity();
        EnsureRigidbody();
        float rapidCooldownTime = 0.0f;
        rapidCooldownTime = m_rapidCooldownTime;
        if (!m_enabled && !(Time.time - m_shootTime < rapidCooldownTime))
        {
            m_shootTime = Time.time;
            Singleton<AudioManager>.Instance.SpawnOneShotEffect(WPFMonoBehaviour.gameData.commonAudioCollection.alienLaserFire, base.transform);
            currentProjectile = UnityEngine.Object.Instantiate(m_projectilePrefab).GetComponent<RailGunProjectile>();
            Physics.IgnoreCollision(GetComponent<Collider>(), currentProjectile.GetComponent<Collider>());
            currentProjectile.transform.parent = base.transform;
            currentProjectile.transform.localPosition = base.transform.right * 2;
            currentProjectile.transform.rotation = base.transform.rotation;
            currentProjectile.m_Acceleration = base.transform.right + base.transform.right * (0.5f * math.pow(e, 0.42f));
            currentProjectile.m_AccelerationMod = base.transform.right * 0.01f;
            Vector3 right = base.transform.right;
            currentProjectile.transform.position = base.rigidbody.position + right;
            currentProjectile.ElectricityMultiplier = e;
            //currentProjectile.GetComponent<Rigidbody>().velocity = base.rigidbody.velocity + right * (2f * math.pow( e + math.E, 2/3f));
            this.m_enabled = false;
        }
    }

    public override void EnsureRigidbody()
    {
        base.EnsureRigidbody();
        base.rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
    }
}