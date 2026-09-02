using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SaveSlotUISetup
{
    private const string MenuPath = "Tools/Android2D/Save/Setup Save Slot UI";
    private const string RootName = "Save Load UI";
    private static readonly Color PanelColor = new Color(0.035f, 0.055f, 0.10f, 0.96f);
    private static readonly Color CardColor = new Color(0.08f, 0.12f, 0.20f, 0.98f);
    private static readonly Color PrimaryColor = new Color(0.12f, 0.48f, 0.78f, 1f);
    private static readonly Color DangerColor = new Color(0.62f, 0.16f, 0.18f, 1f);

    [MenuItem(MenuPath)]
    private static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[SaveSlotUISetup] No valid loaded Scene is active.");
            return;
        }

        Canvas canvas = FindCanvas(scene);
        if (canvas == null)
            return;

        const string undoName = "Setup Save Slot UI";
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(undoName);

        GameObject root = FindDirectChild(canvas.transform, RootName);
        if (root == null)
            root = CreateUIObject(RootName, canvas.transform, undoName);
        Stretch((RectTransform)root.transform);

        SaveLoadMenuController controller = GetOrAdd<SaveLoadMenuController>(root, undoName);

        GameObject openBar = FindOrCreate("Open Buttons", root.transform, undoName);
        SetRect((RectTransform)openBar.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-190f, -42f), new Vector2(360f, 64f));
        Button openSave = CreateButton("Open Save Button", openBar.transform, "SAVE", PrimaryColor, undoName);
        SetRect((RectTransform)openSave.transform, new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f),
            Vector2.zero, new Vector2(160f, 52f));
        Button openLoad = CreateButton("Open Load Button", openBar.transform, "LOAD", PrimaryColor, undoName);
        SetRect((RectTransform)openLoad.transform, new Vector2(0.75f, 0.5f), new Vector2(0.75f, 0.5f),
            Vector2.zero, new Vector2(160f, 52f));

        GameObject mainPanel = FindOrCreate("Main Panel", root.transform, undoName);
        Stretch((RectTransform)mainPanel.transform);
        ConfigureImage(mainPanel, PanelColor, undoName);

        Text title = CreateText("Title", mainPanel.transform, "SAVE GAME", 32, TextAnchor.MiddleCenter, undoName);
        SetRect((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -52f), new Vector2(700f, 62f));

        SaveSlotUI[] slots = new SaveSlotUI[SaveSlotStorage.SlotCount];
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject card = FindOrCreate($"Slot {i + 1}", mainPanel.transform, undoName);
            SetRect((RectTransform)card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -145f - i * 122f), new Vector2(900f, 108f));
            ConfigureImage(card, CardColor, undoName);

            Text slotName = CreateText("Slot Name", card.transform, $"SLOT {i + 1}", 24,
                TextAnchor.MiddleLeft, undoName);
            SetRect((RectTransform)slotName.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(100f, 20f), new Vector2(180f, 42f));

            Text status = CreateText("Status", card.transform, "EMPTY", 16,
                TextAnchor.MiddleLeft, undoName);
            status.color = new Color(0.45f, 0.82f, 1f, 1f);
            SetRect((RectTransform)status.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(100f, -22f), new Vector2(180f, 32f));

            Text details = CreateText("Details", card.transform, "No save data", 16,
                TextAnchor.MiddleLeft, undoName);
            SetRect((RectTransform)details.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-20f, 0f), new Vector2(400f, 88f));

            Button action = CreateButton("Action Button", card.transform, "SAVE", PrimaryColor, undoName);
            SetRect((RectTransform)action.transform, new Vector2(1f, 0.66f), new Vector2(1f, 0.66f),
                new Vector2(-105f, 0f), new Vector2(170f, 42f));
            Button delete = CreateButton("Delete Button", card.transform, "DELETE", DangerColor, undoName);
            SetRect((RectTransform)delete.transform, new Vector2(1f, 0.24f), new Vector2(1f, 0.24f),
                new Vector2(-105f, 0f), new Vector2(170f, 36f));

            SaveSlotUI slot = GetOrAdd<SaveSlotUI>(card, undoName);
            Undo.RecordObject(slot, undoName);
            SerializedObject serializedSlot = new SerializedObject(slot);
            serializedSlot.FindProperty("slotNameText").objectReferenceValue = slotName;
            serializedSlot.FindProperty("detailsText").objectReferenceValue = details;
            serializedSlot.FindProperty("statusText").objectReferenceValue = status;
            serializedSlot.FindProperty("actionButton").objectReferenceValue = action;
            serializedSlot.FindProperty("actionButtonText").objectReferenceValue =
                action.GetComponentInChildren<Text>(true);
            serializedSlot.FindProperty("deleteButton").objectReferenceValue = delete;
            serializedSlot.ApplyModifiedProperties();
            EditorUtility.SetDirty(slot);
            slots[i] = slot;
        }

        Text feedback = CreateText("Feedback", mainPanel.transform, string.Empty, 16,
            TextAnchor.MiddleCenter, undoName);
        feedback.color = new Color(1f, 0.84f, 0.35f, 1f);
        SetRect((RectTransform)feedback.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 94f), new Vector2(800f, 38f));

        Button close = CreateButton("Close Button", mainPanel.transform, "CLOSE", CardColor, undoName);
        SetRect((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 42f), new Vector2(220f, 48f));

        GameObject confirmation = FindOrCreate("Confirmation Panel", mainPanel.transform, undoName);
        SetRect((RectTransform)confirmation.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(540f, 220f));
        ConfigureImage(confirmation, new Color(0.06f, 0.08f, 0.13f, 1f), undoName);

        Text confirmationMessage = CreateText("Message", confirmation.transform,
            "Confirm action?", 22, TextAnchor.MiddleCenter, undoName);
        SetRect((RectTransform)confirmationMessage.transform, new Vector2(0.5f, 0.66f),
            new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(480f, 90f));
        Button confirm = CreateButton("Confirm Button", confirmation.transform, "CONFIRM", DangerColor, undoName);
        SetRect((RectTransform)confirm.transform, new Vector2(0.3f, 0.2f), new Vector2(0.3f, 0.2f),
            Vector2.zero, new Vector2(190f, 48f));
        Button cancel = CreateButton("Cancel Button", confirmation.transform, "CANCEL", CardColor, undoName);
        SetRect((RectTransform)cancel.transform, new Vector2(0.7f, 0.2f), new Vector2(0.7f, 0.2f),
            Vector2.zero, new Vector2(190f, 48f));

        Undo.RecordObject(controller, undoName);
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("openSaveButton").objectReferenceValue = openSave;
        serializedController.FindProperty("openLoadButton").objectReferenceValue = openLoad;
        serializedController.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        serializedController.FindProperty("titleText").objectReferenceValue = title;
        serializedController.FindProperty("feedbackText").objectReferenceValue = feedback;
        SerializedProperty slotViews = serializedController.FindProperty("slotViews");
        slotViews.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++)
            slotViews.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        serializedController.FindProperty("closeButton").objectReferenceValue = close;
        serializedController.FindProperty("confirmationPanel").objectReferenceValue = confirmation;
        serializedController.FindProperty("confirmationMessageText").objectReferenceValue = confirmationMessage;
        serializedController.FindProperty("confirmButton").objectReferenceValue = confirm;
        serializedController.FindProperty("cancelButton").objectReferenceValue = cancel;
        serializedController.ApplyModifiedProperties();
        EditorUtility.SetDirty(controller);

        Undo.RecordObject(mainPanel, undoName);
        mainPanel.SetActive(false);
        Undo.RecordObject(confirmation, undoName);
        confirmation.SetActive(false);
        Undo.RecordObject(root.transform, undoName);
        root.transform.SetAsLastSibling();

        EventSystem eventSystem = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include)
            .FirstOrDefault(candidate => candidate.gameObject.scene == scene);
        if (eventSystem == null)
            Debug.LogWarning("[SaveSlotUISetup] No EventSystem exists in the active Scene. Add one before testing UI clicks.");

        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = root;
        Debug.Log(
            $"[SaveSlotUISetup] Save Slot UI configured in Scene '{scene.name}'. " +
            "Review the layout and press Ctrl+S to save the Scene.",
            root);
    }

    private static Canvas FindCanvas(Scene scene)
    {
        Canvas selected = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<Canvas>()
            : null;
        if (selected != null && selected.gameObject.scene == scene && selected.renderMode != RenderMode.WorldSpace)
            return selected;

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)
            .Where(candidate => candidate.gameObject.scene == scene && candidate.renderMode != RenderMode.WorldSpace)
            .ToArray();
        Canvas named = canvases.FirstOrDefault(candidate => candidate.name == "Canvas");
        if (named != null)
            return named;
        if (canvases.Length == 1)
            return canvases[0];

        Debug.LogError(canvases.Length == 0
            ? "[SaveSlotUISetup] No screen-space Canvas exists in the active Scene."
            : "[SaveSlotUISetup] Multiple Canvases exist. Select the intended Canvas and run the tool again.");
        return null;
    }

    private static GameObject FindOrCreate(string name, Transform parent, string undoName)
    {
        GameObject existing = FindDirectChild(parent, name);
        return existing != null ? existing : CreateUIObject(name, parent, undoName);
    }

    private static GameObject FindDirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child.gameObject;
        }

        return null;
    }

    private static GameObject CreateUIObject(string name, Transform parent, string undoName)
    {
        GameObject created = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(created, undoName);
        Undo.SetTransformParent(created.transform, parent, undoName);
        created.transform.localScale = Vector3.one;
        return created;
    }

    private static T GetOrAdd<T>(GameObject target, string undoName) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Color color,
        string undoName)
    {
        GameObject buttonObject = FindOrCreate(name, parent, undoName);
        Image image = ConfigureImage(buttonObject, color, undoName);
        Button button = GetOrAdd<Button>(buttonObject, undoName);
        Undo.RecordObject(button, undoName);
        button.targetGraphic = image;

        Text text = CreateText("Text", buttonObject.transform, label, 18, TextAnchor.MiddleCenter, undoName);
        Stretch((RectTransform)text.transform);
        return button;
    }

    private static Image ConfigureImage(GameObject target, Color color, string undoName)
    {
        Image image = GetOrAdd<Image>(target, undoName);
        Undo.RecordObject(image, undoName);
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private static Text CreateText(
        string name,
        Transform parent,
        string value,
        int fontSize,
        TextAnchor alignment,
        string undoName)
    {
        GameObject textObject = FindOrCreate(name, parent, undoName);
        Text text = GetOrAdd<Text>(textObject, undoName);
        Undo.RecordObject(text, undoName);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        Undo.RecordObject(rect, "Setup Save Slot UI");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 position,
        Vector2 size)
    {
        Undo.RecordObject(rect, "Setup Save Slot UI");
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
}
