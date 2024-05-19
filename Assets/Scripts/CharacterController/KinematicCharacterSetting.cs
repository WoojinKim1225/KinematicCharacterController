using UnityEngine;


namespace KinematicCharacterSettings {

    [System.Serializable]
    public class ComponentSettings
    {
        public Rigidbody _rigidbody;
        public CapsuleCollider _capsuleCollider;
    }

    [System.Serializable]
    public class MovementSettings
    {
        public Vector3 _viewDirection = Vector3.forward;
        public float _moveSpeed = 4f;
        public float _jumpSpeed = 10f;
        public float _jumpMaxHeight = 2f;
        public float _sprintSpeedMultiplier = 2f;
        public float _crouchSpeedMultiplier = 0.5f;
        public int _maxAirJumpCount = 5;

    
        public KinematicCharacterSettingExtensions.ESpeedControlMode _speedControlMode = KinematicCharacterSettingExtensions.ESpeedControlMode.Constant;
        public KinematicCharacterSettingExtensions.EMovementMode _movementMode = KinematicCharacterSettingExtensions.EMovementMode.Ground;
        public float _moveAcceleration = 25f;
        public float _moveDamp = 10f;
    }

    [System.Serializable]
    public class PhysicsSettings
    {
        public float _skinWidth = 0.01f;
        public int _maxBounces = 5;
        public Vector3 _gravity = Vector3.down * 20f;
    }

    [System.Serializable]
    public class CharacterSizeSettings
    {
        public float _idleHeight = 2f;
        public float _crouchHeight = 1.2f;
        public float _capsuleRadius = 0.5f;
    }

    [System.Serializable]
    public class StepAndSlopeHandleSettings
    {
        public float _maxSlopeAngle = 55f;
        public float _minCeilingAngle = 130f;

        public bool _isUpStepEnabled = true;
        public float _maxStepUpHeight = 0.4f;
        public bool _isDownStepEnabled = true;
        public float _maxStepDownHeight = 0.4f;
    }

    [System.Serializable]
    public class ExternalMovementSettings {
        public Vector3 _groundMove;
        public Vector3 _acceleration;
        public Vector3 _velocity;
        public Vector3 _position;
        public bool _isPositionSet;

        public KinematicCharacterSettingExtensions.ESpeedControlMode _speedControlMode = KinematicCharacterSettingExtensions.ESpeedControlMode.Linear;
        public float _moveAcceleration = 10f;
    }

    [System.Serializable]
    public struct Coordinate<TIS, TOS, TTS, TWS> {
        public TIS IS;
        public TOS OS;
        public TTS TS;
        public TWS WS;
    }

    public static class KinematicCharacterSettingExtensions
    {
        public enum ESpeedControlMode { Constant, Linear, Exponential };
        public enum EMovementMode { Ground, Swim };
    }
}