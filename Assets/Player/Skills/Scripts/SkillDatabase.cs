using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Android2D/Kỹ năng/Cơ sở dữ liệu kỹ năng")]
public sealed class SkillDatabase : ScriptableObject
{
    [SerializeField, InspectorName("Danh sách kỹ năng")] private List<SkillDefinition> skills = new List<SkillDefinition>();

    private readonly Dictionary<string, SkillDefinition> skillsById =
        new Dictionary<string, SkillDefinition>(StringComparer.Ordinal);
    private bool lookupDirty = true;

    public IReadOnlyList<SkillDefinition> Skills => skills;

    public bool TryGetSkill(string skillId, out SkillDefinition skill)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            skill = null;
            return false;
        }

        EnsureLookup();
        return skillsById.TryGetValue(skillId, out skill);
    }

    public bool Validate(List<string> errors, List<string> warnings)
    {
        if (errors == null)
            throw new ArgumentNullException(nameof(errors));
        if (warnings == null)
            throw new ArgumentNullException(nameof(warnings));

        errors.Clear();
        warnings.Clear();

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        HashSet<SkillDefinition> definitions = new HashSet<SkillDefinition>();
        for (int i = 0; i < skills.Count; i++)
        {
            SkillDefinition skill = skills[i];
            if (skill == null)
            {
                errors.Add($"Mục số {i} trong Cơ sở dữ liệu Kỹ năng đang trống.");
                continue;
            }

            definitions.Add(skill);
            if (string.IsNullOrWhiteSpace(skill.SkillId))
                errors.Add($"Kỹ năng '{skill.name}' chưa có skillId.");
            else if (!ids.Add(skill.SkillId))
                errors.Add($"Trùng skillId '{skill.SkillId}'.");

            if (skill.MaxRank <= 0)
                errors.Add($"Kỹ năng '{skill.name}' có maxRank không hợp lệ (phải lớn hơn 0).");
            if (skill.SkillPointCost < 0)
                errors.Add($"Kỹ năng '{skill.name}' có chi phí Điểm Kỹ năng âm.");

            IReadOnlyList<SkillPrerequisite> prerequisites = skill.Prerequisites;
            for (int prerequisiteIndex = 0; prerequisiteIndex < prerequisites.Count; prerequisiteIndex++)
            {
                SkillPrerequisite prerequisite = prerequisites[prerequisiteIndex];
                if (prerequisite == null || prerequisite.Skill == null)
                {
                    errors.Add($"Kỹ năng '{skill.SkillId}' thiếu điều kiện tiên quyết tại vị trí {prerequisiteIndex}.");
                    continue;
                }

                if (prerequisite.Skill == skill)
                    errors.Add($"Kỹ năng '{skill.SkillId}' không được dùng chính nó làm điều kiện tiên quyết.");
                if (!definitions.Contains(prerequisite.Skill) && !skills.Contains(prerequisite.Skill))
                    errors.Add($"Kỹ năng '{skill.SkillId}' tham chiếu điều kiện '{prerequisite.Skill.name}' nằm ngoài Database này.");
                if (prerequisite.RequiredRank < 1 || prerequisite.RequiredRank > prerequisite.Skill.MaxRank)
                    errors.Add($"Kỹ năng '{skill.SkillId}' yêu cầu bậc {prerequisite.RequiredRank} không hợp lệ của '{prerequisite.Skill.SkillId}'.");
            }

            if (skill.Icon == null)
                warnings.Add($"Kỹ năng '{skill.SkillId}' chưa có biểu tượng.");
        }

        Dictionary<SkillDefinition, byte> visitStates = new Dictionary<SkillDefinition, byte>();
        for (int i = 0; i < skills.Count; i++)
        {
            SkillDefinition skill = skills[i];
            if (skill != null && HasCycle(skill, definitions, visitStates))
            {
                errors.Add($"Phát hiện vòng lặp điều kiện tiên quyết bắt đầu từ kỹ năng '{skill.SkillId}'.");
                break;
            }
        }

        lookupDirty = true;
        return errors.Count == 0;
    }

    private bool HasCycle(
        SkillDefinition skill,
        HashSet<SkillDefinition> definitions,
        Dictionary<SkillDefinition, byte> visitStates)
    {
        if (visitStates.TryGetValue(skill, out byte state))
            return state == 1;

        visitStates[skill] = 1;
        IReadOnlyList<SkillPrerequisite> prerequisites = skill.Prerequisites;
        for (int i = 0; i < prerequisites.Count; i++)
        {
            SkillDefinition prerequisite = prerequisites[i]?.Skill;
            if (prerequisite != null
                && definitions.Contains(prerequisite)
                && HasCycle(prerequisite, definitions, visitStates))
            {
                return true;
            }
        }

        visitStates[skill] = 2;
        return false;
    }

    private void EnsureLookup()
    {
        if (!lookupDirty)
            return;

        skillsById.Clear();
        for (int i = 0; i < skills.Count; i++)
        {
            SkillDefinition skill = skills[i];
            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                continue;

            if (!skillsById.TryAdd(skill.SkillId, skill))
                Debug.LogError($"[SkillDatabase] Trùng skillId '{skill.SkillId}'.", this);
        }

        lookupDirty = false;
    }

    private void OnEnable()
    {
        lookupDirty = true;
    }

    private void OnValidate()
    {
        skills ??= new List<SkillDefinition>();
        lookupDirty = true;
    }
}
