using System;
using System.Collections.Generic;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Serializable]
    public class CursorRule
    {
        public PlayerState state;
        public bool showCursor;
    }

    [Header("Cursor Rules")]
    [Tooltip("Default state")]
    public List<CursorRule> rules = new List<CursorRule>
    {
        new CursorRule { state = PlayerState.Free,     showCursor = false },
        new CursorRule { state = PlayerState.Dialogue, showCursor = true  },
        new CursorRule { state = PlayerState.Shop,     showCursor = true  },
        new CursorRule { state = PlayerState.Cutscene, showCursor = false },
    };

    [Header("Fallback")]
    [Tooltip("Used for any new state that has no row in the list above.")]
    public bool showCursorIfNoRule = true;

    void OnEnable()
    {
        PlayerStateManager.OnStateChanged += ApplyCursor;
    }

    void OnDisable()
    {
        PlayerStateManager.OnStateChanged -= ApplyCursor;
    }

    void Start()
    {
        ApplyCursor(PlayerStateManager.CurrentState);
    }

    private void ApplyCursor(PlayerState state)
    {
        bool show = showCursorIfNoRule;

        foreach (CursorRule rule in rules)
        {
            if (rule.state == state)
            {
                show = rule.showCursor;
                break;
            }
        }

        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
    }
}
