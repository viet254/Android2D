using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SkillSystemSetup
{
    private const string DataFolder = "Assets/Data/Skills";
    private const string DatabasePath = DataFolder + "/SkillDatabase.asset";
    private const string VitalityPath = DataFolder + "/Vitality.asset";
    private const string PowerPath = DataFolder + "/Power.asset";
    private const string DashMasteryPath = DataFolder + "/DashMastery.asset";
    private const string UndoName = "Thiết lập Hệ thống Kỹ năng";

    [MenuItem("Tools/Android2D/Skills/Setup Skill System")]
    private static void Setup()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Chỉ có thể thiết lập Hệ thống Kỹ năng khi đang ở Edit Mode.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(scene.path))
        {
            Debug.LogError("Cần lưu Scene hiện tại trước khi thiết lập Hệ thống Kỹ năng.");
            return;
        }

        if (!EnsureFolder("Assets/Data", "Skills"))
            return;

        SkillDefinition vitality = LoadOrCreateSkill(VitalityPath);
        SkillDefinition power = LoadOrCreateSkill(PowerPath);
        SkillDefinition dashMastery = LoadOrCreateSkill(DashMasteryPath);
        if (vitality == null || power == null || dashMastery == null)
            return;

        ConfigureSkill(
            vitality,
            "player_vitality",
            "Sinh Lực",
            "Tăng 10 máu tối đa cho mỗi bậc.",
            SkillCategory.Defense,
            3,
            1,
            0,
            10,
            0,
            0f,
            null);
        ConfigureSkill(
            power,
            "player_power",
            "Cường Công",
            "Tăng 5 sát thương tấn công cho mỗi bậc.",
            SkillCategory.Combat,
            3,
            1,
            10,
            0,
            5,
            0f,
            null);
        ConfigureSkill(
            dashMastery,
            "player_dash_mastery",
            "Tinh Thông Lướt",
            "Tăng 0,5 tốc độ di chuyển cho mỗi bậc. Yêu cầu Sinh Lực bậc 1.",
            SkillCategory.Movement,
            3,
            1,
            20,
            0,
            0,
            0.5f,
            vitality);

        SkillDatabase database = LoadOrCreateDatabase();
        if (database == null)
            return;
        ConfigureDatabase(database, vitality, power, dashMastery);

        PlayerController player = FindSingleScenePlayer(scene);
        if (player == null)
            return;

        PlayerSkillSystem skillSystem = player.GetComponent<PlayerSkillSystem>();
        if (skillSystem == null)
            skillSystem = Undo.AddComponent<PlayerSkillSystem>(player.gameObject);

        Undo.RecordObject(skillSystem, UndoName);
        SerializedObject serializedSkillSystem = new SerializedObject(skillSystem);
        serializedSkillSystem.FindProperty("skillDatabase").objectReferenceValue = database;
        serializedSkillSystem.FindProperty("initialSkillPoints").intValue = 0;
        serializedSkillSystem.FindProperty("skillPointsPerLevel").intValue = 1;
        serializedSkillSystem.ApplyModifiedProperties();
        EditorUtility.SetDirty(skillSystem);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        ValidateDatabase(database);
        Debug.Log(
            $"[Kỹ năng] Đã thiết lập xong trong Scene '{scene.name}'. Player '{player.name}' đang dùng '{DatabasePath}'. " +
            "Hãy kiểm tra component rồi nhấn Ctrl+S. Chạy menu này một lần trong mỗi Scene gameplay có Player riêng.",
            skillSystem);
    }

    [MenuItem("Tools/Android2D/Skills/Validate Skill Database")]
    private static void ValidateMenu()
    {
        SkillDatabase database = AssetDatabase.LoadAssetAtPath<SkillDatabase>(DatabasePath);
        if (database == null)
        {
            Debug.LogError($"[Kỹ năng] Không tìm thấy SkillDatabase tại '{DatabasePath}'. Hãy chạy Setup Skill System trước.");
            return;
        }

        ValidateDatabase(database);
    }

    [MenuItem("Tools/Android2D/Skills/Debug/Add 1 Skill Point")]
    private static void AddDebugSkillPoint()
    {
        PlayerSkillSystem skillSystem = FindPlayModeSkillSystem();
        if (skillSystem == null)
            return;

        skillSystem.AddSkillPoints(1);
        Debug.Log($"[Kỹ năng] Đã cộng 1 Điểm Kỹ năng. Hiện có: {skillSystem.AvailableSkillPoints}.", skillSystem);
    }

    [MenuItem("Tools/Android2D/Skills/Debug/Unlock Or Upgrade Selected Skill")]
    private static void UnlockOrUpgradeSelected()
    {
        PlayerSkillSystem skillSystem = FindPlayModeSkillSystem();
        if (skillSystem == null)
            return;

        SkillDefinition selectedSkill = Selection.activeObject as SkillDefinition;
        if (selectedSkill == null)
        {
            Debug.LogError("[Kỹ năng] Hãy chọn một asset SkillDefinition trong cửa sổ Project trước.");
            return;
        }

        SkillOperationResult result = skillSystem.IsUnlocked(selectedSkill)
            ? skillSystem.Upgrade(selectedSkill)
            : skillSystem.Unlock(selectedSkill);
        if (result.Succeeded)
        {
            Debug.Log(
                $"[Kỹ năng] '{selectedSkill.DisplayName}' hiện ở bậc {skillSystem.GetSkillRank(selectedSkill)}. " +
                $"Điểm còn lại: {skillSystem.AvailableSkillPoints}.",
                skillSystem);
        }
        else
        {
            Debug.LogWarning($"[Kỹ năng] {result.Failure}: {result.Message}", skillSystem);
        }
    }

    private static SkillDefinition LoadOrCreateSkill(string path)
    {
        SkillDefinition skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
        if (skill != null)
            return skill;

        skill = ScriptableObject.CreateInstance<SkillDefinition>();
        AssetDatabase.CreateAsset(skill, path);
        Undo.RegisterCreatedObjectUndo(skill, UndoName);
        return skill;
    }

    private static SkillDatabase LoadOrCreateDatabase()
    {
        SkillDatabase database = AssetDatabase.LoadAssetAtPath<SkillDatabase>(DatabasePath);
        if (database != null)
            return database;

        database = ScriptableObject.CreateInstance<SkillDatabase>();
        AssetDatabase.CreateAsset(database, DatabasePath);
        Undo.RegisterCreatedObjectUndo(database, UndoName);
        return database;
    }

    private static void ConfigureSkill(
        SkillDefinition skill,
        string skillId,
        string displayName,
        string description,
        SkillCategory category,
        int maxRank,
        int cost,
        int sortOrder,
        int maxHealthBonus,
        int attackBonus,
        float moveSpeedBonus,
        SkillDefinition prerequisite)
    {
        Undo.RecordObject(skill, UndoName);
        SerializedObject serialized = new SerializedObject(skill);
        serialized.FindProperty("skillId").stringValue = skillId;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("description").stringValue = description;
        serialized.FindProperty("category").enumValueIndex = (int)category;
        serialized.FindProperty("skillType").enumValueIndex = (int)SkillType.Passive;
        serialized.FindProperty("maxRank").intValue = maxRank;
        serialized.FindProperty("skillPointCost").intValue = cost;
        serialized.FindProperty("unlockLevel").intValue = 1;
        serialized.FindProperty("sortOrder").intValue = sortOrder;
        serialized.FindProperty("maxHealthBonusPerRank").intValue = maxHealthBonus;
        serialized.FindProperty("attackBonusPerRank").intValue = attackBonus;
        serialized.FindProperty("moveSpeedBonusPerRank").floatValue = moveSpeedBonus;

        SerializedProperty prerequisites = serialized.FindProperty("prerequisites");
        prerequisites.ClearArray();
        if (prerequisite != null)
        {
            prerequisites.InsertArrayElementAtIndex(0);
            SerializedProperty entry = prerequisites.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("skill").objectReferenceValue = prerequisite;
            entry.FindPropertyRelative("requiredRank").intValue = 1;
        }

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(skill);
    }

    private static void ConfigureDatabase(
        SkillDatabase database,
        SkillDefinition vitality,
        SkillDefinition power,
        SkillDefinition dashMastery)
    {
        Undo.RecordObject(database, UndoName);
        SerializedObject serialized = new SerializedObject(database);
        SerializedProperty skills = serialized.FindProperty("skills");
        skills.arraySize = 3;
        skills.GetArrayElementAtIndex(0).objectReferenceValue = vitality;
        skills.GetArrayElementAtIndex(1).objectReferenceValue = power;
        skills.GetArrayElementAtIndex(2).objectReferenceValue = dashMastery;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
    }

    private static void ValidateDatabase(SkillDatabase database)
    {
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();
        database.Validate(errors, warnings);

        for (int i = 0; i < warnings.Count; i++)
            Debug.LogWarning($"[Kỹ năng] {warnings[i]}", database);
        for (int i = 0; i < errors.Count; i++)
            Debug.LogError($"[Kỹ năng] {errors[i]}", database);

        if (errors.Count == 0)
        {
            Debug.Log(
                $"[Kỹ năng] Kiểm tra đạt: {database.Skills.Count} kỹ năng, có {warnings.Count} cảnh báo.",
                database);
        }
    }

    private static PlayerController FindSingleScenePlayer(Scene scene)
    {
        PlayerController[] all = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include);
        PlayerController result = null;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].gameObject.scene != scene)
                continue;

            if (result != null)
            {
                Debug.LogError($"[Kỹ năng] Scene '{scene.name}' có nhiều hơn một PlayerController.");
                return null;
            }

            result = all[i];
        }

        if (result == null)
            Debug.LogError($"[Kỹ năng] Không tìm thấy PlayerController trong Scene '{scene.name}'.");
        return result;
    }

    private static PlayerSkillSystem FindPlayModeSkillSystem()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[Kỹ năng] Lệnh kiểm thử chỉ dùng được trong Play Mode.");
            return null;
        }

        PlayerSkillSystem[] systems = UnityEngine.Object.FindObjectsByType<PlayerSkillSystem>(FindObjectsInactive.Exclude);
        if (systems.Length != 1)
        {
            Debug.LogError($"[Kỹ năng] Cần đúng một PlayerSkillSystem đang hoạt động, nhưng tìm thấy {systems.Length}.");
            return null;
        }

        return systems[0];
    }

    private static bool EnsureFolder(string parent, string childName)
    {
        string path = parent + "/" + childName;
        if (AssetDatabase.IsValidFolder(path))
            return true;
        if (!AssetDatabase.IsValidFolder(parent))
        {
            Debug.LogError($"[Kỹ năng] Không tìm thấy thư mục cha '{parent}'.");
            return false;
        }

        return !string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, childName));
    }
}
