using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "Input/Input Reader")]
public class InputReader : ScriptableObject, PlayerInputActions.IPlayerActions, PlayerInputActions.IUIActions
{
    public event Action<Vector2> OnMoveEvent;
    public event Action          OnInteractEvent;
    public event Action          OnCancelEvent;
    public event Action<Vector2> OnNavigateEvent;
    public event Action          OnConfirmEvent;

    private PlayerInputActions _inputActions;

    private void OnEnable()
    {
        if (_inputActions == null)
        {
            _inputActions = new PlayerInputActions();
            _inputActions.Player.SetCallbacks(this);
            _inputActions.UI.SetCallbacks(this);
        }

        PlayerStateManager.OnStateChanged += HandleStateChanged;
        EnablePlayerInput();
    }

    private void OnDisable()
    {
        PlayerStateManager.OnStateChanged -= HandleStateChanged;
        DisableAllInput();
    }

    private void HandleStateChanged(PlayerState newState)
    {
        switch (newState)
        {
            case PlayerState.Free:
                EnablePlayerInput();
                break;
            case PlayerState.Dialogue:
            case PlayerState.Cutscene:
            case PlayerState.Shop:
                EnableUIInput();
                break;
        }
    }

    public void EnablePlayerInput()
    {
        _inputActions.Player.Enable();
        _inputActions.UI.Disable();
    }

    public void EnableUIInput()
    {
        _inputActions.Player.Disable();
        _inputActions.UI.Enable();
    }

    public void DisableAllInput()
    {
        _inputActions?.Player.Disable();
        _inputActions?.UI.Disable();
    }

    void PlayerInputActions.IPlayerActions.OnMove(InputAction.CallbackContext context)
    {
        OnMoveEvent?.Invoke(context.ReadValue<Vector2>());
    }

    void PlayerInputActions.IPlayerActions.OnInteract(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            OnInteractEvent?.Invoke();
    }

    void PlayerInputActions.IUIActions.OnNavigate(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            OnNavigateEvent?.Invoke(context.ReadValue<Vector2>());
    }

    void PlayerInputActions.IUIActions.OnConfirm(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            OnConfirmEvent?.Invoke();
    }

    void PlayerInputActions.IUIActions.OnCancel(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            OnCancelEvent?.Invoke();
    }
}
