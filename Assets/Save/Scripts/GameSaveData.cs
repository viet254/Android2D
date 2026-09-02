using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public int version = 5;
    public string sceneName;
    public PlayerSaveData player = new PlayerSaveData();
    public ProgressionSaveData progression = new ProgressionSaveData();
    public SkillSystemSaveData skills = new SkillSystemSaveData();
    public List<InventorySlotSaveData> inventory = new List<InventorySlotSaveData>();
    public EquipmentSaveData equipment = new EquipmentSaveData();
    public List<EnemySaveData> enemies = new List<EnemySaveData>();
    public List<WorldPickupSaveData> worldPickups = new List<WorldPickupSaveData>();
}

[Serializable]
public class WorldPickupSaveData
{
    public string itemId;
    public int quantity;
    public Vector3SaveData position = new Vector3SaveData();
}

[Serializable]
public class Vector3SaveData
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class PlayerSaveData
{
    public Vector3SaveData position = new Vector3SaveData();
    public int currentHealth;
}

[Serializable]
public class EnemySaveData
{
    public string persistentId;
    public bool alive;
    public Vector3SaveData position = new Vector3SaveData();
    public int currentHealth;
}

[Serializable]
public class ProgressionSaveData
{
    public int level = 1;
    public int currentExperience;
}

[Serializable]
public class SkillSystemSaveData
{
    public int skillPoints;
    public List<SkillRankSaveData> skills = new List<SkillRankSaveData>();
}

[Serializable]
public class SkillRankSaveData
{
    public string skillId;
    public int rank;
}

[Serializable]
public class InventorySlotSaveData
{
    public int slotIndex;
    public string itemId;
    public int quantity;
}

[Serializable]
public class EquipmentSaveData
{
    public string weaponItemId;
}
