using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(KinematicCharacterController))]
[CanEditMultipleObjects]
public class KinematicCharacterControllerEditor : Editor
{
    bool debug;

    SerializedProperty moveReference;
    SerializedProperty jumpReference;
    SerializedProperty crouchReference;
    SerializedProperty sprintReference;

    bool showPhysics;

    SerializedProperty rigidbody;
    GUIContent rigidbodyContent = new GUIContent(
        "    Rigidbody",
        "The rigidbody of a character controller. Will auto allocate. \n\n캐릭터 컨트롤러가 사용할 강체입니다. 자동적으로 배치됩니다.");

    SerializedProperty collider;
    GUIContent colliderContent = new GUIContent(
        "    Collider",
        "The capsule collider belonging to the character controller's child. Will auto allocate. \n\n캐릭터 컨트롤러가 사용할 자식의 캡슐콜라이더입니다. 자동적으로 배치됩니다.");

    bool showColliders;

    SerializedProperty capsuleRadius;
    GUIContent capsuleRadiusContent = new GUIContent(
        "    Capsule Radius",
        "The radius of the capsule collider. \n\nThis determines the width of the player's collision area."
        + "\n--------------------\n"
        + "캡슐 콜라이더의 반지름입니다. \n\n플레이어의 충돌 영역의 너비를 결정합니다."
    );

    SerializedProperty capsuleHeight;
    GUIContent capsuleHeightContent = new GUIContent(
        "    Capsule Height",
        "The idle height of the capsule collider. \n\nThis defines the player's default standing height."
        + "\n--------------------\n"
        + "캡슐 콜라이더의 기본 높이입니다. \n\n플레이어의 기본 서있는 높이를 정의합니다."
    );

    SerializedProperty crouchedCapsuleHeight;
    GUIContent crouchedCapsuleHeightContent = new GUIContent(
        "    Crouched Capsule Height",
        "The height of the capsule collider when crouched. \n\nThis defines the player's height while crouching."
        + "\n--------------------\n"
        + "웅크릴 때의 캡슐 콜라이더 높이입니다. \n\n플레이어가 웅크렸을 때의 높이를 정의합니다."
    );

    SerializedProperty skinWidth;
    GUIContent skinWidthContent = new GUIContent(
        "    Collider Skin Width",
        "Additional space around the collision shape to prevent collision issues and clipping. \n\nThis adds padding to the collider to prevent clipping with other objects."
        + "\n--------------------\n"
        + "충돌 형태 주위에 추가적인 공간을 제공하여 충돌 문제 및 클리핑을 방지합니다. \n\n이는 콜라이더의 경계를 팽창시켜 다른 개체와의 클리핑을 방지합니다."
    );

    SerializedProperty maxBounce;
    GUIContent maxBounceContent = new GUIContent(
        "    Maximum Collision Bounces",
        "The number of cycles used to process collider collisions. \n\nThis determines the number of iterations used for collision resolution."
        + "\n--------------------\n"
        + "충돌 처리에 사용되는 계산 주기의 횟수입니다. \n\n충돌 해결에 사용되는 반복 횟수를 결정합니다."
    );

    bool showMovements;

    SerializedProperty viewDirection;
    GUIContent viewDirectionContent = new GUIContent(
        "    View Direction",
        "The direction the character is facing. \n\nThis sets the character's forward and right directions."
        + "\n--------------------\n"
        + "캐릭터의 바라보는 방향입니다. \n\n캐릭터의 앞방향과 오른쪽 방향을 설정합니다."
    );

    SerializedProperty gravity;
    GUIContent gravityContent = new GUIContent(
        "    Gravity",
        "The acceleration due to gravity affecting the character's movement. \n\nThis determines how quickly the character falls."
        + "\n--------------------\n"
        + "캐릭터의 이동에 영향을 주는 세계 공간 가속도입니다. \n\n캐릭터가 빠르게 떨어지는 속도를 결정합니다."
    );

    SerializedProperty speedControlMode;
    GUIContent speedControlModeContent = new GUIContent(
        "    Speed Control Mode",
        "The interpolation mode for player movement. \n\nThis determines how the player's movement speed changes over time."
        + "\n--------------------\n"
        + "플레이어 이동에 대한 보간 모드입니다. \n\n플레이어의 이동 속도가 시간에 따라 어떻게 변하는지 결정합니다."
    );

    SerializedProperty moveAcceleration;
    GUIContent moveAccelerationContent = new GUIContent(
        "    Move Acceleration",
        "The rate at which the player's movement speed increases. \n\nThis controls how quickly the player accelerates when moving."
        + "\n--------------------\n"
        + "플레이어 이동 속도가 증가/감소하는 속도입니다. \n\n플레이어가 이동할 때 얼마나 빨리 가속/감속하는지 결정합니다."
    );

    SerializedProperty moveDamp;
    GUIContent moveDampContent = new GUIContent(
        "    Move Damp",
        "The rate at which the player's movement speed converges. \n\nThis controls how quickly the player slows down when near the target velocity"
        + "\n--------------------\n"
        + "플레이어 이동 속도가 점근하는 속도입니다. \n\n플레이어가 목표 속도에 얼만큼 빨리 수렴할지를 결정합니다."
    );

    SerializedProperty moveSpeed;
    GUIContent moveSpeedContent = new GUIContent(
        "    Move Speed",
        "The speed at which the player moves in world space. \n\nThis determines the player's movement speed."
        + "\n--------------------\n"
        + "플레이어의 이동하는 속도입니다. \n\n플레이어의 최대 이동 속도를 결정합니다."
    );

    bool useJumpMaxHeight;

    SerializedProperty jumpSpeed;
    GUIContent jumpSpeedContent = new GUIContent(
        "    Jump Speed",
        "The initial speed when the player begins a jump. \n\nThis determines how high the player jumps initially."
        + "\n--------------------\n"
        + "플레이어가 점프를 시작할 때의 초기 속도입니다. \n\n플레이어가 처음 점프할 때 얼마나 높이 점프하는지 결정합니다."
    );

    SerializedProperty maxJumpHeight;
    GUIContent maxJumpHeightContent = new GUIContent(
        "    Maximum Jump Height",
        "The maximum height the player can reach with a jump. \n\nThis determines the maximum height the player can achieve when jumping."
        + "\n--------------------\n"
        + "점프로 플레이어가 도달할 수 있는 최대 높이입니다. \n\n플레이어가 점프를 시작할 때의 초기 속도를 결정합니다."
    );

    SerializedProperty sprintSpeedMultiplier;
    GUIContent sprintSpeedMultiplierContent = new GUIContent(
        "    Sprint Speed Multiplier",
        "The increase in movement speed when sprinting. \n\nThis determines how much faster the player moves when sprinting."
        + "\n--------------------\n"
        + "달리기 중에 이동 속도가 증가하는 비율입니다. \n\n달리기 중에 플레이어가 얼마나 빨리 이동하는지를 결정합니다."
    );
    bool showSlopeAndStepHandle;

    SerializedProperty maxSlopeAngle;
    GUIContent maxSlopeAngleContent = new GUIContent(
        "    Maximum Slope Angle",
        "The maximum angle of a slope the player can climb. \n\nThis determines the steepest slope the player can walk up."
        + "\n--------------------\n"
        + "플레이어가 오를 수 있는 최대 경사의 각도입니다. \n\n플레이어가 오를 수 있는 가장 가파른 경사를 결정합니다."
    );

    SerializedProperty minCeilingAngle;
    GUIContent minCeilingAngleContent = new GUIContent(
        "    Minimum Ceiling Angle",
        "The minimum angle of a ceiling the player can move under. \n\nIf the angle is greater than this value, the player's movement will be blocked."
        + "\n--------------------\n"
        + "플레이어가 움직일 수 있는 최소 천장의 각도입니다. \n\n만약 이 값보다 큰 각도의 천장이라면 플레이어의 움직임이 중단됩니다."
    );

    SerializedProperty isUpStepEnabled;
    GUIContent isUpStepEnabledContent = new GUIContent(
        "   Can Step Up?",
        "Whether the player can step up onto higher surfaces without obstruction. \n\nDetermines if the player can step up onto higher surfaces smoothly."
        + "\n--------------------\n"
        + "플레이어가 장애물을 만나지 않고 위로 올라갈 수 있는지 여부입니다. \n\n플레이어가 매끄럽게 위로 올라갈 수 있는지 여부를 결정합니다."
    );


    SerializedProperty maxStepUpHeight;
    GUIContent maxStepUpHeightContent = new GUIContent(
        "   Maximum Step Up Height",
        "The maximum height the player can step up onto. \n\nDetermines the maximum height difference the player can step up onto without obstruction."
        + "\n--------------------\n"
        + "플레이어가 위로 올라갈 수 있는 최대 높이입니다. \n\n플레이어가 장애물을 만나지 않고 위로 올라갈 수 있는 최대 높이를 결정합니다."
    );


    SerializedProperty isDownStepEnabled;
    GUIContent isDownStepEnabledContent = new GUIContent(
        "   Can Step Down?",
        "Whether the player can step down from higher surfaces without jumping. \n\nDetermines if the player can step down from higher surfaces smoothly without needing to jump."
        + "\n--------------------\n"
        + "플레이어가 공중에 떠 있지 않고 높은 표면에서 내려갈 수 있는지 여부입니다. \n\n플레이어가 매끄럽게 높은 표면에서 내려갈 수 있는지 여부를 결정합니다."
    );

    SerializedProperty maxStepDownHeight;
    GUIContent maxStepDownHeightContent = new GUIContent(
        "   Maximum Step Down Height",
        "The maximum height the player can step down from. \n\nDetermines the maximum height difference the player can step down from smoothly without needing to jump."
        + "\n--------------------\n"
        + "플레이어가 아래로 내려갈 수 있는 최대 높이입니다. \n\n플레이어가 매끄럽게 높은 표면에서 내려갈 수 있는 최대 높이를 결정합니다."
    );

    void OnEnable()
    {
        moveReference = serializedObject.FindProperty("_moveReference");
        jumpReference = serializedObject.FindProperty("_jumpReference");
        crouchReference = serializedObject.FindProperty("_crouchReference");
        sprintReference = serializedObject.FindProperty("_sprintReference");

        rigidbody = serializedObject.FindProperty("_rb");
        collider = serializedObject.FindProperty("_characterCollider");

        capsuleRadius = serializedObject.FindProperty("_capsuleRadius");
        capsuleHeight = serializedObject.FindProperty("_idleHeight");
        crouchedCapsuleHeight = serializedObject.FindProperty("_crouchHeight");

        skinWidth = serializedObject.FindProperty("_skinWidth");
        maxBounce = serializedObject.FindProperty("_maxBounces");

        viewDirection = serializedObject.FindProperty("_viewDirection");

        gravity = serializedObject.FindProperty("_gravity");

        speedControlMode = serializedObject.FindProperty("_speedControlMode");
        moveAcceleration = serializedObject.FindProperty("_moveAcceleration");
        moveDamp = serializedObject.FindProperty("_moveDamp");
        moveSpeed = serializedObject.FindProperty("_moveSpeed");
        sprintSpeedMultiplier = serializedObject.FindProperty("_sprintSpeedMultiplier");

        jumpSpeed = serializedObject.FindProperty("_jumpSpeed");
        maxJumpHeight = serializedObject.FindProperty("_jumpMaxHeight");

        maxSlopeAngle = serializedObject.FindProperty("_maxSlopeAngle");
        minCeilingAngle = serializedObject.FindProperty("_minCeilingAngle");

        isUpStepEnabled = serializedObject.FindProperty("_isUpStepEnabled");
        maxStepUpHeight = serializedObject.FindProperty("_maxStepUpHeight");
        isDownStepEnabled = serializedObject.FindProperty("_isDownStepEnabled");
        maxStepDownHeight = serializedObject.FindProperty("_maxStepDownHeight");
    }

    public override void OnInspectorGUI()
    {
        KinematicCharacterController controller = (KinematicCharacterController)target;
        debug = EditorGUILayout.BeginToggleGroup("Debug", debug);
        EditorGUILayout.EndToggleGroup();

        if (debug)
        {
            base.OnInspectorGUI();
            return;
        }

        serializedObject.Update();

        int height = DrawShapes();

        EditorGUILayout.Space(height + 100f);

        EditorGUILayout.PropertyField(moveReference, new GUIContent("Move Reference"));
        EditorGUILayout.PropertyField(jumpReference, new GUIContent("Jump Reference"));
        EditorGUILayout.PropertyField(crouchReference, new GUIContent("Crouch Reference"));
        EditorGUILayout.PropertyField(sprintReference, new GUIContent("Sprint Reference"));

        showPhysics = EditorGUILayout.BeginFoldoutHeaderGroup(showPhysics, "Physics");

        if (showPhysics)
        {
            EditorGUILayout.PropertyField(rigidbody, rigidbodyContent, true);
            EditorGUILayout.PropertyField(collider, colliderContent, true);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        showColliders = EditorGUILayout.BeginFoldoutHeaderGroup(showColliders, "Colliders/Collisions");

        if (showColliders)
        {
            EditorGUILayout.LabelField("Colliders");
            EditorGUILayout.PropertyField(capsuleRadius, capsuleRadiusContent, true);
            EditorGUILayout.PropertyField(capsuleHeight, capsuleHeightContent, true);
            EditorGUILayout.PropertyField(crouchedCapsuleHeight, crouchedCapsuleHeightContent, true);
            EditorGUILayout.LabelField("Collisions");
            EditorGUILayout.PropertyField(skinWidth, skinWidthContent, true);
            EditorGUILayout.PropertyField(maxBounce, maxBounceContent, true);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        showMovements = EditorGUILayout.BeginFoldoutHeaderGroup(showMovements, "Movements");

        if (showMovements)
        {
            EditorGUILayout.LabelField("Orientation");
            EditorGUILayout.PropertyField(viewDirection, viewDirectionContent, true);

            EditorGUILayout.LabelField("Horizontal Movement");
            EditorGUILayout.PropertyField(speedControlMode, speedControlModeContent, true);
            EditorGUILayout.PropertyField(moveSpeed, moveSpeedContent, true);
            if (speedControlMode.enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(moveAcceleration, moveAccelerationContent, true);
            }
            if (speedControlMode.enumValueIndex == 2)
            {
                EditorGUILayout.PropertyField(moveDamp, moveDampContent, true);
            }

            EditorGUILayout.PropertyField(sprintSpeedMultiplier, sprintSpeedMultiplierContent, true);

            EditorGUILayout.LabelField("Vertical Movement");
            EditorGUILayout.PropertyField(gravity, gravityContent, true);

            useJumpMaxHeight = EditorGUILayout.BeginToggleGroup("Use jump max height / 점프 최대 높이 사용하기", useJumpMaxHeight);
            EditorGUILayout.EndToggleGroup();

            if (useJumpMaxHeight)
            {
                EditorGUILayout.PropertyField(maxJumpHeight, maxJumpHeightContent, true);
            }
            else
            {
                EditorGUILayout.PropertyField(jumpSpeed, jumpSpeedContent, true);
            }


        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        showSlopeAndStepHandle = EditorGUILayout.BeginFoldoutHeaderGroup(showSlopeAndStepHandle, "Slope and Step Handle");

        if (showSlopeAndStepHandle)
        {
            EditorGUILayout.PropertyField(maxSlopeAngle, maxSlopeAngleContent, true);
            EditorGUILayout.PropertyField(minCeilingAngle, minCeilingAngleContent, true);
            EditorGUILayout.PropertyField(isUpStepEnabled, isUpStepEnabledContent, true);
            if (controller.IsUpStepEnabled) EditorGUILayout.PropertyField(maxStepUpHeight, maxStepUpHeightContent, true);
            EditorGUILayout.PropertyField(isDownStepEnabled, isDownStepEnabledContent, true);
            if (controller.IsDownStepEnabled) EditorGUILayout.PropertyField(maxStepDownHeight, maxStepDownHeightContent, true);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }

    int DrawShapes()
    {
        KinematicCharacterController controller = (KinematicCharacterController)target;
        float magnify = 50f;

        int h = (int)(Mathf.Max(controller.JumpMaxHeight, controller.IdleHeight, 2f) * magnify);

        // Calculate the position and size of the capsule
        Vector3 center = new Vector3(200f, h + 50f, 0f);
        float radius = controller.CapusleRadius * magnify;
        float height = controller.IdleHeight * magnify;
        float crouchHeight = controller.CrouchHeight * magnify;

        // Draw the capsule using Handles
        Handles.color = Color.green;
        DrawCapsule(center, radius, height);
        Handles.color = Color.gray;
        Handles.DrawWireArc(center - Vector3.up * (crouchHeight - radius), Vector3.forward, Vector3.left, 180, radius);

        // Draw terrain usig Handles
        Handles.color = Color.white;
        Handles.DrawLine(center + Vector3.left * 2f * magnify, center + Vector3.right * 4f * magnify);
        Handles.DrawLine(center + Vector3.right * 4f * magnify, center + Vector3.right * 4f * magnify + (Vector3.right * Mathf.Cos(controller.MaxSlopeAngle * Mathf.Deg2Rad) - Vector3.up * Mathf.Sin(controller.MaxSlopeAngle * Mathf.Deg2Rad)) * 2f * magnify);
        Handles.DrawDottedLine(center + (Vector3.left * 2f + Vector3.down * controller.JumpMaxHeight) * magnify, center + (Vector3.right * 1f + Vector3.down * controller.JumpMaxHeight) * magnify, 1f);
        DrawArrow(center + Vector3.left * magnify, center + (Vector3.left + Vector3.down * controller.JumpMaxHeight) * magnify);


        DrawArrow(center + Vector3.up * 10f + Vector3.left * magnify * controller.MoveSpeed * Time.fixedDeltaTime * 10f, center + Vector3.up * 10f + Vector3.right * magnify * controller.MoveSpeed * Time.fixedDeltaTime * 10f, false, true);

        return h;
    }

    void DrawCapsule(Vector3 center, float r, float h)
    {
        Vector3 a = Vector3.right * r - Vector3.up * (h - r);
        Vector3 b = Vector3.right * r - Vector3.up * r;
        Vector3 c = -Vector3.right * r - Vector3.up * (h - r);
        Vector3 d = -Vector3.right * r - Vector3.up * r;

        Handles.DrawWireArc(center - Vector3.up * (h - r), Vector3.forward, Vector3.left, 180, r);
        Handles.DrawWireArc(center - Vector3.up * r, Vector3.forward, Vector3.right, 180, r);
        Handles.DrawLine(center + a, center + b);
        Handles.DrawLine(center + c, center + d);
        Handles.DrawDottedLine(center + a, center + c, 1f);
        Handles.DrawDottedLine(center + b, center + d, 1f);
        Handles.DrawDottedLine(center, center - Vector3.up * h, 1f);
    }


    void DrawArrow(Vector3 from, Vector3 to, bool isLeftEnable = true, bool isRightEnable = true)
    {
        Handles.DrawLine(from, to);
        if (isLeftEnable) Handles.DrawLine(to, to + Quaternion.Euler(0, 0, 45f) * (from - to).normalized * 10f);
        if (isRightEnable) Handles.DrawLine(to, to + Quaternion.Euler(0, 0, -45f) * (from - to).normalized * 10f);
    }
}
