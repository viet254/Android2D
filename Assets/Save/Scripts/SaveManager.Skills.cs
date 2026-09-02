using System;
using System.Collections.Generic;
using UnityEngine;

public partial class SaveManager
{
    private const int PreviousVersionWithoutSkills = 4;
    private PlayerSkillSystem skillSystem;

    internal static bool IsSupportedSnapshotVersion(int version)
    {
        return version == CurrentVersion || version == PreviousVersionWithoutSkills;
    }

    internal static bool TryPrepareSaveData(GameSaveData data, out string error)
    {
        if (data == null)
        {
            error = "Dữ liệu Save không chứa snapshot hợp lệ.";
            return false;
        }

        if (!IsSupportedSnapshotVersion(data.version))
        {
            error = $"Không hỗ trợ Save phiên bản {data.version}; phiên bản hiện tại là {CurrentVersion}.";
            return false;
        }

        if (data.version == PreviousVersionWithoutSkills)
        {
            data.version = CurrentVersion;
            data.skills = new SkillSystemSaveData();
        }

        data.skills ??= new SkillSystemSaveData();
        data.skills.skills ??= new List<SkillRankSaveData>();
        if (data.skills.skillPoints < 0)
        {
            error = "Điểm Kỹ năng trong Save không được nhỏ hơn 0.";
            return false;
        }

        error = null;
        return true;
    }

    private void CaptureSkillData(GameSaveData data)
    {
        data.skills = new SkillSystemSaveData
        {
            skillPoints = skillSystem.AvailableSkillPoints
        };

        List<PlayerSkillState> states = skillSystem.CaptureState();
        for (int i = 0; i < states.Count; i++)
        {
            PlayerSkillState state = states[i];
            data.skills.skills.Add(new SkillRankSaveData
            {
                skillId = state.skillId,
                rank = state.rank
            });
        }
    }

    private void CaptureTransitionSkills(PlayerTransitionState transition)
    {
        transition.SkillPoints = skillSystem.AvailableSkillPoints;
        List<PlayerSkillState> states = skillSystem.CaptureState();
        for (int i = 0; i < states.Count; i++)
            transition.Skills.Add(new PlayerSkillState(states[i].skillId, states[i].rank));
    }

    private bool RestoreSkillData(SkillSystemSaveData savedSkills)
    {
        if (skillSystem == null)
        {
            Debug.LogError("[SaveManager] Không tìm thấy PlayerSkillSystem.", this);
            return false;
        }

        List<PlayerSkillState> states = new List<PlayerSkillState>();
        if (savedSkills?.skills != null)
        {
            for (int i = 0; i < savedSkills.skills.Count; i++)
            {
                SkillRankSaveData saved = savedSkills.skills[i];
                if (saved == null || string.IsNullOrWhiteSpace(saved.skillId) || saved.rank <= 0)
                {
                    Debug.LogWarning($"[SaveManager] Đã bỏ qua dữ liệu kỹ năng không hợp lệ tại vị trí {i}.", this);
                    continue;
                }

                states.Add(new PlayerSkillState(saved.skillId, saved.rank));
            }
        }

        skillSystem.RestoreState(Mathf.Max(0, savedSkills?.skillPoints ?? 0), states);
        return true;
    }

    private void RestoreTransitionSkills(PlayerTransitionState transition)
    {
        skillSystem.RestoreState(transition.SkillPoints, transition.Skills);
    }
}
