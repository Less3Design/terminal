using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TerminalBackgroundManipulator : PointerManipulator
{
    private bool _enabled;
    private Vector2 _targetStartPosition { get; set; }
    private Vector3 _pointerStartPosition { get; set; }

    public Action OnLeftClick { get; set; }
    public Action OnRightClick { get; set; }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(PointerDownHandler);
    }
    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(PointerDownHandler);
    }

    private void PointerDownHandler(PointerDownEvent evt)
    {
        _targetStartPosition = target.transform.position;
        _pointerStartPosition = evt.position;
        if (evt.button == (int)MouseButton.LeftMouse)
        {
            OnLeftClick?.Invoke();
            evt.StopImmediatePropagation();
        }
        if (evt.button == (int)MouseButton.RightMouse)
        {
            OnRightClick?.Invoke();
            return;
        }
    }
}
