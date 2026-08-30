# Helper function to move file with .meta
function Move-WithMeta {
    param([string]$Source, [string]$Destination)
    Copy-Item $Source -Destination $Destination
    Copy-Item "$Source.meta" -Destination "$Destination.meta"
    Remove-Item $Source
    Remove-Item "$Source.meta"
    Write-Host "Moved: $Source -> $Destination"
}

# Move Core scripts
Move-WithMeta "Scripts/Health.cs" "Core/Stats/Health.cs"

# Move Player scripts
Move-WithMeta "Scripts/PlayerController.cs" "Player/Character/Scripts/PlayerController.cs"
Move-WithMeta "Scripts/PlayerStats.cs" "Player/Stats/Scripts/PlayerStats.cs"
Move-WithMeta "Scripts/PlayerExperience.cs" "Player/Stats/Scripts/PlayerExperience.cs"
Move-WithMeta "Scripts/PlayerAttack.cs" "Player/Combat/Scripts/PlayerAttack.cs"

# Move Enemy scripts
Move-WithMeta "Scripts/Enemy.cs" "Enemy/Base/Scripts/Enemy.cs"
Move-WithMeta "Scripts/EnemyAI.cs" "Enemy/AI/Scripts/EnemyAI.cs"
Move-WithMeta "Scripts/EnemyAttack.cs" "Enemy/Combat/Scripts/EnemyAttack.cs"
Move-WithMeta "Scripts/ExperienceReward.cs" "Enemy/Loot/Scripts/ExperienceReward.cs"

# Move World scripts
Move-WithMeta "Scripts/MapBounds.cs" "World/Levels/Scripts/MapBounds.cs"

Write-Host "All scripts moved successfully!"
