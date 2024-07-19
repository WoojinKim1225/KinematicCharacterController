using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using KCC;
/*
[CustomEditor(typeof(KinematicCharacterController))]
public class KCCEditor : Editor
{
    [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;
    private VisualElement generalContent;
    private VisualElement movementContent;
    private VisualElement physicsContent;
    private int height;

    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        if (m_VisualTreeAsset != null)
        {
            m_VisualTreeAsset.CloneTree(root);
            SetupToolbar(root);
            BindProperties(root);

            var imguiContainer = root.Q<IMGUIContainer>("Viewer");
            if (imguiContainer != null && imguiContainer.parent != null)
            {
                // Ensure this is run after the UI has been fully initialized and laid out
                imguiContainer.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    var parent = imguiContainer.parent;
                    parent.style.height = height;
                });
            }

            imguiContainer.onGUIHandler = OnViewerIMGUI;

        }
        else
        {
            var label = new Label("VisualTreeAsset is not assigned.");
            root.Add(label);
        }
        return root;
    }

    private void SetupToolbar(VisualElement root)
    {
        var toolbar = new Toolbar();
        var generalButton = new ToolbarButton(() => ShowContent("General")) { text = "General" };
        var movementButton = new ToolbarButton(() => ShowContent("Movement")) { text = "Movement" };
        var physicsButton = new ToolbarButton(() => ShowContent("Physics")) { text = "Physics" };
        toolbar.Add(generalButton);
        toolbar.Add(movementButton);
        toolbar.Add(physicsButton);
        root.Insert(0, toolbar);
        generalContent = root.Q("GeneralContent");
        movementContent = root.Q("MovementContent");
        physicsContent = root.Q("PhysicsContent");
        if (generalContent == null || movementContent == null || physicsContent == null)
        {
            Debug.LogError("One or more content elements are missing in the UXML file");
            return;
        }
        // 초기에는 General 탭만 보이게 설정
        ShowContent("General");
    }
    private void ShowContent(string contentName)
    {
        generalContent.style.display = contentName == "General" ? DisplayStyle.Flex : DisplayStyle.None;
        movementContent.style.display = contentName == "Movement" ? DisplayStyle.Flex : DisplayStyle.None;
        physicsContent.style.display = contentName == "Physics" ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BindProperties(VisualElement root)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty componentSettingsProp = serializedObject.FindProperty("m_componentSettings");

        BindData<Rigidbody>(root, componentSettingsProp, "rigidbody", "RigidbodyField");
        BindData<CapsuleCollider>(root, componentSettingsProp, "capsuleCollider", "ColliderField");

        serializedObject.ApplyModifiedProperties();
    }

    private void BindData<T>(VisualElement root, SerializedProperty componentSettingsProp, string propertyName, string fieldName)
    {
        SerializedProperty prop = componentSettingsProp.FindPropertyRelative(propertyName);
        VisualElement field = root.Q(fieldName);
        if (field == null) return;
        if (typeof(T) == typeof(float))
        {
            FloatField floatField = field as FloatField;
            if (floatField != null)
            {
                floatField.BindProperty(prop);
            }
        }
        else if (typeof(T) == typeof(string))
        {
            TextField textField = field as TextField;
            if (textField != null)
            {
                textField.BindProperty(prop);
            }
        }
        else if (typeof(T) == typeof(int))
        {
            IntegerField intField = field as IntegerField;
            if (intField != null)
            {
                intField.BindProperty(prop);
            }
        }
        else if (typeof(T) == typeof(bool))
        {
            Toggle toggle = field as Toggle;
            if (toggle != null)
            {
                toggle.BindProperty(prop);
            }
        }
        else if (typeof(T) == typeof(Vector3))
        {
            Vector3Field toggle = field as Vector3Field;
            if (toggle != null)
            {
                toggle.BindProperty(prop);
            }
        }
        else if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
        {
            ObjectField objectField = field as ObjectField;
            if (objectField != null)
            {
                objectField.objectType = typeof(T);
                objectField.BindProperty(prop);
            }
        }
    }

    private void OnViewerIMGUI()
    {
        height = DrawShapes() * 2;
    }

    int DrawShapes()
    {
        KinematicCharacterController controller = (KinematicCharacterController)target;
        float magnify = 50f;

        int h = (int)(Mathf.Max(controller.movementSettings.GetJumpMaxHeightStateful(), controller.CharacterSizeSettings.idleHeight, 2f) * magnify);

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
        Handles.DrawDottedLine(center + (Vector3.left * 2f + Vector3.down * controller.movementSettings.GetJumpMaxHeightStateful()) * magnify, center + (Vector3.right * 1f + Vector3.down * controller.movementSettings.GetJumpMaxHeightStateful()) * magnify, 1f);
        DrawArrow(center + Vector3.left * magnify, center + (Vector3.left + Vector3.down * controller.movementSettings.GetJumpMaxHeightStateful()) * magnify);

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
*/