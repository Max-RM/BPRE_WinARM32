using UnityEngine;

public static class UnityExtensions
{
    public static Vector2 ToDirection(this Transform transform)
    {
        return transform.rotation.ToDirection();
    }

    public static Vector2 ToDirection(this Quaternion rotation)
    {
        float x = 1f - (rotation.y * rotation.y * 2f + rotation.z * rotation.z * 2f);
        float y = rotation.x * rotation.y * 2f + rotation.w * rotation.z * 2f;
        return new Vector2(x, y);
    }

    public static bool IsFixed(this Rigidbody rigidbody)
    {
        if (!rigidbody.isKinematic)
        {
            return rigidbody.constraints == RigidbodyConstraints.FreezeAll;
        }

        return true;
    }

    public static Joint FindSpecifiedJoint(this Rigidbody self, Rigidbody other)
    {
        Joint[] components = self.GetComponents<Joint>();
        foreach (Joint joint in components)
        {
            if (joint.connectedBody == other)
            {
                return joint;
            }
        }

        return null;
    }
}