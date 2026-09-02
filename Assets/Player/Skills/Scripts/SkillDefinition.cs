using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Skill", menuName = "Android2D/Kỹ năng/Định nghĩa kỹ năng")]
public sealed class SkillDefinition : ScriptableObject
{
    [Header("Định danh")]
    [SerializeField, InspectorName("ID kỹ năng")] private string skillId;
    [SerializeField, InspectorName("Tên hiển thị")] private string displayName;
    [SerializeField, TextArea(2, 5), InspectorName("Mô tả")] private string description;
    [SerializeField, InspectorName("Biểu tượng")] private Sprite icon;
    [SerializeField, InspectorName("Nhóm kỹ năng")] private SkillCategory category;
    [SerializeField, InspectorName("Loại kỹ năng")] private SkillType skillType = SkillType.Passive;

    [Header("Tiến trình")]
    [SerializeField, Min(1), InspectorName("Bậc tối đa")] private int maxRank = 1;
    [SerializeField, Min(0), InspectorName("Điểm tiêu hao mỗi bậc")] private int skillPointCost = 1;
    [SerializeField, Min(1), InspectorName("Cấp mở khóa")] private int unlockLevel = 1;
    [SerializeField, InspectorName("Thứ tự hiển thị")] private int sortOrder;
    [SerializeField, InspectorName("Điều kiện tiên quyết")] private List<SkillPrerequisite> prerequisites = new List<SkillPrerequisite>();

    [Header("Chỉ số bị động mỗi bậc")]
    [SerializeField, InspectorName("Máu tối đa cộng thêm")] private int maxHealthBonusPerRank;
    [SerializeField, InspectorName("Sát thương cộng thêm")] private int attackBonusPerRank;
    [SerializeField, InspectorName("Tốc độ di chuyển cộng thêm")] private float moveSpeedBonusPerRank;

    public string SkillId => skillId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public SkillCategory Category => category;
    public SkillType Type => skillType;
    public int MaxRank => Mathf.Max(1, maxRank);
    public int SkillPointCost => Mathf.Max(0, skillPointCost);
    public int UnlockLevel => Mathf.Max(1, unlockLevel);
    public int SortOrder => sortOrder;
    public IReadOnlyList<SkillPrerequisite> Prerequisites => prerequisites;
    public int MaxHealthBonusPerRank => maxHealthBonusPerRank;
    public int AttackBonusPerRank => attackBonusPerRank;
    public float MoveSpeedBonusPerRank => moveSpeedBonusPerRank;

    private void OnValidate()
    {
        skillId = skillId == null ? string.Empty : skillId.Trim();
        maxRank = Mathf.Max(1, maxRank);
        skillPointCost = Mathf.Max(0, skillPointCost);
        unlockLevel = Mathf.Max(1, unlockLevel);
        prerequisites ??= new List<SkillPrerequisite>();
    }
}
