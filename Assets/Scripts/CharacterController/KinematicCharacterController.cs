using UnityEngine;
using UnityEngine.InputSystem;
using ReferenceManager;
using StatefulVariables;

[RequireComponent(typeof(Rigidbody))]
public class KinematicCharacterController : MonoBehaviour
{
    #region Variables

    //[Header("Components")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private CapsuleCollider _capsuleCollider;

    [SerializeField] private Vector3 _viewDirection = Vector3.forward;
    public new Rigidbody rigidbody => _rigidbody;
    public Vector3 ViewDirection { get => _viewDirection; set => _viewDirection = value; }
    public void SetViewDirection(Vector3 dir) => _viewDirection = dir;


    //[Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private float _jumpSpeed = 10f;
    [SerializeField] private float _jumpMaxHeight = 2f;
    [SerializeField] private float _sprintSpeedMultiplier = 2f;
    [SerializeField] private float _crouchSpeedMultiplier = 0.5f;
    [SerializeField] private int _maxAirJumpCount = 5;

    public enum ESpeedControlMode {Constant, Linear, Exponential};
    [SerializeField] private ESpeedControlMode _speedControlMode = ESpeedControlMode.Constant;
    [SerializeField] private float _moveAcceleration, _moveDamp;

    public float MoveSpeed => _moveSpeed;
    public ESpeedControlMode SpeedControlMode => _speedControlMode;
    public float JumpMaxHeight => _jumpMaxHeight;

    [SerializeField] private Float jumpSpeed, jumpMaxHeight;
    [SerializeField] private Float _airJump;

    //[Header("Physics Settings")]
    [SerializeField] private float _skinWidth = 0.01f;
    [SerializeField] private int _maxBounces = 5;
    [SerializeField] private Vector3 _gravity = Vector3.down * 20f;
    public Vector3 Gravity {set => _gravity = value; }

    public Vector3Stateful gravity;
    private Vector3 gravityDirection => gravity.Value.normalized;

    //[Header("Character Size")]
    [SerializeField] private float _idleHeight = 2f;
    [SerializeField] private float _crouchHeight = 1.2f;
    [SerializeField] private float _capsuleRadius = 0.5f;

    public float IdleHeight => _idleHeight;
    public float CrouchHeight => _crouchHeight;
    public float CapsuleRadius => _capsuleRadius;
    public float CapsuleHeight => _height.Value;

    [SerializeField] private Float _height, _radius;

    //[Header("Slope And Step Handling")]
    [SerializeField] private float _maxSlopeAngle = 55f;
    [SerializeField] private float _minCeilingAngle = 130f;

    [SerializeField] private bool _isUpStepEnabled;
    [SerializeField] private float _maxStepUpHeight = 0.3f;
    [SerializeField] private bool _isDownStepEnabled;
    [SerializeField] private float _maxStepDownHeight = -0.3f;

    public float MaxSlopeAngle => _maxSlopeAngle;
    public bool IsUpStepEnabled => _isUpStepEnabled;
    public bool IsDownStepEnabled => _isDownStepEnabled;


    private Vector3 _forward, _right;
    public Vector3 Up => _playerUp.normalized;
    public Vector3 Forward => _forward;
    public Vector3 Right => _right;

    private Vector2 _moveVelocityIS;
    private float _jumpVelocityIS;
    private float _crouchInputIS;
    private float _sprintInputIS;

    public Vector3 MoveVelocityIS {set => _moveVelocityIS = value; }
    public float JumpVelocityIS {set => _jumpVelocityIS = value; }
    public float SprintInputIS {set => _sprintInputIS = value; }
    public float CrouchInputIS {set => _crouchInputIS = value;}

    private float _jumpVelocityISBefore;
    private bool _isJumpStarted => _jumpVelocityIS != 0 && _jumpVelocityISBefore == 0;

    private Vector3 _moveVelocityOS, _moveVelocityTargetOS;
    private Vector3 _jumpVelocityOS;
    private float _playerHeightOS;

    private Vector3 _moveVelocityTS;

    private Vector3 _moveVelocityWS;
    private Vector3 _jumpVelocityWS;
    private float _playerHeightWS;

    private Vector3 _horizontalDisplacement, _verticalDisplacement;
    private Vector3 _displacement;
    public Vector3 Displacement => _displacement;
    public Vector3 Velocity => _displacement / Time.fixedDeltaTime;
    public Vector3 HorizontalVelocity => _horizontalDisplacement / Time.fixedDeltaTime;
    public Vector3 VerticalVelocity => _verticalDisplacement / Time.fixedDeltaTime;

    public Vector3 HorizontalDirection => _horizontalDisplacement - ExternalGroundMove * Time.fixedDeltaTime;
    public float Speed => HorizontalDirection.magnitude / Time.fixedDeltaTime;

    private Vector3 _externalGroundMove;
    private Vector3 _externalAcceleration;
    private Vector3 _externalVelocity;
    private Vector3 _externalPosition;
    private bool _isPositionSet;

    public Vector3 ExternalGroundMove {get => _externalGroundMove; set => _externalGroundMove = value; }

    private Vector3 _nextPositionWS;

    [SerializeField] private bool _isGrounded = false;
    private bool _isGroundedBefore;
    private int _groundedDepth;
    private Vector3 beforeWallNormal = Vector3.zero;
    
    [SerializeField] private Vector3 _groundNormal = Vector3.up;
    [SerializeField] private Vector3 _playerUp = Vector3.up;

    private RaycastHit hit;
    [SerializeField] private LayerMask _whatIsGround;
    public LayerMask WhatIsGround => _whatIsGround;
    private Vector3 groundExitDisplacement;

    private bool _isStep;
    private QueryTriggerInteraction queryTrigger = QueryTriggerInteraction.Ignore;
    #endregion

    void Awake() {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _rigidbody = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponentInChildren<CapsuleCollider>();

        _rigidbody.detectCollisions = true;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rigidbody.isKinematic = true;

        _height = new Float(_idleHeight);
        _radius = new Float(_capsuleRadius);

        jumpSpeed = new Float(_jumpSpeed);
        jumpMaxHeight = new Float(Mathf.Sqrt(_jumpSpeed * _jumpSpeed * 0.5f / Mathf.Abs(_gravity.y)));
        _jumpMaxHeight = _jumpSpeed * _jumpSpeed * 0.5f / Mathf.Abs(_gravity.y);

        _airJump = new Float(_maxAirJumpCount);

        gravity = new Vector3Stateful(_gravity);

        _capsuleCollider.bounds.Expand(-2 * _skinWidth);
        _capsuleCollider.height = _height.Value - 2 * _skinWidth;
        _capsuleCollider.radius = _radius.Value - _skinWidth;
    }

    void Update()
    {
        GetDirectionsFromView();
    }

    void FixedUpdate()
    {
        if (_isGrounded) _airJump.Value = _airJump.InitialValue;

        _capsuleCollider.transform.up = _playerUp.normalized;
        //_characterCollider.transform.localPosition = (-_gravityDirection - _playerUp.normalized) * (_radius.Value + _skinWidth);
        

        CalculateObjectSpaceVariables();

        CalculateTangentSpaceVariables();

        CalculateWorldSpaceVariables();

        UpdateProperties();

        beforeWallNormal = Vector3.zero;

        HandleCollisionsAndMovement();

        _jumpVelocityISBefore = _jumpVelocityIS;
    }

    #region Private Methods
    private void GetDirectionsFromView()
    {
        _forward = Vector3.ProjectOnPlane(_viewDirection, -gravityDirection).normalized;
        _right = Vector3.Cross(-gravityDirection, _forward);
    }

    private void CalculateObjectSpaceVariables()
    {
        _jumpVelocityOS = _isGrounded && _jumpVelocityIS > 0 ? _jumpSpeed * (-gravityDirection): Vector3.zero;
        _jumpVelocityOS += _gravity * Time.fixedDeltaTime * 0.5f;

        _playerHeightOS = _crouchInputIS > 0 ? _crouchHeight : _idleHeight;

        float moveSpeedMultiplier = _crouchInputIS > 0f ? _crouchSpeedMultiplier : (_sprintInputIS > 0 ? _sprintSpeedMultiplier : 1f);
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
        if (!_isGrounded) {
            _groundNormal = -gravityDirection;
            _jumpVelocityWS += _gravity * Time.fixedDeltaTime;
            if (_isJumpStarted) {
                if (_airJump.Value-- > 0) {
                    _jumpVelocityWS = _jumpVelocityIS * _jumpSpeed * (-gravityDirection) + _gravity * Time.fixedDeltaTime * 0.5f;
                }
            }
            else if (_isGroundedBefore && _jumpVelocityIS == 0 && _jumpVelocityISBefore == 0) {
                _jumpVelocityWS = groundExitDisplacement / Time.fixedDeltaTime;
                Debug.Log(groundExitDisplacement);
                groundExitDisplacement = Vector3.zero;
            }
        }
        else _jumpVelocityWS = _jumpVelocityOS;

        _moveVelocityWS = (_moveVelocityTS - Vector3.Dot(_groundNormal, _moveVelocityTS) / Vector3.Dot(_groundNormal, -gravityDirection) * (-gravityDirection)).normalized * _moveVelocityTS.magnitude;
        _moveVelocityWS += _externalGroundMove;

        //_jumpVelocityWS += _externalAcceleration;
        //_externalVelocity = Vector3.zero;

        _externalVelocity += _externalAcceleration * Time.fixedDeltaTime;
        _externalVelocity *= Mathf.Exp(-Time.fixedDeltaTime);
        _jumpVelocityWS += _externalVelocity;
        _playerHeightWS = _playerHeightOS * transform.localScale.y;
    }

    private void HandleCollisionsAndMovement()
    {
        if (_isPositionSet) {
            _jumpVelocityWS = _jumpVelocityOS = _gravity * Time.fixedDeltaTime * 0.5f;
            _rigidbody.MovePosition(_externalPosition);
            _isPositionSet = false;
            return;
        }

        _groundedDepth = 0;

        _horizontalDisplacement = CollideAndSlide(_moveVelocityWS * Time.fixedDeltaTime, _rigidbody.position + _height.Value * _playerUp.normalized * 0.5f, 0, false);
        _verticalDisplacement = CollideAndSlide(_jumpVelocityWS * Time.fixedDeltaTime, _rigidbody.position + _height.Value * _playerUp.normalized * 0.5f + _horizontalDisplacement, 0, true);
        Debug.DrawRay(transform.position, _moveVelocityWS * 10f, Color.green);
        Debug.DrawRay(transform.position, _jumpVelocityWS * 10f, Color.red);

        Debug.DrawRay(_rigidbody.position, _horizontalDisplacement, Color.green, 1f);
        Debug.DrawRay(_rigidbody.position + _horizontalDisplacement, _verticalDisplacement, Color.red, 1f);

        if (!_isGrounded && _isGroundedBefore) {
            groundExitDisplacement = _verticalDisplacement + Vector3.Dot(_horizontalDisplacement, gravityDirection) * gravityDirection;
        }

        _displacement = _horizontalDisplacement + _verticalDisplacement;
        _nextPositionWS = _displacement + _rigidbody.position;
        
        _rigidbody.MovePosition(_nextPositionWS);
        Debug.DrawRay(transform.position, Velocity, Color.white);
    }

    private Vector3 CollideAndSlide(Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit = default)
    {
        if (depth >= _maxBounces) return Vector3.zero;
        
        if (depth == 0)
        {
            velInit = vel;
            if (!gravityPass) {
                _isGroundedBefore = _isGrounded;
                _isGrounded = false;
            }
        }

        float dist = vel.magnitude + _skinWidth;
        Vector3 capsulePoint = (_height.Value * 0.5f - _radius.Value) * _playerUp.normalized;
        Vector3 characterLowestPosition = pos - capsulePoint + gravityDirection * _radius.Value;

        if (Physics.CapsuleCast(pos + capsulePoint, pos - capsulePoint, _radius.Value + _skinWidth, vel.normalized, out hit, dist, _whatIsGround, queryTrigger))
        {
            if (hit.collider.isTrigger) return CollideAndSlide(vel, pos, depth+1, gravityPass);
            Vector3 snapToSurface = vel.normalized * (hit.distance - _skinWidth);
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(-gravityDirection, hit.normal);

            // the terrain is inside the collider
            if (snapToSurface.magnitude <= _skinWidth) snapToSurface = Vector3.zero;

            // if flat ground or slope
            if (angle <= _maxSlopeAngle || _isStep)
            {
                _isGrounded = true;
                _groundNormal = (_groundNormal * _groundedDepth + hit.normal).normalized;
                _groundedDepth++;

                if (gravityPass)
                {
                    if (angle <= _maxSlopeAngle) _isStep = false;

                    return snapToSurface;
                }
                
                leftover = projectAndScale(leftover, _groundNormal);
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
                Vector3 flatHit = Vector3.ProjectOnPlane(hit.normal, gravityDirection).normalized;

                if (gravityPass) {
                    if (beforeWallNormal != Vector3.zero && Vector3.Dot(beforeWallNormal, vel) <= 0) {
                        _isGrounded = true;
                        _groundNormal = -gravityDirection;
                    }
                    beforeWallNormal = hit.normal;
                }
                
                float scale = 1 - Vector3.Dot(flatHit, -Vector3.ProjectOnPlane(velInit, gravityDirection).normalized);
                
                if (_isGrounded && !gravityPass)
                { 
                    leftover = projectAndScale(Vector3.ProjectOnPlane(leftover, gravityDirection), -Vector3.ProjectOnPlane(hit.normal, gravityDirection)).normalized * scale;
                }
                else
                {
                    leftover = projectAndScale(leftover, hit.normal) * scale;
                }

                // if upStep
                if (_isUpStepEnabled && _isGroundedBefore && !gravityPass) {
                    Vector3 start = hit.point + Vector3.up - flatHit * 0.01f;

                    bool b = Physics.Raycast(start, gravityDirection, out RaycastHit h, _maxStepUpHeight + 1f, _whatIsGround, queryTrigger);

                    float upStepDistance = Vector3.Dot(h.point - characterLowestPosition, _playerUp);
                    
                    if (b && upStepDistance <= _maxStepUpHeight && Vector3.Angle(h.normal, -gravityDirection) <= MaxSlopeAngle) {
                        _isStep = true;

                        snapToSurface = vel.normalized * dist + upStepDistance * (-gravityDirection);
                        leftover = Vector3.zero;

                        _jumpVelocityWS += _maxStepDownHeight * gravityDirection / Time.fixedDeltaTime;
                    }

                }
            }

            return snapToSurface + CollideAndSlide(leftover, pos + snapToSurface, depth + 1, gravityPass, velInit);
        } 
        
        else if (gravityPass && !_isGrounded && _isGroundedBefore && _jumpVelocityIS == 0) 
        {
            if (_isDownStepEnabled) {
                bool b = Physics.Raycast(pos + _moveVelocityWS * Time.fixedDeltaTime, gravityDirection, out RaycastHit h, _maxStepDownHeight + _height.Value * 0.5f, _whatIsGround, queryTrigger);
                bool b1 = Physics.SphereCast(pos + _moveVelocityWS * Time.fixedDeltaTime + capsulePoint, _capsuleCollider.bounds.extents.x, -_playerUp.normalized, out RaycastHit h2, capsulePoint.magnitude * 2f, _whatIsGround, queryTrigger);

                if (b && !b1 && h.distance > _height.Value * 0.5f + _skinWidth && Vector3.Angle(_playerUp, h.normal) <= MaxSlopeAngle) {
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
        jumpSpeed.OnUpdate(_jumpSpeed);
        _airJump.OnUpdate();
        jumpMaxHeight.OnUpdate(_jumpMaxHeight);
        gravity.OnUpdate(_gravity);

        if (_height.IsChanged)
        {
            _capsuleCollider.height = _height.Value - _skinWidth * 2f;
            _capsuleCollider.transform.localPosition = _playerUp.normalized * _height.Value * 0.5f;
        }

        if (_radius.IsChanged)
        {
            _capsuleCollider.radius = _radius.Value - _skinWidth;
        }

        if (jumpSpeed.IsChanged)
        {
            _jumpMaxHeight = jumpSpeed.Value * jumpSpeed.Value * 0.5f / Mathf.Abs(_gravity.y);
            jumpMaxHeight = new Float(_jumpMaxHeight);
        }
        else if (jumpMaxHeight.IsChanged)
        {
            _jumpSpeed = Mathf.Sqrt(2f * Mathf.Abs(_gravity.y) * jumpMaxHeight.Value);
            jumpSpeed = new Float(_jumpSpeed);
        }
    }
    #endregion

    #region Public Methods
        public void AddForce(Vector3 force, ForceMode forceMode = ForceMode.Force) {
            _externalAcceleration = force / _rigidbody.mass;
        }

        public void SetPosition(Vector3 position) {
            _externalPosition = position;
            _isPositionSet = true;
        }
    #endregion
}