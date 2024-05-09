using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using ReferenceManager;
using System.Collections.Generic;

// Stateful Class for Float
[System.Serializable]
public class Float
{
    [SerializeField] private float _value;
    [SerializeField] private bool _isChanged;
    [SerializeField] private float _beforeValue;
    [SerializeField] private float _initialValue;

    public float Value { get => _value; set => _value = value; }

    public bool IsChanged => _isChanged;

    public float InitialValue => _initialValue;

    public Float(float v)
    {
        _value = v;
        _beforeValue = v;
        _initialValue = v;
        _isChanged = false;
    }

    public void OnUpdate(float v)
    {
        _value = v;
        _isChanged = _beforeValue != _value;
        _beforeValue = _value;
    }
}

[RequireComponent(typeof(Rigidbody))]
public class KinematicCharacterController : MonoBehaviour
{
    #region Variables
    [SerializeField] private InputActionReference _moveReference;
    [SerializeField] private InputActionReference _jumpReference;
    [SerializeField] private InputActionReference _crouchReference;
    [SerializeField] private InputActionReference _sprintReference;

    [SerializeField] private Rigidbody _rb;
    [SerializeField] private CapsuleCollider _characterCollider;

    [SerializeField] private float _skinWidth = 0.01f;
    [SerializeField] private int _maxBounces = 5;


    [SerializeField] private Vector3 _viewDirection = Vector3.forward;
    public Vector3 ViewDirection { get => _viewDirection; set => _viewDirection = value; }
    public void SetViewDirection(Vector3 dir) => _viewDirection = dir;

    [SerializeField] private Vector3 _gravity = Vector3.down * 20f;
    private Vector3 _gravityDirection => _gravity.normalized;

    //-------------------------------------------------------------
    [SerializeField] private float _moveSpeed = 4f;
    public float MoveSpeed => _moveSpeed;

    public enum ESpeedControlMode {Constant, Linear, Exponential};
    [SerializeField] private ESpeedControlMode _speedControlMode = ESpeedControlMode.Constant;
    public ESpeedControlMode SpeedControlMode => _speedControlMode;

    [SerializeField] private float _moveAcceleration, _moveDamp;

    //-------------------------------------------------------------
    [SerializeField] private float _idleHeight = 2f;
    public float IdleHeight => _idleHeight;

    [SerializeField] private float _crouchHeight = 1.2f;
    public float CrouchHeight => _crouchHeight;

    [SerializeField] private float _capsuleRadius = 0.5f;
    public float CapusleRadius => _capsuleRadius;
    [SerializeField] private Float _height, _radius;
    public float CapsuleHeight => _height.Value;

    //-------------------------------------------------------------
    [SerializeField] private float _jumpSpeed = 10f;
    [SerializeField] private float _jumpMaxHeight = 2f;
    public float JumpMaxHeight => _jumpMaxHeight;

    [SerializeField] private Float _jumpS, _jumpMaxH;

    [SerializeField] private float _sprintSpeedMultiplier = 2f;
    [SerializeField] private float _maxSlopeAngle = 55f;
    public float MaxSlopeAngle => _maxSlopeAngle;

    [SerializeField] private float _minCeilingAngle = 130f;

    private Vector3 _forward, _right;

    private Vector2 _moveVelocityIS;
    private float _jumpVelocityIS;
    private float _crouchInputIS;
    private float _sprintInputIS;

    private Vector3 _moveVelocityOS, _moveVelocityTargetOS;
    private Vector3 _jumpVelocityOS;
    private float _playerHeightOS;

    private Vector3 _moveVelocityTS;

    private Vector3 _moveVelocityWS;
    private Vector3 _jumpVelocityWS;
    private float _playerHeightWS;

    private Vector3 _horizontalDisplacement, _verticalDisplacement;

    private Vector3 _externalVelocity;

    private Vector3 _nextPositionWS;

    [SerializeField] private bool _isGrounded = false;
    private bool _isGroudedBefore;
    
    [SerializeField] private Vector3 _groundNormal = Vector3.up;
    [SerializeField] private Vector3 _playerUp = Vector3.up;

    private RaycastHit hit;
    [SerializeField] private LayerMask _whatIsGround;

    [SerializeField] private float _maxStepUpHeight = 0.3f;
    [SerializeField] private float _maxStepDownHeight = -0.3f;
    [SerializeField] private bool _isUpStepEnabled;
    public bool IsUpStepEnabled => _isUpStepEnabled;
    [SerializeField] private bool _isDownStepEnabled;
    public bool IsDownStepEnabled => _isDownStepEnabled;
    private bool _isStep;
    #endregion

    void Awake() {
        ReferenceManagerExtensions.InitAssets(_moveReference, _jumpReference, _crouchReference, _sprintReference);
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _rb = GetComponent<Rigidbody>();
        _characterCollider = GetComponentInChildren<CapsuleCollider>();

        _rb.detectCollisions = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.isKinematic = true;

        _height = new Float(_idleHeight);
        _radius = new Float(_capsuleRadius);

        _jumpS = new Float(_jumpSpeed);
        _jumpMaxH = new Float(Mathf.Sqrt(_jumpSpeed * _jumpSpeed * 0.5f / Mathf.Abs(_gravity.y)));
        _jumpMaxHeight = _jumpSpeed * _jumpSpeed * 0.5f / Mathf.Abs(_gravity.y);

        _characterCollider.bounds.Expand(-2 * _skinWidth);
        _characterCollider.height = _height.Value - 2 * _skinWidth;
        _characterCollider.radius = _radius.Value - _skinWidth;
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
        _moveVelocityIS = context.ReadValue<Vector2>();
    }

    void OnJump(InputAction.CallbackContext context)
    {
        _jumpVelocityIS = context.ReadValue<float>();
    }

    void OnCrouch(InputAction.CallbackContext context)
    {
        _crouchInputIS = context.ReadValue<float>();
    }

    void OnSprint(InputAction.CallbackContext context)
    {
        _sprintInputIS = context.ReadValue<float>();
    }

    void Update()
    {
        GetDirectionsFromView();
    }

    void FixedUpdate()
    {
        CalculateObjectSpaceVariables();

        CalculateTangentSpaceVariables();

        CalculateWorldSpaceVariables();

        UpdateProperties();

        HandleCollisionsAndMovement();
    }

    #region Priate Methods
    private void GetDirectionsFromView()
    {
        _forward = Vector3.ProjectOnPlane(_viewDirection, _playerUp).normalized;
        _right = Vector3.Cross(_playerUp, _forward);
    }

    private void CalculateObjectSpaceVariables()
    {
        _jumpVelocityOS = _isGrounded && _jumpVelocityIS > 0 ? _jumpVelocityIS * _jumpSpeed * _playerUp + _gravity * Time.fixedDeltaTime * 0.5f : _gravity * Time.fixedDeltaTime;
        _playerHeightOS = _crouchInputIS > 0 ? _crouchHeight : _idleHeight;
        float moveSpeedMultiplier = _crouchInputIS > 0f ? 0.5f : (_sprintInputIS > 0 ? _sprintSpeedMultiplier : 1f);
        _moveVelocityTargetOS = new Vector3(_moveVelocityIS.x, 0, _moveVelocityIS.y) * _moveSpeed * moveSpeedMultiplier;
        switch (_speedControlMode)
        {
            case ESpeedControlMode.Constant:
                _moveVelocityOS = _moveVelocityTargetOS;
                break;
            case ESpeedControlMode.Linear:
                if ((_moveVelocityOS - _moveVelocityTargetOS).magnitude < Time.fixedDeltaTime * _moveAcceleration) {
                    _moveVelocityOS = _moveVelocityTargetOS;
                } else {
                    _moveVelocityOS += (_moveVelocityTargetOS - _moveVelocityOS).normalized * Time.fixedDeltaTime * _moveAcceleration;
                }
                break;
            case ESpeedControlMode.Exponential:
                if ((_moveVelocityOS - _moveVelocityTargetOS).magnitude < Mathf.Epsilon) {
                    _moveVelocityOS = _moveVelocityTargetOS;
                } else {
                _moveVelocityOS += (_moveVelocityTargetOS - _moveVelocityOS) * Time.fixedDeltaTime * _moveDamp;
                }
                break;
            default:
                _moveVelocityOS = _moveVelocityTargetOS;
                break;
        }
        
    }

    private void CalculateTangentSpaceVariables()
    {
        _moveVelocityTS = _moveVelocityOS.x * _right + _moveVelocityOS.z * _forward;
    }

    private void CalculateWorldSpaceVariables()
    {
        _moveVelocityWS = (_moveVelocityTS - Vector3.Dot(_groundNormal, _moveVelocityTS) / Vector3.Dot(_groundNormal, _playerUp) * _playerUp).normalized * _moveVelocityTS.magnitude;
        _moveVelocityWS += Vector3.ProjectOnPlane(_externalVelocity, _groundNormal);
        if (!_isGrounded) _jumpVelocityWS += _gravity * Time.fixedDeltaTime;
        else _jumpVelocityWS = _jumpVelocityOS;
        _moveVelocityWS += Vector3.Project(_externalVelocity, _groundNormal);
        _playerHeightWS = _playerHeightOS * transform.localScale.y;
    }

    private void HandleCollisionsAndMovement()
    {
        _horizontalDisplacement = CollideAndSlide(_moveVelocityWS * Time.fixedDeltaTime, _rb.position + _height.Value * _playerUp * 0.5f, 0, false);
        _verticalDisplacement = CollideAndSlide(_jumpVelocityWS * Time.fixedDeltaTime, _rb.position + _height.Value * _playerUp * 0.5f + _horizontalDisplacement, 0, true);

        Debug.DrawRay(_rb.position, _horizontalDisplacement, Color.green, 1f);
        Debug.DrawRay(_rb.position + _horizontalDisplacement, _verticalDisplacement, Color.red, 1f);


        _nextPositionWS = _horizontalDisplacement + _verticalDisplacement + _rb.position;
        float _externalForceEffectAmount;
        if (_externalVelocity.magnitude != 0)
            _externalForceEffectAmount = Vector3.Dot(_horizontalDisplacement + _verticalDisplacement, _externalVelocity.normalized) / (_externalVelocity.magnitude * Time.fixedDeltaTime);
        else _externalForceEffectAmount = 0f;

        _externalVelocity *= Mathf.Clamp01(_externalForceEffectAmount);

        _rb.MovePosition(_nextPositionWS);
    }

    private Vector3 CollideAndSlide(Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit = default)
    {
        if (depth >= _maxBounces) return Vector3.zero;
        
        if (depth == 0)
        {
            velInit = vel;
            if (!gravityPass) {
                _isGroudedBefore = _isGrounded;
                _isGrounded = false;
            }
        }

        float dist = vel.magnitude + _skinWidth;
        Vector3 capsulePoint = (_height.Value * 0.5f - _radius.Value) * _playerUp;
        Vector3 characterGroundPosition = pos - _height.Value * 0.5f * _characterCollider.transform.up;

        if (Physics.CapsuleCast(pos + capsulePoint, pos - capsulePoint, _characterCollider.bounds.extents.x, vel.normalized, out hit, dist, _whatIsGround))
        {
            Vector3 snapToSurface = vel.normalized * (hit.distance - _skinWidth);
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(_playerUp, hit.normal);

            // the terrain is inside the collider
            if (snapToSurface.magnitude <= _skinWidth) snapToSurface = Vector3.zero;

            // if flat ground or slope
            if (angle <= _maxSlopeAngle || _isStep)
            {
                if (gravityPass)
                {
                    _isGrounded = true;
                    _groundNormal = hit.normal;
                    if (angle <= _maxSlopeAngle) {
                        _isStep = false;
                    }

                    return snapToSurface;
                }
                

                leftover = projectAndScale(leftover, hit.normal);
            }
            // if ceiling, cancel upwards velocity
            else if (angle >= _minCeilingAngle) 
            {
                _jumpVelocityWS = Vector3.zero;
                return snapToSurface;
            }
            // if wall
            else 
            {
                //if (gravityPass && _isUpStep) return snapToSurface;
                Vector3 flatHit = new Vector3(hit.normal.x, 0, hit.normal.z).normalized;
                float scale = 1 - Vector3.Dot(flatHit, -new Vector3(velInit.x, 0, velInit.z).normalized);
                
                if (_isGrounded && !gravityPass)
                { 
                    leftover = projectAndScale(new Vector3(leftover.x, 0, leftover.z), -new Vector3(hit.normal.x, 0, hit.normal.z)).normalized * scale;
                }
                else
                {
                    leftover = projectAndScale(leftover, hit.normal) * scale;
                }

                if (_isUpStepEnabled && _isGroudedBefore && !gravityPass) {
                    float hitHeight = Vector3.Dot(hit.point - characterGroundPosition, _playerUp);
                    Vector3 start = hit.point + 0.1f * Vector3.up - flatHit * 0.01f;
                    bool b = Physics.Raycast(start, _gravityDirection, out RaycastHit h, _maxStepUpHeight, _whatIsGround);
                    

                    if (b && hitHeight <= _maxStepUpHeight && Vector3.Angle(h.normal, -_gravityDirection) <= MaxSlopeAngle) {
                        Debug.DrawRay(start, _gravityDirection * h.distance, Color.cyan, 1f);
                        _isStep = true;
                        leftover = vel - snapToSurface + hitHeight * (-_gravityDirection);
                    } else {
                        Debug.DrawRay(start, _gravityDirection * _maxStepUpHeight, b ? Color.magenta : Color.black, 1f);
                    }

                }
            }
            return snapToSurface + CollideAndSlide(leftover, pos + snapToSurface, depth + 1, gravityPass, velInit);
        } 
        
        else if (gravityPass && _isGroudedBefore && _jumpVelocityIS == 0) 
        {
            if (_isDownStepEnabled) {
                Debug.DrawRay(pos, _gravityDirection * ( _maxStepDownHeight + _height.Value * 0.5f), Color.cyan, 1f);
                if (Physics.Raycast(pos, _gravityDirection, out RaycastHit h, _maxStepDownHeight + _height.Value * 0.5f, _whatIsGround) && h.distance > _height.Value * 0.5f + _skinWidth && Vector3.Angle(_playerUp, h.normal) <= MaxSlopeAngle) {
                    Debug.Log(h.distance);
                    _isStep = true;
                    return CollideAndSlide(-_playerUp * _maxStepUpHeight, pos, depth + 1, true, velInit);
                }
            }
            
        }
        
        return vel;
    }


    private Vector3 projectAndScale(Vector3 a, Vector3 n)
    {
        return Vector3.ClampMagnitude(Vector3.ProjectOnPlane(a, n), a.magnitude);
    }

    void UpdateProperties()
    {
        _height.OnUpdate(_playerHeightWS);
        _radius.OnUpdate(_capsuleRadius);
        _jumpS.OnUpdate(_jumpSpeed);
        _jumpMaxH.OnUpdate(_jumpMaxHeight);

        if (_height.IsChanged)
        {
            _characterCollider.height = _height.Value - _skinWidth * 2f;
            _characterCollider.center = _height.Value * 0.5f * Vector3.up;
        }

        if (_radius.IsChanged)
        {
            _characterCollider.radius = _radius.Value - _skinWidth;
        }

        if (_jumpS.IsChanged)
        {
            _jumpMaxHeight = _jumpS.Value * _jumpS.Value * 0.5f / Mathf.Abs(_gravity.y);
            _jumpMaxH = new Float(_jumpMaxHeight);
        }
        else if (_jumpMaxH.IsChanged)
        {
            _jumpSpeed = Mathf.Sqrt(2f * Mathf.Abs(_gravity.y) * _jumpMaxH.Value);
            _jumpS = new Float(_jumpSpeed);
        }
    }
    #endregion

    #region Public Methods
        public void AddForce(Vector3 force, ForceMode forceMode = ForceMode.Force) {
            _externalVelocity += force / _rb.mass * Time.fixedDeltaTime;
        }
    #endregion
}