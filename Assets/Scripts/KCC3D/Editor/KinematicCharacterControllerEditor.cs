using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using System.Collections;

[CustomEditor(typeof(KinematicCharacterController))]
[CanEditMultipleObjects]
public class KinematicCharacterControllerEditor : Editor
{
    private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
    private void OnSceneGUI() {
        KinematicCharacterController controller = (KinematicCharacterController)target;
        Handles.color = new Color(0, 1, 0, 0.2f);
        Handles.DrawSolidArc(controller.transform.position, controller.Up, Vector3.forward, Vector3.SignedAngle(Vector3.forward, controller.Forward, controller.Up), 0.5f);
        Handles.color = new Color(0, 1, 0, 1f);
        Handles.DrawWireDisc(controller.transform.position, controller.Up, 0.5f);
        Vector3 c = controller.HorizontalVelocity.normalized;
        Handles.color = new Color(Mathf.Abs(c.x), Mathf.Abs(c.y), Mathf.Abs(c.z));
        Handles.DrawLine(controller.transform.position, controller.transform.position + controller.HorizontalVelocity);
        c = controller.VerticalVelocity.normalized;
        Handles.color = new Color(Mathf.Abs(c.x), Mathf.Abs(c.y), Mathf.Abs(c.z));
        Handles.DrawLine(controller.transform.position, controller.transform.position + controller.VerticalVelocity);
    }

    public override void OnInspectorGUI()
    {
        KinematicCharacterController controller = (KinematicCharacterController)target;

        serializedObject.Update();

        int height = DrawShapes();

        EditorGUILayout.Space(height + 100f);

        base.OnInspectorGUI();
    }

    int DrawShapes()
    {
        KinematicCharacterController controller = (KinematicCharacterController)target;
        float magnify = 50f;

        int h = (int)(Mathf.Max(controller.movementSettings.jumpMaxHeight.Value, controller.CharacterSizeSettings.idleHeight, 2f) * magnify);

        // Calculate the position and size of the capsule
        Vector3 center = new Vector3(200f, h + 50f, 0f);
        float radius = controller.CharacterSizeSettings.capsuleRadius.Value * magnify;
        float height = controller.CharacterSizeSettings.idleHeight * magnify;
        float crouchHeight = controller.CharacterSizeSettings.crouchHeight * magnify;

        // Draw the capsule using Handles
        Handles.color = Color.green;
        DrawCapsule(center, radius, height);
        Handles.color = Color.gray;
        Handles.DrawWireArc(center - Vector3.up * (crouchHeight - radius), Vector3.forward, Vector3.left, 180, radius);

        // Draw terrain using Handles
        Handles.color = Color.white;
        Handles.DrawLine(center + Vector3.left * 2f * magnify, center + Vector3.right * 4f * magnify);
        Handles.DrawLine(center + Vector3.right * 2f * magnify, center + (Vector3.right * 2f + Vector3.down * controller.UpStepHeight) * magnify);
        Handles.DrawLine(center + Vector3.right * 3f * magnify, center + (Vector3.right * 3f + Vector3.up * controller.DownStepHeight) * magnify);

        Handles.DrawLine(center + Vector3.right * 4f * magnify, center + Vector3.right * 4f * magnify + (Vector3.right * Mathf.Cos(controller.MaxSlopeAngle * Mathf.Deg2Rad) - Vector3.up * Mathf.Sin(controller.MaxSlopeAngle * Mathf.Deg2Rad)) * 2f * magnify);
        Handles.DrawDottedLine(center + (Vector3.left * 2f + Vector3.down * controller.movementSettings.jumpMaxHeight.Value) * magnify, center + (Vector3.right * 1f + Vector3.down * controller.movementSettings.jumpMaxHeight.Value) * magnify, 1f);
        DrawArrow(center + Vector3.left * magnify, center + (Vector3.left + Vector3.down * controller.movementSettings.jumpMaxHeight.Value) * magnify);

        DrawArrow(center + Vector3.up * 10f + Vector3.left * magnify * controller.movementSettings.moveSpeed * Time.fixedDeltaTime * 10f, center + Vector3.up * 10f + Vector3.right * magnify * controller.movementSettings.moveSpeed * Time.fixedDeltaTime * 10f, false, true);

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
