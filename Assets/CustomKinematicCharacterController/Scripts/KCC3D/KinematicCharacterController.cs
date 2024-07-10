using UnityEngine;
using StatefulVariables;
using KinematicCharacterSettings;
using Unity.VisualScripting;
using System.Collections.Generic;


public class KinematicCharacterController : MonoBehaviour
{
    private float dt;
    [SerializeField] private ComponentSettings m_componentSettings = new ComponentSettings();
    public ComponentSettings componentSettings => m_componentSettings;

    [SerializeField] private MovementSettings m_movementSettings = new MovementSettings();
    public MovementSettings movementSettings => m_movementSettings;

    public Vector3 ViewDirection { get => m_movementSettings.viewDirection; set => m_movementSettings.viewDirection = value; }

    [SerializeField] private PhysicsSettings m_physicsSettings = new PhysicsSettings();

    private Vector3Stateful _gravityStateful;
    private Vector3 gravityDirection => _gravityStateful.Value.normalized;

    [SerializeField] private CharacterSizeSettings m_characterSizeSettings = new CharacterSizeSettings();
    public CharacterSizeSettings CharacterSizeSettings => m_characterSizeSettings;

    private Vector3 capsuleFocusUp => (m_characterSizeSettings.height.Value * 0.5f - m_characterSizeSettings.capsuleRadius.Value) * _playerUp.normalized;
    private Vector3 capsuleFocusDown => -(m_characterSizeSettings.height.Value * 0.5f - m_characterSizeSettings.capsuleRadius.Value) * _playerUp.normalized;

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
    private bool _isGroundedBeforeFrame;
    public bool IsGrounded => _isGrounded.Value;
    private Bool _isCollidedStateful;

    private bool _isCollidedHorizontal, _isCollidedVertical;
    private int _groundedDepth;
    private Vector3 beforeWallNormal = Vector3.zero;

    [SerializeField] private Vector3 _groundNormal = Vector3.up;
    [SerializeField] private List<Vector3> normals = new List<Vector3>();
    [SerializeField] private Vector3 _playerUp = Vector3.up;

    public LayerMask WhatIsGround => m_physicsSettings._whatIsGround;
    private bool _isUpStep;
    private bool _isDownStep;
    readonly QueryTriggerInteraction queryTrigger = QueryTriggerInteraction.Ignore;
    private Vector3 accelerationGive;
    private Vector3 impulseGive;

    public Rigidbody parent;
    Bool isKinematicHit;
    public Vector3 parentPos, parentPosBefore;
    public Quaternion parentRot;

    private bool isAfterStart = false;

    void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (gameObject.TryGetComponent(out Rigidbody rb)) m_componentSettings._rigidbody = rb;
        else m_componentSettings._rigidbody = gameObject.AddComponent<Rigidbody>();

        m_componentSettings._rigidbody.mass = 1f;
        m_componentSettings._rigidbody.detectCollisions = true;
        m_componentSettings._rigidbody.isKinematic = true;
        m_componentSettings._rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        m_componentSettings._rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        m_componentSettings._capsuleCollider.gameObject.SetActive(true);
        m_componentSettings._capsuleCollider.transform.localPosition = Vector3.up;
        m_componentSettings._capsuleCollider.height = m_characterSizeSettings.idleHeight - 2f * m_physicsSettings.skinWidth;
        m_componentSettings._capsuleCollider.radius = m_characterSizeSettings.capsuleRadius.Value - m_physicsSettings.skinWidth;
        m_componentSettings._capsuleCollider.bounds.Expand(-2 * m_physicsSettings.skinWidth);

        InitStateful();
    }

    void Update()
    {
        GetDirectionsFromView();
    }

    void FixedUpdate()
    {
        dt = Time.fixedDeltaTime;

        if (!isAfterStart)
        {
            if (Physics.Raycast(transform.position - Vector3.Normalize(m_physicsSettings._gravity), Vector3.Normalize(m_physicsSettings._gravity), out RaycastHit startHit, 2f, WhatIsGround))
            {
                _nextPositionWS = m_componentSettings._rigidbody.transform.position = startHit.point - Vector3.Normalize(m_physicsSettings._gravity) * m_physicsSettings.skinWidth * 3f;
            }
            isAfterStart = true;
            return;
        }

        UpdateGravity(Time.fixedDeltaTime);

        if (_isGrounded.Value) m_movementSettings.airJump.Value = m_movementSettings.airJump.InitialValue;

        m_componentSettings._capsuleCollider.transform.up = _playerUp.normalized;

        CalculateObjectSpaceVariables(Time.fixedDeltaTime);

        CalculateTangentSpaceVariables();

        CalculateWorldSpaceVariables(Time.fixedDeltaTime);

        UpdateProperties();

        beforeWallNormal = Vector3.zero;

        HandleCollisionsAndMovement(Time.fixedDeltaTime);

        foreach (Vector3 normal in normals)
        {
            if (Vector3.Dot(m_externalMovementSettings._velocity, normal) < 0)
            {
                m_externalMovementSettings._velocity = Vector3.ProjectOnPlane(m_externalMovementSettings._velocity, normal) + normal * m_physicsSettings.skinWidth;
            }
        }

        _isGroundedBeforeFrame = _isGrounded.Value;
    }

    private void InitStateful()
    {
        m_movementSettings.InitProperties();
        m_characterSizeSettings.InitProperties();

        _isGrounded = new Bool(false);
        _isCollidedStateful = new Bool(false);


        _gravityStateful = new Vector3Stateful(m_physicsSettings._gravity);
        m_jumpVelocity.IS = new Float(0);

        _externalDragStateful = new Vector2Stateful(m_externalMovementSettings._contactDrag, m_externalMovementSettings._airDrag);

        isKinematicHit = new Bool(false);
    }

    private void GetDirectionsFromView()
    {
        _forward = Vector3.ProjectOnPlane(m_movementSettings.viewDirection, -gravityDirection).normalized;
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
        if (_isGrounded.Value && (m_jumpVelocity.IS.Value > 0 || m_movementSettings.jumpBufferTime.Value > 0))
        {
            m_jumpVelocity.OS = m_movementSettings.jumpSpeed.Value * (-gravityDirection);
        }
        else m_jumpVelocity.OS = Vector3.zero;
        m_jumpVelocity.OS += _gravityStateful.Value * dt * 0.5f;

        if (!_isGrounded.Value && m_jumpVelocity.IS.Value > 0 && m_jumpVelocity.IS.BeforeValue == 0)
            m_movementSettings.jumpBufferTime.Reset();

        m_playerHeight.OS = m_playerHeight.IS > 0 ? m_characterSizeSettings.crouchHeight : m_characterSizeSettings.idleHeight;

        float moveSpeedMultiplier = m_playerHeight.IS > 0f ? m_movementSettings._crouchSpeedMultiplier : (m_sprintInput.IS > 0 ? m_movementSettings._sprintSpeedMultiplier : 1f);
        Vector3 _moveVelocityTargetOS = new Vector3(m_moveVelocity.IS.x, 0, m_moveVelocity.IS.y) * m_movementSettings.moveSpeed * moveSpeedMultiplier;

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
            if (_isJumpStarted && m_movementSettings.airJump.Value-- > 0)
            {
                m_jumpVelocity.WS = m_movementSettings.jumpSpeed.Value * (-gravityDirection) + _gravityStateful.Value * dt * 0.5f;
            }
            else if (m_movementSettings.coyoteTime.Value > 0 && _isJumpStarted)
            {
                m_movementSettings.coyoteTime.Value = 0;
                m_jumpVelocity.WS = m_movementSettings.jumpSpeed.Value * (-gravityDirection) + _gravityStateful.Value * dt * 0.5f;
            }
            else
            {
                m_jumpVelocity.WS += _gravityStateful.Value * dt;
            }
        }
        else if (m_movementSettings.jumpBufferTime.Value > 0)
        {
            m_jumpVelocity.WS = m_movementSettings.jumpSpeed.Value * (-gravityDirection) + _gravityStateful.Value * dt * 0.5f;

        }
        else m_jumpVelocity.WS = m_jumpVelocity.OS;

        m_moveVelocity.WS = (m_moveVelocity.TS - Vector3.Dot(_groundNormal, m_moveVelocity.TS) / Vector3.Dot(_groundNormal, -gravityDirection) * (-gravityDirection)).normalized * m_moveVelocity.TS.magnitude;

        m_externalMovementSettings._acceleration = impulseGive + accelerationGive;


        m_externalMovementSettings._velocityBefore = m_externalMovementSettings._velocity;
        m_externalMovementSettings._velocity += m_externalMovementSettings._acceleration * dt;
        float drag = _isGrounded.Value ? _externalDragStateful.Value.x : _externalDragStateful.Value.y;
        if (m_externalMovementSettings._velocity.magnitude < dt * drag)
            m_externalMovementSettings._velocity = Vector3.zero;

        m_externalMovementSettings._velocity *= 1 - drag / componentSettings._rigidbody.mass * dt;

        if (isKinematicHit.Value && !isKinematicHit.BeforeValue)
        {
            parentPos = parent.transform.position;
            parentPosBefore = parent.transform.position;
        }
        else if (isKinematicHit.Value && isKinematicHit.BeforeValue)
        {
            parentPosBefore = parentPos;
            parentPos = parent.transform.position;
            m_moveVelocity.WS += (parentPos - parentPosBefore) / dt;
        }
        else if (!isKinematicHit.Value && isKinematicHit.BeforeValue)
        {
            m_externalMovementSettings._velocity += (parentPos - parentPosBefore) / dt;
        }

        m_jumpVelocity.WS -= Vector3.Project(m_externalMovementSettings._velocityBefore, gravityDirection);
        m_jumpVelocity.WS += Vector3.Project(m_externalMovementSettings._velocity, gravityDirection);

        m_moveVelocity.WS += Vector3.ProjectOnPlane(m_externalMovementSettings._velocity, gravityDirection);

        m_externalMovementSettings._velocityBefore.y -= m_externalMovementSettings._velocity.y;
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

            accelerationGive = Vector3.zero;
            impulseGive = Vector3.zero;
            return;
        }

        _groundedDepth = 0;
        normals.Clear();

        isKinematicHit.Value = false;

        _horizontalDisplacement = CollideAndSlide(m_moveVelocity.WS * dt, m_componentSettings._rigidbody.transform.position - m_characterSizeSettings.height.Value * gravityDirection * 0.5f, 0, false);
        _verticalDisplacement = CollideAndSlide(m_jumpVelocity.WS * dt, m_componentSettings._rigidbody.transform.position - m_characterSizeSettings.height.Value * gravityDirection * 0.5f + _horizontalDisplacement, 0, true);

        if (!isKinematicHit.Value) parent = null;

        _isCollidedStateful.Value = _isCollidedHorizontal || _isCollidedVertical;

        _displacement = _horizontalDisplacement + _verticalDisplacement;
        _nextPositionWS = _displacement + m_componentSettings._rigidbody.transform.position;

        m_componentSettings._rigidbody.MovePosition(_nextPositionWS);

        accelerationGive = Vector3.zero;
        impulseGive = Vector3.zero;
    }

    private Vector3 CollideAndSlide(Vector3 vel, Vector3 pos, int depth, bool state, Vector3 velInit = default)
    {
        if (depth >= m_physicsSettings._maxBounces) return Vector3.zero;

        if (depth == 0)
        {
            velInit = vel;
            switch (state)
            {
                case false:
                    _isCollidedHorizontal = false;
                    _isGrounded.OnUpdate(false);
                    //_isGrounded.Value = false;
                    break;
                case true:
                    _isCollidedVertical = false;
                    break;
            }
        }

        float dist = vel.magnitude + m_physicsSettings.skinWidth;
        Vector3 capsulePoint = (m_characterSizeSettings.height.Value * 0.5f - m_characterSizeSettings.capsuleRadius.Value) * _playerUp.normalized;
        Vector3 characterLowestPosition = pos - capsulePoint + gravityDirection * m_characterSizeSettings.capsuleRadius.Value;

        if (Physics.CapsuleCast(pos + capsuleFocusUp, pos + capsuleFocusDown, m_characterSizeSettings.capsuleRadius.Value + m_physicsSettings.skinWidth * 0.5f, vel.normalized, out RaycastHit hit, dist, m_physicsSettings._whatIsGround, queryTrigger))
        {
            if (hit.collider.isTrigger) return CollideAndSlide(vel, pos, depth + 1, state);
            normals.Add(hit.normal);
            Vector3 flatHit = Vector3.ProjectOnPlane(hit.normal, gravityDirection).normalized;
            float scale = 1 - Vector3.Dot(flatHit, -Vector3.ProjectOnPlane(velInit, gravityDirection).normalized);

            switch (state)
            {
                case false:
                    _isCollidedHorizontal = true;
                    break;
                case true:
                    _isCollidedVertical = true;
                    break;
            }

            Vector3 snapToSurface = vel.normalized * (hit.distance - m_physicsSettings.skinWidth * 0.5f);
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(-gravityDirection, hit.normal);

            // the terrain is inside the collider
            if (snapToSurface.magnitude <= m_physicsSettings.skinWidth) snapToSurface = Vector3.zero;

            // if flat ground or slope
            if (angle <= m_stepAndSlopeHandleSettings._maxSlopeAngle || _isUpStep || _isDownStep)
            {
                _isGrounded.Value = true;
                _groundNormal = (_groundNormal * _groundedDepth + hit.normal).normalized;
                _groundedDepth++;

                if (hit.collider.attachedRigidbody != null)
                {
                    isKinematicHit.Value = true;
                    parent = hit.rigidbody;
                }

                if (state)
                {
                    if (angle <= m_stepAndSlopeHandleSettings._maxSlopeAngle)
                    {
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
                switch (state)
                {
                    case false:
                        if (StepUp(characterLowestPosition, hit, flatHit, vel, out Vector3 additionalUpStep))
                        {
                            snapToSurface += additionalUpStep;
                        }
                        else
                        {
                            if (_isGrounded.Value)
                                leftover = Vector3.Project(leftover, Vector3.Cross(hit.normal, _groundNormal).normalized);
                            else
                                leftover = projectAndScale(leftover, hit.normal) * scale;
                        }
                        break;
                    case true:
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

        else if (state && _isGroundedBeforeFrame && !_isCollidedHorizontal && m_jumpVelocity.IS.Value == 0)
        {
            if (StepDown(characterLowestPosition, vel, out Vector3 additionalDownStep)) {
                _isDownStep = true;
                return CollideAndSlide(additionalDownStep, pos, depth + 1, true, velInit);
            }
        }
        m_movementSettings.coyoteTime.Reset();
        return vel;
    }

    private Vector3 projectAndScale(Vector3 a, Vector3 n)
    {
        return Vector3.ClampMagnitude(Vector3.ProjectOnPlane(a, n), a.magnitude);
    }

    private void UpdateProperties()
    {
        m_characterSizeSettings.UpdateProperties(m_playerHeight.WS);
        _gravityStateful.OnUpdate();
        m_jumpVelocity.IS.OnUpdate();

        _externalDragStateful.OnUpdate(m_externalMovementSettings._contactDrag, m_externalMovementSettings._airDrag);

        _isCollidedStateful.OnUpdate();

        if (m_characterSizeSettings.height.IsChanged)
        {
            m_componentSettings._capsuleCollider.height = m_characterSizeSettings.height.Value - m_physicsSettings.skinWidth * 2f;
            m_componentSettings._capsuleCollider.transform.localPosition = _playerUp.normalized * m_characterSizeSettings.height.Value * 0.5f;
        }

        if (m_characterSizeSettings.capsuleRadius.IsChanged)
        {
            m_componentSettings._capsuleCollider.radius = m_characterSizeSettings.capsuleRadius.Value - m_physicsSettings.skinWidth;
        }

        m_movementSettings.UpdateProperties(_gravityStateful.Value, _isGrounded.Value, dt);

        isKinematicHit.OnUpdate();
    }

    private bool StepUp(Vector3 characterLowestPosition, RaycastHit hit, Vector3 flatHit, Vector3 vel, out Vector3 additionalUpStep)
    {
        if (m_stepAndSlopeHandleSettings._isUpStepEnabled && !_isDownStep && _isGrounded.BeforeValue)
        {
            Vector3 pivot = characterLowestPosition + Vector3.ProjectOnPlane(hit.point - characterLowestPosition, gravityDirection) - gravityDirection * m_stepAndSlopeHandleSettings._maxStepUpHeight;

            // check wall height is less than max step up height
            if (Physics.Raycast(pivot + flatHit * m_characterSizeSettings.capsuleRadius.Value,
                -flatHit,
                m_characterSizeSettings.capsuleRadius.Value + 0.01f,
                WhatIsGround,
                queryTrigger)
            )
            {
                additionalUpStep = Vector3.zero;
                return false;
            }

            // check ground on step
            if (!Physics.Raycast(pivot - flatHit * 0.01f, gravityDirection, out RaycastHit h1, m_stepAndSlopeHandleSettings._maxStepUpHeight, m_physicsSettings._whatIsGround, queryTrigger))
            {
                additionalUpStep = Vector3.zero;
                return false;
            }

            // check next position
            bool b2 = Physics.SphereCast(characterLowestPosition + Vector3.ProjectOnPlane(vel, gravityDirection) + (m_stepAndSlopeHandleSettings._maxStepUpHeight + m_characterSizeSettings.capsuleRadius.Value) * (-gravityDirection),
                m_characterSizeSettings.capsuleRadius.Value + m_physicsSettings.skinWidth,
                gravityDirection,
                out RaycastHit h2,
                m_stepAndSlopeHandleSettings._maxStepUpHeight,
                WhatIsGround,
                queryTrigger);

            float upStepDistance = Vector3.Dot(h2.point + (h2.normal + gravityDirection) * m_characterSizeSettings.capsuleRadius.Value - characterLowestPosition, -gravityDirection) + m_physicsSettings.skinWidth;
            if (b2 && upStepDistance <= m_stepAndSlopeHandleSettings._maxStepUpHeight && upStepDistance > 0f && Vector3.Angle(h1.normal, -gravityDirection) <= MaxSlopeAngle && Vector3.Angle(-Vector3.ProjectOnPlane(hit.normal, gravityDirection), Vector3.ProjectOnPlane(vel, gravityDirection)) <= m_stepAndSlopeHandleSettings._maxStepUpWallAngle)
            {
                _isUpStep = true;
                additionalUpStep = upStepDistance * (-gravityDirection);
                return true;
            }
        }
        additionalUpStep = Vector3.zero;
        return false;
    }

    private bool StepDown(Vector3 characterLowestPosition, Vector3 vel, out Vector3 additionalDownStep)
    {
        if (m_stepAndSlopeHandleSettings._isDownStepEnabled && m_movementSettings.jumpBufferTime.Value <= 0)
        {
            Ray r = new Ray(characterLowestPosition + vel, gravityDirection);
            bool b = Physics.Raycast(r, out RaycastHit h, m_stepAndSlopeHandleSettings._maxStepDownHeight, m_physicsSettings._whatIsGround, queryTrigger);
            bool b1 = Physics.SphereCast(
                r.origin - (m_characterSizeSettings.capsuleRadius.Value + m_playerHeight.WS * 0.5f) * gravityDirection,
                m_componentSettings._capsuleCollider.bounds.extents.x,
                gravityDirection,
                out RaycastHit h2,
                m_stepAndSlopeHandleSettings._maxStepDownHeight + m_playerHeight.WS * 0.5f,
                m_physicsSettings._whatIsGround, queryTrigger);

            if (!_isUpStep && b && b1 && Vector3.Dot(characterLowestPosition - (h2.point + (h2.normal + gravityDirection) * m_characterSizeSettings.capsuleRadius.Value), _playerUp) > 0 && Vector3.Angle(_playerUp, h.normal) <= MaxSlopeAngle)
            {
                additionalDownStep = gravityDirection * m_stepAndSlopeHandleSettings._maxStepUpHeight;
                return true;
            }
        }
        additionalDownStep = Vector3.zero;
        return false;
    }

    public void SetMoveVelocityIS(Vector3 value) => m_moveVelocity.IS = value;

    public void SetJumpVelocityIS(float value) => m_jumpVelocity.IS.Value = value;

    public void SetSprintInputIS(float value) => m_sprintInput.IS = value;

    public void SetPlayerHeightIS(float value) => m_playerHeight.IS = value;


    public void AddForce(Vector3 force, ForceMode forceMode = ForceMode.Force)
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

    public void AddRelativeForce(Vector3 force, ForceMode forceMode = ForceMode.Force)
    {
        AddForce(transform.TransformDirection(force), forceMode);
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
