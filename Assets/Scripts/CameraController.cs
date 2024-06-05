using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ReferenceManager;
using Unity.Mathematics;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public bool showGizmos;
    [SerializeField] private InputActionReference _cameraRotateReference;
    [SerializeField] private KinematicCharacterController kcc;
    private new Camera camera;


    [SerializeField] private float _cameraSensitivity = 100f;

    private enum ECameraMode{
        FirstPerson, ThirdPerson
    };
    [SerializeField] private ECameraMode cameraMode;

    [SerializeField] private Vector3 _cameraUp = Vector3.up;
    [SerializeField] private Vector3 _targetOffset;
    [SerializeField] private Vector3 _cameraOffset;
    [SerializeField] private LayerMask _whatIsGround;

    private Vector2 _cameraVelocityIS;
    private Vector2 _cameraRotationOS;
    private Quaternion _cameraRotationWS;
    
    private float offset;
    private float r;
    private Vector3 s;
    [SerializeField] private Mesh cubeMesh;

    private enum ECameraCollisionMode{None, RayCast, SphereCast, BoxCast};
    [SerializeField] private ECameraCollisionMode cameraCollisionMode;

    void OnEnable()
    {
        ReferenceManagerExtensions.EnableReference(_cameraRotateReference, OnCameraRotate);
        camera = Camera.main;
    }

    void OnDisable() 
    {
        ReferenceManagerExtensions.DisableReference(_cameraRotateReference, OnCameraRotate);
    }

    void OnCameraRotate(InputAction.CallbackContext context) => _cameraVelocityIS = context.ReadValue<Vector2>();




    void LateUpdate()
    {
        _cameraRotationOS += _cameraVelocityIS * Time.deltaTime * _cameraSensitivity;
        _cameraRotationOS.y = Mathf.Clamp(_cameraRotationOS.y, -89.9f, 89.9f);
        _cameraRotationOS.x %= 360f;
        _cameraRotationWS = Quaternion.FromToRotation(Vector3.up, _cameraUp.normalized) * Quaternion.Euler(-_cameraRotationOS.y, _cameraRotationOS.x, 0);

        if (cameraMode == ECameraMode.ThirdPerson) {
            OffsetUpdate();

            transform.position = kcc.transform.TransformPoint(_targetOffset) + _cameraRotationWS * new Vector3(_cameraOffset.x, _cameraOffset.y, offset);

        } else if (cameraMode == ECameraMode.FirstPerson) {

            transform.position = kcc.transform.TransformPoint(_targetOffset) + _cameraRotationWS * _cameraOffset;

        }
        transform.rotation = _cameraRotationWS;
        kcc.SetViewDirection(transform.forward);
    }

    void OnDrawGizmos()
    {
        if (camera == null || !showGizmos) return;
        switch (cameraCollisionMode) {
            case ECameraCollisionMode.RayCast:

                break;
            case ECameraCollisionMode.SphereCast:
                Gizmos.DrawWireSphere(camera.transform.position, r);
                break;
            case ECameraCollisionMode.BoxCast:
                Gizmos.DrawWireMesh(cubeMesh, camera.transform.position, camera.transform.rotation, s * 2f);
                
                break;
        }
    }

    void OffsetUpdate()
    {
        float halfHeight =  camera.nearClipPlane * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * camera.aspect;
        float dist = _cameraOffset.z;

        switch (cameraCollisionMode) {
            case ECameraCollisionMode.RayCast:
                Vector3 UL = -halfWidth * transform.right + halfHeight * transform.up;
                Vector3 UR = halfWidth * transform.right + halfHeight * transform.up;
                Vector3 DL = -halfWidth * transform.right - halfHeight * transform.up;
                Vector3 DR = halfWidth * transform.right - halfHeight * transform.up;

                bool isULHit = Physics.Raycast(kcc.transform.TransformPoint(_targetOffset) + UL, -transform.forward, out RaycastHit h1, -_cameraOffset.z, _whatIsGround, QueryTriggerInteraction.Ignore);
                bool isURHit = Physics.Raycast(kcc.transform.TransformPoint(_targetOffset) + UR, -transform.forward, out RaycastHit h2, -_cameraOffset.z, _whatIsGround, QueryTriggerInteraction.Ignore);
                bool isDLHit = Physics.Raycast(kcc.transform.TransformPoint(_targetOffset) + DL, -transform.forward, out RaycastHit h3, -_cameraOffset.z, _whatIsGround, QueryTriggerInteraction.Ignore);
                bool isDRHit = Physics.Raycast(kcc.transform.TransformPoint(_targetOffset) + DR, -transform.forward, out RaycastHit h4, -_cameraOffset.z, _whatIsGround, QueryTriggerInteraction.Ignore);

                dist = Mathf.Max(isULHit ? -h1.distance : _cameraOffset.z, isURHit ? -h2.distance : _cameraOffset.z, isDLHit ? -h3.distance : _cameraOffset.z, isDRHit ? -h4.distance : _cameraOffset.z);

                break;
            case ECameraCollisionMode.SphereCast:
                r = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);
                bool isHit = Physics.SphereCast(kcc.transform.TransformPoint(_targetOffset), r, -transform.forward, out RaycastHit h, -_cameraOffset.z, _whatIsGround, QueryTriggerInteraction.Ignore);
                if (isHit && Vector3.Distance(h.point + h.normal * r, kcc.transform.TransformPoint(_targetOffset)) < -_cameraOffset.z) {
                    dist = -Vector3.Distance(h.point + h.normal * r, kcc.transform.TransformPoint(_targetOffset));
                }
                break;
            case ECameraCollisionMode.BoxCast:
                s = new Vector3(halfWidth, halfHeight, camera.nearClipPlane);
                isHit = Physics.BoxCast(kcc.transform.TransformPoint(_targetOffset), s, -transform.forward, out h, camera.transform.rotation, -_cameraOffset.z, _whatIsGround, QueryTriggerInteraction.Ignore);
                if (isHit && Vector3.Dot(h.point - kcc.transform.TransformPoint(_targetOffset), -transform.forward) - camera.nearClipPlane < -_cameraOffset.z) {
                    dist = Vector3.Dot(h.point - kcc.transform.TransformPoint(_targetOffset), transform.forward) + camera.nearClipPlane;
                }
                
                break;
        }

        offset = dist;
    }
}
