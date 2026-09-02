using System;
using UnityEngine;

[Serializable]
public sealed class PlayerSkillState
{
    [InspectorName("ID kỹ năng")] public string skillId;
    [InspectorName("Bậc hiện tại")] public int rank;

    public PlayerSkillState()
    {
    }

    public PlayerSkillState(string skillId, int rank)
    {
        this.skillId = skillId;
        this.rank = rank;
    }
}
