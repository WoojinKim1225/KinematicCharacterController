using UnityEngine;
using UnityEngine.InputSystem;
using ReferenceManager;
using StatefulVariables;
using KinematicCharacterSettings;
using Unity.VisualScripting;


public class KinematicCharacterController : MonoBehaviour
{
    #region Variables

    [SerializeField] private ComponentSettings _componentSettings = new ComponentSettings();
    public ComponentSettings componentSettings => _componentSettings;

    private Vector3 _rigidbodyPosition => _componentSettings._rigidbody.transform.position;

    [SerializeField] private MovementSettings _movementSettings = new MovementSettings();

    public MovementSettings movementSettings => _movementSettings;

    public Vector3 ViewDirection { get => _movementSettings._viewDirection; set => _movementSettings._viewDirection = value; }

    private Float jumpSpeedStateful, jumpMaxHeightStateful;
    private Float _airJumpStateful;

    private Float jumpBufferTimeStateful;
    private Float coyoteTimeStateful;

    [SerializeField] private PhysicsSettings _physicsSettings = new PhysicsSettings();

    private float _skinWidth => _physicsSettings._skinWidth * 0.5f;

    private Vector3Stateful _gravityStateful;
    private Vector3 gravityDirection => _gravityStateful.Value.normalized;
    private Vector3 accelerationDirection => (_physicsSettings._gravity + _externalMovementSettings._acceleration).normalized;

    /*********************************************************************************************************/

    [SerializeField] private CharacterSizeSettings _characterSizeSettings = new CharacterSizeSettings();
    public CharacterSizeSettings CharacterSizeSettings => _characterSizeSettings;

    public float IdleHeight => _characterSizeSettings._idleHeight;
    public float CrouchHeight => _characterSizeSettings._crouchHeight;
    public float CapsuleRadius => _characterSizeSettings._capsuleRadius;
    public float CapsuleHeight => _heightStateful.Value;

    public float height => _playerHeight.WS == 0 ? IdleHeight : _playerHeight.WS;

    private Float _heightStateful, _radiusStateful;
    public float HeightValue => _heightStateful.Value;

    /*********************************************************************************************************/

    [SerializeField] private StepAndSlopeHandleSettings _stepAndSlopeHandleSettings = new StepAndSlopeHandleSettings();


    public float MaxSlopeAngle => _stepAndSlopeHandleSettings._maxSlopeAngle;
    public bool IsUpStepEnabled { get => _stepAndSlopeHandleSettings._isUpStepEnabled; set => _stepAndSlopeHandleSettings._isUpStepEnabled = value; }
    public float UpStepHeight => _stepAndSlopeHandleSettings._maxStepUpHeight;
    public float DownStepHeight => _stepAndSlopeHandleSettings._maxStepDownHeight;
    public bool IsDownStepEnabled { get => _stepAndSlopeHandleSettings._isDownStepEnabled; set => _stepAndSlopeHandleSettings._isDownStepEnabled = value; }

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

    private Vector2Stateful _externalDragStateful;

    public float ExternalContactDrag { set => _externalMovementSettings._contactDrag = value; }
    public float ExternalAirDrag { set => _externalMovementSettings._airDrag = value; }

    public void ExternalDragReset()
    {
        _externalMovementSettings._contactDrag = _externalDragStateful.InitialValue.x;
        _externalMovementSettings._airDrag = _externalDragStateful.InitialValue.y;
        _externalDragStateful.Reset();
    }

    private Vector3 _nextPositionWS;

    private Bool _isGrounded;
    private bool _isGroundedExit => !_isGrounded.Value && _isGrounded.BeforeValue;
    private bool _isGroundedEnter => _isGrounded.Value && !_isGrounded.BeforeValue;
    private Bool _isCollidedStateful;

    private bool _isCollidedHorizontal, _isCollidedVertical;
    private int _groundedDepth;
    private Vector3 beforeWallNormal = Vector3.zero;

    [SerializeField] private Vector3 _groundNormal = Vector3.up;
    [SerializeField] private Vector3 _playerUp = Vector3.up;

    public LayerMask WhatIsGround => _physicsSettings._whatIsGround;
    private Vector3 groundExitDisplacement;
    private bool _isUpStep;
    private bool _isDownStep;
    readonly QueryTriggerInteraction queryTrigger = QueryTriggerInteraction.Ignore;
    readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
    [SerializeField] private Vector3 accelerationGive;
    public Vector3 impulseGive;

    #endregion

    void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb)) _componentSettings._rigidbody = rb;
        else _componentSettings._rigidbody = gameObject.AddComponent<Rigidbody>();

        _componentSettings._rigidbody.mass = 1f;
        _componentSettings._rigidbody.detectCollisions = true;
        _componentSettings._rigidbody.isKinematic = true;
        _componentSettings._rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _componentSettings._rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _componentSettings._capsuleCollider.gameObject.SetActive(true);
        _componentSettings._capsuleCollider.transform.localPosition = Vector3.up;
        _componentSettings._capsuleCollider.height = _characterSizeSettings._idleHeight - 2f * _skinWidth;
        _componentSettings._capsuleCollider.radius = _characterSizeSettings._capsuleRadius - _skinWidth;
        _componentSettings._capsuleCollider.bounds.Expand(-2 * _skinWidth);

        InitStateful();
    }

    void Start()
    {
        if (Physics.Raycast(transform.position - Vector3.Normalize(_physicsSettings._gravity), Vector3.Normalize(_physicsSettings._gravity), out RaycastHit startHit, 1.01f, WhatIsGround, queryTrigger))
        {
            transform.position = startHit.point - Vector3.Normalize(_physicsSettings._gravity) * _skinWidth * 2f;
        }
    }

    void Update()
    {
        GetDirectionsFromView();
    }

    void FixedUpdate()
    {
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

    private void InitStateful()
    {
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

        jumpBufferTimeStateful = new Float(_movementSettings._jumpBufferTime) { Value = 0 };
        coyoteTimeStateful = new Float(_movementSettings._coyoteTime) { Value = 0 };
    }

    private void GetDirectionsFromView()
    {
        _forward = Vector3.ProjectOnPlane(_movementSettings._viewDirection, -gravityDirection).normalized;
        _right = Vector3.Cross(-gravityDirection, _forward);

    }

    private void UpdateGravity()
    {
        if (_physicsSettings._gravityMode == KinematicCharacterSettingExtensions.EGravityMode.Single)
        {
            _gravityStateful.Value = _physicsSettings._gravity;
        }
        else
        {
            float verticalSpeed = _verticalDisplacement.y / Time.fixedDeltaTime;
            _gravityStateful.Value = _physicsSettings._gravityList[0].gravity;
            for (int i = 1; i < _physicsSettings._gravityList.Length; i++)
            {
                if (_physicsSettings._gravityList[i].verticalSpeedThreshold > verticalSpeed)
                {
                    _gravityStateful.Value = _physicsSettings._gravityList[i].gravity;
                }
            }
        }
    }

    private void CalculateObjectSpaceVariables()
    {
        if (_isGrounded.Value && _jumpVelocity.IS.Value > 0) _jumpVelocity.OS = _movementSettings._jumpSpeed * (-gravityDirection);
        else if (_isGrounded.Value && jumpBufferTimeStateful.Value > 0)
        {
            _jumpVelocity.OS = _movementSettings._jumpSpeed * (-gravityDirection);
        }
        else _jumpVelocity.OS = Vector3.zero;
        _jumpVelocity.OS += _gravityStateful.Value * Time.fixedDeltaTime * 0.5f;
        if (!_isGrounded.Value && _jumpVelocity.IS.Value > 0 && _jumpVelocity.IS.BeforeValue == 0)
            jumpBufferTimeStateful.Value = _movementSettings._jumpBufferTime;

        _playerHeight.OS = _playerHeight.IS > 0 ? CrouchHeight : IdleHeight;

        float moveSpeedMultiplier = _playerHeight.IS > 0f ? _movementSettings._crouchSpeedMultiplier : (_sprintInput.IS > 0 ? _movementSettings._sprintSpeedMultiplier : 1f);
        Vector3 _moveVelocityTargetOS = new Vector3(_moveVelocity.IS.x, 0, _moveVelocity.IS.y) * _movementSettings._moveSpeed * moveSpeedMultiplier;

        switch (_movementSettings._speedControlMode)
        {
            case KinematicCharacterSettingExtensions.ESpeedControlMode.Constant:
                _moveVelocity.OS = _moveVelocityTargetOS;
                break;
            case KinematicCharacterSettingExtensions.ESpeedControlMode.Linear:
                if ((_moveVelocity.OS - _moveVelocityTargetOS).magnitude < Time.fixedDeltaTime * _movementSettings._moveAcceleration) _moveVelocity.OS = _moveVelocityTargetOS;
                else
                {
                    _moveVelocity.OS += (_moveVelocityTargetOS - _moveVelocity.OS).normalized * Time.fixedDeltaTime * _movementSettings._moveAcceleration;
                }
                break;
            case KinematicCharacterSettingExtensions.ESpeedControlMode.Exponential:
                if ((_moveVelocity.OS - _moveVelocityTargetOS).magnitude < Mathf.Epsilon) _moveVelocity.OS = _moveVelocityTargetOS;
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
        if (!_isGrounded.Value)
        {
            _groundNormal = -gravityDirection;
            if (_isJumpStarted && _airJumpStateful.Value-- > 0)
            {
                _jumpVelocity.WS = _movementSettings._jumpSpeed * (-gravityDirection) + _gravityStateful.Value * Time.fixedDeltaTime * 0.5f;
            }
            else if (coyoteTimeStateful.Value > 0 && _isJumpStarted)
            {
                coyoteTimeStateful.Value = 0;
                _jumpVelocity.WS = _movementSettings._jumpSpeed * (-gravityDirection) + _gravityStateful.Value * Time.fixedDeltaTime * 0.5f;
            }
            /*
            else if (_isGrounded.BeforeValue && _jumpVelocity.IS.Value == 0 && _jumpVelocity.IS.BeforeValue == 0)
            {
                _jumpVelocity.WS = groundExitDisplacement / Time.fixedDeltaTime;
                groundExitDisplacement = Vector3.zero;
            }
            */
            else
            {
                _jumpVelocity.WS += _gravityStateful.Value * Time.fixedDeltaTime;
            }
        }
        else if (jumpBufferTimeStateful.Value > 0)
        {
            _jumpVelocity.WS = _movementSettings._jumpSpeed * (-gravityDirection) + _gravityStateful.Value * Time.fixedDeltaTime * 0.5f;

        }
        else _jumpVelocity.WS = _jumpVelocity.OS;

        _moveVelocity.WS = (_moveVelocity.TS - Vector3.Dot(_groundNormal, _moveVelocity.TS) / Vector3.Dot(_groundNormal, -gravityDirection) * (-gravityDirection)).normalized * _moveVelocity.TS.magnitude;

        _moveVelocity.WS += _externalMovementSettings._groundMove;

        _externalMovementSettings._acceleration = impulseGive + accelerationGive;

        _externalMovementSettings._velocity += _externalMovementSettings._acceleration * Time.fixedDeltaTime;
        float drag = _isGrounded.Value ? _externalDragStateful.Value.x : _externalDragStateful.Value.y;
        if (_externalMovementSettings._velocity.magnitude < Time.fixedDeltaTime * drag)
            _externalMovementSettings._velocity = Vector3.zero;

        _externalMovementSettings._velocity *= 1 - drag / componentSettings._rigidbody.mass * Time.fixedDeltaTime;

        _jumpVelocity.WS += Vector3.Project(_externalMovementSettings._velocity - _externalMovementSettings._velocityBefore, gravityDirection);
        _moveVelocity.WS += Vector3.ProjectOnPlane(_externalMovementSettings._velocity, gravityDirection);


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

        _externalMovementSettings._velocityBefore = _externalMovementSettings._velocity;

        _isCollidedStateful.Value = _isCollidedHorizontal || _isCollidedVertical;

        if (_isGroundedExit)
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
            switch (state)
            {
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

        float dist = vel.magnitude + _skinWidth;
        Vector3 capsulePoint = (_heightStateful.Value * 0.5f - _radiusStateful.Value) * _playerUp.normalized;
        Vector3 characterLowestPosition = pos - capsulePoint + gravityDirection * _radiusStateful.Value;

        if (Physics.CapsuleCast(pos + capsulePoint, pos - capsulePoint, _radiusStateful.Value + _skinWidth, vel.normalized, out RaycastHit hit, dist, _physicsSettings._whatIsGround, queryTrigger))
        {
            if (hit.collider.isTrigger) return CollideAndSlide(vel, pos, depth + 1, state);
            Vector3 flatHit = Vector3.ProjectOnPlane(hit.normal, gravityDirection).normalized;
            float scale = 1 - Vector3.Dot(flatHit, -Vector3.ProjectOnPlane(velInit, gravityDirection).normalized);

            switch (state)
            {
                case 0:
                    _isCollidedHorizontal = true;
                    break;
                case 1:
                    _isCollidedVertical = true;
                    break;
                default:
                    break;
            }

            if (Vector3.Dot(_externalMovementSettings._velocity, hit.normal) < 0) _externalMovementSettings._velocity = Vector3.ProjectOnPlane(_externalMovementSettings._velocity, hit.normal);

            Vector3 snapToSurface = vel.normalized * (hit.distance - _skinWidth);
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(-gravityDirection, hit.normal);

            // the terrain is inside the collider
            if (snapToSurface.magnitude <= _skinWidth) snapToSurface = Vector3.zero;

            // if flat ground or slope
            if (angle <= _stepAndSlopeHandleSettings._maxSlopeAngle || _isUpStep || _isDownStep)
            {
                _isGrounded.Value = true;
                _groundNormal = (_groundNormal * _groundedDepth + hit.normal).normalized;
                _groundedDepth++;

                if (state == 1)
                {
                    if (angle <= _stepAndSlopeHandleSettings._maxSlopeAngle) {
                        _isUpStep = false;
                        _isDownStep = false;
                    }

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
                Vector3 pivot = characterLowestPosition + Vector3.ProjectOnPlane(hit.point - characterLowestPosition, -gravityDirection) - gravityDirection * _stepAndSlopeHandleSettings._maxStepUpHeight;

                bool b0 = Physics.Raycast(pivot + flatHit * _radiusStateful.Value, -flatHit, out RaycastHit h0, _radiusStateful.Value + 0.1f, WhatIsGround, queryTrigger);
                bool b1 = Physics.Raycast(pivot - flatHit * 0.01f, gravityDirection, out RaycastHit h1, _stepAndSlopeHandleSettings._maxStepUpHeight + 1f, _physicsSettings._whatIsGround, queryTrigger);
                bool b2 = Physics.SphereCast(characterLowestPosition + vel + (_stepAndSlopeHandleSettings._maxStepUpHeight + _radiusStateful.Value + 0.1f) * (-gravityDirection), _radiusStateful.Value, gravityDirection, out RaycastHit h2, _stepAndSlopeHandleSettings._maxStepUpHeight, WhatIsGround, queryTrigger);

                float upStepDistance = Vector3.Dot(h2.point + h2.normal * _radiusStateful.Value + gravityDirection * _radiusStateful.Value - characterLowestPosition, _playerUp) + _skinWidth;

                switch (state)
                {
                    case 0:
                        if (_isGrounded.Value)
                        {
                            //leftover = projectAndScale(Vector3.ProjectOnPlane(leftover, gravityDirection), -Vector3.ProjectOnPlane(hit.normal, gravityDirection)).normalized * scale;
                            leftover = Vector3.Project(leftover, Vector3.Cross(hit.normal, _groundNormal).normalized);
                            Debug.Log(depth);
                            Debug.DrawRay(pos, leftover, Color.yellow);
                        }
                        else
                        {
                            leftover = projectAndScale(leftover, hit.normal) * scale;
                        }

                        if (_stepAndSlopeHandleSettings._isUpStepEnabled && !_isDownStep && _isGrounded.BeforeValue)
                        {

                            if (!b0 && b1 && b2 && upStepDistance <= _stepAndSlopeHandleSettings._maxStepUpHeight && Vector3.Angle(h1.normal, -gravityDirection) <= MaxSlopeAngle)
                            {
                                _isUpStep = true;

                                snapToSurface += upStepDistance * (-gravityDirection);
                                //leftover = Vector3.zero;
                                //return snapToSurface + CollideAndSlide(leftover, pos, depth + 1, state, velInit);
                                //_jumpVelocity.WS += _stepAndSlopeHandleSettings._maxStepDownHeight * gravityDirection / Time.fixedDeltaTime;
                            }

                        }
                        break;
                    case 1:
                        leftover = projectAndScale(leftover, hit.normal) * scale;
                        if (beforeWallNormal != Vector3.zero && Vector3.Dot(beforeWallNormal, vel) <= 0)
                        {
                            _isGrounded.Value = true;
                            _groundNormal = -gravityDirection;
                        }
                        beforeWallNormal = hit.normal;
                        break;
                }
            }

            return snapToSurface + CollideAndSlide(leftover, pos + snapToSurface, depth + 1, state, velInit);
        }

        else if (state == 1 && _isGroundedExit && _jumpVelocity.IS.Value == 0)
        {
            if (_stepAndSlopeHandleSettings._isDownStepEnabled && jumpBufferTimeStateful.Value <= 0)
            {
                bool b = Physics.Raycast(pos + _moveVelocity.WS * Time.fixedDeltaTime, gravityDirection, out RaycastHit h, _stepAndSlopeHandleSettings._maxStepDownHeight + _heightStateful.Value * 0.5f, _physicsSettings._whatIsGround, queryTrigger);
                bool b1 = Physics.SphereCast(pos + _moveVelocity.WS * Time.fixedDeltaTime + capsulePoint, _componentSettings._capsuleCollider.bounds.extents.x, -_playerUp.normalized, out RaycastHit h2, capsulePoint.magnitude * 2f, _physicsSettings._whatIsGround, queryTrigger);

                if (!_isUpStep && b && !b1 && h.distance > _heightStateful.Value * 0.5f + _skinWidth && Vector3.Angle(_playerUp, h.normal) <= MaxSlopeAngle)
                {
                    // has ground beneath player, stepping ground
                    _isDownStep = true;
                    //_isStep = true;
                    return CollideAndSlide(-_playerUp * _stepAndSlopeHandleSettings._maxStepUpHeight, pos, depth + 1, 1, velInit);
                }
                else
                {
                    // player is falling
                    coyoteTimeStateful.Value = _movementSettings._coyoteTime;
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

        if (jumpBufferTimeStateful.Value > 0f)
        {
            jumpBufferTimeStateful.Value -= Time.fixedDeltaTime;
        }

        if (coyoteTimeStateful.Value > 0f)
        {
            coyoteTimeStateful.Value -= Time.fixedDeltaTime;
        }
    }

    #endregion

    #region Public Methods

    public void AddForce(Vector3 force, Object from, ForceMode forceMode = ForceMode.Force)
    {
        switch (forceMode)
        {
            case ForceMode.Force:
                accelerationGive += force / _componentSettings._rigidbody.mass;
                break;
            case ForceMode.Impulse:
                impulseGive += force / _componentSettings._rigidbody.mass;
                break;
            case ForceMode.Acceleration:
                accelerationGive += force;
                break;
            case ForceMode.VelocityChange:
                impulseGive += force;
                break;
        }

    }

    public void AddRelativeForce(Vector3 force, GameObject from, ForceMode forceMode = ForceMode.Force)
    {
        AddForce(transform.TransformDirection(force), from, forceMode);
    }

    public Vector3 GetAccumulatedForce()
    {
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

    public void MoveRotation(Quaternion rotation)
    {
        _forward = rotation * Vector3.forward;
        _right = rotation * Vector3.right;
        _playerUp = rotation * Vector3.up;
    }

    public void Move(Vector3 position, Quaternion rotation)
    {
        MovePosition(position);
        MoveRotation(rotation);
    }

    #endregion
}
