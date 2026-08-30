# Script to organize sprites and animations

# Helper function to move file with .meta
function Move-WithMeta {
    param([string]$Source, [string]$Destination)
    if (Test-Path $Source) {
        Copy-Item $Source -Destination $Destination -Force
        if (Test-Path "$Source.meta") {
            Copy-Item "$Source.meta" -Destination "$Destination.meta" -Force
            Remove-Item "$Source.meta"
        }
        Remove-Item $Source
        Write-Host "Moved: $Source -> $Destination"
    } else {
        Write-Host "Source not found: $Source"
    }
}

# Helper function to move directory with .meta
function Move-DirWithMeta {
    param([string]$Source, [string]$Destination)
    if (Test-Path $Source) {
        Copy-Item $Source -Destination $Destination -Recurse -Force
        if (Test-Path "$Source.meta") {
            Copy-Item "$Source.meta" -Destination "$Destination.meta" -Force
            Remove-Item "$Source.meta"
        }
        Remove-Item $Source -Recurse -Force
        Write-Host "Moved folder: $Source -> $Destination"
    } else {
        Write-Host "Source folder not found: $Source"
    }
}

Write-Host "Moving Player sprites..."
# Male hero sprites to Player
Move-WithMeta "Sprites/male_hero-combo_1.png" "Player/Character/Sprites/male_hero-combo_1.png"
Move-WithMeta "Sprites/male_hero-combo_1_end.png" "Player/Character/Sprites/male_hero-combo_1_end.png"
Move-WithMeta "Sprites/male_hero-design.png" "Player/Character/Sprites/male_hero-design.png"
Move-WithMeta "Sprites/male_hero-fall.png" "Player/Character/Sprites/male_hero-fall.png"
Move-WithMeta "Sprites/male_hero-fall_loop.png" "Player/Character/Sprites/male_hero-fall_loop.png"
Move-WithMeta "Sprites/male_hero-idle.png" "Player/Character/Sprites/male_hero-idle.png"
Move-WithMeta "Sprites/male_hero-jump.png" "Player/Character/Sprites/male_hero-jump.png"
Move-WithMeta "Sprites/male_hero-run.png" "Player/Character/Sprites/male_hero-run.png"
Move-WithMeta "Sprites/male_hero-walk.png" "Player/Character/Sprites/male_hero-walk.png"
Move-WithMeta "Sprites/idle.png" "Player/Character/Sprites/idle.png"
Move-WithMeta "Sprites/gnu-120x100.png" "Player/Character/Sprites/gnu-120x100.png"
Move-WithMeta "Sprites/warrior_spritesheet.png" "Player/Character/Sprites/warrior_spritesheet.png"
Move-WithMeta "Sprites/vampire_hunter_spritesheet.png" "Player/Character/Sprites/vampire_hunter_spritesheet.png"

Write-Host "Moving Enemy sprites..."
# Orc sprites to Enemy
Move-WithMeta "Sprites/Orc_Attack01.png" "Enemy/EnemyTypes/Orc/Sprites/Orc_Attack01.png"
Move-WithMeta "Sprites/Orc_Attack02.png" "Enemy/EnemyTypes/Orc/Sprites/Orc_Attack02.png"
Move-WithMeta "Sprites/Orc_Death.png" "Enemy/EnemyTypes/Orc/Sprites/Orc_Death.png"
Move-WithMeta "Sprites/Orc_Hurt.png" "Enemy/EnemyTypes/Orc/Sprites/Orc_Hurt.png"
Move-WithMeta "Sprites/Orc_Idle.png" "Enemy/EnemyTypes/Orc/Sprites/Orc_Idle.png"
Move-WithMeta "Sprites/Orc_Walk.png" "Enemy/EnemyTypes/Orc/Sprites/Orc_Walk.png"

Write-Host "Moving Tilemap sprites..."
# Tilemap sprites
Move-WithMeta "Sprites/world_tileset.png" "World/Tilemaps/Sprites/world_tileset.png"
Move-WithMeta "Sprites/world_tileset_v2.png" "World/Tilemaps/Sprites/world_tileset_v2.png"
Move-WithMeta "Sprites/InfernoTiles.png" "World/Tilemaps/Sprites/InfernoTiles.png"

Write-Host "Moving Item sprites..."
# Item sprites
Move-WithMeta "Sprites/items-palette-swaps.png" "Items/Sprites/items-palette-swaps.png"

Write-Host "Moving UI and Interactable sprites..."
# UI sprites folder
Move-DirWithMeta "Sprites/UI" "UI/HUD/Sprites/UI"

# Key icons for interactables
Move-DirWithMeta "Sprites/Key_Icon" "World/Interactables/Sprites/Key_Icon"

Write-Host "Moving Player animations..."
# Player animations
Move-WithMeta "Animation/Player.controller" "Player/Character/Animations/Player.controller"
Move-WithMeta "Animation/PlayerCombo1.anim" "Player/Character/Animations/PlayerCombo1.anim"
Move-WithMeta "Animation/PlayerFall.anim" "Player/Character/Animations/PlayerFall.anim"
Move-WithMeta "Animation/PlayerFallLoop.anim" "Player/Character/Animations/PlayerFallLoop.anim"
Move-WithMeta "Animation/PlayerHurt.anim" "Player/Character/Animations/PlayerHurt.anim"
Move-WithMeta "Animation/PlayerIdle.anim" "Player/Character/Animations/PlayerIdle.anim"
Move-WithMeta "Animation/PlayerJump.anim" "Player/Character/Animations/PlayerJump.anim"
Move-WithMeta "Animation/PlayerRun.anim" "Player/Character/Animations/PlayerRun.anim"

Write-Host "Moving Elisa animations..."
# Elisa character animations (keeping as separate character variant)
Move-WithMeta "Animation/Elisa.controller" "Player/Character/Animations/Elisa.controller"
Move-WithMeta "Animation/ElisaAttack.anim" "Player/Character/Animations/ElisaAttack.anim"
Move-WithMeta "Animation/ElisaIdle.anim" "Player/Character/Animations/ElisaIdle.anim"
Move-WithMeta "Animation/ElisaJump.anim" "Player/Character/Animations/ElisaJump.anim"
Move-WithMeta "Animation/ElisaRun.anim" "Player/Character/Animations/ElisaRun.anim"

Write-Host "Moving Enemy animations..."
# Enemy animations folder
Move-DirWithMeta "Animation/Enemy" "Enemy/EnemyTypes/Orc/Animations/Enemy"

Write-Host "All assets moved successfully!"
