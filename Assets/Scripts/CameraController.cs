using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ReferenceManager;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private new Camera camera;
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
        camera = Camera.main;
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
        float halfWidth =  camera.nearClipPlane * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfHeight = halfWidth / camera.aspect;

        Vector3 UL = -halfWidth * transform.right + halfHeight * transform.up;
        Vector3 UR = halfWidth * transform.right + halfHeight * transform.up;
        Vector3 DL = -halfWidth * transform.right - halfHeight * transform.up;
        Vector3 DR = halfWidth * transform.right - halfHeight * transform.up;

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
