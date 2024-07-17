using UnityEngine;
using UnityEngine.InputSystem;
using ReferenceManager;
using StatefulVariables;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    [SerializeField] private InputActionReference _moveReference;
    [SerializeField] private InputActionReference _jumpReference;
    [SerializeField] private InputActionReference _crouchReference;
    [SerializeField] private InputActionReference _sprintReference;

    private Vector2 _moveValue;
    private float _jumpValue;
    private float _crouchValue;
    private float _sprintValue;

    private ObjectStateful controllingObject;
    private int toggle;

    [SerializeField] private KinematicCharacterController _kcc;

    private void Awake() {
        ReferenceManagerExtensions.InitAssets(_moveReference, _jumpReference, _crouchReference, _sprintReference);
        controllingObject = new ObjectStateful(_kcc);
    }
    void OnEnable()
    {
        ReferenceManagerExtensions.EnableReference(_moveReference, OnMove);
        ReferenceManagerExtensions.EnableReference(_jumpReference, OnJump);
        ReferenceManagerExtensions.EnableReference(_crouchReference, OnCrouch);
        ReferenceManagerExtensions.EnableReference(_sprintReference, OnSprint);
    }

    void OnDisable()
    {
        ReferenceManagerExtensions.DisableReference(_moveReference, OnMove);
        ReferenceManagerExtensions.DisableReference(_jumpReference, OnJump);
        ReferenceManagerExtensions.DisableReference(_crouchReference, OnCrouch);
        ReferenceManagerExtensions.DisableReference(_sprintReference, OnSprint);
    }
    void OnMove(InputAction.CallbackContext context)
    {
        _moveValue = context.ReadValue<Vector2>();
    }

    void OnJump(InputAction.CallbackContext context)
    {
        _jumpValue = context.ReadValue<float>();
    }

    void OnCrouch(InputAction.CallbackContext context)
    {
        _crouchValue = context.ReadValue<float>();
    }

    void OnSprint(InputAction.CallbackContext context)
    {
        _sprintValue = context.ReadValue<float>();
    }

    private void Update() {
        controllingObject.Value = _kcc;

        controllingObject.OnUpdate();

        _kcc.SetMoveVelocityIS(_moveValue);
        _kcc.SetJumpVelocityIS(_jumpValue);
        _kcc.SetPlayerHeightIS(_crouchValue);
        _kcc.SetSprintInputIS(_sprintValue);

    }
}
