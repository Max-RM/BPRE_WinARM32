using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButton : UnityEngine.UI.Button, IDragHandler, IEventSystemHandler
{
    public enum ButtonState
    {
        Normal = 0,
        Highlighted = 1,
        Pressed = 2,
        Selected = 3,
        Disabled = 4
    }

    public bool IsPointerDown { get; private set; }
    public bool IsPointerInside { get; private set; }
    public ButtonState CurrentState => ToButtonState(base.currentSelectionState);

    public event Action<PointerEventData> PointerDown;
    public event Action<PointerEventData> PointerUp;
    public event Action<PointerEventData> PointerEnter;
    public event Action<PointerEventData> PointerExit;
    public event Action<PointerEventData> PointerClick;
    public event Action<PointerEventData> Drag;
    public event Action<ButtonState> StateTransitioned;
    protected override void OnDisable()
    {
        base.OnDisable();
        IsPointerDown = false;
        IsPointerInside = false;
    }

    public void ResetPointer()
    {
        IsPointerDown = false;
        IsPointerInside = false;
        InstantClearState();
    }

    public void ResetEvents()
    {
        this.PointerClick = null;
        this.StateTransitioned = null;
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        this.StateTransitioned?.Invoke(ToButtonState(state));
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            IsPointerDown = true;
        }

        this.PointerDown?.Invoke(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            IsPointerDown = false;
        }

        this.PointerUp?.Invoke(eventData);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        IsPointerInside = true;
        this.PointerEnter?.Invoke(eventData);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        IsPointerInside = false;
        this.PointerExit?.Invoke(eventData);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        this.PointerClick?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        this.Drag?.Invoke(eventData);
    }

    private static ButtonState ToButtonState(SelectionState state)
    {
        return ((state == SelectionState.Normal) ? ButtonState.Normal : (state == SelectionState.Highlighted) ? ButtonState.Highlighted : (state == SelectionState.Pressed) ? ButtonState.Pressed : (state == SelectionState.Highlighted) ? ButtonState.Selected : (state == SelectionState.Disabled) ? ButtonState.Disabled : throw new InvalidOperationException());
    }
}