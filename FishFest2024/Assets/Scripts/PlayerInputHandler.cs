using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    PlayerManager playerManager;
    public Vector2 mousePosition;
    public Vector2 mouseMovement;
    public Vector2 aimDirection;

    private InputAction _movePositionAction;
    private Vector2 startMousePos;

    private void Awake()
    {
        playerManager = GetComponent<PlayerManager>();
    }

    private void OnEnable()
    {
        _movePositionAction = InputManager.inputActions.Player.MousePostition;

        InputManager.inputActions.Player.JumpPress.performed += OnJumpPress;

        InputManager.inputActions.Player.JumpRelease.performed += OnJumpRelease;

        InputManager.inputActions.Player.JumpCancel.performed += OnJumpCancel;

        InputManager.inputActions.Player.PauseGame.performed += OnPauseGame;
    }

    private void OnDisable()
    {
        InputManager.inputActions.Player.JumpPress.performed -= OnJumpPress;

        InputManager.inputActions.Player.JumpRelease.performed -= OnJumpRelease;

        InputManager.inputActions.Player.Disable();
    }

    public void TickInput()
    {
        MoveInput();
    }

    private void MoveInput()
    {
        if (!playerManager.isControllable) {
            return;
        }
        mousePosition = _movePositionAction.ReadValue<Vector2>();
        aimDirection = (startMousePos - mousePosition).normalized;
    }

    private void OnJumpPress(InputAction.CallbackContext obj)
    {
        if (!playerManager.isControllable) {
            return;
        }
        // Record mouse position on press
        startMousePos = mousePosition;
        // Set flag
        playerManager.isAiming = true;
        playerManager.isAimTriggered = true;
    }

    private void OnJumpRelease(InputAction.CallbackContext obj)
    {
        if (!playerManager.isControllable) {
            playerManager.isAiming = false;
            return;
        }
        if (playerManager.isAiming)
        {
            // Compute the mouse movement delta
            Vector2 endMousePos = mousePosition;
            mouseMovement = endMousePos - startMousePos;
            // Set flag
            playerManager.isAiming = false;
            playerManager.isJumpPerformed = true;
        }
    }

    private void OnJumpCancel(InputAction.CallbackContext obj)
    {
        // Set flag
        playerManager.isAiming = false;
    }

    private void OnPauseGame(InputAction.CallbackContext obj)
    {
        playerManager.PauseGame();
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && playerManager.isAiming)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(Camera.main.ScreenToWorldPoint(startMousePos), Camera.main.ScreenToWorldPoint(mousePosition));
        }
    }
}
