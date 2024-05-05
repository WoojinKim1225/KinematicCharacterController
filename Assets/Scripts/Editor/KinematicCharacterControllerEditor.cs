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
        "The capsule collider's radius. \n\n플레이어 콜라이더의 반지름입니다.");

    SerializedProperty capsuleHeight;
    GUIContent capsuleHeightContent = new GUIContent(
        "    Capsule Height", 
        "The capsule collider's idle height. \n\n플레이어 콜라이더의 기본 높이입니다.");

    SerializedProperty crouchedCapsuleHeight;
    GUIContent crouchedCapsuleHeightContent = new GUIContent(
        "    Crouched Capsule Height", 
        "The capsule collider's height when crouched. \n\n플레이어 콜라이더가 웅크렸을 때의 높이입니다.");

    SerializedProperty skinWidth;
    GUIContent skinWidthContent = new GUIContent(
        "    Collider Skin Width",
        "Additional space around the collision shape, preventing collision issues and passage problems. \n\n캐릭터 콜리전 주위에 추가적인 공간입니다. 충돌 처리와 통과 문제를 방지합니다."
    );

    SerializedProperty maxBounce;
    GUIContent maxBounceContent = new GUIContent(
        "    Maximum Collision Bounces",
        "The number of computation cycles used to process collider collisions. Too low a value can make movement inaccurate, while too high a value can burden the computation. \n\n콜라이더 충돌을 처리하는 데 사용되는 연산 주기입니다. 값이 너무 낮으면 이동이 부정확해질 수 있고, 값이 너무 높으면 연산에 부하가 걸릴 수 있습니다."
    );

    bool showMovements;

    SerializedProperty viewDirection;
    GUIContent viewDirectionContent = new GUIContent(
        "    View Direction", 
        "The character's view direction. Sets the character's forward and right direction. \nCan set with SetViewDirection(Vector3 dir). \n\n캐릭터의 방향입니다. 앞방향과 옆방향을 설정합니다. \nSetViewDirection(Vector3 dir)으로 값을 변경할 수 있습니다.");
    

    SerializedProperty gravity;
    GUIContent gravityContent = new GUIContent(
        "    Gravity",
        "The acceleration in world space that changes the velocity of the character. \nIt is recommended to set this value to 20 or higher in the downward direction. \n\n캐릭터의 속도를 변경하는 항상 적용되는 가속도입니다. \n이 값은 아래 방향으로 20 이상으로 설정하는 것이 권장됩니다.");

    SerializedProperty moveSpeed;
    GUIContent moveSpeedContent = new GUIContent(
        "    MoveSpeed",
        "The speed of the player moves in world space. \n\n캐릭터의 이동속도입니다."
    );

    bool useJumpMaxHeight;

    SerializedProperty jumpSpeed;
    GUIContent jumpSpeedContent = new GUIContent(
        "    Jump Speed",
        "The start speed of a player begins jump. \n\n점프를 시작하는 속도입니다."
    );

    SerializedProperty maxJumpHeight;
    GUIContent maxJumpHeightContent = new GUIContent(
        "    Maximum Jump Height",
        "The maximum height the character can reach by jumping. \n\n점프를 하여 도달할 수 있는 최대 높이입니다."
    );

    SerializedProperty sprintSpeedMultiplier;
    GUIContent sprintSpeedMultiplierContent = new GUIContent(
        "    Sprint Speed Multiplier",
        "The increase in movement speed when the sprint key is pressed. \n\n달리기 키를 눌렀을 때의 이동 속도 증가량입니다."
    );

    bool showSlopeAndStepHandle;

    SerializedProperty maxSlopeAngle;
    GUIContent maxSlopeAngleContent = new GUIContent(
        "    Maximum Slope Angle",
        "The angle of a slope that can player move. \n\n플레이어가 움직일 수 있는 최대 경사 각도입니다."
    );

    SerializedProperty minCeilingAngle;
    GUIContent minCeilingAngleContent = new GUIContent(
        "    Minimum Ceiling Angle",
        "The angle of a ceiling that can player move. If an angle is greater that this value, player's movement will be canceled. \n\n플레이어가 이동할 수 있는 천장의 각도입니다. 이 값보다 큰 각도의 경우 플레이어의 이동이 취소됩니다."
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

        moveSpeed = serializedObject.FindProperty("_moveSpeed");
        sprintSpeedMultiplier = serializedObject.FindProperty("_sprintSpeedMultiplier");

        jumpSpeed = serializedObject.FindProperty("_jumpSpeed");
        maxJumpHeight = serializedObject.FindProperty("_jumpMaxHeight");

        maxSlopeAngle = serializedObject.FindProperty("_maxSlopeAngle");
        minCeilingAngle = serializedObject.FindProperty("_minCeilingAngle");
    }

    public override void OnInspectorGUI()
    {

        debug = EditorGUILayout.BeginToggleGroup("Debug", debug);
        EditorGUILayout.EndToggleGroup();

        if (debug) {
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

        if (showPhysics) {
            EditorGUILayout.PropertyField(rigidbody, rigidbodyContent, true);
            EditorGUILayout.PropertyField(collider, colliderContent, true);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        showColliders = EditorGUILayout.BeginFoldoutHeaderGroup(showColliders, "Colliders/Collisions");

        if (showColliders) {
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

        if (showMovements) {
            EditorGUILayout.LabelField("Orientation");
            EditorGUILayout.PropertyField(viewDirection, viewDirectionContent, true);

            EditorGUILayout.LabelField("Horizontal Movement");
            EditorGUILayout.PropertyField(moveSpeed, moveSpeedContent, true);
            EditorGUILayout.PropertyField(sprintSpeedMultiplier, sprintSpeedMultiplierContent, true);

            EditorGUILayout.LabelField("Vertical Movement");
            EditorGUILayout.PropertyField(gravity, gravityContent, true);

            useJumpMaxHeight = EditorGUILayout.BeginToggleGroup("Use jump max height / 점프 최대 높이 사용하기", useJumpMaxHeight);
            EditorGUILayout.EndToggleGroup();

            if (useJumpMaxHeight) {
                EditorGUILayout.PropertyField(maxJumpHeight, maxJumpHeightContent, true);
            } else {
                EditorGUILayout.PropertyField(jumpSpeed, jumpSpeedContent, true);
            }

            
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        showSlopeAndStepHandle = EditorGUILayout.BeginFoldoutHeaderGroup(showSlopeAndStepHandle, "Slope and Step Handle");

        if (showSlopeAndStepHandle) {
            EditorGUILayout.PropertyField(maxSlopeAngle, maxSlopeAngleContent, true);
            EditorGUILayout.PropertyField(minCeilingAngle, minCeilingAngleContent, true);
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
        DrawArrow(center + Vector3.left * magnify * controller.MoveSpeed * Time.fixedDeltaTime * 10f + Vector3.up * 10f, center + Vector3.up * 10f, false, true);
        return h;
    }

    void DrawCapsule(Vector3 center, float r, float h){
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


    void DrawArrow(Vector3 from, Vector3 to, bool isLeftEnable= true, bool isRightEnable = true) {
        Handles.DrawLine(from,to);
        if (isLeftEnable) Handles.DrawLine(to, to + Quaternion.Euler(0, 0, 45f) * (from - to).normalized * 10f);
        if (isRightEnable) Handles.DrawLine(to, to + Quaternion.Euler(0, 0, -45f) * (from - to).normalized * 10f);
    }
}
