using System;
using UnityEngine;

public class ButtonCallback : MonoBehaviour
{
    public Action onOkCallback;
    public Action onCancelCallback;
    public void OkButtonPressed()
    {
        onOkCallback?.Invoke();
    }

    public void CancelButtonPressed()
    {
        onCancelCallback?.Invoke();
    }
}