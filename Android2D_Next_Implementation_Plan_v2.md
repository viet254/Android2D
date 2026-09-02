# Android2D — Next Implementation Plan

## Mục tiêu tài liệu

Tài liệu này là kế hoạch triển khai tiếp theo cho dự án `Android2D`, bắt đầu từ trạng thái hiện tại:

- Milestone A — Core Combat: đã có nền.
- Milestone B — Progression: đã có nền.
- Milestone C — EnemyData: đã có nền.
- Milestone D — PlayerStats: đã có nền.
- Milestone E — Item Foundation: đã hoàn thành.
- Milestone F — Inventory: đã có `InventorySlot`, `Inventory`, `ItemPickup` và pickup `TrainingSword` trong `SampleScene`.
- Chưa triển khai hoàn chỉnh: Equipment, Inventory UI, Consumables/UseItem, Loot Table, Save/Load, Skill, Levels, Character Select.

Mục tiêu tiếp theo không phải mở rộng hàng loạt hệ thống mới, mà phải:

1. Khóa nghiệm thu tích hợp A–F.
2. Sửa các xung đột kiến trúc còn tồn tại.
3. Hoàn thiện Inventory Foundation.
4. Xây dựng Equipment theo vertical slice tối thiểu.
5. Nối Weapon → PlayerStats → Combat.
6. Chỉ sau khi logic Equipment ổn định mới làm UI.

---

# 0. Quy tắc bắt buộc khi Codex thực hiện

## 0.1. Không phá cấu trúc đã có nếu không cần thiết

Trước khi sửa:

- Đọc toàn bộ script liên quan.
- Tìm reference/call-site trước khi đổi API public.
- Không đổi tên class, field serialize, asset hoặc namespace nếu không có lý do rõ ràng.
- Không di chuyển file/asset chỉ để “đẹp cấu trúc”.
- Không xóa code đang dùng nếu chưa xác nhận không còn reference.

## 0.2. Không tạo hệ thống song song

Không tạo class mới nếu chức năng tương đương đã tồn tại.

Ví dụ không được tạo thêm:

- `PlayerHealth.cs` nếu đã có `Health.cs`.
- `PlayerInventory.cs` nếu `Inventory.cs` hiện đã là inventory của Player.
- `WeaponInventory.cs`.
- `EquipmentManager.cs` và `Equipment.cs` cùng giữ trạng thái trang bị.
- Một hệ thống damage thứ hai ngoài `IDamageable` / `DamageInfo` / `Health`.

## 0.3. Single source of truth

Mỗi loại trạng thái runtime chỉ có một nguồn chính.

Bắt buộc:

```text
Current HP  -> Health
Inventory   -> Inventory
Equipment   -> Equipment
EXP/Level   -> hệ Progression hiện tại
Static/Base Player Stats -> PlayerStats
Enemy configuration -> EnemyData
Item configuration -> ItemData / subclass
```

Không được để hai component cùng sở hữu độc lập cùng một trạng thái.

## 0.4. ScriptableObject chỉ chứa dữ liệu cấu hình

Không dùng `ScriptableObject` để lưu trạng thái runtime thay đổi theo từng instance.

Ví dụ:

```text
WeaponData.damage = dữ liệu cấu hình
Equipment.currentWeapon = trạng thái runtime
Health.currentHealth = trạng thái runtime
```

## 0.5. Không làm UI trước logic

Inventory UI và Equipment UI chỉ được triển khai sau khi:

- Add item hoạt động.
- Inventory full hoạt động đúng.
- Equip hoạt động.
- Unequip hoạt động.
- Damage thay đổi theo weapon hoạt động.

## 0.6. Không tự tạo prefab nếu chưa cần

Hiện tại `TrainingSwordPickup` đang nằm trực tiếp trong `SampleScene`.

Không bắt buộc tạo `TrainingSwordPickup.prefab` trong phase đầu.

Chỉ tạo prefab nếu nó thực sự cần cho loot spawning hoặc tái sử dụng.

## 0.7. Không để mất item

Mọi thao tác sau phải có tính an toàn:

- Pickup.
- Equip.
- Unequip.
- Swap.
- Remove item.

Nếu thao tác không thể hoàn thành thì dữ liệu phải giữ nguyên.

## 0.8. Sau mỗi phase

Codex phải:

1. Liệt kê file đã tạo.
2. Liệt kê file đã sửa.
3. Nêu lý do từng thay đổi.
4. Nêu các bước Play Mode cần test.
5. Không tuyên bố “pass” nếu chưa chạy được Unity Play Mode.
6. Nếu không thể chạy Unity, ghi rõ `NEEDS UNITY PLAY MODE VERIFICATION`.

---

# PHASE 1 — Audit và nghiệm thu nền A–F

## Mục tiêu

Không thêm gameplay feature mới.

Mục tiêu là xác nhận những hệ thống đã có đang nối đúng với nhau và loại bỏ các lỗi kiến trúc trước Equipment.

---

## 1.1. Audit Core Combat

Kiểm tra các thành phần hiện có liên quan:

```text
IDamageable
DamageType
DamageInfo
Health
Player Attack
Enemy Attack
Enemy death
```

Xác nhận flow:

```text
Attacker
   ↓
DamageInfo
   ↓
IDamageable.TakeDamage(...)
   ↓
Health
   ↓
HP <= 0
   ↓
Death
```

### Yêu cầu

- Không bypass `IDamageable` bằng cách sửa HP trực tiếp từ attack script.
- Không có nhiều đường damage khác nhau cho Player và Enemy nếu không cần.
- `Health` phải chống gọi death nhiều lần.
- Sau khi chết, entity không được phát death event nhiều lần.
- Enemy đã chết không tiếp tục chase/attack.

### Nếu chưa có death guard

Bổ sung trạng thái tương đương:

```csharp
private bool isDead;
```

và đảm bảo logic chết chỉ chạy một lần.

Không bắt buộc dùng đúng tên biến trên nếu code hiện có đã giải quyết tương đương.

---

## 1.2. Audit Progression

Kiểm tra:

```text
ExperienceReward
EXP manager/component hiện tại
Level
EXP events
EXP HUD
```

Xác nhận flow:

```text
Enemy Death
    ↓
ExperienceReward
    ↓
Player EXP
    ↓
Level processing
    ↓
EXP event
    ↓
HUD refresh
```

### Yêu cầu

- Một enemy chỉ reward EXP một lần.
- EXP reward phải phụ thuộc đúng dữ liệu hiện tại.
- Nếu `EnemyData` có EXP reward thì không giữ một giá trị hard-code khác ở Enemy.
- Level-up không reset sai EXP nếu thiết kế hiện tại đang có overflow/carry-over.
- UI không tự sửa giá trị EXP gameplay.

---

## 1.3. Audit EnemyData

Tìm toàn bộ field cấu hình Orc đang tồn tại trong:

- `EnemyData`
- Orc-specific scripts
- AI
- attack
- reward

Đối chiếu các giá trị có khả năng gồm:

```text
Max Health
Move Speed
Chase Range
Attack Range
Attack Damage
Attack Cooldown
EXP Reward
```

### Yêu cầu

Nếu một giá trị đã thuộc `EnemyData`, runtime Orc phải đọc từ `EnemyData`.

Không được tồn tại hai nguồn độc lập kiểu:

```text
OrcData.moveSpeed = 3
EnemyAI.moveSpeed = 4
```

nếu cả hai cùng đại diện cho cùng một cấu hình.

### Kết quả cần đạt

```text
OrcData.asset
   ↓
Enemy runtime components
```

`EnemyData` là nguồn configuration.

---

# PHASE 2 — Chuẩn hóa Player Health / PlayerStats

## Mục tiêu

Giải quyết dứt điểm nguy cơ `PlayerStats` và `Health` cùng giữ HP runtime.

---

## 2.1. Audit PlayerStats

Đọc toàn bộ `PlayerStats`.

Phân loại field thành:

### Base/configuration stats

Ví dụ:

```text
MaxHealth
BaseAttack
Defense
MoveSpeed
CriticalChance
```

nếu thực sự tồn tại.

### Runtime state

Ví dụ:

```text
CurrentHealth
CurrentMana
```

---

## 2.2. Quy tắc HP

Sau phase này:

```text
PlayerStats
   ↓ cung cấp MaxHealth hoặc stat-derived max HP
Health
   ↓ sở hữu CurrentHealth
```

Bắt buộc:

```text
CurrentHealth = chỉ do Health quản lý
```

Không để:

```text
PlayerStats.currentHealth
Health.currentHealth
```

cùng hoạt động độc lập.

---

## 2.3. Nếu PlayerStats hiện đang giữ CurrentHealth

Refactor an toàn:

1. Tìm tất cả nơi đọc/ghi `PlayerStats.CurrentHealth`.
2. Chuyển nơi cần runtime HP sang `Health`.
3. Nếu UI cần HP:
   - đọc/event từ `Health`.
4. Nếu damage cần HP:
   - dùng `Health`.
5. Nếu PlayerStats cần MaxHealth:
   - giữ MaxHealth.
6. Không xóa field serialized ngay nếu có nguy cơ phá Scene/Prefab trước khi audit reference.

Nếu cần migration tạm thời, phải ghi rõ trong report.

---

## 2.4. Initialization

Đảm bảo Health của Player được init từ MaxHealth đúng một lần.

Ví dụ logic mong muốn:

```text
PlayerStats.MaxHealth
      ↓
Health Initialize/SetMaxHealth
      ↓
CurrentHealth = MaxHealth
```

Không reset `CurrentHealth` ngoài ý muốn mỗi frame hoặc mỗi lần stat refresh.

---

## Acceptance Criteria Phase 2

- Chỉ `Health` sở hữu HP hiện tại.
- Player nhận damage bình thường.
- Player chết đúng một lần.
- MaxHealth vẫn lấy đúng nguồn.
- Không có NullReference mới.
- Không còn code gameplay cập nhật song song hai current HP.

---

# PHASE 3 — Hoàn thiện Inventory Foundation

## Mục tiêu

Khóa Milestone F trước Equipment.

---

## 3.1. Audit các class hiện tại

Đọc:

```text
InventorySlot
Inventory
ItemPickup
ItemData
WeaponData
ItemType
ItemRarity
```

Không redesign toàn bộ nếu implementation hiện tại đã đáp ứng yêu cầu.

---

## 3.2. Chuẩn hóa API AddItem

`Inventory.AddItem(...)` phải trả được kết quả thành công/thất bại.

Mục tiêu:

```text
true  = item đã thực sự vào Inventory
false = Inventory không nhận được item
```

Nếu API hiện tại đã trả bool hoặc kết quả tương đương thì giữ.

---

## 3.3. Pickup transaction

Flow bắt buộc:

```text
Player enters pickup trigger
        ↓
ItemPickup requests Inventory.AddItem
        ↓
SUCCESS ?
   ├─ yes -> destroy/disable pickup
   └─ no  -> pickup remains
```

Không được:

```text
Destroy pickup
↓
thử AddItem
```

---

## 3.4. Inventory full

Xác nhận capacity.

Test case:

```text
Inventory đầy
    ↓
Player chạm TrainingSword
    ↓
AddItem = false
    ↓
TrainingSwordPickup vẫn tồn tại
```

Không được:

- destroy pickup;
- mất item;
- overwrite slot;
- tạo slot vượt capacity.

---

## 3.5. Stack behavior

Audit dữ liệu item hiện có để xác định có field kiểu:

```text
MaxStack
IsStackable
StackSize
```

Nếu chưa có hệ thống stack rõ ràng:

- Không tự mở rộng quá mức trong phase này.
- Weapon mặc định xử lý như non-stack nếu thiết kế hiện tại cho phép.
- Ghi lại giới hạn hiện tại.

Nếu stack đã tồn tại thì phải test:

```text
same item + available stack -> tăng quantity
same item + full stack -> tìm slot khác
no slot -> fail
```

---

## 3.6. Inventory events

Nếu Inventory hiện chưa có event thay đổi, có thể thêm event nhẹ để chuẩn bị cho UI.

Ví dụ về mặt kiến trúc:

```text
Inventory data changed
       ↓
OnInventoryChanged
```

Nhưng:

- Không tạo UI ở phase này.
- Không để event trở thành bắt buộc nếu hệ thống hiện tại chưa cần.
- Không tạo event framework phức tạp.

---

## Acceptance Criteria Phase 3

- Nhặt TrainingSword khi túi còn chỗ.
- TrainingSword vào đúng Inventory.
- Pickup biến mất chỉ sau khi add thành công.
- Khi Inventory đầy, pickup vẫn ở Scene.
- Không duplicate item do trigger gọi nhiều lần.
- Không phát sinh lỗi Console từ Inventory/Pickup.

---

# PHASE 4 — Equipment Foundation

## Mục tiêu

Tạo hệ Equipment độc lập với Inventory.

Vertical slice mục tiêu:

```text
TrainingSword
   ↓
Inventory
   ↓ Equip
Equipment
   ↓
PlayerStats
   ↓
Player Attack Damage
```

---

## 4.1. Thư mục

Ưu tiên cấu trúc:

```text
Assets/Equipment/
└── Scripts/
    ├── Equipment.cs
    ├── EquipmentSlot.cs
    └── EquipmentSlotType.cs
```

Nếu repo hiện có convention khác rõ ràng thì tuân theo convention hiện tại.

Không tạo duplicate nếu đã có file tương đương.

---

## 4.2. EquipmentSlotType

Tạo enum nếu chưa có.

Tối thiểu phải hỗ trợ:

```csharp
Weapon
```

Có thể khai báo trước các slot dự kiến nếu phù hợp:

```text
Weapon
Helmet
Chest
Gloves
Boots
Accessory
```

Nhưng chỉ implement gameplay bắt buộc cho `Weapon` ở milestone này.

Không xây Armor system đầy đủ trong phase này.

---

## 4.3. EquipmentSlot

Trách nhiệm:

```text
Slot Type
Equipped Item
```

Không chứa logic Player combat.

Không chứa Inventory capacity.

Không tự instantiate item.

---

## 4.4. Equipment

`Equipment` là source of truth cho item đang trang bị.

Tối thiểu cung cấp behavior:

```text
GetEquippedItem(slot)
Equip(...)
Unequip(...)
```

Tên API cụ thể có thể phù hợp với codebase hiện tại.

### Quy tắc

- `Inventory` giữ item sở hữu.
- `Equipment` giữ item đang trang bị.
- Một item không được tồn tại đồng thời như hai bản sao logic độc lập trong Inventory và Equipment.

---

# PHASE 5 — Equip / Unequip Transaction

## Mục tiêu

Thao tác trang bị không gây duplicate hoặc mất item.

---

## 5.1. Equip từ Inventory

Flow mục tiêu:

```text
Inventory contains TrainingSword
          ↓
Request Equip
          ↓
Validate item is equippable
          ↓
Validate slot = Weapon
          ↓
Remove/transfer item from Inventory
          ↓
Equipment.Weapon = TrainingSword
```

### Nếu Weapon slot đang trống

Equip bình thường.

### Nếu Weapon slot đã có item

Không implement swap phức tạp nếu chưa cần.

Ưu tiên một trong hai cách đơn giản:

```text
A. Unequip old weapon về Inventory trước, chỉ tiếp tục nếu thành công.
```

hoặc transaction atomic tương đương.

Điều bắt buộc:

- Không mất old weapon.
- Không duplicate new weapon.
- Nếu inventory không còn chỗ cho old weapon thì trạng thái ban đầu phải giữ nguyên.

---

## 5.2. Unequip

Flow:

```text
Equipment.Weapon
      ↓
Check Inventory can accept item
      ↓
AddItem success
      ↓
clear Equipment.Weapon
```

Không được clear equipment trước khi biết item đã về Inventory an toàn.

---

## 5.3. Transaction safety

Mọi equip/unequip phải đảm bảo:

```text
FAIL -> state unchanged
SUCCESS -> state changed exactly once
```

Đây là yêu cầu bắt buộc.

---

## Acceptance Criteria Phase 5

### Test A

```text
TrainingSword trong Inventory
Weapon slot trống
Equip
→ Inventory mất TrainingSword
→ Equipment Weapon = TrainingSword
```

### Test B

```text
TrainingSword đang equipped
Inventory còn chỗ
Unequip
→ Equipment Weapon trống
→ TrainingSword trở lại Inventory
```

### Test C

```text
TrainingSword đang equipped
Inventory đầy
Unequip
→ Unequip fail
→ TrainingSword vẫn equipped
→ không mất item
```

### Test D

Thao tác equip/unequip nhiều lần:

- không nhân đôi TrainingSword;
- tổng số TrainingSword logic không đổi.

---

# PHASE 6 — Weapon → PlayerStats → Combat Integration

## Mục tiêu

`TrainingSword` phải có ảnh hưởng thật đến damage Player.

---

## 6.1. Audit WeaponData

Đọc `WeaponData.cs`.

Xác định field damage hiện có.

Không tự thêm field mới nếu `WeaponData` đã có damage tương đương.

Ví dụ logic:

```text
WeaponData
└── Damage
```

---

## 6.2. Công thức damage

Tạo một nguồn duy nhất tính final player attack damage.

Mục tiêu:

```text
Final Attack Damage
    =
Player Base Attack
    +
Equipped Weapon Damage
```

Chỉ dùng công thức trên nếu phù hợp với model stat hiện tại.

Nếu project hiện có công thức khác thì giữ nguyên và chỉ đưa Equipment vào đúng pipeline.

Điều bắt buộc là:

```text
PlayerAttack
    ↓
không hard-code damage riêng
    ↓
lấy final damage từ PlayerStats/stat resolver
```

---

## 6.3. PlayerStats integration

Ưu tiên:

```text
PlayerStats
├── BaseAttack
└── FinalAttack / GetAttackDamage()
```

Trong đó final attack đọc weapon hiện tại từ Equipment hoặc được cập nhật qua stat recalculation.

Không để:

```text
PlayerAttack.damage
PlayerStats.attack
WeaponData.damage
```

trở thành ba nguồn final damage khác nhau.

---

## 6.4. Không mutate WeaponData runtime

Không làm:

```csharp
weaponData.damage += playerBonus;
```

`WeaponData` là asset shared.

Tính damage runtime ngoài asset.

---

## 6.5. PlayerAttack

Sửa Player attack để dùng damage cuối cùng.

Flow:

```text
Player Attack input
       ↓
PlayerAttack
       ↓
PlayerStats final attack
       ↓
DamageInfo
       ↓
Enemy IDamageable
       ↓
Health
```

---

## Acceptance Criteria Phase 6

Thiết lập test có thể quan sát rõ:

```text
Không equip TrainingSword
→ Orc nhận base damage

Equip TrainingSword
→ Orc nhận damage đã tăng đúng theo WeaponData/stat formula

Unequip TrainingSword
→ damage quay lại giá trị không có weapon
```

Không hard-code riêng cho `TrainingSword`.

Bất kỳ `WeaponData` khác cùng hệ thống phải có thể hoạt động theo pipeline tương tự.

---

# PHASE 7 — Integration Test Vertical Slice A–G

## Mục tiêu

Kiểm thử toàn pipeline trước UI.

---

## 7.1. Test sequence bắt buộc

### Test 1 — Player core

- Move.
- Jump.
- Attack.

### Test 2 — Enemy

- Orc chase.
- Orc attack.
- Orc nhận damage.
- Orc chết đúng một lần.

### Test 3 — EXP

- Orc death reward EXP.
- EXP chỉ cộng một lần.
- HUD refresh.
- Level-up đúng theo logic hiện tại.

### Test 4 — EnemyData

Thay đổi một giá trị dễ nhận biết trong `OrcData.asset`, sau đó xác nhận runtime phản ánh giá trị đó.

Ví dụ chỉ chọn một field phù hợp để test:

```text
MoveSpeed
hoặc
AttackDamage
```

Không thay đổi asset vĩnh viễn nếu chỉ phục vụ test; trả lại giá trị ban đầu sau test.

### Test 5 — Inventory pickup

```text
Empty Inventory
→ pickup TrainingSword
→ Inventory contains TrainingSword
```

### Test 6 — Full inventory

```text
Full Inventory
→ touch pickup
→ pickup remains
```

### Test 7 — Equip

```text
Inventory TrainingSword
→ Equip
→ Equipment Weapon = TrainingSword
```

### Test 8 — Damage

```text
No weapon damage = A
Equipped weapon damage = B
B phải phản ánh bonus từ weapon theo công thức
```

### Test 9 — Unequip

```text
Unequip
→ item returns to Inventory
→ damage returns
```

### Test 10 — Full inventory unequip

```text
Inventory full
→ Unequip
→ fail safely
→ weapon remains equipped
```

---

## 7.2. Console

Sau Play Mode:

- Không compile error.
- Không runtime exception.
- Không missing reference.
- Không duplicate death reward.
- Không spam log vô hạn.

Nếu có warning không liên quan trực tiếp, liệt kê riêng; không tự xóa warning bằng cách che giấu lỗi.

---

# PHASE 8 — Inventory / Equipment UI

Chỉ bắt đầu phase này sau khi Phase 7 pass hoặc logic đã được xác nhận đầy đủ bằng kiểm thử tương đương.

## Mục tiêu

Hiển thị dữ liệu, không sở hữu dữ liệu.

---

## 8.1. Kiến trúc

```text
Inventory
    ↓
events/read API
    ↓
InventoryUI
```

```text
Equipment
    ↓
events/read API
    ↓
EquipmentUI
```

UI không được là source of truth.

---

## 8.2. Files đề xuất

Nếu chưa có convention khác:

```text
Assets/UI/Inventory/
├── InventoryUI.cs
├── InventorySlotUI.cs
├── EquipmentUI.cs
└── EquipmentSlotUI.cs
```

Có thể tách Scripts subfolder nếu project đang áp dụng convention đó.

---

## 8.3. Inventory UI tối thiểu

Hiển thị:

- slot rỗng;
- item icon;
- quantity nếu hệ thống có stack;
- TrainingSword icon hiện tại.

Không cần drag and drop.

---

## 8.4. Equipment UI tối thiểu

Hiển thị ít nhất:

```text
Weapon Slot
```

Nếu enum đã có các slot khác thì có thể render placeholder, nhưng không cần gameplay đầy đủ.

---

## 8.5. Interaction

Tối thiểu:

```text
Click/Select Inventory Weapon
→ Equip
```

và:

```text
Click/Select Equipped Weapon
→ Unequip
```

Không cần:

- drag & drop;
- compare tooltip;
- item sorting;
- filtering;
- complex tooltip;
- controller navigation nâng cao;

trong iteration đầu.

---

## Acceptance Criteria Phase 8

- UI phản ánh đúng Inventory.
- Nhặt sword → icon xuất hiện.
- Equip → icon rời Inventory và xuất hiện Equipment Weapon.
- Unequip → icon trở lại Inventory.
- Full Inventory → unequip fail và UI không sai trạng thái.
- UI refresh qua dữ liệu thật, không giả lập local state.
- Không lỗi Console.

---

# PHASE 9 — Consumable / UseItem Foundation

Chỉ thực hiện sau Equipment UI nếu Milestone tiếp theo vẫn theo roadmap hiện tại.

## Mục tiêu

Bổ sung khả năng dùng item, trước mắt bằng một consumable đơn giản.

---

## 9.1. Không đặt logic item-specific trong Inventory

Không xây:

```csharp
if (item == potion) ...
else if (item == food) ...
else if (item == scroll) ...
```

thành một switch lớn trong `Inventory`.

Cần abstraction phù hợp với architecture hiện tại.

---

## 9.2. Vertical slice đề xuất

Tạo một item test dạng healing consumable chỉ khi project chưa có item tương đương.

Flow:

```text
Consumable Pickup
       ↓
Inventory
       ↓
Use
       ↓
Health.Heal(...)
       ↓
Quantity decreases/remove slot
```

Không triển khai nhiều loại consumable cùng lúc.

---

# PHASE 10 — Loot Table

Chỉ làm sau khi item/inventory/equipment flow ổn định.

## Mục tiêu

Enemy death có thể spawn item qua data-driven loot.

---

## 10.1. Architecture

Đề xuất:

```text
LootTable
LootEntry
LootDropper
```

Flow:

```text
Enemy Death
    ↓
LootDropper
    ↓
LootTable
    ↓
roll
    ↓
ItemPickup spawn
```

Không nhét random drop hard-code vào `Enemy.cs`.

---

## 10.2. Pickup prefab

Đến phase này mới đánh giá việc tạo reusable `ItemPickup` prefab.

Nếu cần spawn runtime:

```text
Generic ItemPickup prefab
    ↓
assign ItemData
```

Ưu tiên generic pickup thay vì tạo một prefab riêng cho mọi item nếu codebase phù hợp.

---

# PHASE 11 — Save / Load Foundation

Không làm trước khi Inventory và Equipment data model ổn định.

## Mục tiêu

Lưu được ít nhất:

```text
Level
EXP
Inventory
Equipment
```

Có thể thêm vị trí/HP tùy roadmap nhưng không tự mở rộng nếu chưa cần.

---

## 11.1. Stable Item ID

Mỗi item cần ID ổn định.

Ví dụ:

```text
training_sword
```

Không dùng:

- display name làm identifier chính;
- instance ID Unity;
- object reference trực tiếp trong JSON save.

---

## 11.2. Save representation

Ví dụ về mặt dữ liệu:

```text
Inventory Entry
├── itemId
└── quantity
```

```text
Equipment Entry
├── slot
└── itemId
```

Không serialize trực tiếp `ScriptableObject` reference như dữ liệu save portable.

---

# PHASE 12 — Skill Foundation

Chỉ bắt đầu sau khi core gameplay state đủ ổn định.

Tách rõ:

```text
Stats
Abilities
```

Ví dụ stat:

```text
+ Max HP
+ Attack
```

Ví dụ ability:

```text
Dash
Double Jump
Heavy Attack
Magic
```

Không đưa tất cả skill behavior vào `PlayerStats`.

---

# Thứ tự thực hiện bắt buộc

Codex thực hiện theo đúng thứ tự sau:

```text
1. Audit A–F
2. Fix Player Health / PlayerStats ownership
3. Finalize Inventory pickup/full behavior
4. Build Equipment foundation
5. Implement safe Equip/Unequip
6. Connect WeaponData → PlayerStats → PlayerAttack
7. Run/report integration tests
8. Inventory + Equipment UI
9. Consumable / UseItem
10. Loot Table
11. Save / Load
12. Skill Foundation
```

Không nhảy trực tiếp sang Skill, Loot, Save hoặc Character Select trước khi Equipment vertical slice ổn định.

---

# Điểm dừng của lần triển khai tiếp theo

Nếu đây là một lần Codex execution lớn, ưu tiên **dừng sau Phase 7**.

Không cần triển khai Phase 8–12 trong cùng một lần nếu chưa xác minh logic.

Deliverable chính của iteration tiếp theo phải là:

```text
TrainingSword Pickup
        ↓
Inventory
        ↓
Equip
        ↓
Equipment Weapon
        ↓
PlayerStats final attack
        ↓
PlayerAttack
        ↓
DamageInfo
        ↓
Orc Health
        ↓
Death
        ↓
EXP Reward exactly once
```

và:

```text
Unequip
   ↓
Inventory
```

với transaction an toàn khi Inventory đầy.

---

# Definition of Done cho iteration tiếp theo

Iteration chỉ được coi là hoàn thành về mặt code khi thỏa các điều sau:

- A–F đã được audit lại.
- Không còn hai nguồn Current HP độc lập của Player.
- Inventory full không làm mất pickup.
- TrainingSword có thể được equip.
- TrainingSword có thể được unequip.
- Equip/unequip không duplicate item.
- Unequip khi Inventory full không mất item.
- Equipment là source of truth cho item đang trang bị.
- WeaponData ảnh hưởng Player damage qua stat/combat pipeline chung.
- PlayerAttack không hard-code TrainingSword.
- Orc vẫn nhận damage qua core damage system.
- Enemy death không chạy nhiều lần.
- EXP reward không chạy nhiều lần.
- Code compile được theo static inspection.
- Nếu Codex không trực tiếp chạy Unity Play Mode, report phải ghi `NEEDS UNITY PLAY MODE VERIFICATION`.

---

# Báo cáo bắt buộc Codex phải xuất sau khi sửa

Cuối execution, xuất report theo format:

## 1. Files Created

```text
path/to/file
- mục đích
```

## 2. Files Modified

```text
path/to/file
- thay đổi gì
- lý do
```

## 3. Architecture Decisions

Ghi rõ:

- Current HP source of truth.
- Inventory source of truth.
- Equipment source of truth.
- Final attack damage được tính ở đâu.
- Enemy EXP reward lấy từ đâu.

## 4. Behavior Implemented

Ghi các flow đã hoàn thiện.

## 5. Unity Editor Setup Required

Nếu cần gắn component/reference trong Inspector, ghi từng bước cụ thể.

Ví dụ:

```text
SampleScene
→ Player
→ Add/verify Equipment component
→ assign required PlayerStats reference
```

Không được ghi chung chung “setup references”.

## 6. Tests Performed

Phân biệt:

```text
STATIC VERIFIED
```

và:

```text
UNITY PLAY MODE VERIFIED
```

Nếu không chạy Unity:

```text
NEEDS UNITY PLAY MODE VERIFICATION
```

## 7. Remaining Issues

Chỉ liệt kê issue thực sự còn tồn tại.

## 8. Recommended Next Step

Nếu Phase 1–7 hoàn tất:

```text
Next: Phase 8 — Inventory / Equipment UI
```

---

# Không thực hiện trong iteration này

Trừ khi bắt buộc để sửa dependency trực tiếp, không làm:

- Skill Tree.
- Character Select.
- Multi-level campaign.
- Save slot UI.
- Quest system.
- Shop.
- Crafting.
- Armor stat system đầy đủ.
- Random procedural loot phức tạp.
- Drag-and-drop Inventory.
- Item sorting/filtering.
- Tooltip nâng cao.
- Refactor toàn bộ project.
- Rename hàng loạt assets.
- Tạo abstraction không có use case hiện tại.

Mục tiêu là hoàn thiện vertical slice hiện có trước khi mở rộng chiều ngang.
