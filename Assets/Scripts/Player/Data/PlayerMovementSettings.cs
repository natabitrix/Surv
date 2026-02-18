// Assets/Scripts/Player/Data/PlayerMovementSettings.cs
using UnityEngine;

namespace Assets.Scripts.Player.Data
{
    [CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "Player/Movement Settings")]
    public class PlayerMovementSettings : ScriptableObject
    {

        [Tooltip("Высота коллайдера Character в положении лежа")]
        public float LieCharacterColliderHeight = 0.5f;

        [Header("Ground Movement")]
        public float MoveSpeed = 2f;
        public float SprintSpeed = 6f;
        public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10f;
        public float InputSmoothingRate = 20f;
        public float KeyboardTurnSpeed = 90f;

        [Header("Jump & Gravity")]
        public float JumpHeight = 1.2f;
        public float Gravity = -15f;
        public float JumpTimeout = 0.5f;
        public float FallTimeout = 0.15f;
        public float TerminalVelocity = 53f;

        // [Header("Crouch")]
        // public float CrouchTimeout = 0.5f;

        [Header("Ladder")]
        public float LadderClimbSpeed = 3f;

        [Header("Swim")]
        public float SwimSpeed = 2f;
        public float SwimSprintSpeed = 6f;
        public float SwimVerticalSpeed = 1.5f;

        [Tooltip("Насколько приподнять тело в воде в покое")]
        public float ShiftIdleAboveWater = 0.3f;
        [Tooltip("На какой глубине медленная скорость погружения")]
        public float DepthIdleDownSlow = 0.5f;
        [Tooltip("Скорость всплытия по прыжку")]
        public float DiveUpJumpSpeed = 1.5f;
        [Tooltip("Скорость погружения по приседанию")]
        public float DiveDownCrouchSpeed = -1.5f;

        [Tooltip("Скорость погружения в покое")]
        public float DiveDownIdleSpeed = -0.2f;
        [Tooltip("Скорость погружения в покое у поверхности")]
        public float DiveDownIdleSlowSpeed = -0.05f;

        [Tooltip("Угол снижения камеры для плытия вверх")]
        public float DiveCinemachineAngleUp = -10f;
        [Tooltip("Угол поднятия камеры для плытия вниз")]
        public float DiveCinemachineAngleDown = 20f;


        [Header("Ground Check")]
        public float GroundedOffset = 0f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Water")]
        public LayerMask WaterLayers;

        [Header("Camera")]
        public float TopClamp = 50f;
        public float BottomClamp = -70f;
        public float CameraAngleOverride = 0f;

        [Header("Audio")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Header("System")]
        public bool ShowCustomGizmo = false;
    }
}