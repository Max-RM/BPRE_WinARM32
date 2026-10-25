using System;
using UnityEngine;
using UnityEngine.Serialization;

public class ForceFieldLight : PointLight
{
    [FormerlySerializedAs("m_ForceCoefficent")]
    public float m_ForceCoefficient;
    public float m_ForceRange;
    public GameObject m_activeVariant;
    public GameObject m_inactiveVariant;
    private Vector3 AddExplosionForce(GameObject target, float forceFactor)
    {
        if ((int)base.m_gridRotation >= 4)
            forceFactor *= -1f;
        Vector3 vector = target.transform.position - base.transform.position;
        float f = Mathf.Max(vector.magnitude, 1f);
        float num = forceFactor * m_ForceCoefficient / Mathf.Pow(f, 1.5f);
        Rigidbody component = target.GetComponent<Rigidbody>();
        if (component.mass < 0.1f)
        {
            num *= component.mass;
        }
        else if (component.mass < 0.4f)
        {
            num *= component.mass / 0.4f;
        }

        Pig component2 = target.GetComponent<Pig>();
        if ((bool)component2)
        {
            component2.PrepareForTNT(base.transform.position, num);
            num *= 1.15f;
        }

        component.AddForce(num * vector.normalized, ForceMode.Impulse);
        return num * vector.normalized;
    }

    protected int CountChildColliders(GameObject obj, int count)
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

    protected GameObject FindParentWithRigidBody(GameObject obj)
    {
        //save your precious call stack
        while (true)
        {
            if ((bool)obj.GetComponent<Rigidbody>())
            {
                return obj;
            }

            if ((bool)obj.transform.parent)
            {
                obj = obj.transform.parent.gameObject;
                continue;
            }

            return null;
            break;
        }
    }

    protected override void OnTouch()
    {
        activated = !activated;
        if ((bool)base.rigidbody)
        {
            base.rigidbody.WakeUp();
        }

        UpdateSprite();
    }

    public override void PrePlaced()
    {
        base.PrePlaced();
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if ((int)base.m_gridRotation >= 4)
        {
            activeSprite.SetActive(false);
            inactiveSprite.SetActive(false);
            m_activeVariant.SetActive(activated);
            m_inactiveVariant.SetActive(!activated);
        }
        else
        {
            m_activeVariant.SetActive(false);
            m_inactiveVariant.SetActive(false);
            activeSprite.SetActive(activated);
            inactiveSprite.SetActive(!activated);
        }
    }

    public void FixedUpdate()
    {
        UpdateSprite();
        if (base.HasGeneratorRef || !activated || GameTime.IsPaused() || m_hp <= 0)
        {
            return;
        }

        if (enclosedInto == null)
        {
            Collider[] array1 = Physics.OverlapSphere(base.transform.position, m_ForceRange);
            foreach (Collider _collider in array1)
            {
                BasePart component = _collider.GetComponent<BasePart>();
                Rigidbody _rigidbody = _collider.GetComponent<Rigidbody>();
                bool basePartCheck = (bool)component && !component.Equals(this) && component.m_hp > 0;
                bool rigidBodyCheck = (bool)_rigidbody && (!_rigidbody.Equals(this.rigidbody));
                if (basePartCheck || rigidBodyCheck)
                {
                    GameObject findParentWithRigidBody = FindParentWithRigidBody(_collider.gameObject);
                    if (findParentWithRigidBody != null)
                    {
                        int num = CountChildColliders(findParentWithRigidBody, 0);
                        Vector3 force = AddExplosionForce(findParentWithRigidBody, m_ForceCoefficient / ((float)num * Vector3.SqrMagnitude(findParentWithRigidBody.transform.position - base.transform.position) + 0.2f));
                        base.rigidbody.AddForce(-force, ForceMode.Impulse);
                    }
                }
            }

            return;
        }

        // I HEREBY SACRIFICE MY SPACE COMPLEXITY TO GET BETTER RESTS
        Collider[] array = Physics.OverlapSphere(base.transform.position, m_ForceRange);
        foreach (Collider _collider in array)
        {
            BasePart component = _collider.GetComponent<BasePart>();
            Rigidbody _rigidbody = _collider.GetComponent<Rigidbody>();
            bool basePartCheck = (bool)component && !component.Equals(this) && !component.Equals(base.enclosedInto) && component.m_hp > 0;
            bool rigidBodyCheck = (bool)_rigidbody && (!_rigidbody.Equals(this.rigidbody) && !_rigidbody.Equals(enclosedInto.rigidbody));
            if (basePartCheck || rigidBodyCheck)
            {
                GameObject findParentWithRigidBody = FindParentWithRigidBody(_collider.gameObject);
                if (findParentWithRigidBody != null)
                {
                    int num = CountChildColliders(findParentWithRigidBody, 0);
                    Vector3 force = AddExplosionForce(findParentWithRigidBody, m_ForceCoefficient / ((float)num * Vector3.SqrMagnitude(findParentWithRigidBody.transform.position - base.transform.position) + 0.2f));
                    base.rigidbody.AddForce(-force, ForceMode.Impulse);
                }
            }
        }
    }
}