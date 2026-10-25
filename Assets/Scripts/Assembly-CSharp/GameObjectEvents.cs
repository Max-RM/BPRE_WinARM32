using System;
using UnityEngine;

public class GameObjectEvents : MonoBehaviour
{
    public Action<bool> OnVisible;
    public Action<bool> OnEnabled;
    private void OnEnable()
    {
        OnEnabled?.Invoke(obj: true);
    }

    private void OnDisable()
    {
        OnEnabled?.Invoke(obj: false);
    }

    private void OnBecameVisible()
    {
        OnVisible?.Invoke(obj: true);
    }

    private void OnBecameInvisible()
    {
        OnVisible?.Invoke(obj: false);
    }
}