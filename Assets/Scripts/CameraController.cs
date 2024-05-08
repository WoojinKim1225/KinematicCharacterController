using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ReferenceManager;

public class CameraController : MonoBehaviour
{
    [SerializeField] private InputActionReference _cameraRotateReference;
    [SerializeField] private KinematicCharacterController kcc;
    public float cameraSensitivity = 100f;
    [SerializeField] private Vector3 targetOffset;
    [SerializeField] private Vector3 cameraOffset;
    private Vector2 _cameraVelocityIS;
    private Vector2 _cameraRotationOS;
    private Quaternion _cameraRotationWS;
    [SerializeField] private LayerMask _whatIsGround;
    private float offset;

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

    void FixedUpdate()
    {
        if (Physics.Raycast(kcc.transform.TransformPoint(targetOffset), -transform.forward, out RaycastHit hit, -cameraOffset.z + 0.2f, _whatIsGround)) {
            offset =  -hit.distance + 0.2f;
        } else {
            offset = cameraOffset.z;
        }
    }

    void LateUpdate()
    {
        _cameraRotationOS += _cameraVelocityIS * Time.deltaTime * cameraSensitivity;
        _cameraRotationOS.y = Mathf.Clamp(_cameraRotationOS.y, -89.9f, 89.9f);
        _cameraRotationOS.x %= 360f;
        _cameraRotationWS = Quaternion.Euler(-_cameraRotationOS.y, _cameraRotationOS.x, 0);

        transform.position = kcc.transform.TransformPoint(targetOffset) + _cameraRotationWS * new Vector3(cameraOffset.x, cameraOffset.y, offset);
        transform.rotation = _cameraRotationWS;
        kcc.SetViewDirection(transform.forward);
    }
}
