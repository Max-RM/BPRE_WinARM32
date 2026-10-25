using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class RailGunProjectile : MonoBehaviour
{
    public float ElectricityMultiplier = 1f;
    private GameObject Point;
    public Vector3 m_Acceleration;
    public Vector3 m_AccelerationMod;
    public double FrequencyFactor;
    public double RadiusFactor;
    public double ExplodeRadius;
    private bool flag = false;
    private TrailRenderer trailRenderer;
    private TrailRenderer childTrail;
    private void Start()
    {
        ExplodeRadius = RadiusFactor * 0.6f * math.pow(ElectricityMultiplier * 0.5f, 0.2f);
        Point = base.transform.Find("Point").gameObject;
        this.trailRenderer = GetComponent<TrailRenderer>();
        this.childTrail = this.Point.GetComponent<TrailRenderer>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (GameTime.IsPaused())
        {
            return;
        }

        base.GetComponent<Rigidbody>().velocity = m_Acceleration;
        CircleMove();
    }

    private void FixedUpdate()
    {
        if (FrequencyFactor > 9d)
        {
            RadiusFactor *= 1.03d;
        }

        if (RadiusFactor >= ExplodeRadius && flag)
        {
            Collider[] colliders = Physics.OverlapSphere(base.transform.position, (float)ExplodeRadius);
            foreach (Collider _collider in colliders)
            {
                Rigidbody e = _collider.GetComponent<Rigidbody>();
                if (!e)
                    continue;
                Vector3 dir = base.transform.position - e.transform.position;
                Vector3 a = (math.log(math.abs(dir) + new float3(0.0185f, 0.0185f, 0.0185f))); //WOW Ln A VECTOR IS SO COOL
                e.AddForce(dir.normalized * (a.magnitude * math.log(this.ElectricityMultiplier + math.E)), ForceMode.Impulse);
                BasePart d = e.GetComponent<BasePart>();
                if (!d)
                    continue;
                d.Hurt(a.magnitude * (float)math.sqrt(FrequencyFactor * 2.718281828));
            }

            transform.localScale *= (float)this.ExplodeRadius;
            this.GetComponent<Collider>().enabled = false;
            Effect();
            //GetComponent<Rigidbody>().velocity = Vector3.zero;
            Invoke(nameof(Destruction), 0.1f);
        }
    }

    public void Effect()
    {
        this.childTrail.enabled = false;
        this.trailRenderer.time = 0.4f;
        this.trailRenderer.widthMultiplier = (float)this.RadiusFactor;
        transform.localScale = new Vector3((float)this.RadiusFactor, (float)this.RadiusFactor, 1);
    }

    public void Destruction()
    {
        RadiusFactor = 0d;
        FrequencyFactor = 0d;
        Destroy(gameObject);
    }

    private void CircleMove()
    {
        Point.transform.localPosition = new Vector3((float)(RadiusFactor * math.cos(Time.time * FrequencyFactor)), (float)(RadiusFactor * math.sin(Time.time * FrequencyFactor)), 0);
    }

    public void LateUpdate()
    {
        if (GameTime.IsPaused())
        {
            return;
        }
    //m_Acceleration += m_AccelerationMod;
    }

    public void OnCollisionEnter(Collision collision)
    {
        flag = true;
        FrequencyFactor = 49d;
    }

    public void OnLightEnter(EntityLightCollision collision)
    {
    }

    private void OnDestroy()
    {
        Destroy(Point);
    }
}