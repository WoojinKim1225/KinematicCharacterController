using UnityEngine;
using UnityEngine.InputSystem;
using ReferenceManager;
using StatefulVariables;
using KinematicCharacterSettings;
using System.Collections;
using Unity.VisualScripting;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public struct Capsule {
    public Vector3 pointUp, pointDown;
    public float radius;
}

[RequireComponent(typeof(Rigidbody))]
public class KinematicCharacterController : MonoBehaviour
{
    #region Variables

    [SerializeField] private ComponentSettings _componentSettings = new ComponentSettings();
    
    public new Rigidbody rigidbody => _componentSettings._rigidbody;
    private Vector3 _rigidbodyPosition => _componentSettings._rigidbody.transform.position;

    /*********************************************************************************************************/

    [SerializeField] private MovementSettings _movementSettings = new MovementSettings();

    public Vector3 ViewDirection { get => _movementSettings._viewDirection; set => _movementSettings._viewDirection = value; }
    public void SetViewDirection(Vector3 dir) => _movementSettings._viewDirection = dir;

    public float MoveSpeed => _movementSettings._moveSpeed;
    public KinematicCharacterSettingExtensions.ESpeedControlMode SpeedControlMode => _movementSettings._speedControlMode;
    public KinematicCharacterSettingExtensions.EMovementMode MovementMode {set => _movementSettings._movementMode = value; }
    public float JumpMaxHeight => _movementSettings._jumpMaxHeight;

    private Float jumpSpeed, jumpMaxHeight;
    private Float _airJump;

    /*********************************************************************************************************/

    [SerializeField] private PhysicsSettings _physicsSettings = new PhysicsSettings();

    private float _skinWidth => _physicsSettings._skinWidth;
    public Vector3 Gravity {get => _physicsSettings._gravity; set => _physicsSettings._gravity = value; }

    private Vector3Stateful _gravity;
    private Vector3 gravityDirection => _physicsSettings._gravity.normalized;
    private Vector3 accelerationDirection => (_physicsSettings._gravity + _externalMovementSettings._acceleration).normalized;

    /*********************************************************************************************************/

    [SerializeField] private CharacterSizeSettings _characterSizeSettings = new CharacterSizeSettings();

    public float IdleHeight => _characterSizeSettings._idleHeight;
    public float CrouchHeight => _characterSizeSettings._crouchHeight;
    public float CapsuleRadius => _characterSizeSettings._capsuleRadius;
    public float CapsuleHeight => _height.Value;

    private Float _height, _radius;
    public float HeightValue => _height.Value;

    /*********************************************************************************************************/

    [SerializeField] private StepAndSlopeHandleSettings _stepAndSlopeHandleSettings = new StepAndSlopeHandleSettings();


    public float MaxSlopeAngle => _stepAndSlopeHandleSettings._maxSlopeAngle;
    public bool IsUpStepEnabled {get => _stepAndSlopeHandleSettings._isUpStepEnabled; set => _stepAndSlopeHandleSettings._isUpStepEnabled = value; }
    public bool IsDownStepEnabled {get => _stepAndSlopeHandleSettings._isDownStepEnabled; set => _stepAndSlopeHandleSettings._isDownStepEnabled = value; }

    private Vector3 _forward, _right;
    public Vector3 Up => _playerUp.normalized;
    public Vector3 Forward => _forward;
    public Vector3 Right => _right;

    private Coordinate<Vector2, Vector3, Vector3, Vector3> _moveVelocity;
    private Coordinate<Float, Vector3, Null, Vector3> _jumpVelocity;
    private Coordinate<float, float, Null, float> _playerHeight;
    private Coordinate<float, Null, Null, Null> _sprintInput;
    
    public Vector3 MoveVelocityIS { set => _moveVelocity.IS = value; }
    public float JumpVelocityIS { set => _jumpVelocity.IS.Value = value; }
    public float SprintInputIS { set => _sprintInput.IS = value; }
    public float PlayerHeightIS { set => _playerHeight.IS = value; }

    private bool _isJumpStarted => _jumpVelocity.IS.Value != 0 && _jumpVelocity.IS.BeforeValue == 0;

    private Vector3 _horizontalDisplacement, _verticalDisplacement;
    private Vector3 _displacement;

    public Vector3 Displacement => _displacement;
    public Vector3 Velocity => _displacement / Time.fixedDeltaTime;
    public Vector3 HorizontalVelocity => _horizontalDisplacement / Time.fixedDeltaTime;
    public Vector3 VerticalVelocity => _verticalDisplacement / Time.fixedDeltaTime;

    public Vector3 HorizontalDirection => _horizontalDisplacement - ExternalGroundMove * Time.fixedDeltaTime;
    public float Speed => HorizontalDirection.magnitude / Time.fixedDeltaTime;

    [SerializeField] private ExternalMovementSettings _externalMovementSettings = new ExternalMovementSettings();

    public Vector3 ExternalGroundMove { get => _externalMovementSettings._groundMove; set => _externalMovementSettings._groundMove = value; }

    private Vector3 _nextPositionWS;

    //[SerializeField] private bool _isGrounded = false;
    //private bool _isGroundedBefore;
    private Bool _isGrounded;
    private int _groundedDepth;
    private Vector3 beforeWallNormal = Vector3.zero;

    [SerializeField] private Vector3 _groundNormal = Vector3.up;
    [SerializeField] private Vector3 _playerUp = Vector3.up;

    private RaycastHit hit;
    
    public LayerMask WhatIsGround => _physicsSettings._whatIsGround;
    private Vector3 groundExitDisplacement;

    private bool _isStep;

    readonly QueryTriggerInteraction queryTrigger = QueryTriggerInteraction.Ignore;
    readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

    public Dictionary<GameObject, Vector3> accelerationGive, impulseGive;

    public List<Capsule> positions;

    #endregion

    void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _componentSettings._rigidbody = GetComponent<Rigidbody>();
        _componentSettings._capsuleCollider = GetComponentInChildren<CapsuleCollider>();

        _componentSettings._rigidbody.detectCollisions = true;
        _componentSettings._rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _componentSettings._rigidbody.isKinematic = true;

        InitStateful();

        _componentSettings._capsuleCollider.bounds.Expand(-2 * _physicsSettings._skinWidth);
        _componentSettings._capsuleCollider.height = _height.Value - 2 * _physicsSettings._skinWidth;
        _componentSettings._capsuleCollider.radius = _radius.Value - _physicsSettings._skinWidth;

        accelerationGive = new Dictionary<GameObject, Vector3>();
        impulseGive = new Dictionary<GameObject, Vector3>();

        positions = new List<Capsule>();
    }

    void Update()
    {
        GetDirectionsFromView();
    }

    void FixedUpdate()
    {
        positions.Clear();

        if (_isGrounded.Value) _airJump.Value = _airJump.InitialValue;

        _componentSettings._capsuleCollider.transform.up = _playerUp.normalized;

        CalculateObjectSpaceVariables();

        CalculateTangentSpaceVariables();

        CalculateWorldSpaceVariables();

        UpdateProperties();

        beforeWallNormal = Vector3.zero;

        HandleCollisionsAndMovement();
    }

    #region Private Methods

    private void InitStateful() {
        _isGrounded = new Bool(false);
        
        _height = new Float(IdleHeight);
        _radius = new Float(CapsuleRadius);

        jumpSpeed = new Float(_movementSettings._jumpSpeed);
        jumpMaxHeight = new Float(Mathf.Sqrt(_movementSettings._jumpSpeed * _movementSettings._jumpSpeed * 0.5f / Mathf.Abs(Gravity.y)));
        _movementSettings._jumpMaxHeight = _movementSettings._jumpSpeed * _movementSettings._jumpSpeed * 0.5f / Mathf.Abs(Gravity.y);

        _airJump = new Float(_movementSettings._maxAirJumpCount);

        _gravity = new Vector3Stateful(Gravity);
        _jumpVelocity.IS = new Float(0);
    }

    private void GetDirectionsFromView()
    {
        switch (_movementSettings._movementMode)
        {
            case KinematicCharacterSettingExtensions.EMovementMode.Ground:
                _forward = Vector3.ProjectOnPlane(_movementSettings._viewDirection, -gravityDirection).normalized;
                _right = Vector3.Cross(-gravityDirection, _forward);
                break;
            case KinematicCharacterSettingExtensions.EMovementMode.Swim:
                _forward = _movementSettings._viewDirection.normalized;
                _right = Vector3.Cross(-gravityDirection, _forward);
                break;
        }
    }

    private void CalculateObjectSpaceVariables()
    {
        _jumpVelocity.OS = _isGrounded.Value && _jumpVelocity.IS.Value > 0 ? _movementSettings._jumpSpeed * (-gravityDirection) : Vector3.zero;
        _jumpVelocity.OS += Gravity * Time.fixedDeltaTime * 0.5f;

        _playerHeight.OS = _playerHeight.IS > 0 ? CrouchHeight : IdleHeight;

        float moveSpeedMultiplier = _playerHeight.IS > 0f ? _movementSettings._crouchSpeedMultiplier : (_sprintInput.IS > 0 ? _movementSettings._sprintSpeedMultiplier : 1f);
        Vector3 _moveVelocityTargetOS = new Vector3(_moveVelocity.IS.x, 0, _moveVelocity.IS.y) * _movementSettings._moveSpeed * moveSpeedMultiplier;
        
        switch (_movementSettings._speedControlMode)
        {
            case KinematicCharacterSettingExtensions.ESpeedControlMode.Constant:
                _moveVelocity.OS = _moveVelocityTargetOS;
                break;
            case KinematicCharacterSettingExtensions.ESpeedControlMode.Linear:
                if ((_moveVelocity.OS - _moveVelocityTargetOS).magnitude < Time.fixedDeltaTime * _movementSettings._moveAcceleration)
                {
                    _moveVelocity.OS = _moveVelocityTargetOS;
                }
                else
                {
                    _moveVelocity.OS += (_moveVelocityTargetOS - _moveVelocity.OS).normalized * Time.fixedDeltaTime * _movementSettings._moveAcceleration;
                }
                break;
            case KinematicCharacterSettingExtensions.ESpeedControlMode.Exponential:
                if ((_moveVelocity.OS - _moveVelocityTargetOS).magnitude < Mathf.Epsilon)
                {
                    _moveVelocity.OS = _moveVelocityTargetOS;
                }
                else
                {
                    _moveVelocity.OS += (_moveVelocityTargetOS - _moveVelocity.OS) * Time.fixedDeltaTime * _movementSettings._moveDamp;
                }
                break;
            default:
                _moveVelocity.OS = _moveVelocityTargetOS;
                break;
        }
        
    }

    private void CalculateTangentSpaceVariables()
    {
        _moveVelocity.TS = _moveVelocity.OS.x * _right + _moveVelocity.OS.z * _forward;
    }

    private void CalculateWorldSpaceVariables()
    {
        if (_externalMovementSettings._velocity != Vector3.zero)
        {
            _jumpVelocity.WS = _externalMovementSettings._velocity;
        }

        if (!_isGrounded.Value)
        {
            _groundNormal = -gravityDirection;
            _jumpVelocity.WS += Gravity * Time.fixedDeltaTime;
            if (_isJumpStarted && _airJump.Value-- > 0)
            {
                 _jumpVelocity.WS = _jumpVelocity.IS.Value * _movementSettings._jumpSpeed * (-gravityDirection) + Gravity * Time.fixedDeltaTime * 0.5f;
            }
            else if (_isGrounded.BeforeValue && _jumpVelocity.IS.Value == 0 && _jumpVelocity.IS.BeforeValue == 0)
            {
                _jumpVelocity.WS = groundExitDisplacement / Time.fixedDeltaTime;
                groundExitDisplacement = Vector3.zero;
            }
        }
        else _jumpVelocity.WS = _jumpVelocity.OS;


        if (_movementSettings._movementMode == KinematicCharacterSettingExtensions.EMovementMode.Ground)
        {
            _moveVelocity.WS = (_moveVelocity.TS - Vector3.Dot(_groundNormal, _moveVelocity.TS) / Vector3.Dot(_groundNormal, -gravityDirection) * (-gravityDirection)).normalized * _moveVelocity.TS.magnitude;
        }
        else
        {
            _moveVelocity.WS = _moveVelocity.TS;
        }

        _moveVelocity.WS += _externalMovementSettings._groundMove;

        _externalMovementSettings._acceleration = accelerationGive.Aggregate(Vector3.zero, (acc, val) => acc + val.Value)
            + impulseGive.Aggregate(Vector3.zero, (acc, val) => acc + val.Value);

        _jumpVelocity.WS += Vector3.Project(_externalMovementSettings._acceleration, gravityDirection) * Time.fixedDeltaTime;
        _moveVelocity.WS += Vector3.ProjectOnPlane(_externalMovementSettings._acceleration, gravityDirection) * Time.fixedDeltaTime;

        _playerHeight.WS = _playerHeight.OS * transform.localScale.y;
    }

    private void HandleCollisionsAndMovement()
    {
        if (_externalMovementSettings._isPositionSet)
        {
            _jumpVelocity.WS = _jumpVelocity.OS = Gravity * Time.fixedDeltaTime * 0.5f;
            _componentSettings._rigidbody.MovePosition(_externalMovementSettings._position);
            _externalMovementSettings._isPositionSet = false;
            return;
        }

        _groundedDepth = 0;

        _horizontalDisplacement = CollideAndSlide(_moveVelocity.WS * Time.fixedDeltaTime, _rigidbodyPosition + _height.Value * _playerUp.normalized * 0.5f, 0, false);
        _verticalDisplacement = CollideAndSlide(_jumpVelocity.WS * Time.fixedDeltaTime, _rigidbodyPosition + _height.Value * _playerUp.normalized * 0.5f + _horizontalDisplacement, 0, true);

        if (!_isGrounded.Value && _isGrounded.BeforeValue)
        {
            groundExitDisplacement = _verticalDisplacement + Vector3.Dot(_horizontalDisplacement, gravityDirection) * gravityDirection;
        }

        _displacement = _horizontalDisplacement + _verticalDisplacement;
        _nextPositionWS = _displacement + _rigidbodyPosition;

        _componentSettings._rigidbody.MovePosition(_nextPositionWS);
    }

    private Vector3 CollideAndSlide(Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit = default)
    {
        if (depth >= _physicsSettings._maxBounces) return Vector3.zero;

        if (depth == 0)
        {
            velInit = vel;
            if (!gravityPass)
            {
                //_isGroundedBefore = _isGrounded;
                //_isGrounded = false;
                _isGrounded.OnUpdate(false);
            }
        }

        float dist = vel.magnitude + _physicsSettings._skinWidth;
        Vector3 capsulePoint = (_height.Value * 0.5f - _radius.Value) * _playerUp.normalized;
        Vector3 characterLowestPosition = pos - capsulePoint + gravityDirection * _radius.Value;
        
        positions.Add(new Capsule{pointUp = pos + capsulePoint, pointDown = pos - capsulePoint, radius = _radius.Value});

        if (Physics.CapsuleCast(pos + capsulePoint, pos - capsulePoint, _radius.Value + _physicsSettings._skinWidth, vel.normalized, out hit, dist, _physicsSettings._whatIsGround, queryTrigger))
        {
            if (hit.collider.isTrigger) return CollideAndSlide(vel, pos, depth + 1, gravityPass);
            Vector3 snapToSurface = vel.normalized * (hit.distance - _skinWidth);
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(-gravityDirection, hit.normal);

            // the terrain is inside the collider
            if (snapToSurface.magnitude <= _skinWidth) snapToSurface = Vector3.zero;

            // if flat ground or slope
            if (angle <= _stepAndSlopeHandleSettings._maxSlopeAngle || _isStep)
            {
                _isGrounded.Value = true;
                _groundNormal = (_groundNormal * _groundedDepth + hit.normal).normalized;
                _groundedDepth++;

                if (gravityPass)
                {
                    if (angle <= _stepAndSlopeHandleSettings._maxSlopeAngle) _isStep = false;

                    return snapToSurface;
                }

                leftover = projectAndScale(leftover, _groundNormal);
            }
            // if ceiling, cancel upwards velocity
            else if (angle >= _stepAndSlopeHandleSettings._minCeilingAngle)
            {
                _jumpVelocity.WS = Vector3.zero;
                return snapToSurface;
            }
            // if wall
            else
            {
                Vector3 flatHit = Vector3.ProjectOnPlane(hit.normal, gravityDirection).normalized;

                if (gravityPass)
                {
                    if (beforeWallNormal != Vector3.zero && Vector3.Dot(beforeWallNormal, vel) <= 0)
                    {
                        _isGrounded.Value = true;
                        _groundNormal = -gravityDirection;
                    }
                    beforeWallNormal = hit.normal;
                }

                float scale = 1 - Vector3.Dot(flatHit, -Vector3.ProjectOnPlane(velInit, gravityDirection).normalized);

                if (_isGrounded.Value && !gravityPass)
                {
                    leftover = projectAndScale(Vector3.ProjectOnPlane(leftover, gravityDirection), -Vector3.ProjectOnPlane(hit.normal, gravityDirection)).normalized * scale;
                }
                else
                {
                    leftover = projectAndScale(leftover, hit.normal) * scale;
                }

                // if upStep
                if (_stepAndSlopeHandleSettings._isUpStepEnabled && _isGrounded.BeforeValue && !gravityPass)
                {
                    Vector3 start = hit.point + Vector3.up - flatHit * 0.01f;

                    bool b = Physics.Raycast(start, gravityDirection, out RaycastHit h, _stepAndSlopeHandleSettings._maxStepUpHeight + 1f, _physicsSettings._whatIsGround, queryTrigger);

                    float upStepDistance = Vector3.Dot(h.point - characterLowestPosition, _playerUp);

                    if (b && upStepDistance <= _stepAndSlopeHandleSettings._maxStepUpHeight && Vector3.Angle(h.normal, -gravityDirection) <= MaxSlopeAngle)
                    {
                        _isStep = true;

                        snapToSurface = vel.normalized * dist + upStepDistance * (-gravityDirection);
                        leftover = Vector3.zero;

                        _jumpVelocity.WS += _stepAndSlopeHandleSettings._maxStepDownHeight * gravityDirection / Time.fixedDeltaTime;
                    }

                }
            }

            return snapToSurface + CollideAndSlide(leftover, pos + snapToSurface, depth + 1, gravityPass, velInit);
        }

        else if (gravityPass && !_isGrounded.Value && _isGrounded.BeforeValue && _jumpVelocity.IS.Value == 0)
        {
            if (_stepAndSlopeHandleSettings._isDownStepEnabled)
            {
                bool b = Physics.Raycast(pos + _moveVelocity.WS * Time.fixedDeltaTime, gravityDirection, out RaycastHit h, _stepAndSlopeHandleSettings._maxStepDownHeight + _height.Value * 0.5f, _physicsSettings._whatIsGround, queryTrigger);
                bool b1 = Physics.SphereCast(pos + _moveVelocity.WS * Time.fixedDeltaTime + capsulePoint, _componentSettings._capsuleCollider.bounds.extents.x, -_playerUp.normalized, out RaycastHit h2, capsulePoint.magnitude * 2f, _physicsSettings._whatIsGround, queryTrigger);

                if (b && !b1 && h.distance > _height.Value * 0.5f + _skinWidth && Vector3.Angle(_playerUp, h.normal) <= MaxSlopeAngle)
                {
                    _isStep = true;

                    return CollideAndSlide(-_playerUp * _stepAndSlopeHandleSettings._maxStepUpHeight, pos, depth + 1, true, velInit);
                }
            }

        }

        return vel;
    }

    private Vector3 projectAndScale(Vector3 a, Vector3 n)
    {
        return Vector3.ClampMagnitude(Vector3.ProjectOnPlane(a, n), a.magnitude);
    }

    private void UpdateProperties()
    {
        _height.OnUpdate(_playerHeight.WS);
        _radius.OnUpdate(CapsuleRadius);
        jumpSpeed.OnUpdate(_movementSettings._jumpSpeed);
        _airJump.OnUpdate();
        jumpMaxHeight.OnUpdate(_movementSettings._jumpMaxHeight);
        _gravity.OnUpdate(Gravity);
        _jumpVelocity.IS.OnUpdate();

        if (_height.IsChanged)
        {
            _componentSettings._capsuleCollider.height = _height.Value - _skinWidth * 2f;
            _componentSettings._capsuleCollider.transform.localPosition = _playerUp.normalized * _height.Value * 0.5f;
        }

        if (_radius.IsChanged)
        {
            _componentSettings._capsuleCollider.radius = _radius.Value - _skinWidth;
        }

        if (jumpSpeed.IsChanged)
        {
            _movementSettings._jumpMaxHeight = jumpSpeed.Value * jumpSpeed.Value * 0.5f / Mathf.Abs(Gravity.y);
            jumpMaxHeight = new Float(_movementSettings._jumpMaxHeight);
        }
        else if (jumpMaxHeight.IsChanged)
        {
            _movementSettings._jumpSpeed = Mathf.Sqrt(2f * Mathf.Abs(Gravity.y) * jumpMaxHeight.Value);
            jumpSpeed = new Float(_movementSettings._jumpSpeed);
        }

        if (_isGrounded.Value) _airJump.Reset();
    }

    private void CollisionPairUpdate(Dictionary<GameObject, Vector3> dict, GameObject from, Vector3 accel, ForceMode forceMode) {
        if (!dict.ContainsKey(from)) {
            if (accel == Vector3.zero) {
                
            } else {
                dict.Add(from, accel);
                if (forceMode == ForceMode.Impulse || forceMode == ForceMode.VelocityChange)
                    StartCoroutine(ExAccelReset(dict, from));
            }
        }
        else {
            if (accel == Vector3.zero) {
                dict.Remove(from);
            }
            else if (forceMode == ForceMode.Impulse || forceMode == ForceMode.VelocityChange) {
                dict[from] += accel;
            } else {
                dict[from] = accel;
            }
        }
    }

    #endregion

    #region Public Methods
    
    public void AddForce(Vector3 force, GameObject from, ForceMode forceMode = ForceMode.Force)
    {
        
        switch (forceMode) {
            case ForceMode.Force:
                CollisionPairUpdate(accelerationGive, from, force/_componentSettings._rigidbody.mass, forceMode);
            break;
            case ForceMode.Impulse:
                CollisionPairUpdate(impulseGive, from, force/_componentSettings._rigidbody.mass, forceMode);
            break;
            case ForceMode.Acceleration:
                CollisionPairUpdate(accelerationGive, from, force, forceMode);
            break;
            case ForceMode.VelocityChange:
                CollisionPairUpdate(impulseGive, from, force, forceMode);
            break;
        }
        
    }
    

    IEnumerator ExAccelReset(Dictionary<GameObject, Vector3> keyValuePairs, GameObject key) {
        while (keyValuePairs[key] != Vector3.zero) {
            yield return waitForFixedUpdate;
            switch (_externalMovementSettings._speedControlMode) {
                case KinematicCharacterSettingExtensions.ESpeedControlMode.Constant:
                    keyValuePairs[key] = Vector3.zero;
                    keyValuePairs.Remove(key);
                    yield break;
                case KinematicCharacterSettingExtensions.ESpeedControlMode.Linear:
                    if (keyValuePairs[key].magnitude <= _externalMovementSettings._moveAcceleration * Time.fixedDeltaTime) {
                        keyValuePairs[key] = Vector3.zero;
                        keyValuePairs.Remove(key);
                        yield break;
                    }
                    Vector3 horizontalAccel = Vector3.ProjectOnPlane(keyValuePairs[key], gravityDirection);
                    horizontalAccel -= horizontalAccel.normalized * _externalMovementSettings._moveAcceleration * Time.fixedDeltaTime;
                    keyValuePairs[key] = horizontalAccel;
                    break;
                case KinematicCharacterSettingExtensions.ESpeedControlMode.Exponential:
                    if (keyValuePairs[key].magnitude <= _externalMovementSettings._moveAcceleration) {
                        keyValuePairs[key] = Vector3.zero;
                        keyValuePairs.Remove(key);
                        yield break;
                    }
                    horizontalAccel = Vector3.ProjectOnPlane(keyValuePairs[key], gravityDirection);
                    horizontalAccel -= horizontalAccel * _externalMovementSettings._moveAcceleration * Time.fixedDeltaTime;
                    keyValuePairs[key] = horizontalAccel;
                    break;
                default:
                    keyValuePairs[key] = Vector3.zero;
                    keyValuePairs.Remove(key);
                    yield break;
            }
        }
        yield return null;
    }

    public void SetVelocity(Vector3 velocity)
    {
        _externalMovementSettings._velocity = velocity;
    }

    public void SetPosition(Vector3 position)
    {
        _externalMovementSettings._position = position;
        _externalMovementSettings._isPositionSet = true;
    }
    #endregion
}