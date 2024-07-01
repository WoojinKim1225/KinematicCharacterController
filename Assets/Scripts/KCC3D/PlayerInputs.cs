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

    [SerializeField] private List<Object> _controllingObjects;
    [SerializeField] private InputActionProperty switchProperty;
    private ObjectStateful controllingObject;
    private int toggle;

    private KinematicCharacterController controller0;
    private KCCFly controller1;

    private void Awake() {
        ReferenceManagerExtensions.InitAssets(_moveReference, _jumpReference, _crouchReference, _sprintReference);
        controllingObject = new ObjectStateful(_controllingObjects[0]);
    }
    void OnEnable()
    {
        ReferenceManagerExtensions.EnableReference(_moveReference, OnMove);
        ReferenceManagerExtensions.EnableReference(_jumpReference, OnJump);
        ReferenceManagerExtensions.EnableReference(_crouchReference, OnCrouch);
        ReferenceManagerExtensions.EnableReference(_sprintReference, OnSprint);

        switchProperty.action.Enable();
        switchProperty.action.started += OnToggle;

        controller0 = (KinematicCharacterController)_controllingObjects[0];
        controller1 = (KCCFly)_controllingObjects[1];
    }

    void OnDisable()
    {
        ReferenceManagerExtensions.DisableReference(_moveReference, OnMove);
        ReferenceManagerExtensions.DisableReference(_jumpReference, OnJump);
        ReferenceManagerExtensions.DisableReference(_crouchReference, OnCrouch);
        ReferenceManagerExtensions.DisableReference(_sprintReference, OnSprint);

        switchProperty.action.Disable();
        switchProperty.action.started -= OnToggle;
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

    void OnToggle(InputAction.CallbackContext context) {
        toggle = (toggle+1) % _controllingObjects.Count;
    }

    private void Update() {
        controllingObject.Value = _controllingObjects[toggle];

        controllingObject.OnUpdate();

        switch (toggle) {
            case 0:
                controller0.MoveVelocityIS = _moveValue;
                controller0.JumpVelocityIS = _jumpValue;
                controller0.PlayerHeightIS = _crouchValue;
                controller0.SprintInputIS = _sprintValue;

                controller1.gameObject.SetActive(false);
                controller1.WingAngleIS = Vector2.zero;
                controller1.WingFoldIS = 0;
                break;
            case 1:
                controller1.gameObject.SetActive(true);
                controller1.WingAngleIS = _moveValue;
                controller1.WingFoldIS = 1 - _crouchValue;

                controller0.MoveVelocityIS = controller0.IsGrounded ? _moveValue : Vector2.zero;
                controller0.JumpVelocityIS = _jumpValue;
                controller0.PlayerHeightIS = 0;
                controller0.SprintInputIS = 0;
                break;
            default:
                break;
        }
    }
}
