using System;
using UnityEngine;

[Serializable]
public sealed class SkillPrerequisite
{
    [SerializeField, InspectorName("Kỹ năng yêu cầu")] private SkillDefinition skill;
    [SerializeField, Min(1), InspectorName("Bậc yêu cầu")] private int requiredRank = 1;

    public SkillDefinition Skill => skill;
    public int RequiredRank => Mathf.Max(1, requiredRank);
}
