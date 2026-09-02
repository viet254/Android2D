public enum SkillOperationFailure
{
    None,
    InvalidSkill,
    AlreadyUnlocked,
    NotUnlocked,
    InsufficientSkillPoints,
    PlayerLevelTooLow,
    MissingPrerequisite,
    MaxRankReached
}

public readonly struct SkillOperationResult
{
    public bool Succeeded { get; }
    public SkillOperationFailure Failure { get; }
    public string Message { get; }

    private SkillOperationResult(bool succeeded, SkillOperationFailure failure, string message)
    {
        Succeeded = succeeded;
        Failure = failure;
        Message = message;
    }

    public static SkillOperationResult Success()
    {
        return new SkillOperationResult(true, SkillOperationFailure.None, string.Empty);
    }

    public static SkillOperationResult Fail(SkillOperationFailure failure, string message)
    {
        return new SkillOperationResult(false, failure, message);
    }
}
