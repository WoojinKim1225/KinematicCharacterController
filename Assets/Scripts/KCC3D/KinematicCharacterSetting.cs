using UnityEngine;


namespace KinematicCharacterSettings
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
        public Rigidbody _rigidbody;

        public CapsuleCollider _capsuleCollider;
    }

    [System.Serializable]
    public class MovementSettings
    {
        // Sets character's forward and right directions
        public Vector3 _viewDirection;

        [Tooltip("Determines player's movement speed.")]
        public float _moveSpeed = 4f;

        [Tooltip("Controls initial jump speed.")]
        public float _jumpSpeed = 10f;

        [Tooltip("Controls maximum jump height.")]
        public float _jumpMaxHeight = 2f;


        public float _jumpBufferTime = 0.1f;
        public float _coyoteTime = 0.1f;
        
        [Tooltip("Increases movement speed while sprinting.")]
        public float _sprintSpeedMultiplier = 2f;

        [Tooltip("Decreases movement speed while crouching.")]
        public float _crouchSpeedMultiplier = 0.5f;

        [Tooltip("Sets maximum number of midair jumps.")]
        public int _maxAirJumpCount = 5;

        [Tooltip("Controls character speed mode.")]
        public KinematicCharacterSettingExtensions.ESpeedControlMode _speedControlMode = KinematicCharacterSettingExtensions.ESpeedControlMode.Constant;
        
        [Tooltip("Acceleration in move speed when linear mode.")]
        public float _moveAcceleration = 25f;
        
        [Tooltip("Damp in move speed when exponential mode.")]
        public float _moveDamp = 10f;
        
        // [Tooltip("Controls character movement mode.")]
        // public KinematicCharacterSettingExtensions.EMovementMode _movementMode = KinematicCharacterSettingExtensions.EMovementMode.Ground;
    }

    [System.Serializable]
    public class PhysicsSettings
    {
        [Tooltip("Additional space around the collision shape to prevent collision issues and clipping.")]
        public float _skinWidth = 0.01f;

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
        public float _idleHeight = 2f;

        [Delayed]
        [Tooltip("The height of the capsule collider when crouched.")]
        public float _crouchHeight = 1.2f;

        [Delayed]
        [Tooltip("The radius of the capsule collider.")]
        public float _capsuleRadius = 0.5f;
    }

    [System.Serializable]
    public class StepAndSlopeHandleSettings
    {
        [Tooltip("Maximum angle of a slope the player can walk up.")]
        public float _maxSlopeAngle = 55f;

        [Tooltip("Minimum angle of a ceiling the player's movement won't be canceled.")]
        public float _minCeilingAngle = 130f;

        [Tooltip("Can the player can step up onto higher surfaces without obstruction?")]
        public bool _isUpStepEnabled = true;

        [Tooltip("The maximum height the player can step up onto.")]
        public float _maxStepUpHeight = 0.4f;

        [Tooltip("Can the player can step down from higher surfaces without falling?")]
        public bool _isDownStepEnabled = true;

        [Tooltip("The maximum height the player can step down from.")]
        public float _maxStepDownHeight = 0.4f;
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

        public float _contactDrag = 0.5f;
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
        public enum ESpeedControlMode { Constant, Linear, Exponential }
        // public enum EMovementMode { Ground, Swim }
        public enum EGravityMode {Single, Multiple}
    }
}
