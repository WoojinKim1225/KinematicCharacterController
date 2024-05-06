using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using ReferenceManager;

// Stateful Class for Float
public class Float
{
    private float _value;
    private bool _isChanged;
    private float _beforeValue;
    private float _initialValue;

    public float Value { get => _value; set => this._value = value; }

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
    [SerializeField] private float _moveSpeed = 4f;
    public float MoveSpeed => _moveSpeed;

    [SerializeField] private float _idleHeight = 2f;
    public float IdleHeight => _idleHeight;

    [SerializeField] private float _crouchHeight = 1.2f;
    public float CrouchHeight => _crouchHeight;

    [SerializeField] private float _capsuleRadius = 0.5f;
    public float CapusleRadius => _capsuleRadius;
    private Float _height, _radius;

    [SerializeField] private float _jumpSpeed = 10f;
    [SerializeField] private float _jumpMaxHeight = 2f;
    public float JumpMaxHeight => _jumpMaxHeight;

    private Float _jumpS, _jumpMaxH;

    [SerializeField] private float _sprintSpeedMultiplier = 2f;
    [Range(0f, 89.99f)]
    [SerializeField] private float _maxSlopeAngle = 55f;
    public float MaxSlopeAngle => _maxSlopeAngle;

    [SerializeField] private float _minCeilingAngle = 130f;

    private Vector3 _forward, _right;

    private Vector2 _moveVelocityIS;
    private float _jumpVelocityIS;
    private float _crouchInputIS;
    private float _sprintInputIS;

    private Vector3 _moveVelocityOS;
    private Vector3 _jumpVelocityOS;
    private float _playerHeightOS;

    private Vector3 _moveVelocityTS;

    private Vector3 _moveVelocityWS;
    private Vector3 _jumpVelocityWS;
    private float _playerHeightWS;

    private Vector3 _hitPointDisplacement;
    private Vector3 _horizontalDisplacement, _verticalDisplacement, _stepDisplacement;
    private Vector3 _nextPositionWS;

    [SerializeField] private bool _isGrounded = false;
    [SerializeField] private Vector3 _groundNormal = Vector3.up;
    [SerializeField] private Vector3 _playerUp = Vector3.up;

    private RaycastHit hit;
    [SerializeField] private LayerMask _whatIsGround;

    [SerializeField] private float _stepHeight = 0.3f;
    #endregion

    void Awake() {
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

    #region Methods
    void GetDirectionsFromView()
    {
        _forward = Vector3.ProjectOnPlane(_viewDirection, _playerUp).normalized;
        _right = Vector3.Cross(_playerUp, _forward);
    }

    void CalculateObjectSpaceVariables()
    {
        _moveVelocityOS = new Vector3(_moveVelocityIS.x, 0, _moveVelocityIS.y) * _moveSpeed * (_sprintInputIS > 0 ? _sprintSpeedMultiplier : 1f);
        _jumpVelocityOS = _isGrounded && _jumpVelocityIS > 0 ? _jumpVelocityIS * _jumpSpeed * _playerUp + _gravity * Time.fixedDeltaTime * 0.5f : _gravity * Time.fixedDeltaTime;
        _playerHeightOS = _crouchInputIS > 0 ? _crouchHeight : _idleHeight;
    }

    void CalculateTangentSpaceVariables()
    {
        _moveVelocityTS = _moveVelocityOS.x * _right + _moveVelocityOS.z * _forward;
    }

    void CalculateWorldSpaceVariables()
    {
        _moveVelocityWS = Quaternion.FromToRotation(_playerUp, _groundNormal) * _moveVelocityTS;
        if (!_isGrounded) _jumpVelocityWS += _gravity * Time.fixedDeltaTime;
        else _jumpVelocityWS = _jumpVelocityOS;
        _playerHeightWS = _playerHeightOS * transform.localScale.y;
    }

    void HandleCollisionsAndMovement()
    {
        _hitPointDisplacement = Vector3.zero;
        _horizontalDisplacement = CollideAndSlide(_moveVelocityWS * Time.fixedDeltaTime, _rb.position + _height.Value * _playerUp * 0.5f, 0, false);
        _verticalDisplacement = CollideAndSlide(_jumpVelocityWS * Time.fixedDeltaTime, _rb.position + _height.Value * _playerUp * 0.5f + _horizontalDisplacement, 0, true);

        _nextPositionWS = _horizontalDisplacement + _verticalDisplacement + _hitPointDisplacement + _rb.position;
        _rb.MovePosition(_nextPositionWS);
    }

    Vector3 CollideAndSlide(Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit = default)
    {
        if (depth >= _maxBounces)
        {
            return Vector3.zero;
        }
        if (depth == 0)
        {
            velInit = vel;
            if (!gravityPass) _isGrounded = false;
        }

        float dist = vel.magnitude + _skinWidth;

        /*
        Debug.DrawRay(pos - _playerUp * (_height.Value * 0.5f - 0.1f), vel.normalized * _characterCollider.bounds.extents.x * 2f, Color.white);
        if (!gravityPass && Physics.Raycast(pos - _playerUp * (_height.Value * 0.5f - 0.1f), vel.normalized, out stepHit, _characterCollider.bounds.extents.x, _whatIsGround)) {
            Debug.Log("step!");
        }
        */

        if (Physics.CapsuleCast(pos + (_height.Value * 0.5f - _radius.Value) * _playerUp, pos - (_height.Value * 0.5f - _radius.Value) * _playerUp, _characterCollider.bounds.extents.x, vel.normalized, out hit, dist, _whatIsGround))
        {
            Debug.DrawRay(hit.point, hit.normal, gravityPass? Color.cyan : Color.magenta);

            Vector3 snapToSurface = vel.normalized * (hit.distance - _skinWidth);
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(_playerUp, hit.normal);

            if (snapToSurface.magnitude <= _skinWidth)
            {
                snapToSurface = Vector3.zero;
            }

            if (angle <= _maxSlopeAngle)  // if flat ground or slope...
            {
                if (gravityPass)
                {
                    _isGrounded = true;
                    _groundNormal = hit.normal;
                    
                    return snapToSurface;
                }

                leftover = projectAndScale(leftover, hit.normal);
            }
            else if (angle >= _minCeilingAngle) // if ceiling...
            {
                _jumpVelocityWS = Vector3.zero;
                return snapToSurface;
            }
            else // if wall...
            {
                float scale = 1 - Vector3.Dot(new Vector3(hit.normal.x, 0, hit.normal.z).normalized, -new Vector3(velInit.x, 0, velInit.z).normalized);

                if (_isGrounded && !gravityPass)
                {
                    leftover = projectAndScale(new Vector3(leftover.x, 0, leftover.z), -new Vector3(hit.normal.x, 0, hit.normal.z)).normalized * scale;
                }
                else
                {
                    leftover = projectAndScale(leftover, hit.normal) * scale;
                }

                if (!gravityPass) {
                    Vector3 stepPointA = new Vector3(pos.x, pos.y - _height.Value * 0.5f + _stepHeight, pos.z);
                    Vector3 stepDirA = -new Vector3(hit.normal.x, 0, hit.normal.z).normalized;
                    float stepSizeA = _radius.Value + vel.magnitude;

                    Debug.DrawRay(stepPointA, stepDirA * stepSizeA, Color.red);
                    if (!Physics.Raycast(stepPointA, stepDirA, stepSizeA, _whatIsGround)) {
                        Debug.DrawRay(stepPointA + stepDirA * stepSizeA + _playerUp, -_playerUp * 2f, Color.red);
                        if (Physics.Raycast(stepPointA + stepDirA * stepSizeA + _playerUp, -_playerUp, out RaycastHit stepHit, 2f, _whatIsGround)) {
                            Vector3 a = new Vector3(hit.normal.x, 0, hit.normal.z).normalized * (_radius.Value * new Vector3(hit.normal.x, 0, hit.normal.z).magnitude - vel.magnitude);
                            float b = Mathf.Sqrt(_radius.Value * _radius.Value - Vector3.Dot(a, a));
                            leftover = vel - snapToSurface;
                            leftover.y = stepHit.point.y + b - _radius.Value + _height.Value * 0.5f - pos.y;
                        }
                    }
                }
            }

            return snapToSurface + CollideAndSlide(leftover, pos + snapToSurface, depth + 1, gravityPass, velInit);
        }
        return vel;
    }

    Vector3 projectAndScale(Vector3 a, Vector3 n)
    {
        float mag = a.magnitude;
        return Vector3.ProjectOnPlane(a, n).normalized * mag;
    }

    void UpdateProperties()
    {
        _height.OnUpdate(_playerHeightWS);
        _radius.OnUpdate(_capsuleRadius);
        _jumpS.OnUpdate(_jumpSpeed);
        _jumpMaxH.OnUpdate(_jumpMaxHeight);

        if (_height.IsChanged)
        {
            _characterCollider.height = _height.Value;
            _characterCollider.center = _height.Value * 0.5f * Vector3.up;
        }

        if (_radius.IsChanged)
        {
            _characterCollider.radius = _radius.Value;
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
}