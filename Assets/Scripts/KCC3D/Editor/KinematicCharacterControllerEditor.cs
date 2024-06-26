using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

[CustomEditor(typeof(KinematicCharacterController))]
[CanEditMultipleObjects]
public class KinematicCharacterControllerEditor : Editor
{
    bool debug;

    bool showComponents;

    void OnEnable()
    {   
        
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

        int h = (int)(Mathf.Max(controller.movementSettings._jumpMaxHeight, controller.IdleHeight, 2f) * magnify);

        // Calculate the position and size of the capsule
        Vector3 center = new Vector3(200f, h + 50f, 0f);
        float radius = controller.CapsuleRadius * magnify;
        float height = controller.IdleHeight * magnify;
        float crouchHeight = controller.CrouchHeight * magnify;

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
        Handles.DrawDottedLine(center + (Vector3.left * 2f + Vector3.down * controller.movementSettings._jumpMaxHeight) * magnify, center + (Vector3.right * 1f + Vector3.down * controller.movementSettings._jumpMaxHeight) * magnify, 1f);
        DrawArrow(center + Vector3.left * magnify, center + (Vector3.left + Vector3.down * controller.movementSettings._jumpMaxHeight) * magnify);

        DrawArrow(center + Vector3.up * 10f + Vector3.left * magnify * controller.movementSettings._moveSpeed * Time.fixedDeltaTime * 10f, center + Vector3.up * 10f + Vector3.right * magnify * controller.movementSettings._moveSpeed * Time.fixedDeltaTime * 10f, false, true);

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
