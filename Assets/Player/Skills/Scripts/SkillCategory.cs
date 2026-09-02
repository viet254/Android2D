using UnityEngine;

public enum SkillCategory
{
    [InspectorName("Chiến đấu")] Combat,
    [InspectorName("Di chuyển")] Movement,
    [InspectorName("Phòng thủ")] Defense,
    [InspectorName("Phép thuật")] Magic,
    [InspectorName("Hỗ trợ")] Utility
}
