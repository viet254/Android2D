using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerExperience))]
public sealed class PlayerSkillSystem : MonoBehaviour
{
    [Header("Cấu hình")]
    [SerializeField, InspectorName("Cơ sở dữ liệu kỹ năng")] private SkillDatabase skillDatabase;
    [SerializeField, Min(0), InspectorName("Điểm kỹ năng ban đầu")] private int initialSkillPoints;
    [SerializeField, Min(0), InspectorName("Điểm nhận mỗi cấp")] private int skillPointsPerLevel = 1;

    [Header("Trạng thái khi chạy")]
    [SerializeField, Min(0), InspectorName("Điểm kỹ năng hiện có")] private int availableSkillPoints;
    [SerializeField, InspectorName("Kỹ năng đã học")] private List<PlayerSkillState> skillStates = new List<PlayerSkillState>();

    private readonly Dictionary<string, PlayerSkillState> statesById =
        new Dictionary<string, PlayerSkillState>(StringComparer.Ordinal);
    private PlayerStats playerStats;
    private PlayerExperience playerExperience;

    public SkillDatabase Database => skillDatabase;
    public int AvailableSkillPoints => availableSkillPoints;
    public IReadOnlyList<PlayerSkillState> SkillStates => skillStates;

    public event Action<SkillDefinition, int> OnSkillUnlocked;
    public event Action<SkillDefinition, int> OnSkillUpgraded;
    public event Action<int> OnSkillPointsChanged;
    public event Action OnSkillsRestored;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        playerExperience = GetComponent<PlayerExperience>();
        availableSkillPoints = Mathf.Max(0, initialSkillPoints);
        RebuildRuntimeState();
        RecalculatePassiveModifiers();
    }

    private void OnEnable()
    {
        if (playerExperience == null)
            playerExperience = GetComponent<PlayerExperience>();
        if (playerExperience != null)
            playerExperience.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        if (playerExperience != null)
            playerExperience.OnLevelUp -= HandleLevelUp;
    }

    private void Start()
    {
        RecalculatePassiveModifiers();
    }

    public int GetSkillRank(SkillDefinition skill)
    {
        return skill == null ? 0 : GetSkillRank(skill.SkillId);
    }

    public int GetSkillRank(string skillId)
    {
        return !string.IsNullOrWhiteSpace(skillId)
            && statesById.TryGetValue(skillId, out PlayerSkillState state)
                ? state.rank
                : 0;
    }

    public bool IsUnlocked(SkillDefinition skill)
    {
        return GetSkillRank(skill) > 0;
    }

    public SkillOperationResult CanUnlock(SkillDefinition skill)
    {
        SkillOperationResult commonResult = ValidateCommonRequirements(skill);
        if (!commonResult.Succeeded)
            return commonResult;

        if (GetSkillRank(skill) > 0)
        {
            return SkillOperationResult.Fail(
                SkillOperationFailure.AlreadyUnlocked,
                $"Kỹ năng '{skill.DisplayName}' đã được mở khóa.");
        }

        return SkillOperationResult.Success();
    }

    public SkillOperationResult Unlock(SkillDefinition skill)
    {
        SkillOperationResult result = CanUnlock(skill);
        if (!result.Succeeded)
            return result;

        SpendSkillPointsInternal(skill.SkillPointCost);
        PlayerSkillState state = GetOrCreateState(skill.SkillId);
        state.rank = 1;
        RecalculatePassiveModifiers();
        OnSkillUnlocked?.Invoke(skill, state.rank);
        return SkillOperationResult.Success();
    }

    public SkillOperationResult CanUpgrade(SkillDefinition skill)
    {
        SkillOperationResult validResult = ValidateSkill(skill);
        if (!validResult.Succeeded)
            return validResult;

        int currentRank = GetSkillRank(skill);
        if (currentRank <= 0)
        {
            return SkillOperationResult.Fail(
                SkillOperationFailure.NotUnlocked,
                $"Cần mở khóa kỹ năng '{skill.DisplayName}' trước khi nâng bậc.");
        }

        if (currentRank >= skill.MaxRank)
        {
            return SkillOperationResult.Fail(
                SkillOperationFailure.MaxRankReached,
                $"Kỹ năng '{skill.DisplayName}' đã đạt bậc tối đa.");
        }

        return ValidateProgressionRequirements(skill);
    }

    public SkillOperationResult Upgrade(SkillDefinition skill)
    {
        SkillOperationResult result = CanUpgrade(skill);
        if (!result.Succeeded)
            return result;

        SpendSkillPointsInternal(skill.SkillPointCost);
        PlayerSkillState state = GetOrCreateState(skill.SkillId);
        state.rank++;
        RecalculatePassiveModifiers();
        OnSkillUpgraded?.Invoke(skill, state.rank);
        return SkillOperationResult.Success();
    }

    public void AddSkillPoints(int amount)
    {
        if (amount <= 0)
            return;

        availableSkillPoints += amount;
        OnSkillPointsChanged?.Invoke(availableSkillPoints);
    }

    public bool SpendSkillPoints(int amount)
    {
        if (amount < 0 || availableSkillPoints < amount)
            return false;

        SpendSkillPointsInternal(amount);
        return true;
    }

    public List<PlayerSkillState> CaptureState()
    {
        List<PlayerSkillState> result = new List<PlayerSkillState>(statesById.Count);
        if (skillDatabase == null)
            return result;

        IReadOnlyList<SkillDefinition> definitions = skillDatabase.Skills;
        for (int i = 0; i < definitions.Count; i++)
        {
            SkillDefinition skill = definitions[i];
            int rank = GetSkillRank(skill);
            if (skill != null && rank > 0)
                result.Add(new PlayerSkillState(skill.SkillId, rank));
        }

        return result;
    }

    public void RestoreState(int skillPoints, IReadOnlyList<PlayerSkillState> savedStates)
    {
        availableSkillPoints = Mathf.Max(0, skillPoints);
        skillStates.Clear();
        statesById.Clear();

        if (savedStates != null)
        {
            for (int i = 0; i < savedStates.Count; i++)
            {
                PlayerSkillState savedState = savedStates[i];
                if (savedState == null || string.IsNullOrWhiteSpace(savedState.skillId) || savedState.rank <= 0)
                    continue;

                if (skillDatabase == null
                    || !skillDatabase.TryGetSkill(savedState.skillId, out SkillDefinition skill))
                {
                    Debug.LogWarning($"[PlayerSkillSystem] Bỏ qua skillId không tồn tại '{savedState.skillId}' khi tải dữ liệu.", this);
                    continue;
                }

                if (statesById.ContainsKey(savedState.skillId))
                {
                    Debug.LogWarning($"[PlayerSkillSystem] Bỏ qua skillId bị trùng trong dữ liệu lưu '{savedState.skillId}'.", this);
                    continue;
                }

                PlayerSkillState restoredState = new PlayerSkillState(
                    savedState.skillId,
                    Mathf.Clamp(savedState.rank, 1, skill.MaxRank));
                skillStates.Add(restoredState);
                statesById.Add(restoredState.skillId, restoredState);
            }
        }

        RecalculatePassiveModifiers();
        OnSkillPointsChanged?.Invoke(availableSkillPoints);
        OnSkillsRestored?.Invoke();
    }

    private SkillOperationResult ValidateCommonRequirements(SkillDefinition skill)
    {
        SkillOperationResult validResult = ValidateSkill(skill);
        return validResult.Succeeded ? ValidateProgressionRequirements(skill) : validResult;
    }

    private SkillOperationResult ValidateSkill(SkillDefinition skill)
    {
        if (skill == null
            || skillDatabase == null
            || string.IsNullOrWhiteSpace(skill.SkillId)
            || !skillDatabase.TryGetSkill(skill.SkillId, out SkillDefinition registeredSkill)
            || registeredSkill != skill)
        {
            return SkillOperationResult.Fail(
                SkillOperationFailure.InvalidSkill,
                "Kỹ năng không hợp lệ hoặc chưa được đăng ký trong Cơ sở dữ liệu Kỹ năng của Player.");
        }

        return SkillOperationResult.Success();
    }

    private SkillOperationResult ValidateProgressionRequirements(SkillDefinition skill)
    {
        int level = playerExperience != null ? playerExperience.CurrentLevel : 1;
        if (level < skill.UnlockLevel)
        {
            return SkillOperationResult.Fail(
                SkillOperationFailure.PlayerLevelTooLow,
                $"Cần đạt Cấp {skill.UnlockLevel} để mở kỹ năng '{skill.DisplayName}'.");
        }

        IReadOnlyList<SkillPrerequisite> prerequisites = skill.Prerequisites;
        for (int i = 0; i < prerequisites.Count; i++)
        {
            SkillPrerequisite prerequisite = prerequisites[i];
            if (prerequisite == null
                || prerequisite.Skill == null
                || GetSkillRank(prerequisite.Skill) < prerequisite.RequiredRank)
            {
                return SkillOperationResult.Fail(
                    SkillOperationFailure.MissingPrerequisite,
                    $"Chưa đáp ứng điều kiện tiên quyết của kỹ năng '{skill.DisplayName}'.");
            }
        }

        if (availableSkillPoints < skill.SkillPointCost)
        {
            return SkillOperationResult.Fail(
                SkillOperationFailure.InsufficientSkillPoints,
                $"Kỹ năng '{skill.DisplayName}' cần {skill.SkillPointCost} Điểm Kỹ năng.");
        }

        return SkillOperationResult.Success();
    }

    private PlayerSkillState GetOrCreateState(string skillId)
    {
        if (statesById.TryGetValue(skillId, out PlayerSkillState state))
            return state;

        state = new PlayerSkillState(skillId, 0);
        statesById.Add(skillId, state);
        skillStates.Add(state);
        return state;
    }

    private void SpendSkillPointsInternal(int amount)
    {
        availableSkillPoints -= amount;
        OnSkillPointsChanged?.Invoke(availableSkillPoints);
    }

    private void HandleLevelUp(int newLevel)
    {
        if (skillPointsPerLevel > 0)
            AddSkillPoints(skillPointsPerLevel);
    }

    private void RebuildRuntimeState()
    {
        statesById.Clear();
        for (int i = skillStates.Count - 1; i >= 0; i--)
        {
            PlayerSkillState state = skillStates[i];
            if (state == null
                || string.IsNullOrWhiteSpace(state.skillId)
                || state.rank <= 0
                || statesById.ContainsKey(state.skillId))
            {
                skillStates.RemoveAt(i);
                continue;
            }

            statesById.Add(state.skillId, state);
        }
    }

    private void RecalculatePassiveModifiers()
    {
        int maxHealthBonus = 0;
        int attackBonus = 0;
        float moveSpeedBonus = 0f;

        if (skillDatabase != null)
        {
            IReadOnlyList<SkillDefinition> definitions = skillDatabase.Skills;
            for (int i = 0; i < definitions.Count; i++)
            {
                SkillDefinition skill = definitions[i];
                if (skill == null || skill.Type != SkillType.Passive)
                    continue;

                int rank = GetSkillRank(skill);
                maxHealthBonus += skill.MaxHealthBonusPerRank * rank;
                attackBonus += skill.AttackBonusPerRank * rank;
                moveSpeedBonus += skill.MoveSpeedBonusPerRank * rank;
            }
        }

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
        if (playerStats != null)
            playerStats.ApplySkillModifiers(maxHealthBonus, attackBonus, moveSpeedBonus);
    }

    private void OnValidate()
    {
        initialSkillPoints = Mathf.Max(0, initialSkillPoints);
        skillPointsPerLevel = Mathf.Max(0, skillPointsPerLevel);
        skillStates ??= new List<PlayerSkillState>();
    }
}
