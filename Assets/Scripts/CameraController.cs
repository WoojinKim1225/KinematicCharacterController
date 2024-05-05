using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ReferenceManager;

public class CameraController : MonoBehaviour
{
    [SerializeField] private InputActionReference _cameraRotateReference;
    [SerializeField] private KinematicCharacterController characterController;
    [SerializeField] private Vector3 targetOffset;
    [SerializeField] private Vector3 cameraOffset;
    private Vector2 _cameraVelocityIS;
    private Vector2 _cameraRotationOS;
    private Quaternion _cameraRotationWS;

    void OnEnable()
    {
        ReferenceManagerExtensions.EnableReference(_cameraRotateReference, OnCameraRotate);
    }

    void OnDisable() 
    {
        ReferenceManagerExtensions.DisableReference(_cameraRotateReference, OnCameraRotate);
    }

    void OnCameraRotate(InputAction.CallbackContext context) {
        _cameraVelocityIS = context.ReadValue<Vector2>();
    }

    void LateUpdate()
    {
        _cameraRotationOS += _cameraVelocityIS * Time.deltaTime * Mathf.Rad2Deg * 0.2f;
        _cameraRotationOS.y = Mathf.Clamp(_cameraRotationOS.y, -89.9f, 89.9f);
        _cameraRotationOS.x %= 360f;
        _cameraRotationWS = Quaternion.Euler(-_cameraRotationOS.y, _cameraRotationOS.x, 0);
        transform.position = characterController.transform.TransformPoint(targetOffset) + _cameraRotationWS * cameraOffset;
        transform.rotation = _cameraRotationWS;
        characterController.SetViewDirection(transform.forward);
    }
}
