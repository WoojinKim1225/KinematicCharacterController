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


public class KinematicCharacterController : MonoBehaviour
{
    #region Variables

    [SerializeField] private ComponentSettings _componentSettings = new ComponentSettings();
    private Vector3 _rigidbodyPosition => _componentSettings._dimension == KinematicCharacterSettingExtensions.EDimension.ThreeDimension ? _componentSettings._rigidbody.transform.position : _componentSettings._rigidbody2D.transform.position;
    public float mass => _componentSettings._dimension == KinematicCharacterSettingExtensions.EDimension.ThreeDimension ? _componentSettings._rigidbody.mass : _componentSettings._rigidbody2D.mass;

    [SerializeField] private MovementSettings _movementSettings = new MovementSettings();

    public Vector3 ViewDirection { get => _movementSettings._viewDirection; set => _movementSettings._viewDirection = value; }
    public void SetViewDirection(Vector3 dir) => _movementSettings._viewDirection = dir;

    public float MoveSpeed => _movementSettings._moveSpeed;
    public KinematicCharacterSettingExtensions.ESpeedControlMode SpeedControlMode => _movementSettings._speedControlMode;
    public KinematicCharacterSettingExtensions.EMovementMode MovementMode {set => _movementSettings._movementMode = value; }
    public float JumpMaxHeight => _movementSettings._jumpMaxHeight;

    private Float jumpSpeedStateful, jumpMaxHeightStateful;
    private Float _airJumpStateful;

    [SerializeField] private PhysicsSettings _physicsSettings = new PhysicsSettings();

    private float _skinWidth => _physicsSettings._skinWidth;

    private Vector3Stateful _gravityStateful;
    private Vector3 gravityDirection => _gravityStateful.Value.normalized;
    private Vector3 accelerationDirection => (_physicsSettings._gravity + _externalMovementSettings._acceleration).normalized;

    /*********************************************************************************************************/

    [SerializeField] private CharacterSizeSettings _characterSizeSettings = new CharacterSizeSettings();

    public float IdleHeight => _characterSizeSettings._idleHeight;
    public float CrouchHeight => _characterSizeSettings._crouchHeight;
    public float CapsuleRadius => _characterSizeSettings._capsuleRadius;
    public float CapsuleHeight => _heightStateful.Value;

    private Float _heightStateful, _radiusStateful;
    public float HeightValue => _heightStateful.Value;

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
    //private Vector3 _externalVelocityWS;
    
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

//-------------------------------------------------------------------

    [SerializeField] private ExternalMovementSettings _externalMovementSettings = new ExternalMovementSettings();

    public Vector3 ExternalGroundMove { get => _externalMovementSettings._groundMove; set => _externalMovementSettings._groundMove = value; }

    private Vector2Stateful _externalDragStateful;

    public float ExternalContactDrag {set => _externalMovementSettings._contactDrag = value; }
    public float ExternalAirDrag {set => _externalMovementSettings._airDrag = value; }

    public void ExternalDragReset() {
        _externalMovementSettings._contactDrag = _externalDragStateful.InitialValue.x;
        _externalMovementSettings._airDrag = _externalDragStateful.InitialValue.y;
        _externalDragStateful.Reset();
    }


    private Vector3 _nextPositionWS;

    private Bool _isGrounded;
    private Bool _isCollidedStateful;

    private bool _isCollidedHorizontal, _isCollidedVertical;
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

    //public Dictionary<Object, Vector3> accelerationGive;//, impulseGive;
    public Vector3 accelerationGive;
    public Vector3 impulseGive;

    public List<Capsule> positions;

    #endregion

    void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        

        switch (_componentSettings._dimension) {
            case KinematicCharacterSettingExtensions.EDimension.TwoDimension:
                _componentSettings._rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
                _componentSettings._rigidbody2D.mass = 1f;
                _componentSettings._rigidbody2D.isKinematic = true;
                _componentSettings._rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
                _componentSettings._rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                GameObject colliderObject = new GameObject("Collider", typeof(CapsuleCollider2D));
                colliderObject.transform.SetParent(transform);
                _componentSettings._capsuleCollider2D = colliderObject.GetComponent<CapsuleCollider2D>();
                _componentSettings._capsuleCollider2D.transform.localPosition = Vector3.up;
                _componentSettings._capsuleCollider2D.size = new Vector2(1f, 2f);
                _componentSettings._capsuleCollider2D.bounds.Expand(-2 * _physicsSettings._skinWidth);
                break;
            case KinematicCharacterSettingExtensions.EDimension.ThreeDimension:
                _componentSettings._rigidbody = gameObject.AddComponent(typeof(Rigidbody)) as Rigidbody;
                _componentSettings._rigidbody.mass = 1f;
                _componentSettings._rigidbody.detectCollisions = true;
                _componentSettings._rigidbody.isKinematic = true;
                _componentSettings._rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                _componentSettings._rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                colliderObject = new GameObject("Collider", typeof(CapsuleCollider));
                colliderObject.transform.SetParent(transform);
                _componentSettings._capsuleCollider = colliderObject.GetComponent<CapsuleCollider>();
                _componentSettings._capsuleCollider.transform.localPosition = Vector3.up;
                _componentSettings._capsuleCollider.height = 2f;
                _componentSettings._capsuleCollider.radius = 0.5f;
                _componentSettings._capsuleCollider.bounds.Expand(-2 * _physicsSettings._skinWidth);
                break;
            default:
                break;
        }

        InitStateful();

        //accelerationGive = new Dictionary<Object, Vector3>();
        //impulseGive = new Dictionary<Object, Vector3>();
    }

    void Update()
    {
        GetDirectionsFromView();
    }

    void FixedUpdate()
    {
        positions.Clear();

        UpdateGravity();

        if (_isGrounded.Value) _airJumpStateful.Value = _airJumpStateful.InitialValue;

        _componentSettings._capsuleCollider.transform.up = _playerUp.normalized;

        CalculateObjectSpaceVariables();

        CalculateTangentSpaceVariables();

        CalculateWorldSpaceVariables();

        UpdateProperties();

        beforeWallNormal = Vector3.zero;

        HandleCollisionsAndMovement();
        accelerationGive = Vector3.zero;

        impulseGive = Vector3.zero;
    }

    #region Private Methods

    private void InitStateful() {
        _isGrounded = new Bool(false);
        _isCollidedStateful = new Bool(false);
        
        _heightStateful = new Float(IdleHeight);
        _radiusStateful = new Float(CapsuleRadius);

        jumpSpeedStateful = new Float(_movementSettings._jumpSpeed);
        jumpMaxHeightStateful = new Float(Mathf.Sqrt(_movementSettings._jumpSpeed * _movementSettings._jumpSpeed * 0.5f / Mathf.Abs(_physicsSettings._gravity.y)));
        _movementSettings._jumpMaxHeight = _movementSettings._jumpSpeed * _movementSettings._jumpSpeed * 0.5f / Mathf.Abs(_physicsSettings._gravity.y);

        _airJumpStateful = new Float(_movementSettings._maxAirJumpCount);

        _gravityStateful = new Vector3Stateful(_physicsSettings._gravity);
        _jumpVelocity.IS = new Float(0);

        _externalDragStateful = new Vector2Stateful(_externalMovementSettings._contactDrag, _externalMovementSettings._airDrag);
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

    private void UpdateGravity() {
        if (_physicsSettings._gravityMode == KinematicCharacterSettingExtensions.EGravityMode.Single) {
            _gravityStateful.Value = _physicsSettings._gravity;
        } else {
            float verticalSpeed = _verticalDisplacement.y / Time.fixedDeltaTime;
            _gravityStateful.Value = _physicsSettings._gravityList[0].gravity;
            for (int i = 1; i < _physicsSettings._gravityList.Length; i++) {
                if (_physicsSettings._gravityList[i].verticalSpeedThreshold > verticalSpeed) {
                    _gravityStateful.Value = _physicsSettings._gravityList[i].gravity;
                }
            }
        }
    }

    private void CalculateObjectSpaceVariables()
    {
        _jumpVelocity.OS = _isGrounded.Value && _jumpVelocity.IS.Value > 0 ? _movementSettings._jumpSpeed * (-gravityDirection) : Vector3.zero;
        _jumpVelocity.OS += _gravityStateful.Value * Time.fixedDeltaTime * 0.5f;

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
    private Vector3 _externalVelocityBefore;
    private void CalculateWorldSpaceVariables()
    {
        if (!_isGrounded.Value)
        {
            _groundNormal = -gravityDirection;
            if (_isJumpStarted && _airJumpStateful.Value-- > 0)
            {
                _jumpVelocity.WS = _jumpVelocity.IS.Value * _movementSettings._jumpSpeed * (-gravityDirection) + _gravityStateful.Value * Time.fixedDeltaTime * 0.5f;
            }
            else if (_isGrounded.BeforeValue && _jumpVelocity.IS.Value == 0 && _jumpVelocity.IS.BeforeValue == 0)
            {
                _jumpVelocity.WS = groundExitDisplacement / Time.fixedDeltaTime;
                groundExitDisplacement = Vector3.zero;
            } else {
                _jumpVelocity.WS += _gravityStateful.Value * Time.fixedDeltaTime;
            }
        }
        else _jumpVelocity.WS = _jumpVelocity.OS;


        if (_movementSettings._movementMode == KinematicCharacterSettingExtensions.EMovementMode.Ground)
        {
            _moveVelocity.WS = (_moveVelocity.TS - Vector3.Dot(_groundNormal, _moveVelocity.TS) / Vector3.Dot(_groundNormal, -gravityDirection) * (-gravityDirection)).normalized * _moveVelocity.TS.magnitude;
        }
        else _moveVelocity.WS = _moveVelocity.TS;

        _moveVelocity.WS += _externalMovementSettings._groundMove;

        _externalMovementSettings._acceleration = impulseGive + accelerationGive;

        _externalMovementSettings._velocity += _externalMovementSettings._acceleration * Time.fixedDeltaTime;
        float drag = _isGrounded.Value ? _externalDragStateful.Value.x : _externalDragStateful.Value.y;
        if (_externalMovementSettings._velocity.magnitude < Time.fixedDeltaTime * drag)
            _externalMovementSettings._velocity = Vector3.zero;

        _externalMovementSettings._velocity = _externalMovementSettings._velocity * (1 - drag / mass * Time.fixedDeltaTime);

        _jumpVelocity.WS += Vector3.Project(_externalMovementSettings._velocity - _externalVelocityBefore, gravityDirection);
        _moveVelocity.WS += Vector3.ProjectOnPlane(_externalMovementSettings._velocity, gravityDirection);

        _externalVelocityBefore = _externalMovementSettings._velocity;

        _playerHeight.WS = _playerHeight.OS * transform.localScale.y;
    }

    private void HandleCollisionsAndMovement()
    {
        if (_externalMovementSettings._isPositionSet)
        {
            _jumpVelocity.WS = _jumpVelocity.OS = _gravityStateful.Value * Time.fixedDeltaTime * 0.5f;
            _componentSettings._rigidbody.MovePosition(_externalMovementSettings._position);
            _externalMovementSettings._isPositionSet = false;
            return;
        }

        _groundedDepth = 0;

        _horizontalDisplacement = CollideAndSlide(_moveVelocity.WS * Time.fixedDeltaTime, _rigidbodyPosition - _heightStateful.Value * gravityDirection * 0.5f, 0, 0);
        _verticalDisplacement = CollideAndSlide(_jumpVelocity.WS * Time.fixedDeltaTime, _rigidbodyPosition - _heightStateful.Value * gravityDirection * 0.5f + _horizontalDisplacement, 0, 1);



        _isCollidedStateful.Value = _isCollidedHorizontal || _isCollidedVertical;

        if (!_isGrounded.Value && _isGrounded.BeforeValue)
        {
            groundExitDisplacement = _verticalDisplacement + Vector3.Dot(_horizontalDisplacement, gravityDirection) * gravityDirection;
        }

        _displacement = _horizontalDisplacement + _verticalDisplacement;
        _nextPositionWS = _displacement + _rigidbodyPosition;

        _componentSettings._rigidbody.MovePosition(_nextPositionWS);
    }

    private Vector3 CollideAndSlide(Vector3 vel, Vector3 pos, int depth, int state, Vector3 velInit = default)
    {
        if (depth >= _physicsSettings._maxBounces) return Vector3.zero;

        if (depth == 0)
        {
            velInit = vel;
            switch (state) {
                case 0:
                    _isCollidedHorizontal = false;
                    _isGrounded.OnUpdate(false);
                    break;
                case 1:
                    _isCollidedVertical = false;
                    break;
                default:
                    break;
            }
        }

        float dist = vel.magnitude + _physicsSettings._skinWidth;
        Vector3 capsulePoint = (_heightStateful.Value * 0.5f - _radiusStateful.Value) * _playerUp.normalized;
        Vector3 characterLowestPosition = pos - capsulePoint + gravityDirection * _radiusStateful.Value;

        positions.Add(new Capsule{pointUp = pos + vel * 10f + capsulePoint, pointDown = pos + vel * 10f - capsulePoint, radius = _radiusStateful.Value});

        if (Physics.CapsuleCast(pos + capsulePoint, pos - capsulePoint, _radiusStateful.Value + _physicsSettings._skinWidth, vel.normalized, out hit, dist, _physicsSettings._whatIsGround, queryTrigger))
        {
            if (hit.collider.isTrigger) return CollideAndSlide(vel, pos, depth + 1, state);
            Vector3 flatHit = Vector3.ProjectOnPlane(hit.normal, gravityDirection).normalized;
            float scale = 1 - Vector3.Dot(flatHit, -Vector3.ProjectOnPlane(velInit, gravityDirection).normalized);

            switch (state) {
                case 0:
                    _isCollidedHorizontal = true;
                    break;
                case 1:
                    _isCollidedVertical = true;
                    break;
                default:
                    break;
            }

            _externalMovementSettings._velocity = Vector3.ProjectOnPlane(_externalMovementSettings._velocity, hit.normal);

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

                if (state == 1)
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
                if (state == 1)
                {
                    if (beforeWallNormal != Vector3.zero && Vector3.Dot(beforeWallNormal, vel) <= 0)
                    {
                        _isGrounded.Value = true;
                        _groundNormal = -gravityDirection;
                    }
                    beforeWallNormal = hit.normal;
                }



                if (_isGrounded.Value && state == 0)
                {
                    leftover = projectAndScale(Vector3.ProjectOnPlane(leftover, gravityDirection), -Vector3.ProjectOnPlane(hit.normal, gravityDirection)).normalized * scale;
                }
                else
                {
                    leftover = projectAndScale(leftover, hit.normal) * scale;
                }

                // if upStep
                if (_stepAndSlopeHandleSettings._isUpStepEnabled && _isGrounded.BeforeValue && state == 0)
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

            return snapToSurface + CollideAndSlide(leftover, pos + snapToSurface, depth + 1, state, velInit);
        }

        else if (state == 1 && !_isGrounded.Value && _isGrounded.BeforeValue && _jumpVelocity.IS.Value == 0)
        {
            if (_stepAndSlopeHandleSettings._isDownStepEnabled)
            {
                bool b = Physics.Raycast(pos + _moveVelocity.WS * Time.fixedDeltaTime, gravityDirection, out RaycastHit h, _stepAndSlopeHandleSettings._maxStepDownHeight + _heightStateful.Value * 0.5f, _physicsSettings._whatIsGround, queryTrigger);
                bool b1 = Physics.SphereCast(pos + _moveVelocity.WS * Time.fixedDeltaTime + capsulePoint, _componentSettings._capsuleCollider.bounds.extents.x, -_playerUp.normalized, out RaycastHit h2, capsulePoint.magnitude * 2f, _physicsSettings._whatIsGround, queryTrigger);

                if (b && !b1 && h.distance > _heightStateful.Value * 0.5f + _skinWidth && Vector3.Angle(_playerUp, h.normal) <= MaxSlopeAngle)
                {
                    _isStep = true;

                    return CollideAndSlide(-_playerUp * _stepAndSlopeHandleSettings._maxStepUpHeight, pos, depth + 1, 1, velInit);
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
        _heightStateful.OnUpdate(_playerHeight.WS);
        _radiusStateful.OnUpdate(CapsuleRadius);
        jumpSpeedStateful.OnUpdate(_movementSettings._jumpSpeed);
        _airJumpStateful.OnUpdate();
        jumpMaxHeightStateful.OnUpdate(_movementSettings._jumpMaxHeight);
        _gravityStateful.OnUpdate();
        _jumpVelocity.IS.OnUpdate();

        _externalDragStateful.OnUpdate(_externalMovementSettings._contactDrag, _externalMovementSettings._airDrag);

        _isCollidedStateful.OnUpdate();

        if (_heightStateful.IsChanged)
        {
            _componentSettings._capsuleCollider.height = _heightStateful.Value - _skinWidth * 2f;
            _componentSettings._capsuleCollider.transform.localPosition = _playerUp.normalized * _heightStateful.Value * 0.5f;
        }

        if (_radiusStateful.IsChanged)
        {
            _componentSettings._capsuleCollider.radius = _radiusStateful.Value - _skinWidth;
        }

        if (jumpSpeedStateful.IsChanged)
        {
            _movementSettings._jumpMaxHeight = jumpSpeedStateful.Value * jumpSpeedStateful.Value * 0.5f / Mathf.Abs(_gravityStateful.Value.y);
            jumpMaxHeightStateful = new Float(_movementSettings._jumpMaxHeight);
        }
        else if (jumpMaxHeightStateful.IsChanged)
        {
            _movementSettings._jumpSpeed = Mathf.Sqrt(2f * Mathf.Abs(_gravityStateful.Value.y) * jumpMaxHeightStateful.Value);
            jumpSpeedStateful = new Float(_movementSettings._jumpSpeed);
        }

        if (_isGrounded.Value) _airJumpStateful.Reset();
    }

    #endregion

    #region Public Methods
    
    public void AddForce(Vector3 force, Object from, ForceMode forceMode = ForceMode.Force)
    {
        switch (forceMode) {
            case ForceMode.Force:
                accelerationGive += force/mass;
            break;
            case ForceMode.Impulse:
                impulseGive += force/mass;
            break;
            case ForceMode.Acceleration:
                accelerationGive += force;
            break;
            case ForceMode.VelocityChange:
                impulseGive += force;
            break;
        }
        
    }
    

    public void AddRelativeForce(Vector3 force, GameObject from, ForceMode forceMode = ForceMode.Force) {
        AddForce(transform.TransformDirection(force), from, forceMode);
    }

    public Vector3 GetAccumulatedForce() {
        return Vector3.zero;
    }

    public void SetVelocity(Vector3 velocity)
    {
        _externalMovementSettings._velocity = velocity;
    }

    public void MovePosition(Vector3 position)
    {
        _externalMovementSettings._position = position;
        _externalMovementSettings._isPositionSet = true;
    }

    public void MoveRotation(Quaternion rotation) {
        _forward = rotation * Vector3.forward;
        _right = rotation * Vector3.right;
        _playerUp = rotation * Vector3.up;
    }

    public void Move(Vector3 position, Quaternion rotation) {
        MovePosition(position);
        MoveRotation(rotation);
    }

    #endregion
}