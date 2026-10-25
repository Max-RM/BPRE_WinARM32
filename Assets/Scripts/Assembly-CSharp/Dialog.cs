using UnityEngine;

public class Dialog : MonoBehaviour
{
    public delegate void OnClose();
    public delegate void OnOpen();
    public event OnClose onClose;
    public event OnOpen onOpen;
    public void Open()
    {
        base.gameObject.SetActive(value: true);
        this.onOpen?.Invoke();
    }

    public void Close()
    {
        base.gameObject.SetActive(value: false);
        this.onClose?.Invoke();
    }

    private void OnEnable()
    {
        Singleton<GuiManager>.Instance.GrabPointer(this);
        Singleton<KeyListener>.Instance.GrabFocus(this);
        KeyListener.keyReleased += HandleKeyReleased;
    }

    private void OnDisable()
    {
        Singleton<GuiManager>.Instance.ReleasePointer(this);
        Singleton<KeyListener>.Instance.ReleaseFocus(this);
        KeyListener.keyReleased -= HandleKeyReleased;
    }

    private void HandleKeyReleased(KeyCode obj)
    {
        if (obj == KeyCode.Escape)
        {
            Close();
        }
    }
}