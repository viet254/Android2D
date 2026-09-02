using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Android2D/Levels/Level Data")]
public sealed class LevelData : ScriptableObject
{
    [SerializeField] private string levelId;
    [SerializeField] private string sceneName;
    [SerializeField] private string displayName;
    [SerializeField] private LevelData nextLevel;

    public string LevelId => levelId;
    public string SceneName => sceneName;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? levelId : displayName;
    public LevelData NextLevel => nextLevel;

    public bool IsValid => !string.IsNullOrWhiteSpace(levelId)
        && !string.IsNullOrWhiteSpace(sceneName);
}
