# Android2D — Kế hoạch triển khai tiếp theo cho Codex

## Trạng thái dự án hiện tại

- Phase 11 — Save/Load Snapshot: **PASS**
- Phase 12 — Scene/Level Progression: **PASS**
- Phase 13 — Save UI + Multiple Save Slots: **PASS**
- Save snapshot format: **version 4**

Bước tiếp theo dự kiến: **Phase 14 — Skill System Foundation**.

---

## 0. Phạm vi

Dự án hiện tại đã hoàn thành bước tái cấu trúc thư mục theo module.

Không tái cấu trúc lại toàn bộ project.

Không thay project bằng repo khác.

Không copy nguyên framework/repo bên ngoài vào project.

Mục tiêu tiếp theo là xây nền gameplay có thể mở rộng cho game 2D Dark Fantasy trên Unity 6.

Thứ tự triển khai bắt buộc:

```text
Core Combat
    ↓
Health / Damage / Death
    ↓
EXP / Level
    ↓
EnemyData + Enemy Framework
    ↓
Player Stats
    ↓
ItemData + WeaponData
    ↓
Inventory
    ↓
Equipment
    ↓
Skill / Ability
    ↓
Loot / Chest
    ↓
Save / Load
    ↓
UI
    ↓
Levels
    ↓
Character Select
```

Không triển khai Inventory, Skill Tree, Equipment hoặc hệ thống lớn phía sau trước khi các bước nền phía trước hoạt động ổn định.

---

## 1. CORE-01 — Tạo interface nhận damage

Tạo:

```text
Assets/Core/Combat/IDamageable.cs
```

Mục tiêu:

- Player và Enemy đều có thể nhận damage qua cùng một interface.
- Weapon, spell, projectile, trap và enemy attack không phụ thuộc trực tiếp vào class cụ thể của Player/Enemy.

Yêu cầu:

```csharp
public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo);
}
```

Có thể điều chỉnh chữ ký nếu kiến trúc hiện tại yêu cầu, nhưng phải giữ nguyên mục tiêu: nguồn damage chỉ cần biết đối tượng có `IDamageable`.

---

## 2. CORE-02 — Tạo DamageType

Tạo:

```text
Assets/Core/Combat/DamageType.cs
```

Ban đầu hỗ trợ tối thiểu:

```text
Physical
Fire
Ice
Lightning
Dark
Magic
True
```

Có thể dùng `enum`.

Không xây resistance system phức tạp ở bước này.

---

## 3. CORE-03 — Tạo DamageInfo

Tạo:

```text
Assets/Core/Combat/DamageInfo.cs
```

Dùng để truyền dữ liệu damage thay vì truyền một `int damage`.

Tối thiểu nên chứa:

```text
Amount
DamageType
Source
```

Có thể thêm các field nếu code hiện tại thực sự cần, ví dụ:

```text
IsCritical
Knockback
HitPoint
```

nhưng không thêm hệ thống chưa được sử dụng.

Mục tiêu:

```text
Sword
Axe
Bow
Fireball
Enemy Attack
Trap
```

đều có thể truyền damage qua cùng một cấu trúc.

---

## 4. CORE-04 — Refactor Health.cs

File hiện tại:

```text
Assets/Core/Stats/Health.cs
```

Yêu cầu:

- Một `Health.cs` dùng chung cho Player và Enemy.
- Implement `IDamageable`.
- Quản lý:
  - Max Health
  - Current Health
  - TakeDamage
  - Heal
  - Death state
- Không chứa logic riêng của Orc/Player/Boss.
- Không tự cấp EXP.
- Không trực tiếp xử lý loot.
- Không trực tiếp điều khiển UI cụ thể.

API tối thiểu mong muốn:

```text
CurrentHealth
MaxHealth
IsDead
TakeDamage(DamageInfo)
Heal(...)
```

Nên có event/callback cho:

```text
OnHealthChanged
OnDamaged
OnDeath
```

để UI, animation, loot và EXP có thể đăng ký mà không phụ thuộc cứng vào `Health.cs`.

Không tạo:

```text
OrcHealth.cs
SkeletonHealth.cs
DemonHealth.cs
```

chỉ vì các enemy có HP khác nhau.

---

## 5. CORE-05 — Chuẩn hóa Death flow

Mục tiêu:

```text
Attack
  ↓
DamageInfo
  ↓
IDamageable
  ↓
Health
  ↓
CurrentHealth <= 0
  ↓
OnDeath
```

Player và Enemy có thể phản ứng với `OnDeath` khác nhau.

Enemy death có thể:

```text
Play death animation
Disable combat
Disable AI
Disable collider nếu cần
Reward EXP
Drop loot
Destroy/disable object sau animation
```

Player death có thể:

```text
Play death animation
Disable input
Show Game Over sau này
```

Không đặt tất cả logic trên trực tiếp vào `Health.cs`.

---

## 6. CORE-06 — Refactor PlayerAttack và EnemyAttack

Các file hiện tại:

```text
Assets/Player/Combat/Scripts/PlayerAttack.cs
Assets/Enemy/Combat/Scripts/EnemyAttack.cs
```

Yêu cầu:

- Không gọi trực tiếp một class Enemy cụ thể để trừ HP.
- Tìm `IDamageable`.
- Tạo `DamageInfo`.
- Gọi:

```text
TakeDamage(DamageInfo)
```

PlayerAttack và EnemyAttack phải dùng cùng pipeline damage.

Ví dụ kiến trúc:

```text
PlayerAttack
    ↓
DamageInfo
    ↓
Enemy Health

EnemyAttack
    ↓
DamageInfo
    ↓
Player Health
```

---

## 7. PROGRESSION-01 — Hoàn thiện EXP

File hiện tại:

```text
Assets/Player/Stats/Scripts/PlayerExperience.cs
Assets/Enemy/Loot/Scripts/ExperienceReward.cs
```

Mục tiêu:

```text
Enemy chết
    ↓
ExperienceReward
    ↓
PlayerExperience
    ↓
Current EXP tăng
```

Yêu cầu:

- Enemy chỉ cấp EXP một lần.
- Không cấp EXP khi chỉ bị damage.
- Không hard-code trực tiếp logic từng loại enemy trong `PlayerExperience`.
- EXP reward sau này phải có thể lấy từ `EnemyData`.

API dự kiến:

```text
AddExperience(int amount)
CurrentExperience
CurrentLevel
ExperienceToNextLevel
```

---

## 8. PROGRESSION-02 — Level system

Mở rộng `PlayerExperience` hoặc tách riêng `PlayerLevel` nếu kiến trúc sạch hơn.

Yêu cầu:

- Có `CurrentLevel`.
- Có EXP hiện tại.
- Có EXP cần để lên level tiếp theo.
- Có event:

```text
OnExperienceChanged
OnLevelUp
```

Không hard-code UI trong progression system.

Không cố định công thức EXP phức tạp nếu chưa cần.

Có thể dùng công thức đơn giản hoặc serialized progression data, nhưng code phải dễ thay đổi sau này.

---

## 9. ENEMY-01 — Tạo EnemyData bằng ScriptableObject

Tạo:

```text
Assets/Enemy/Stats/EnemyData.cs
```

hoặc vị trí tương đương phù hợp cấu trúc hiện tại.

Asset dữ liệu lưu trong:

```text
Assets/Data/Enemies/
```

`EnemyData` tối thiểu chứa:

```text
ID
DisplayName
MaxHealth
Damage
MoveSpeed
DetectionRange
AttackRange
ExperienceReward
```

Có thể thêm:

```text
AttackCooldown
```

nếu AI hiện tại đang dùng.

Không đưa logic vào `EnemyData`.

---

## 10. ENEMY-02 — Tạo OrcData.asset

Tạo một asset thử nghiệm:

```text
Assets/Data/Enemies/OrcData.asset
```

Chuyển các giá trị phù hợp của Orc hiện tại sang `OrcData`.

Prefab Orc phải tham chiếu đến `OrcData`.

Mục tiêu kiểm chứng:

- Thay MaxHealth trong `OrcData` → Orc dùng giá trị mới.
- Thay Damage → Orc attack dùng giá trị mới.
- Thay MoveSpeed → AI dùng giá trị mới.
- Thay ExperienceReward → EXP nhận được thay đổi.

Không tạo enemy mới ở bước này.

Chỉ dùng Orc hiện tại để xác minh framework.

---

## 11. ENEMY-03 — Refactor Enemy.cs

File:

```text
Assets/Enemy/Base/Scripts/Enemy.cs
```

Yêu cầu:

- Nhận `EnemyData`.
- Khởi tạo các component cần thiết từ data.
- Không chứa mọi logic AI, combat, health, loot trong một class.
- Là cầu nối dữ liệu chung của enemy nếu cần.

Mục tiêu:

```text
EnemyData
   ↓
Enemy prefab
   ├── Enemy
   ├── Health
   ├── EnemyAI
   ├── EnemyAttack
   ├── ExperienceReward
   └── Animator
```

---

## 12. ENEMY-04 — Refactor EnemyAI

File:

```text
Assets/Enemy/AI/Scripts/EnemyAI.cs
```

Mục tiêu ngắn hạn:

```text
Idle
Patrol
Chase
Attack
Dead
```

Nếu code hiện tại đang hoạt động, không rewrite toàn bộ một lần.

Refactor từng phần để:

- MoveSpeed lấy từ `EnemyData`.
- DetectionRange lấy từ `EnemyData`.
- AttackRange lấy từ `EnemyData`.
- AI dừng khi Health chết.
- Không quản lý EXP.
- Không quản lý inventory/loot trực tiếp.

Không thêm advanced AI như dodge, flee, block, phase hoặc boss AI ở bước này.

---

## 13. PLAYER-01 — Refactor PlayerStats

File:

```text
Assets/Player/Stats/Scripts/PlayerStats.cs
```

Mục tiêu:

Tạo nơi quản lý stats cơ bản của Player.

Tối thiểu:

```text
Attack
Defense
MoveSpeed
```

Health vẫn do `Health.cs` quản lý.

Có thể chuẩn bị:

```text
MagicPower
CriticalChance
CriticalDamage
```

nhưng không cần triển khai logic hoàn chỉnh nếu chưa sử dụng.

Quan trọng:

- Equipment và Skill Tree sau này phải modifier `PlayerStats`.
- `PlayerController` không nên chứa toàn bộ stats.

---

## 14. ITEM-01 — Tạo ItemData base

Chỉ thực hiện sau khi các phần trên chạy ổn.

Tạo:

```text
Assets/Items/ItemData.cs
```

hoặc vị trí tương đương theo cấu trúc hiện tại.

ScriptableObject base chứa:

```text
ID
DisplayName
Description
Icon
ItemType
Rarity
MaxStack
```

Không xây Inventory UI ở bước này.

---

## 15. ITEM-02 — Tạo WeaponData

Tạo:

```text
Assets/Items/Weapons/WeaponData.cs
```

Kế thừa hoặc tham chiếu `ItemData`.

Tối thiểu:

```text
Damage
AttackSpeed
DamageType
```

Có thể chuẩn bị:

```text
ManaCost
Projectile
VFX
Animation
```

nhưng chỉ triển khai nếu thực sự dùng.

---

## 16. Sau Item mới triển khai Inventory

Inventory cần hỗ trợ tối thiểu:

```text
AddItem
RemoveItem
Stack
Capacity
UseItem
```

Tách logic khỏi UI.

Kiến trúc:

```text
ItemData
    ↓
Inventory System
    ↓
Inventory UI
```

Không để UI là nơi lưu inventory thật.

---

## 17. Equipment

Chỉ xây sau Inventory.

Slot dự kiến:

```text
Weapon
Offhand
Helmet
Armor
Gloves
Boots
Accessory
```

Equipment phải tác động lên `PlayerStats`.

Không hard-code từng weapon vào PlayerController.

---

## 18. Skill / Ability

Chỉ xây sau PlayerStats + Equipment.

Thiết kế bằng data-driven architecture.

Tối thiểu cần hỗ trợ:

```text
Skill ID
Name
Description
Prerequisite
Skill Point Cost
Passive / Active
```

Skill Tree UI xây sau logic.

---

## 19. Loot / Chest

Chỉ xây sau Item + Inventory.

Tạo `LootTable`.

Dùng chung cho:

```text
Enemy Drop
Chest
Boss Reward
```

Chest cần ID ổn định để Save System có thể ghi nhớ rương đã mở.

---

## 20. Save / Load

Chỉ bắt đầu khi đã có dữ liệu thực sự cần lưu.

Dự kiến lưu:

```text
Current Level
Player Level
EXP
Player Stats cần thiết
Inventory
Equipment
Unlocked Skills
Opened Chests
Checkpoint
```

Không serialize trực tiếp toàn bộ MonoBehaviour/GameObject graph.

Tạo save DTO/data riêng.

---

## 21. UI

Sau khi systems đã hoạt động.

Thứ tự UI:

```text
HUD
    ↓
Game Over / Victory
    ↓
Inventory UI
    ↓
Equipment UI
    ↓
Skill Tree UI
    ↓
Main Menu
    ↓
Character Select
```

HUD tối thiểu:

```text
HP
EXP
Level
```

UI nhận dữ liệu qua events hoặc public read-only state.

Không để gameplay logic nằm trong UI script.

---

## 22. Levels

Sau khi core systems ổn định:

```text
Level01
Level02
Level03
Dungeon
Boss
```

Cần chuẩn hóa:

```text
Spawn Point
Checkpoint
Enemy Spawn
Door
Chest
Level Exit
Boss Trigger
```

---

## 23. Character Select

Không copy Player prefab + code thành nhiều hệ thống riêng.

Tạo `CharacterData`.

Mỗi character có thể khác:

```text
Sprite / animation
Base stats
Starting weapon
Abilities
Skill tree
```

nhưng vẫn dùng chung framework:

```text
PlayerController
Health
PlayerStats
Inventory
Equipment
Skills
```

---

## 24. Animation architecture

Không mở rộng Animator bằng quá nhiều transition phụ thuộc lẫn nhau.

Mục tiêu dài hạn:

```text
Gameplay State
    ↓
Animation Controller
```

Gameplay code quyết định state:

```text
Idle
Run
Jump
Fall
Attack
Hurt
Dead
```

Animator chịu trách nhiệm phát animation tương ứng.

Không đặt logic gameplay quan trọng trong animation transition conditions nếu có thể quản lý rõ ràng từ code.

---

## 25. VFX architecture

Không xây VFX lớn ở giai đoạn Core.

Sau này tổ chức:

```text
Assets/VFX/
├── Combat/
├── Magic/
├── Hit/
├── Death/
└── Environment/
```

VFX phải có thể được gán qua data/prefab thay vì hard-code đường dẫn.

---

## 26. Quy tắc dependency

Phải tránh:

```text
Core → Enemy
Core → Player
Core → UI
```

Ưu tiên:

```text
Player → Core
Enemy → Core
Items → Core
UI → Gameplay systems
```

`Core` không được phụ thuộc ngược vào hệ thống gameplay cụ thể nếu không cần thiết.

---

## 27. Quy tắc refactor cho Codex

Codex phải:

1. Đọc code hiện tại trước khi sửa.
2. Không rewrite class đang hoạt động nếu chỉ cần refactor nhỏ.
3. Giữ hành vi gameplay hiện tại.
4. Không đổi tên GameObject/Animator parameter tùy tiện.
5. Không đổi GUID asset.
6. Không xóa `.meta`.
7. Không di chuyển asset thêm nếu không cần.
8. Không thay package Unity.
9. Không thay Input System.
10. Không thay Cinemachine setup.
11. Không sửa Tilemap nếu task không liên quan.
12. Không thêm dependency/package bên ngoài nếu chưa được yêu cầu.
13. Không thêm framework lớn.
14. Không tạo code trùng chức năng.
15. Ưu tiên component reuse + ScriptableObject data.

---

## 28. Quy tắc tương thích Unity

Project đang dùng Unity 6.

Code mới phải:

- Compile trên Unity 6.
- Dùng API Unity hiện tại.
- Không sử dụng API obsolete nếu có lựa chọn tương đương rõ ràng.
- Không thêm package không cần thiết.
- Giữ Android compatibility.

---

## 29. Quy trình thực hiện cho Codex

### Milestone A — Core Combat

Triển khai:

```text
IDamageable
DamageType
DamageInfo
Health refactor
PlayerAttack refactor
EnemyAttack refactor
Death event
```

Sau đó:

- Compile project.
- Sửa tất cả compile errors phát sinh do thay đổi API.
- Không sang milestone tiếp theo nếu milestone này chưa ổn.

### Milestone B — Progression

Triển khai:

```text
PlayerExperience
Level
ExperienceReward
OnExperienceChanged
OnLevelUp
```

Kiểm tra:

```text
Enemy chết
→ Player nhận EXP đúng một lần
→ đủ EXP thì Level Up
```

### Milestone C — EnemyData

Triển khai:

```text
EnemyData ScriptableObject
OrcData.asset
Enemy.cs integration
EnemyAI integration
EnemyAttack integration
Health integration
ExperienceReward integration
```

Kiểm tra Orc hiện tại hoạt động từ data.

### Milestone D — PlayerStats

Chuẩn hóa stats để chuẩn bị cho:

```text
Equipment
Skills
Weapons
Magic
```

Không xây các hệ thống đó trong milestone này.

### Milestone E — Item Foundation

Triển khai:

```text
ItemData
WeaponData
ItemType
Rarity
```

Tạo một weapon test nếu cần để kiểm chứng.

---

## 30. Acceptance Criteria trước khi dừng vòng triển khai hiện tại

Codex chỉ coi giai đoạn nền hoàn thành khi:

- Project compile không có error.
- Player vẫn di chuyển.
- Player vẫn jump.
- Player animation vẫn chạy.
- Player attack vẫn gây damage.
- Enemy vẫn detect/chase/attack.
- Enemy vẫn nhận damage.
- Enemy chết đúng một lần.
- Player nhận EXP đúng một lần.
- Level Up hoạt động.
- Orc lấy stats từ `OrcData`.
- `Health.cs` dùng chung cho Player và Enemy.
- Không có `Missing Script`.
- Không có prefab reference bị hỏng.
- Không có scene reference bị hỏng.
- Không thay đổi asset unrelated.
- Không thêm package ngoài.
- Không tạo Inventory/Skill Tree/UI lớn trước khi hoàn thành nền.

---

## 31. Kết quả mong muốn sau vòng thực hiện tiếp theo

Sau khi hoàn thành các milestone A-D, kiến trúc phải đạt:

```text
PlayerAttack
     ↓
DamageInfo
     ↓
IDamageable
     ↓
Health
     ↓
Death Event
```

và:

```text
EnemyData
   ↓
Enemy
├── Health
├── AI
├── Attack
└── EXP Reward
```

và:

```text
Enemy Death
    ↓
EXP Reward
    ↓
PlayerExperience
    ↓
Level Up
```

Đây là nền bắt buộc trước khi triển khai:

```text
Item
Inventory
Equipment
Skill Tree
Magic
Chest
Save
UI
Multiple Characters
Multiple Enemies
Multiple Levels
```
