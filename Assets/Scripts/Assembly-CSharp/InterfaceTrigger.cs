using System;
using UnityEngine;

public class InterfaceTrigger : MonoBehaviour
{
    private InterfacePart m_part;
    private void Awake()
    {
        m_part = base.transform.parent.GetComponent<InterfacePart>();
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleTrigger(other);
    }

    public void OnTriggerStay(Collider other)
    {
        HandleTrigger(other);
    }

    private void HandleTrigger(Collider other)
    {
        if (other.name != "InterfaceTrigger")
        {
            return;
        }

        InterfacePart part = m_part;
        InterfacePart component = other.transform.parent.GetComponent<InterfacePart>();
        Vector3 vector = part.rigidbody.position - part.rigidbody.velocity * Time.fixedDeltaTime;
        Vector3 vector2 = component.rigidbody.position - component.rigidbody.velocity * Time.fixedDeltaTime;
        float num = vector2.x - vector.x;
        float num2 = vector2.y - vector.y;
        if (num * num + num2 * num2 > 2f)
        {
            return;
        }

        Vector3 right = part.transform.right;
        Vector3 right2 = component.transform.right;
        Vector.InvTransform(right.x, right.y, right2.x, right2.y, out var resultX, out var resultY);
        float num3 = 0.1f;
        if (!(Math.Abs(resultX) > num3) || !(Math.Abs(resultY) > num3))
        {
            Vector.InvTransform(num, num2, right.x, right.y, out var resultX2, out var resultY2);
            Vector.InvTransform(0f - num, 0f - num2, right2.x, right2.y, out var resultX3, out var resultY3);
            float num4 = Math.Abs(resultX2);
            float num5 = Math.Abs(resultY2);
            float num6 = Math.Abs(resultX3);
            float num7 = Math.Abs(resultY3);
            if (((num4 < 1.1f && num5 < 0.9f) || (num4 < 0.9f && num5 < 1.1f)) && ((num6 < 1.1f && num7 < 0.9f) || (num6 < 0.9f && num7 < 1.1f)))
            {
                BitDirection direction = ((!(resultY2 > 0f - resultX2)) ? ((resultY2 > resultX2) ? BitDirection.Left : BitDirection.Down) : ((resultY2 < resultX2) ? BitDirection.Right : BitDirection.Up));
                BitDirection direction2 = ((!(resultY3 > 0f - resultX3)) ? ((resultY3 > resultX3) ? BitDirection.Left : BitDirection.Down) : ((resultY3 < resultX3) ? BitDirection.Right : BitDirection.Up));
                part.AddConnection(component.Wire, direction);
                component.AddConnection(part.Wire, direction2);
            }
        }
    }
}