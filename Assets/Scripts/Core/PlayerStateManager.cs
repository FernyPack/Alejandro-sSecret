using System;
using UnityEngine;

public enum PlayerState
{
    Free,
    Dialogue,
    Cutscene,
    Shop,
}

public static class PlayerStateManager
{
    private static PlayerState _currentState = PlayerState.Free;

    public static event Action<PlayerState> OnStateChanged;

    public static PlayerState CurrentState => _currentState;

    public static void SetState(PlayerState newState)
    {
        _currentState = newState;
        Debug.Log($"[PlayerState] → {newState}");
        OnStateChanged?.Invoke(newState);
    }

    public static bool IsState(PlayerState state) => _currentState == state;

    public static bool IsFree => _currentState == PlayerState.Free;
}
