using System;
using UnityEngine;

public static class Vector
{
    public static float Length(float valueX, float valueY)
    {
        return UnityEngine.Mathf.Sqrt(valueX * valueX + valueY * valueY);
    }

    public static float Length(Vector2 value)
    {
        float x = value.x;
        float y = value.y;
        return UnityEngine.Mathf.Sqrt(x * x + y * y);
    }

    public static float Length(Vector3 value)
    {
        float x = value.x;
        float y = value.y;
        return UnityEngine.Mathf.Sqrt(x * x + y * y);
    }

    public static float LengthSquared(float valueX, float valueY)
    {
        return valueX * valueX + valueY * valueY;
    }

    public static float LengthSquared(Vector2 value)
    {
        float x = value.x;
        float y = value.y;
        return x * x + y * y;
    }

    public static float LengthSquared(Vector3 value)
    {
        float x = value.x;
        float y = value.y;
        return x * x + y * y;
    }

    public static float Distance(float leftX, float leftY, float rightX, float rightY)
    {
        float num = leftX - rightX;
        float num2 = leftY - rightY;
        return UnityEngine.Mathf.Sqrt(num * num + num2 * num2);
    }

    public static float Distance(Vector2 left, Vector2 right)
    {
        float num = left.x - right.x;
        float num2 = left.y - right.y;
        return UnityEngine.Mathf.Sqrt(num * num + num2 * num2);
    }

    public static float Distance(Vector3 left, Vector3 right)
    {
        float num = left.x - right.x;
        float num2 = left.y - right.y;
        return UnityEngine.Mathf.Sqrt(num * num + num2 * num2);
    }

    public static float DistanceSquared(float leftX, float leftY, float rightX, float rightY)
    {
        float num = leftX - rightX;
        float num2 = leftY - rightY;
        return num * num + num2 * num2;
    }

    public static float DistanceSquared(Vector2 left, Vector2 right)
    {
        float num = left.x - right.x;
        float num2 = left.y - right.y;
        return num * num + num2 * num2;
    }

    public static float DistanceSquared(Vector3 left, Vector3 right)
    {
        float num = left.x - right.x;
        float num2 = left.y - right.y;
        return num * num + num2 * num2;
    }

    public static float Dot(float leftX, float leftY, float rightX, float rightY)
    {
        return leftX * rightX + leftY * rightY;
    }

    public static float Dot(Vector2 left, Vector2 right)
    {
        return left.x * right.x + left.y * right.y;
    }

    public static float Dot(Vector3 left, Vector3 right)
    {
        return left.x * right.x + left.y * right.y;
    }

    public static float Cross(float leftX, float leftY, float rightX, float rightY)
    {
        return leftX * rightY - leftY * rightX;
    }

    public static float Cross(Vector2 left, Vector2 right)
    {
        return left.x * right.y - left.y * right.x;
    }

    public static float Cross(Vector3 left, Vector3 right)
    {
        return left.x * right.y - left.y * right.x;
    }

    public static void InvTransform(float valueX, float valueY, float directionX, float directionY, out float resultX, out float resultY)
    {
        resultX = valueX * directionX + valueY * directionY;
        resultY = valueX * (0f - directionY) + valueY * directionX;
    }

    public static void Transform(float valueX, float valueY, float directionX, float directionY, out float resultX, out float resultY)
    {
        resultX = valueX * directionX + valueY * (0f - directionY);
        resultY = valueX * directionY + valueY * directionX;
    }

    public static void Transform(Vector2 value, Vector2 direction, out float resultX, out float resultY)
    {
        resultX = value.x * direction.x + value.y * (0f - direction.y);
        resultY = value.x * direction.y + value.y * direction.x;
    }

    public static Vector2 Transform(Vector2 value, Vector2 direction)
    {
        float x = value.x * direction.x + value.y * (0f - direction.y);
        float y = value.x * direction.y + value.y * direction.x;
        return new Vector2(x, y);
    }

    public static void Transform(Vector3 value, Vector3 direction, out float resultX, out float resultY)
    {
        resultX = value.x * direction.x + value.y * (0f - direction.y);
        resultY = value.x * direction.y + value.y * direction.x;
    }

    public static Vector3 Transform(Vector3 value, Vector3 direction)
    {
        float x = value.x * direction.x + value.y * (0f - direction.y);
        float y = value.x * direction.y + value.y * direction.x;
        return new Vector3(x, y);
    }

    public static void InvTransform(Vector2 value, Vector2 direction, out float resultX, out float resultY)
    {
        resultX = value.x * direction.x + value.y * direction.y;
        resultY = value.x * (0f - direction.y) + value.y * direction.x;
    }

    public static Vector2 InvTransform(Vector2 value, Vector2 direction)
    {
        float x = value.x * direction.x + value.y * direction.y;
        float y = value.x * (0f - direction.y) + value.y * direction.x;
        return new Vector2(x, y);
    }

    public static void InvTransform(Vector3 value, Vector3 direction, out float resultX, out float resultY)
    {
        resultX = value.x * direction.x + value.y * direction.y;
        resultY = value.x * (0f - direction.y) + value.y * direction.x;
    }

    public static Vector3 InvTransform(Vector3 value, Vector3 direction)
    {
        float x = value.x * direction.x + value.y * direction.y;
        float y = value.x * (0f - direction.y) + value.y * direction.x;
        return new Vector3(x, y);
    }
}