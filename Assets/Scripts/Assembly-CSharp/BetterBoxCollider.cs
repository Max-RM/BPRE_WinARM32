//#define USEBETTERCOLLIDER
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;

public class BetterBoxCollider : MonoBehaviour
{
    [SerializeField]
    public BoxCollider BoxCollider;
    [SerializeField]
    public MovableLight Parent;
    public ForceMode ForceMode;
    private float HalfWidth;
    public float ForceCoefficient;
    private float time;
    private Vector3 DiagonalVector;
    // Start is called before the first frame update
    private void Start()
    {
        BoxCollider.isTrigger = false;
        HalfWidth = BoxCollider.size.y * 0.5f;
    }

    private void Update()
    {
        Vector3 size = BoxCollider.size;
        DiagonalVector = new Vector3(size.x * 0.5f, size.y * 0.5f, 0);
    }

    private void OnCollisionEnter(Collision collision)
    {
    }

#if USEBETTERCOLLIDER
    

    [BurstCompile(CompileSynchronously = true)]
    private void OnCollisionStay(Collision collisionInfo)
    {
        if (Time.time - time < 0.02f) return;
        time = Time.time;
        Rigidbody hitRigidbody = collisionInfo.collider.attachedRigidbody;
        ContactPoint[] contacts = new ContactPoint[10];//this hardcoded number trolled me
        int count = collisionInfo.GetContacts(contacts);
        Vector3 average = Vector3.zero;
        foreach (ContactPoint contact in contacts)
        {
            average += contact.point;
        }
        average /= count;
        average.z = 0;
        average = transform.InverseTransformDirection(average);
        if (Mathf.Abs(average.y) <= HalfWidth)
        {
            average.y = 0;
        }
        else
        {
            average.x = 0;
        }

        Vector3 force = -(average.normalized * ForceCoefficient);
        force.z = 0;
        Debug.Log("Added Force\n" +
                  force);
        if (hitRigidbody)
        {
            force *= (hitRigidbody.velocity.magnitude+1f);
            hitRigidbody.AddForce(force,ForceMode);
            Parent.rigidbody.AddForce(-force,ForceMode);
        }
        else
        {
            Parent.rigidbody.AddForce(-force,ForceMode);
        }
    }
#endif
    private bool IsInside(Vector3 worldPosition)
    {
        Vector3 a = transform.InverseTransformDirection(worldPosition);
        return a.sqrMagnitude < DiagonalVector.sqrMagnitude;
    }

    private bool IsInsideLocal(Vector3 localPosition)
    {
        return localPosition.sqrMagnitude < DiagonalVector.sqrMagnitude;
    }
}