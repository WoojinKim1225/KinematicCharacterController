using UnityEngine;


namespace KinematicCharacterEnum {
    public static class KinematicCharacterEnumExtensions
    {
        public enum ESpeedControlMode { Constant, Linear, Exponential };
        public enum EMovementMode { Ground, Swim };
    }
}