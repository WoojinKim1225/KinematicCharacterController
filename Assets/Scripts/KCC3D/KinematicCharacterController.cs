using UnityEngine;
using StatefulVariables;
using KinematicCharacterSettings;
using Unity.VisualScripting;


public class KinematicCharacterController : MonoBehaviour
{
    private float dt;
    [SerializeField] private ComponentSettings m_componentSettings = new ComponentSettings();
    public ComponentSettings componentSettings => m_componentSettings;

    private Vector3 _rigidbodyPosition => m_componentSettings._rigidbody.transform.position;

    [SerializeField] private MovementSettings m_movementSettings = new MovementSettings();

    public MovementSettings movementSettings => m_movementSettings;

    public Vector3 ViewDirection { get => m_movementSettings._viewDirection; set => m_movementSettings._viewDirection = value; }

    private Float jumpSpeedStateful, jumpMaxHeightStateful;
    private Float _airJumpStateful;

    private Float jumpBufferTimeStateful;
    private Float coyoteTimeStateful;

    [SerializeField] private PhysicsSettings m_physicsSettings = new PhysicsSettings();

    private float _skinWidth => m_physicsSettings._skinWidth * 0.5f;

    private Vector3Stateful _gravityStateful;
    private Vector3 gravityDirection => _gravityStateful.Value.normalized;
    private Vector3 accelerationDirection => (m_physicsSettings._gravity + m_externalMovementSettings._acceleration).normalized;

    /*********************************************************************************************************/

    [SerializeField] private CharacterSizeSettings m_characterSizeSettings = new CharacterSizeSettings();
    public CharacterSizeSettings CharacterSizeSettings => m_characterSizeSettings;

    public float IdleHeight => m_characterSizeSettings._idleHeight;
    public float CrouchHeight => m_characterSizeSettings._crouchHeight;
    public float CapsuleRadius => m_characterSizeSettings._capsuleRadius;
    public float CapsuleHeight => _heightStateful.Value;

    public float height => m_playerHeight.WS == 0 ? IdleHeight : m_playerHeight.WS;

    private Float _heightStateful, _radiusStateful;
    public float HeightValue => _heightStateful.Value;

    /*********************************************************************************************************/

    [SerializeField] private StepAndSlopeHandleSettings m_stepAndSlopeHandleSettings = new StepAndSlopeHandleSettings();


    public float MaxSlopeAngle => m_stepAndSlopeHandleSettings._maxSlopeAngle;
    public bool IsUpStepEnabled { get => m_stepAndSlopeHandleSettings._isUpStepEnabled; set => m_stepAndSlopeHandleSettings._isUpStepEnabled = value; }
    public float UpStepHeight => m_stepAndSlopeHandleSettings._maxStepUpHeight;
    public float DownStepHeight => m_stepAndSlopeHandleSettings._maxStepDownHeight;
    public bool IsDownStepEnabled { get => m_stepAndSlopeHandleSettings._isDownStepEnabled; set => m_stepAndSlopeHandleSettings._isDownStepEnabled = value; }

    private Vector3 _forward, _right;
    public Vector3 Up => _playerUp.normalized;
    public Vector3 Forward => _forward;
    public Vector3 Right => _right;

    private Coordinate<Vector2, Vector3, Vector3, Vector3> m_moveVelocity;
    private Coordinate<Float, Vector3, Null, Vector3> m_jumpVelocity;
    private Coordinate<float, float, Null, float> m_playerHeight;
    private Coordinate<float, Null, Null, Null> m_sprintInput;


    public Vector3 MoveVelocityIS { set => m_moveVelocity.IS = value; }
    public float JumpVelocityIS { set => m_jumpVelocity.IS.Value = value; }
    public float SprintInputIS { set => m_sprintInput.IS = value; }
    public float PlayerHeightIS { set => m_playerHeight.IS = value; }

    private bool _isJumpStarted => m_jumpVelocity.IS.Value != 0 && m_jumpVelocity.IS.BeforeValue == 0;

    private Vector3 _horizontalDisplacement, _verticalDisplacement;
    private Vector3 _displacement;

    public Vector3 Displacement => _displacement;
    public Vector3 Velocity => _displacement / dt;
    public Vector3 HorizontalVelocity => _horizontalDisplacement / dt;
    public Vector3 VerticalVelocity => _verticalDisplacement / dt;

    public Vector3 HorizontalDirection => _horizontalDisplacement - ExternalGroundMove * dt;
    public float Speed => HorizontalDirection.magnitude / dt;

    [SerializeField] private ExternalMovementSettings m_externalMovementSettings = new ExternalMovementSettings();

    public Vector3 ExternalGroundMove { get => m_externalMovementSettings._groundMove; set => m_externalMovementSettings._groundMove = value; }

    private Vector2Stateful _externalDragStateful;

    public float ExternalContactDrag { set => m_externalMovementSettings._contactDrag = value; }
    public float ExternalAirDrag { set => m_externalMovementSettings._airDrag = value; }

    public void ExternalDragReset()
    {
        m_externalMovementSettings._contactDrag = _externalDragStateful.InitialValue.x;
        m_externalMovementSettings._airDrag = _externalDragStateful.InitialValue.y;
        _externalDragStateful.Reset();
    }

    private Vector3 _nextPositionWS;

    private Bool _isGrounded;
    public bool IsGrounded =>  _isGrounded.Value;
    private bool _isGroundedExit => !_isGrounded.Value && _isGrounded.BeforeValue;
    private bool _isGroundedEnter => _isGrounded.Value && !_isGrounded.BeforeValue;
    private Bool _isCollidedStateful;

    private bool _isCollidedHorizontal, _isCollidedVertical;
    private int _groundedDepth;
    private Vector3 beforeWallNormal = Vector3.zero;

    [SerializeField] private Vector3 _groundNormal = Vector3.up;
    [SerializeField] private Vector3 _playerUp = Vector3.up;

    public LayerMask WhatIsGround => m_physicsSettings._whatIsGround;
    private Vector3 groundExitDisplacement;
    private bool _isUpStep;
    private bool _isDownStep;
    readonly QueryTriggerInteraction queryTrigger = QueryTriggerInteraction.Ignore;
    [SerializeField] private Vector3 accelerationGive;
    public Vector3 impulseGive;

    public Rigidbody parent;
    Bool isKinematicHit;
    public Vector3 parentPos, parentPosBefore;
    public Quaternion parentRot;

    private bool isAfterStart = false;

    void Awake()
    {
        dt = Time.fixedDeltaTime;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (gameObject.TryGetComponent<Rigidbody>(out Rigidbody rb)) m_componentSettings._rigidbody = rb;
        else m_componentSettings._rigidbody = gameObject.AddComponent<Rigidbody>();

        m_componentSettings._rigidbody.mass = 1f;
        m_componentSettings._rigidbody.detectCollisions = true;
        m_componentSettings._rigidbody.isKinematic = true;
        m_componentSettings._rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        m_componentSettings._rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        m_componentSettings._capsuleCollider.gameObject.SetActive(true);
        m_componentSettings._capsuleCollider.transform.localPosition = Vector3.up;
        m_componentSettings._capsuleCollider.height = m_characterSizeSettings._idleHeight - 2f * _skinWidth;
        m_componentSettings._capsuleCollider.radius = m_characterSizeSettings._capsuleRadius - _skinWidth;
        m_componentSettings._capsuleCollider.bounds.Expand(-2 * _skinWidth);

        InitStateful();
    }

    void Start()
    {
    }

    void Update()
    {
        GetDirectionsFromView();
    }

    void FixedUpdate()
    {
        if (!isAfterStart)
        {
            Debug.DrawRay(transform.position - Vector3.Normalize(m_physicsSettings._gravity), Vector3.Normalize(m_physicsSettings._gravity) * 2f, Color.yellow, 10f);
            if (Physics.Raycast(transform.position - Vector3.Normalize(m_physicsSettings._gravity), Vector3.Normalize(m_physicsSettings._gravity), out RaycastHit startHit, 2f, WhatIsGround))
            {
                _nextPositionWS = m_componentSettings._rigidbody.transform.position = startHit.point - Vector3.Normalize(m_physicsSettings._gravity) * _skinWidth * 3f;
            }
            isAfterStart = true;
            return;

        }

        UpdateGravity(Time.fixedDeltaTime);

        if (_isGrounded.Value) _airJumpStateful.Value = _airJumpStateful.InitialValue;

        m_componentSettings._capsuleCollider.transform.up = _playerUp.normalized;

        CalculateObjectSpaceVariables(Time.fixedDeltaTime);

        CalculateTangentSpaceVariables();

        CalculateWorldSpaceVariables(Time.fixedDeltaTime);

        UpdateProperties();

        beforeWallNormal = Vector3.zero;

        HandleCollisionsAndMovement(Time.fixedDeltaTime);
        accelerationGive = Vector3.zero;
        impulseGive = Vector3.zero;
    }



    private void InitStateful()
    {
        _isGrounded = new Bool(false);
        _isCollidedStateful = new Bool(false);

        _heightStateful = new Float(IdleHeight);
        _radiusStateful = new Float(CapsuleRadius);

        jumpSpeedStateful = new Float(m_movementSettings._jumpSpeed);
        jumpMaxHeightStateful = new Float(Mathf.Sqrt(m_movementSettings._jumpSpeed * m_movementSettings._jumpSpeed * 0.5f / Mathf.Abs(m_physicsSettings._gravity.y)));
        m_movementSettings._jumpMaxHeight = m_movementSettings._jumpSpeed * m_movementSettings._jumpSpeed * 0.5f / Mathf.Abs(m_physicsSettings._gravity.y);

        _airJumpStateful = new Float(m_movementSettings._maxAirJumpCount);

        _gravityStateful = new Vector3Stateful(m_physicsSettings._gravity);
        m_jumpVelocity.IS = new Float(0);

        _externalDragStateful = new Vector2Stateful(m_externalMovementSettings._contactDrag, m_externalMovementSettings._airDrag);

        jumpBufferTimeStateful = new Float(m_movementSettings._jumpBufferTime) { Value = 0 };
        coyoteTimeStateful = new Float(m_movementSettings._coyoteTime) { Value = 0 };

        isKinematicHit = new Bool(false);
    }

    private void GetDirectionsFromView()
    {
        _forward = Vector3.ProjectOnPlane(m_movementSettings._viewDirection, -gravityDirection).normalized;
        _right = Vector3.Cross(-gravityDirection, _forward);

    }

    private void UpdateGravity(float dt)
    {
        if (m_physicsSettings._gravityMode == KinematicCharacterSettingExtensions.EGravityMode.Single)
        {
            _gravityStateful.Value = m_physicsSettings._gravity;
        }
        else
        {
            float verticalSpeed = _verticalDisplacement.y / dt;
            _gravityStateful.Value = m_physicsSettings._gravityList[0].gravity;
            for (int i = 1; i < m_physicsSettings._gravityList.Length; i++)
            {
                if (m_physicsSettings._gravityList[i].verticalSpeedThreshold > verticalSpeed)
                {
                    _gravityStateful.Value = m_physicsSettings._gravityList[i].gravity;
                }
            }
        }
    }

    private void CalculateObjectSpaceVariables(float dt)
    {
        if (_isGrounded.Value && (m_jumpVelocity.IS.Value > 0 || jumpBufferTimeStateful.Value > 0)) m_jumpVelocity.OS = m_movementSettings._jumpSpeed * (-gravityDirection);
        else m_jumpVelocity.OS = Vector3.zero;
        m_jumpVelocity.OS += _gravityStateful.Value * dt * 0.5f;

        if (!_isGrounded.Value && m_jumpVelocity.IS.Value > 0 && m_jumpVelocity.IS.BeforeValue == 0)
            jumpBufferTimeStateful.Value = m_movementSettings._jumpBufferTime;

        m_playerHeight.OS = m_playerHeight.IS > 0 ? CrouchHeight : IdleHeight;

        float moveSpeedMultiplier = m_playerHeight.IS > 0f ? m_movementSettings._crouchSpeedMultiplier : (m_sprintInput.IS > 0 ? m_movementSettings._sprintSpeedMultiplier : 1f);
        Vector3 _moveVelocityTargetOS = new Vector3(m_moveVelocity.IS.x, 0, m_moveVelocity.IS.y) * m_movementSettings._moveSpeed * moveSpeedMultiplier;

        switch (m_movementSettings._speedControlMode)
        {
            case KinematicCharacterSettingExtensions.ESpeedControlMode.Constant:
                m_moveVelocity.OS = _moveVelocityTargetOS;
                break;
            case KinematicCharacterSettingExtensions.ESpeedControlMode.Linear:
                if ((m_moveVelocity.OS - _moveVelocityTargetOS).magnitude < dt * m_movementSettings._moveAcceleration) m_moveVelocity.OS = _moveVelocityTargetOS;
                else
                {
                    m_moveVelocity.OS += (_moveVelocityTargetOS - m_moveVelocity.OS).normalized * dt * m_movementSettings._moveAcceleration;
                }
                break;
            case KinematicCharacterSettingExtensions.ESpeedControlMode.Exponential:
                if ((m_moveVelocity.OS - _moveVelocityTargetOS).magnitude < Mathf.Epsilon) m_moveVelocity.OS = _moveVelocityTargetOS;
                else
                {
                    m_moveVelocity.OS += (_moveVelocityTargetOS - m_moveVelocity.OS) * dt * m_movementSettings._moveDamp;
                }
                break;
            default:
                m_moveVelocity.OS = _moveVelocityTargetOS;
                break;
        }
    }

    private void CalculateTangentSpaceVariables()
    {
        m_moveVelocity.TS = m_moveVelocity.OS.x * _right + m_moveVelocity.OS.z * _forward;
    }

    private void CalculateWorldSpaceVariables(float dt)
    {
        if (!_isGrounded.Value)
        {
            _groundNormal = -gravityDirection;
            if (_isJumpStarted && _airJumpStateful.Value-- > 0)
            {
                m_jumpVelocity.WS = m_movementSettings._jumpSpeed * (-gravityDirection) + _gravityStateful.Value * dt * 0.5f;
            }
            else if (coyoteTimeStateful.Value > 0 && _isJumpStarted)
            {
                coyoteTimeStateful.Value = 0;
                m_jumpVelocity.WS = m_movementSettings._jumpSpeed * (-gravityDirection) + _gravityStateful.Value * dt * 0.5f;
            }
            else
            {
                m_jumpVelocity.WS += _gravityStateful.Value * dt;
            }
        }
        else if (jumpBufferTimeStateful.Value > 0)
        {
            m_jumpVelocity.WS = m_movementSettings._jumpSpeed * (-gravityDirection) + _gravityStateful.Value * dt * 0.5f;

        }
        else m_jumpVelocity.WS = m_jumpVelocity.OS;

        m_moveVelocity.WS = (m_moveVelocity.TS - Vector3.Dot(_groundNormal, m_moveVelocity.TS) / Vector3.Dot(_groundNormal, -gravityDirection) * (-gravityDirection)).normalized * m_moveVelocity.TS.magnitude;

        //m_moveVelocity.WS += m_externalMovementSettings._groundMove;

        m_externalMovementSettings._acceleration = impulseGive + accelerationGive;


        m_externalMovementSettings._velocityBefore = m_externalMovementSettings._velocity;
        m_externalMovementSettings._velocity += m_externalMovementSettings._acceleration * dt;
        float drag = _isGrounded.Value ? _externalDragStateful.Value.x : _externalDragStateful.Value.y;
        if (m_externalMovementSettings._velocity.magnitude < dt * drag)
            m_externalMovementSettings._velocity = Vector3.zero;

        m_externalMovementSettings._velocity *= 1 - drag / componentSettings._rigidbody.mass * dt;

        if (isKinematicHit.Value && !isKinematicHit.BeforeValue) {
            parentPos = parent.transform.position;
            parentPosBefore = parent.transform.position;
        } else if (isKinematicHit.Value && isKinematicHit.BeforeValue) {
            parentPosBefore = parentPos;
            parentPos = parent.transform.position;
            m_moveVelocity.WS += (parentPos - parentPosBefore) / dt;
        } else if (!isKinematicHit.Value && isKinematicHit.BeforeValue) {
            m_externalMovementSettings._velocity += (parentPos - parentPosBefore) / dt;
        }

        m_jumpVelocity.WS -= Vector3.Project(m_externalMovementSettings._velocityBefore, gravityDirection);
        m_jumpVelocity.WS += Vector3.Project(m_externalMovementSettings._velocity, gravityDirection);
        //m_jumpVelocity.WS += -m_externalMovementSettings._velocityBefore + m_externalMovementSettings._velocity;
        m_moveVelocity.WS += Vector3.ProjectOnPlane(m_externalMovementSettings._velocity, gravityDirection);

        m_externalMovementSettings._velocityBefore.y -=m_externalMovementSettings._velocity.y;
        m_externalMovementSettings._velocity.y = 0;

        m_playerHeight.WS = m_playerHeight.OS * transform.localScale.y;
    }


    private void HandleCollisionsAndMovement(float dt)
    {
        if (m_externalMovementSettings._isPositionSet)
        {
            m_jumpVelocity.WS = m_jumpVelocity.OS = _gravityStateful.Value * dt * 0.5f;
            m_componentSettings._rigidbody.MovePosition(m_externalMovementSettings._position);
            m_externalMovementSettings._isPositionSet = false;
            return;
        }

        _groundedDepth = 0;

        isKinematicHit.Value = false;

        _horizontalDisplacement = CollideAndSlide(m_moveVelocity.WS * dt, _rigidbodyPosition - _heightStateful.Value * gravityDirection * 0.5f, 0, 0);
        _verticalDisplacement = CollideAndSlide(m_jumpVelocity.WS * dt, _rigidbodyPosition - _heightStateful.Value * gravityDirection * 0.5f + _horizontalDisplacement, 0, 1);
        
        if (!isKinematicHit.Value) {
            parent = null;
        }

        _isCollidedStateful.Value = _isCollidedHorizontal || _isCollidedVertical;

        if (_isGroundedExit)
        {
            groundExitDisplacement = _verticalDisplacement + Vector3.Dot(_horizontalDisplacement, gravityDirection) * gravityDirection;
        }

        _displacement = _horizontalDisplacement + _verticalDisplacement;
        _nextPositionWS = _displacement + _rigidbodyPosition;

        m_componentSettings._rigidbody.MovePosition(_nextPositionWS);
    }


    private Vector3 CollideAndSlide(Vector3 vel, Vector3 pos, int depth, int state, Vector3 velInit = default)
    {
        if (depth >= m_physicsSettings._maxBounces) return Vector3.zero;

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

        if (Physics.CapsuleCast(pos + capsulePoint, pos - capsulePoint, _radiusStateful.Value + _skinWidth, vel.normalized, out RaycastHit hit, dist, m_physicsSettings._whatIsGround, queryTrigger))
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

            if (Vector3.Dot(m_externalMovementSettings._velocity, hit.normal) < 0) {
                m_externalMovementSettings._velocity = Vector3.ProjectOnPlane(m_externalMovementSettings._velocity, hit.normal);
                Debug.Log("aaaaaaa");
            }

            Vector3 snapToSurface = vel.normalized * (hit.distance - _skinWidth);
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(-gravityDirection, hit.normal);

            // the terrain is inside the collider
            if (snapToSurface.magnitude <= _skinWidth) snapToSurface = Vector3.zero;

            // if flat ground or slope
            if (angle <= m_stepAndSlopeHandleSettings._maxSlopeAngle || _isUpStep || _isDownStep)
            {
                _isGrounded.Value = true;
                _groundNormal = (_groundNormal * _groundedDepth + hit.normal).normalized;
                _groundedDepth++;

                if (hit.collider.attachedRigidbody != null) {
                    isKinematicHit.Value = true;
                    parent = hit.rigidbody;
                }   

                if (state == 1)
                {
                    if (angle <= m_stepAndSlopeHandleSettings._maxSlopeAngle) {
                        _isUpStep = false;
                        _isDownStep = false;
                    }

                    return snapToSurface;
                }

                leftover = projectAndScale(leftover, _groundNormal);
            }
            // if ceiling, cancel upwards velocity
            else if (angle >= m_stepAndSlopeHandleSettings._minCeilingAngle)
            {
                m_jumpVelocity.WS = Vector3.zero;
                return snapToSurface;
            }
            // if wall
            else
            {
                Vector3 pivot = characterLowestPosition + Vector3.ProjectOnPlane(hit.point - characterLowestPosition, -gravityDirection) - gravityDirection * m_stepAndSlopeHandleSettings._maxStepUpHeight;

                bool b0 = Physics.Raycast(pivot + flatHit * _radiusStateful.Value, -flatHit, out RaycastHit h0, _radiusStateful.Value + 0.1f, WhatIsGround, queryTrigger);
                bool b1 = Physics.Raycast(pivot - flatHit * 0.01f, gravityDirection, out RaycastHit h1, m_stepAndSlopeHandleSettings._maxStepUpHeight + 1f, m_physicsSettings._whatIsGround, queryTrigger);
                bool b2 = Physics.SphereCast(characterLowestPosition + vel + (m_stepAndSlopeHandleSettings._maxStepUpHeight + _radiusStateful.Value + 0.1f) * (-gravityDirection), _radiusStateful.Value, gravityDirection, out RaycastHit h2, m_stepAndSlopeHandleSettings._maxStepUpHeight, WhatIsGround, queryTrigger);

                float upStepDistance = Vector3.Dot(h2.point + h2.normal * _radiusStateful.Value + gravityDirection * _radiusStateful.Value - characterLowestPosition, _playerUp) + _skinWidth;

                switch (state)
                {
                    case 0:
                        if (_isGrounded.Value)
                        {
                            leftover = Vector3.Project(leftover, Vector3.Cross(hit.normal, _groundNormal).normalized);
                            Debug.Log(depth);
                            Debug.DrawRay(pos, leftover, Color.yellow);
                        }
                        else
                        {
                            leftover = projectAndScale(leftover, hit.normal) * scale;
                        }

                        if (m_stepAndSlopeHandleSettings._isUpStepEnabled && !_isDownStep && _isGrounded.BeforeValue)
                        {

                            if (!b0 && b1 && b2 && upStepDistance <= m_stepAndSlopeHandleSettings._maxStepUpHeight && Vector3.Angle(h1.normal, -gravityDirection) <= MaxSlopeAngle)
                            {
                                _isUpStep = true;
                                snapToSurface += upStepDistance * (-gravityDirection);
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

        else if (state == 1 && _isGroundedExit && m_jumpVelocity.IS.Value == 0)
        {
            if (m_stepAndSlopeHandleSettings._isDownStepEnabled && jumpBufferTimeStateful.Value <= 0)
            {
                bool b = Physics.Raycast(pos + m_moveVelocity.WS * dt, gravityDirection, out RaycastHit h, m_stepAndSlopeHandleSettings._maxStepDownHeight + _heightStateful.Value * 0.5f, m_physicsSettings._whatIsGround, queryTrigger);
                bool b1 = Physics.SphereCast(pos + m_moveVelocity.WS * dt + capsulePoint, m_componentSettings._capsuleCollider.bounds.extents.x, -_playerUp.normalized, out RaycastHit h2, capsulePoint.magnitude * 2f, m_physicsSettings._whatIsGround, queryTrigger);

                if (!_isUpStep && b && !b1 && h.distance > _heightStateful.Value * 0.5f + _skinWidth && Vector3.Angle(_playerUp, h.normal) <= MaxSlopeAngle)
                {
                    // has ground beneath player, stepping ground
                    _isDownStep = true;
                    //_isStep = true;
                    return CollideAndSlide(-_playerUp * m_stepAndSlopeHandleSettings._maxStepUpHeight, pos, depth + 1, 1, velInit);
                }
                else
                {
                    // player is falling
                    coyoteTimeStateful.Value = m_movementSettings._coyoteTime;
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
        _heightStateful.OnUpdate(m_playerHeight.WS);
        _radiusStateful.OnUpdate(CapsuleRadius);
        jumpSpeedStateful.OnUpdate(m_movementSettings._jumpSpeed);
        _airJumpStateful.OnUpdate();
        jumpMaxHeightStateful.OnUpdate(m_movementSettings._jumpMaxHeight);
        _gravityStateful.OnUpdate();
        m_jumpVelocity.IS.OnUpdate();

        _externalDragStateful.OnUpdate(m_externalMovementSettings._contactDrag, m_externalMovementSettings._airDrag);

        _isCollidedStateful.OnUpdate();

        if (_heightStateful.IsChanged)
        {
            m_componentSettings._capsuleCollider.height = _heightStateful.Value - _skinWidth * 2f;
            m_componentSettings._capsuleCollider.transform.localPosition = _playerUp.normalized * _heightStateful.Value * 0.5f;
        }

        if (_radiusStateful.IsChanged)
        {
            m_componentSettings._capsuleCollider.radius = _radiusStateful.Value - _skinWidth;
        }

        if (jumpSpeedStateful.IsChanged)
        {
            m_movementSettings._jumpMaxHeight = jumpSpeedStateful.Value * jumpSpeedStateful.Value * 0.5f / Mathf.Abs(_gravityStateful.Value.y);
            jumpMaxHeightStateful = new Float(m_movementSettings._jumpMaxHeight);
        }
        else if (jumpMaxHeightStateful.IsChanged)
        {
            m_movementSettings._jumpSpeed = Mathf.Sqrt(2f * Mathf.Abs(_gravityStateful.Value.y) * jumpMaxHeightStateful.Value);
            jumpSpeedStateful = new Float(m_movementSettings._jumpSpeed);
        }

        if (_isGrounded.Value) _airJumpStateful.Reset();

        if (jumpBufferTimeStateful.Value > 0f)
        {
            jumpBufferTimeStateful.Value -= dt;
        }

        if (coyoteTimeStateful.Value > 0f)
        {
            coyoteTimeStateful.Value -= dt;
        }

        isKinematicHit.OnUpdate();
        //parentPos.OnUpdate();
    }



    public void AddForce(Vector3 force, Object from, ForceMode forceMode = ForceMode.Force)
    {
        switch (forceMode)
        {
            case ForceMode.Force:
                accelerationGive += force / m_componentSettings._rigidbody.mass;
                break;
            case ForceMode.Impulse:
                impulseGive += force / m_componentSettings._rigidbody.mass;
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
        m_externalMovementSettings._velocity = velocity;
    }

    public void MovePosition(Vector3 position)
    {
        m_externalMovementSettings._position = position;
        m_externalMovementSettings._isPositionSet = true;
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

}
