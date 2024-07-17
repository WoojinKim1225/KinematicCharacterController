using UnityEngine;
using StatefulVariables;
using System.Xml;


namespace KCCSettings
{
    [System.Serializable]
    public struct GravitySet {
        /// <summary>
        /// the threshold of a player's vertical speed that changes player's gravity.
        /// </summary>
        public float verticalSpeedThreshold;
        /// <summary>
        /// the gravity that the player's get after the threshold
        /// </summary>
        public Vector3 gravity;
    }
    
    [System.Serializable]
    public class ComponentSettings
    {
        public Rigidbody rigidbody;

        public CapsuleCollider capsuleCollider;
    }

    [System.Serializable]
    public class MovementSettings
    {
        [Tooltip("Sets character's forward and right directions")]
        public Vector3 viewDirection;

        [Tooltip("Determines player's movement speed.")]
        public float moveSpeed = 4f;

        [Tooltip("Controls initial jump speed.")]
        [SerializeField] private float _jumpSpeed = 10f;
        public float JumpSpeed => _jumpSpeed;

        private FloatStateful _jumpSpeedStateful;
        public float GetJumpSpeedStateful() => _jumpSpeedStateful.Value;

        [Tooltip("Controls maximum jump height.")]
        [SerializeField] private float _jumpMaxHeight = 2.5f;
        public float JumpMaxHeightInputValue => _jumpMaxHeight;

        private FloatStateful _jumpMaxHeightStateful;
        public float GetJumpMaxHeightStateful() => _jumpMaxHeightStateful.Value;

        [SerializeField] private float _jumpBufferTime = 0.2f;
        public float JumpBufferTimeInputValue => _jumpBufferTime;
        private Timer _jumpBufferTimer;
        public float GetJumpBufferTimer() => _jumpBufferTimer.Value;
        public void ResetJumpBufferTimer(float value) => _jumpBufferTimer.Reset(value);

        [SerializeField] private float _coyoteTime = 0.2f;
        public float CoyoteTimeInputValue => _coyoteTime;
        private Timer _coyoteTimer;
        public float GetCoyoteTimer() => _coyoteTimer.Value;
        public void SetCoyoteTimerValue2End() => _coyoteTimer.Value = _coyoteTimer.EndValue;
        public void ResetCoyoteTimerValue(float value) => _coyoteTimer.Reset(value);
        
        [Tooltip("Increases movement speed while sprinting.")]
        public float _sprintSpeedMultiplier = 2f;

        [Tooltip("Decreases movement speed while crouching.")]
        public float _crouchSpeedMultiplier = 0.5f;

        [Tooltip("Sets maximum number of midair jumps.")]
        [SerializeField] private int _maxAirJumpCount = 1;
        public int MaxAirJumpCount => _maxAirJumpCount;
        private Timer airJumpTimer;
        public int GetAirJumpTimer() => (int)airJumpTimer.Value;
        public void UpdateAirJumpTimer(float value) => airJumpTimer.OnSubtract(value);

        [Tooltip("Controls character speed mode.")]
        [SerializeField] private KinematicCharacterSettingExtensions.ESpeedControlMode _speedControlMode = KinematicCharacterSettingExtensions.ESpeedControlMode.Constant;
        public KinematicCharacterSettingExtensions.ESpeedControlMode SpeedControlMode => _speedControlMode;
        
        [Tooltip("Acceleration in move speed when linear mode.")]
        [SerializeField] private float _moveAcceleration = 25f;
        public float MoveAcceleration => _moveAcceleration;
        
        [Tooltip("Damp in move speed when exponential mode.")]
        public float _moveDamp = 10f;

        public AnimationCurve _moveAnimationCurve;

        public void InitProperties(){
            _jumpSpeedStateful.Reset(_jumpSpeed);
            _jumpMaxHeightStateful.Reset();
            airJumpTimer.Init(_maxAirJumpCount, 0);
            _jumpBufferTimer.Init(_jumpBufferTime, 0);
            _coyoteTimer.Init(_coyoteTime, 0);
        }
            
        public void UpdateProperties(Vector3 gravity, bool isGrounded, float dt) {
            _jumpSpeedStateful.OnUpdate(_jumpSpeed);
            _jumpMaxHeightStateful.OnUpdate(_jumpMaxHeight);
            airJumpTimer.OnUpdate(_maxAirJumpCount);

            if (_jumpSpeedStateful.IsChanged) {
                _jumpMaxHeightStateful.Reset(_jumpSpeedStateful.Value * _jumpSpeedStateful.Value * 0.5f / Mathf.Abs(gravity.y));
                _jumpMaxHeight = _jumpMaxHeightStateful.Value;
            }
            else if (_jumpMaxHeightStateful.IsChanged) {
                _jumpSpeedStateful.Reset(Mathf.Sqrt(2f * Mathf.Abs(gravity.y) * _jumpMaxHeightStateful.Value));
                _jumpSpeed = _jumpSpeedStateful.Value;
            }

            //airJumpTimer.OnUpdate();
            if (isGrounded) airJumpTimer.Reset(_maxAirJumpCount);
            _jumpBufferTimer.OnSubtract(dt);
            _coyoteTimer.OnSubtract(dt);
        }
    }

    [System.Serializable]
    public class PhysicsSettings
    {
        [Tooltip("Additional space around the collision shape to prevent collision issues and clipping.")]
        public float skinWidth = 0.01f;

        [Tooltip("Number of cycles used to process collider collisions.")]
        public int _maxBounces = 5;

        public KinematicCharacterSettingExtensions.EGravityMode _gravityMode = KinematicCharacterSettingExtensions.EGravityMode.Single;

        [Tooltip("Acceleration due to gravity affecting the character's movement.")]
        public Vector3 _gravity;

        public GravitySet[] _gravityList; 

        [Tooltip("Layer that collides with character's collider")]
        public LayerMask _whatIsGround = Physics.AllLayers;
    }

    [System.Serializable]
    public class CharacterSizeSettings
    {
        [Delayed]
        [Tooltip("The idle height of the capsule collider.")]
        public float idleHeight = 2f;

        [Delayed]
        [Tooltip("The height of the capsule collider when crouched.")]
        public float crouchHeight = 1.2f;

        [Tooltip("The radius of the capsule collider.")]
        public FloatStateful capsuleRadius = new FloatStateful(0.5f);

        public FloatStateful height;

        public void InitProperties(){
            capsuleRadius.Reset();
            height.Reset(idleHeight);
        }

        public void UpdateProperties(float h) {
            capsuleRadius.OnUpdate();
            height.OnUpdate(h);
        }
    }

    [System.Serializable]
    public class StepAndSlopeHandleSettings
    {
        [Tooltip("Maximum angle of a slope the player can walk up.")]
        public float _maxSlopeAngle = 55f;

        [Tooltip("Minimum angle of a ceiling the player's movement won't be canceled.")]
        public float _minCeilingAngle = 175f;

        [Tooltip("Can the player can step up onto higher surfaces without obstruction?")]
        public bool _isUpStepEnabled = true;

        [Tooltip("The maximum height the player can step up onto.")]
        public float _maxStepUpHeight = 0.4f;

        [Tooltip("Can the player can step down from higher surfaces without falling?")]
        public bool _isDownStepEnabled = true;

        [Tooltip("The maximum height the player can step down from.")]
        public float _maxStepDownHeight = 0.4f;

        public float _maxStepUpWallAngle = 45f;
    }

    [System.Serializable]
    public class ExternalMovementSettings
    {
        public Vector3 _groundMove;
        public Vector3 _acceleration;
        public Vector3 _velocity;
        public Vector3 _velocityBefore;
        public Vector3 _position;
        public bool _isPositionSet;

        public float _contactDrag = 8f;
        public float _airDrag = 1f;
    }

    [System.Serializable]
    public struct Coordinate<TIS, TOS, TTS, TWS>
    {
        public TIS IS;
        public TOS OS;
        public TTS TS;
        public TWS WS;
    }

    public static class KinematicCharacterSettingExtensions
    {
        public enum EDimension {TwoDimension, ThreeDimension}
        public enum ESpeedControlMode { Constant, Linear, Exponential, CustomCurve}
        public enum EGravityMode {Single, Multiple}
    }
}
