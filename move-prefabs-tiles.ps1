# Move Prefabs and Tile assets

Write-Host "Moving Prefab assets..."
# Move Enemy prefab to Orc prefabs
Move-Item "Prefabs/Enemy.prefab" "Enemy/EnemyTypes/Orc/Prefabs/Enemy.prefab" -Force
Move-Item "Prefabs/Enemy.prefab.meta" "Enemy/EnemyTypes/Orc/Prefabs/Enemy.prefab.meta" -Force
Write-Host "Moved Enemy.prefab"

Write-Host "Moving Tile assets..."
# Move all tile assets to World/Tilemaps/Tiles
# InfernoTiles assets
Get-ChildItem "Tile/InfernoTiles_*.asset" | ForEach-Object {
    Move-Item $_.FullName ("World/Tilemaps/Tiles/" + $_.Name) -Force
    Move-Item ($_.FullName + ".meta") ("World/Tilemaps/Tiles/" + $_.Name + ".meta") -Force
}

# world_tileset assets
Get-ChildItem "Tile/world_tileset_*.asset" | Where-Object { $_.Name -notmatch "v2" } | ForEach-Object {
    Move-Item $_.FullName ("World/Tilemaps/Tiles/" + $_.Name) -Force
    Move-Item ($_.FullName + ".meta") ("World/Tilemaps/Tiles/" + $_.Name + ".meta") -Force
}

# world_tileset_v2 assets
Get-ChildItem "Tile/world_tileset_v2_*.asset" | ForEach-Object {
    Move-Item $_.FullName ("World/Tilemaps/Tiles/" + $_.Name) -Force
    Move-Item ($_.FullName + ".meta") ("World/Tilemaps/Tiles/" + $_.Name + ".meta") -Force
}

# Tile palette
Move-Item "Tile/New Tile Palette.prefab" "World/Tilemaps/Tiles/New Tile Palette.prefab" -Force
Move-Item "Tile/New Tile Palette.prefab.meta" "World/Tilemaps/Tiles/New Tile Palette.prefab.meta" -Force
Write-Host "Moved Tile Palette"

Write-Host "All Prefab and Tile assets moved successfully!"
Write-Host "Tile count moved to World/Tilemaps/Tiles/"
