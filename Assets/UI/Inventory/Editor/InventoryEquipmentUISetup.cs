using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class InventoryEquipmentUISetup
{
    private const string MenuPath = "Tools/Android2D/UI/Setup Inventory Equipment UI";
    private const string RootName = "Inventory Equipment UI";

    [MenuItem(MenuPath)]
    private static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Inventory/Equipment UI setup failed: no valid Scene is open.");
            return;
        }

        PlayerController player = FindPlayer(scene);
        Canvas canvas = FindCanvas(scene);
        if (player == null || canvas == null)
            return;

        const string undoName = "Setup Inventory Equipment UI";
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(undoName);

        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory == null)
            inventory = Undo.AddComponent<Inventory>(player.gameObject);

        Equipment equipment = player.GetComponent<Equipment>();
        if (equipment == null)
            equipment = Undo.AddComponent<Equipment>(player.gameObject);

        GameObject root = GetOrCreateChild(canvas.transform, RootName, undoName);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        SetRect(rootRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(460f, 430f), new Vector2(-20f, -20f), undoName);

        GameObject inventoryPanel = GetOrCreateChild(root.transform, "Inventory Panel", undoName);
        RectTransform inventoryPanelRect = inventoryPanel.GetComponent<RectTransform>();
        SetRect(inventoryPanelRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(320f, 420f), Vector2.zero, undoName);
        ConfigurePanel(inventoryPanel, new Color(0.045f, 0.055f, 0.08f, 0.94f), undoName);

        GameObject inventoryTitle = GetOrCreateChild(inventoryPanel.transform, "Inventory Title", undoName);
        SetRect(inventoryTitle.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(290f, 38f), new Vector2(0f, -8f), undoName);
        ConfigureText(inventoryTitle, "INVENTORY", 20, TextAnchor.MiddleCenter, new Color(0.9f, 0.82f, 0.56f), undoName);

        GameObject slotsRootObject = GetOrCreateChild(inventoryPanel.transform, "Inventory Slots", undoName);
        RectTransform slotsRoot = slotsRootObject.GetComponent<RectTransform>();
        SetRect(slotsRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(296f, 360f), new Vector2(0f, -52f), undoName);
        GridLayoutGroup grid = GetOrAddComponent<GridLayoutGroup>(slotsRootObject);
        Undo.RecordObject(grid, undoName);
        grid.cellSize = new Vector2(64f, 64f);
        grid.spacing = new Vector2(8f, 8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperCenter;

        InventorySlotUI slotTemplate = ConfigureInventorySlotTemplate(slotsRoot, undoName);
        InventoryUI inventoryUI = GetOrAddComponent<InventoryUI>(inventoryPanel);
        AssignInventoryUI(inventoryUI, inventory, equipment, slotsRoot, slotTemplate, undoName);

        GameObject equipmentPanel = GetOrCreateChild(root.transform, "Equipment Panel", undoName);
        RectTransform equipmentPanelRect = equipmentPanel.GetComponent<RectTransform>();
        SetRect(equipmentPanelRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(130f, 420f), Vector2.zero, undoName);
        ConfigurePanel(equipmentPanel, new Color(0.07f, 0.045f, 0.055f, 0.94f), undoName);

        GameObject equipmentTitle = GetOrCreateChild(equipmentPanel.transform, "Equipment Title", undoName);
        SetRect(equipmentTitle.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(118f, 48f), new Vector2(0f, -8f), undoName);
        ConfigureText(equipmentTitle, "EQUIPMENT", 15, TextAnchor.MiddleCenter, new Color(0.9f, 0.82f, 0.56f), undoName);

        EquipmentSlotUI weaponSlotUI = ConfigureEquipmentSlot(equipmentPanel.transform, undoName);
        EquipmentUI equipmentUI = GetOrAddComponent<EquipmentUI>(equipmentPanel);
        AssignEquipmentUI(equipmentUI, equipment, weaponSlotUI, undoName);

        EventSystem eventSystem = UnityEngine.Object
            .FindObjectsByType<EventSystem>(FindObjectsInactive.Include)
            .FirstOrDefault(candidate => candidate.gameObject.scene == scene);
        if (eventSystem == null)
            Debug.LogWarning("Inventory/Equipment UI was created, but the Scene has no EventSystem. Add one before testing clicks.");

        MarkDirty(root);
        MarkDirty(inventoryPanel);
        MarkDirty(slotsRootObject);
        MarkDirty(equipmentPanel);
        EditorUtility.SetDirty(inventory);
        EditorUtility.SetDirty(equipment);
        EditorSceneManager.MarkSceneDirty(scene);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log(
            $"Inventory/Equipment UI setup complete in Scene '{scene.name}'. " +
            "Review the top-right UI, then press Ctrl+S to save the Scene.",
            root);
    }

    private static InventorySlotUI ConfigureInventorySlotTemplate(Transform parent, string undoName)
    {
        GameObject slot = GetOrCreateChild(parent, "Inventory Slot Template", undoName);
        ConfigurePanel(slot, new Color(0.13f, 0.15f, 0.2f, 1f), undoName);
        Button button = GetOrAddComponent<Button>(slot);
        Undo.RecordObject(button, undoName);
        Image slotBackground = slot.GetComponent<Image>();
        Undo.RecordObject(slotBackground, undoName);
        slotBackground.raycastTarget = true;
        button.targetGraphic = slotBackground;

        GameObject iconObject = GetOrCreateChild(slot.transform, "Icon", undoName);
        SetStretch(iconObject.GetComponent<RectTransform>(), 6f, undoName);
        Image icon = GetOrAddComponent<Image>(iconObject);
        Undo.RecordObject(icon, undoName);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        GameObject quantityObject = GetOrCreateChild(slot.transform, "Quantity", undoName);
        SetRect(quantityObject.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(28f, 22f), new Vector2(-3f, 3f), undoName);
        Text quantity = ConfigureText(quantityObject, string.Empty, 15, TextAnchor.LowerRight, Color.white, undoName);

        GameObject emptyObject = GetOrCreateChild(slot.transform, "Empty", undoName);
        SetStretch(emptyObject.GetComponent<RectTransform>(), 4f, undoName);
        Text empty = ConfigureText(emptyObject, "EMPTY", 10, TextAnchor.MiddleCenter, new Color(0.48f, 0.5f, 0.58f), undoName);

        InventorySlotUI slotUI = GetOrAddComponent<InventorySlotUI>(slot);
        Undo.RecordObject(slotUI, undoName);
        SerializedObject serialized = new SerializedObject(slotUI);
        serialized.FindProperty("button").objectReferenceValue = button;
        serialized.FindProperty("iconImage").objectReferenceValue = icon;
        serialized.FindProperty("quantityText").objectReferenceValue = quantity;
        serialized.FindProperty("emptyText").objectReferenceValue = empty;
        serialized.ApplyModifiedProperties();

        Undo.RecordObject(slot, undoName);
        slot.SetActive(false);
        return slotUI;
    }

    private static EquipmentSlotUI ConfigureEquipmentSlot(Transform parent, string undoName)
    {
        GameObject slot = GetOrCreateChild(parent, "Weapon Slot", undoName);
        SetRect(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(92f, 92f), new Vector2(0f, -70f), undoName);
        ConfigurePanel(slot, new Color(0.16f, 0.12f, 0.15f, 1f), undoName);
        Button button = GetOrAddComponent<Button>(slot);
        Undo.RecordObject(button, undoName);
        Image slotBackground = slot.GetComponent<Image>();
        Undo.RecordObject(slotBackground, undoName);
        slotBackground.raycastTarget = true;
        button.targetGraphic = slotBackground;

        GameObject iconObject = GetOrCreateChild(slot.transform, "Icon", undoName);
        SetStretch(iconObject.GetComponent<RectTransform>(), 8f, undoName);
        Image icon = GetOrAddComponent<Image>(iconObject);
        Undo.RecordObject(icon, undoName);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        GameObject emptyObject = GetOrCreateChild(slot.transform, "Empty", undoName);
        SetStretch(emptyObject.GetComponent<RectTransform>(), 4f, undoName);
        Text empty = ConfigureText(emptyObject, "WEAPON\nEMPTY", 12, TextAnchor.MiddleCenter, new Color(0.62f, 0.55f, 0.58f), undoName);

        EquipmentSlotUI slotUI = GetOrAddComponent<EquipmentSlotUI>(slot);
        Undo.RecordObject(slotUI, undoName);
        SerializedObject serialized = new SerializedObject(slotUI);
        serialized.FindProperty("button").objectReferenceValue = button;
        serialized.FindProperty("iconImage").objectReferenceValue = icon;
        serialized.FindProperty("emptyText").objectReferenceValue = empty;
        serialized.ApplyModifiedProperties();
        return slotUI;
    }

    private static void AssignInventoryUI(
        InventoryUI target,
        Inventory inventory,
        Equipment equipment,
        Transform slotsRoot,
        InventorySlotUI template,
        string undoName)
    {
        Undo.RecordObject(target, undoName);
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty("inventory").objectReferenceValue = inventory;
        serialized.FindProperty("equipment").objectReferenceValue = equipment;
        serialized.FindProperty("slotsRoot").objectReferenceValue = slotsRoot;
        serialized.FindProperty("slotTemplate").objectReferenceValue = template;
        serialized.ApplyModifiedProperties();
    }

    private static void AssignEquipmentUI(
        EquipmentUI target,
        Equipment equipment,
        EquipmentSlotUI weaponSlotUI,
        string undoName)
    {
        Undo.RecordObject(target, undoName);
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty("equipment").objectReferenceValue = equipment;
        serialized.FindProperty("weaponSlotUI").objectReferenceValue = weaponSlotUI;
        serialized.ApplyModifiedProperties();
    }

    private static PlayerController FindPlayer(Scene scene)
    {
        PlayerController[] players = UnityEngine.Object
            .FindObjectsByType<PlayerController>(FindObjectsInactive.Include)
            .Where(candidate => candidate.gameObject.scene == scene)
            .ToArray();

        if (players.Length == 1)
            return players[0];

        Debug.LogError(players.Length == 0
            ? "Inventory/Equipment UI setup failed: no PlayerController exists in the active Scene."
            : "Inventory/Equipment UI setup failed: multiple PlayerController objects exist in the active Scene.");
        return null;
    }

    private static Canvas FindCanvas(Scene scene)
    {
        Canvas selectedCanvas = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<Canvas>()
            : null;
        if (selectedCanvas != null && selectedCanvas.gameObject.scene == scene && selectedCanvas.renderMode != RenderMode.WorldSpace)
            return selectedCanvas;

        Canvas[] canvases = UnityEngine.Object
            .FindObjectsByType<Canvas>(FindObjectsInactive.Include)
            .Where(candidate => candidate.gameObject.scene == scene && candidate.renderMode != RenderMode.WorldSpace)
            .ToArray();

        if (canvases.Length == 1)
            return canvases[0];

        Canvas namedCanvas = canvases.FirstOrDefault(candidate => candidate.name == "Canvas");
        if (namedCanvas != null)
            return namedCanvas;

        Debug.LogError(canvases.Length == 0
            ? "Inventory/Equipment UI setup failed: no screen-space Canvas exists in the active Scene."
            : "Inventory/Equipment UI setup failed: multiple screen-space Canvases exist. Select the intended Canvas and run the menu again.");
        return null;
    }

    private static GameObject GetOrCreateChild(Transform parent, string name, string undoName)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        GameObject created = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(created, undoName);
        Undo.SetTransformParent(created.transform, parent, undoName);
        created.transform.localScale = Vector3.one;
        return created;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static void ConfigurePanel(GameObject target, Color color, string undoName)
    {
        Image image = GetOrAddComponent<Image>(target);
        Undo.RecordObject(image, undoName);
        image.color = color;
        image.raycastTarget = false;
    }

    private static Text ConfigureText(
        GameObject target,
        string value,
        int fontSize,
        TextAnchor alignment,
        Color color,
        string undoName)
    {
        Text text = GetOrAddComponent<Text>(target);
        Undo.RecordObject(text, undoName);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 size,
        Vector2 position,
        string undoName)
    {
        Undo.RecordObject(rect, undoName);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }

    private static void SetStretch(RectTransform rect, float padding, string undoName)
    {
        Undo.RecordObject(rect, undoName);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
        rect.localScale = Vector3.one;
    }

    private static void MarkDirty(GameObject target)
    {
        EditorUtility.SetDirty(target);
        foreach (Component component in target.GetComponents<Component>())
            EditorUtility.SetDirty(component);
    }
}
