using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UI_KeyEventhandler : MonoBehaviour
{
    Dictionary<Key, Action> _keyDownActions = new Dictionary<Key, Action>();

    public void AddKeyBinding(Key key, Action action)
    {
        if (!_keyDownActions.ContainsKey(key))
            _keyDownActions[key] = action;
        else
            _keyDownActions[key] += action;
    }

    void Update()
    {
        foreach (var kv in _keyDownActions)
        {
            if (Keyboard.current[kv.Key].wasPressedThisFrame)
                kv.Value?.Invoke();
        }
    }
}
