# Gameplay 1 推进记录

## 文档目的

本文用于持续记录 `gameplay_1` 的实际推进过程，包括已经完成的阶段、过程中做出的设计调整、验证结果和后续待办。

后续继续推进 Gameplay 1 时，应在本文追加新的记录，而不是覆盖已有内容。

---

## 2026-04-28 本聊天总结

### 阶段 1：存活时间替代分数

已完成。

- 扩展 `GameSession`：
  - 新增 `ElapsedRunTime`
  - 新增 `FinalSurvivalTime`
  - 新增 `RunTimeChanged`
  - `GameOver` 信号改为传递存活时间
- 调整 `Level01`：
  - 关卡开始时调用 `StartNewRun()`
  - 每帧推进存活时间
  - 玩家死亡后按当前存活时间结算
- 调整 `Hud`：
  - 移除可见分数 UI
  - 顶部中央显示 `mm:ss` 存活时间
  - Game Over 面板显示 `Survived mm:ss`

验证结果：

- `dotnet build .\test_godot.sln` 通过，0 warning，0 error。

说明：

- 旧 `Score` / `AddScore()` 当时暂时保留，后续阶段 5 已把敌人死亡奖励改成经验掉落。

---

### 阶段 2：内容数据与配置访问入口

已完成，并经历了一次配置格式调整。

最初实现：

- 新增配置模型：
  - 武器
  - 被动
  - 敌人
  - 关卡
  - 刷怪表
  - 升级池
  - 经验曲线
- 新增 `GameConfigManager` Autoload：
  - 负责加载配置
  - 按 ID 查询配置
  - 启动时检查重复 ID、缺失引用、路径和关键数值

配置格式调整：

- 最初使用 JSON，后改为 CSV。
- Godot 会把 `.csv` 当翻译表导入并生成大量 `.translation` 文件。
- 最终改为 `.tsv`，使用 `FileAccess.GetCsvLine("\t")` 读取。
- 删除了 `.translation`、`.csv.import` 和旧 `.csv` 文件。

当前配置文件位于 `asset/config/`：

- `weapons.tsv`
- `passives.tsv`
- `enemies.tsv`
- `levels.tsv`
- `spawn_schedules.tsv`
- `spawn_schedule_entries.tsv`
- `spawn_enemy_weights.tsv`
- `upgrade_pools.tsv`
- `upgrade_rewards.tsv`
- `experience_curves.tsv`
- `experience_curve_levels.tsv`

相关文档：

- `CONFIG_BOUNDARIES.md`

验证结果：

- `dotnet build .\test_godot.sln` 通过。
- TSV 路径和 ID 引用检查通过。

---

### 阶段 3：武器自动攻击与武器抽象调整

已完成第一版自动攻击，并在后续根据新需求调整了武器抽象。

第一版完成内容：

- 武器不再依赖玩家手动射击输入。
- `WeaponInventory` 根据关卡配置中的 `initial_weapon_id` 动态生成初始武器。
- `MagicWandWeapon` 保留环绕玩家表现，并按冷却自动攻击。
- 支持投射物发射器的 3 种发射行为：
  - `MouseDirection`
  - `NearestEnemy`
  - `RandomDirection`

后续抽象调整：

- 原设计把“武器”默认理解为“发射器”，这个假设过窄。
- 已将武器概念调整为“局内自动生效的战斗能力”。
- 新增更抽象的武器行为类型：
  - `ProjectileEmitter`
  - `GroundArea`
  - `PlayerAura`
  - `OrbitingObject`
  - `AreaPulse`
  - `TargetedStrike`
- 原来的 `MouseDirection` / `NearestEnemy` / `RandomDirection` 改为 `ProjectileEmitter` 的发射行为，而不是所有武器的通用类型。
- 代码结构调整：
  - `Weapon2D` 变为通用武器运行时基类。
  - 新增 `ProjectileEmitterWeapon2D`，承接投射物、枪口、发射动画、发射行为和阴影逻辑。
  - `MagicWandWeapon` 改为继承 `ProjectileEmitterWeapon2D`。
- 配置调整：
  - `weapons.tsv` 增加 `behavior_type`
  - `weapons.tsv` 增加 `projectile_fire_mode`

验证结果：

- `dotnet build .\test_godot.sln` 通过。
- 武器 TSV 行为字段和路径引用检查通过。

说明：

- 当前还没有实现圣水池、光环、落雷等非发射器武器。
- 当前只是把架构调整到可以承载这些后续武器。

---

### 阶段 4：时间驱动刷怪

已完成。

根据补充需求进行了以下调整：

- 当前具体敌人 `Enemy` 实际含义是史莱姆，已改名为 `SlimeEnemy`。
- 保留 `EnemyBase` 作为所有敌人的通用基类。
- 新增史莱姆配置：
  - `slime_small`
  - `slime_medium`
  - `slime_large`
- 新增 `visual_scale` 字段，用于表现不同大小史莱姆。
- 新增 `SpawnDirector`：
  - 读取关卡配置中的 `SpawnScheduleId`
  - 根据存活秒数选择当前刷怪阶段
  - 控制出怪间隔
  - 控制单次生成数量
  - 控制场上最大敌人数
  - 根据权重随机选择敌人类型
- `Level01` 移除固定 `EnemyScene`、`SpawnTimer` 和固定最大数量逻辑，改由 `SpawnDirector` 驱动。

关卡 1 当前刷怪表：

- 0 到 5 分钟：只刷小史莱姆，每 60 秒提高场上最大数量。
- 5 到 10 分钟：小史莱姆和中史莱姆混刷，权重逐渐倾向中史莱姆。
- 10 到 15 分钟：只刷中史莱姆，数量每分钟提升。

验证结果：

- `dotnet build .\test_godot.sln` 通过。
- TSV 路径和 ID 引用检查通过。

---

### 阶段 5：经验掉落与拾取

已完成。

- 新增 `ExperienceController` Autoload：
  - 管理当前等级
  - 管理当前经验
  - 管理当前升级所需经验
  - 经验满时发出 `LevelUpRequested`
- 新增 `ExperienceGem`：
  - 先用 `Polygon2D` 表示视觉
  - 预留 `TextureSlot`，后续可替换贴图
  - 掉落时先做抛物弧线
  - 落地后才可被拾取
  - 进入玩家拾取范围后飞向玩家中心
  - 到达玩家中心后消失并增加经验
- 玩家新增 `PickupRange` 属性：
  - 从关卡配置 `initial_pickup_range` 初始化
  - 后续可被被动道具强化
- 敌人死亡不再加分，改为根据 `ExperienceValue` 掉落经验物。
- HUD 新增经验条：
  - 监听 `ExperienceController.ExperienceChanged`
  - 显示当前等级经验进度

验证结果：

- `dotnet build .\test_godot.sln` 通过，0 warning，0 error。

说明：

- 经验满时当前只发出升级请求并让经验条停在满值。
- 升级三选一流程留到后续阶段实现。

---

### 阶段 6 前的计划调整

原计划中阶段 6 直接实现“升级三选一”。经过讨论后，认为三选一之前还缺少关键依赖：

- 武器持有状态
- 武器等级状态
- 被动持有状态
- 被动等级状态
- 玩家属性聚合层
- 至少一些可实际生效的武器和被动奖励

因此文档已调整：

- 阶段 6 改为“升级前置内容与持有状态”。
- 原“升级三选一”顺延为阶段 7。

阶段 6 新目标：

- 整理 `WeaponInventory`
- 新增 `PassiveInventory`
- 新增或整理 `PlayerStats`
- 补齐可用武器和被动奖励内容
- HUD 显示持有武器和被动
- 至少一种被动能真实改变玩家属性

---

## 当前重要设计结论

1. 武器不是发射器，而是自动生效的战斗能力。
2. `ProjectileEmitter` 只是第一种武器行为类型。
3. `MouseDirection`、`NearestEnemy`、`RandomDirection` 是投射物发射行为，不是所有武器的通用模式。
4. 配置使用 TSV，不使用 JSON 或 CSV。
5. 刷怪配置跟随关卡，通过 `LevelConfig.SpawnScheduleId` 关联。
6. 刷怪时间单位为秒，每个时间节点配置会持续生效到下一个节点。
7. 经验物必须先落地，落地后才可拾取。
8. 玩家拾取范围是属性，后续可被被动强化。

---

## 下一步建议

优先进入新的阶段 6：升级前置内容与持有状态。

建议先做：

1. `PlayerStats`
2. `PassiveInventory`
3. `WeaponInventory` 等级和升级能力补全
4. `weapons.tsv` 支持武器升级成长字段
5. `passives.tsv` 接入实际属性效果
6. HUD 左右两侧显示武器和被动持有列表

完成后再进入阶段 7：升级三选一。

---

## 2026-04-28 追加记录：阶段 6 到阶段 9 推进

### 初始状态核对

本轮开始时先对照 `IMPLEMENTATION_PLAN.md` 检查了当前完成度：

- 阶段 1 到阶段 3 已基本完成。
- 阶段 4 的代码主体已完成，但刷怪配置仍有后续可调空间。
- 阶段 5 已完成经验掉落和拾取基础闭环。
- 阶段 6、7、8 尚未完成。
- 阶段 9 属于体验调优和验收阶段，需要在核心闭环完成后进行。

---

### 阶段 6：升级前置内容与持有状态

已完成。

实现内容：

- 新增 `PlayerStats`：
  - 集中维护移动速度、最大生命、经验拾取范围、武器伤害倍率、武器冷却倍率。
  - 被动奖励通过该层统一生效。
- 新增 `PassiveInventory`：
  - 支持最大持有 4 个被动。
  - 支持新增被动。
  - 支持升级已有被动。
  - 支持过滤满栏和满级状态。
- 扩展 `WeaponInventory`：
  - 记录武器 ID、等级、配置和运行时实例。
  - 支持新增武器。
  - 支持升级已有武器。
  - 支持最大持有 4 个武器。
  - 支持满栏和满级过滤。
- 扩展 `Weapon2D` 和 `ProjectileEmitterWeapon2D`：
  - 武器可绑定 `PlayerStats`。
  - 武器升级会提升伤害、降低冷却，并在等级提升后增加弹体数量。
  - `magic_lens` 和 `quick_charm` 可通过 `PlayerStats` 影响武器表现。
- 扩展 `CombatComponent`：
  - 支持最大生命变化时调整当前生命。
- 更新玩家场景：
  - 挂载 `PlayerStats`。
  - 挂载 `PassiveInventory`。
  - 初始化时根据关卡配置设置生命、移动速度、拾取范围和初始武器。
- 更新 HUD：
  - 左侧显示武器持有列表。
  - 右侧显示被动持有列表。
  - 武器槽和被动槽使用灰模占位结构，预留后续图标贴图替换空间。

后续微调：

- 武器列表移动到左上角。
- 被动列表移动到右上角。
- 多个环绕类武器会按当前武器数量均匀分布在圆形轨道上，避免复用同一场景时重叠。

验证结果：

- `dotnet build` 通过。
- `git diff --check` 通过。

---

### 阶段 7：升级三选一

已完成。

实现内容：

- 新增 `UpgradeManager`：
  - 监听 `ExperienceController.LevelUpRequested`。
  - 从关卡配置引用的升级池中读取奖励。
  - 根据当前持有武器和被动过滤候选奖励。
  - 过滤满栏后的新武器、新被动奖励。
  - 过滤已满级武器和被动的升级奖励。
  - 按权重随机抽取最多 3 个候选项。
  - 应用玩家选择。
- 新增升级 UI：
  - `scene/ui/upgrade/UpgradeChoicePanel.tscn`
  - `scene/ui/upgrade/UpgradeChoicePanel.cs`
  - 显示 3 张卡。
  - 每张卡显示名称、类型和简短效果说明。
  - 使用灰模占位结构，后续可替换卡牌背景、边框和图标。
- 升级流程：
  - 经验满后暂停局内战斗。
  - 显示三选一界面。
  - 点击奖励后应用奖励。
  - 调用 `ExperienceController.CompletePendingLevelUp()`。
  - 关闭界面并恢复战斗。

奖励应用规则：

- 新武器：加入 `WeaponInventory`。
- 武器升级：提升对应武器等级。
- 新被动：加入 `PassiveInventory`。
- 被动升级：提升对应被动等级。

验证结果：

- `dotnet build` 通过。
- `git diff --check` 通过。

---

### 阶段 8：主菜单、关卡选择与结算流程

已完成。

实现内容：

- 改造主菜单：
  - `Start Game`
  - `Settings`
  - `Quit`
- `Start Game` 不再直接进入 `Level01`，而是进入关卡选择页。
- 新增关卡选择页：
  - 当前从 `GameConfigManager.GetAllLevelConfigs()` 读取可进入关卡。
  - 点击关卡后记录关卡配置 ID，并加载对应场景。
- 新增设置占位页：
  - 当前只包含返回按钮。
  - 后续可接入音量、全屏等设置。
- 扩展 `GameSession`：
  - 新增 `SelectedLevelConfigId`。
  - 新增 `SelectLevelConfig()`。
- `Level01`：
  - 优先读取 `GameSession.SelectedLevelConfigId`。
  - 直接运行关卡场景时回退到导出字段 `LevelConfigId`。
- 改造结算流程：
  - Game Over 按钮从 `Restart` 改为 `Confirm`。
  - Confirm 后解除暂停并返回主菜单。

验证结果：

- `dotnet build` 通过。
- `git diff --check` 通过。

---

### HUD 灰模调整

已完成。

调整内容：

- 顶部存活时间面板不再使用旧贴图。
- Game Over 结算面板不再使用旧贴图。
- 移除 HUD 场景中对以下资源的引用：
  - `ui_panel_score.png`
  - `ui_panel_basic.png`
- 时间面板和结算面板改为与其他 HUD 一致的纯色描边占位风格。

验证结果：

- `dotnet build` 通过。
- `git diff --check` 通过。
- 检查确认 `Hud.tscn` 不再引用上述两个 UI texture。

---

### 阶段 9：最小验收与自动化 smoke test

阶段 9 的手感部分已经由人工验收通过；本轮补充了自动化 smoke test。

新增自动验收目录：

- `tests/acceptance/GameplayAcceptanceRunner.cs`
- `tests/acceptance/GameplayAcceptanceRunner.tscn`

自动验收覆盖：

- Autoload 存在并加载：
  - `GameSession`
  - `GameConfigManager`
  - `ExperienceController`
- 配置可读取：
  - `level_01`
  - 初始武器
  - 最近敌人武器
  - 随机方向武器
  - 被动配置
  - 升级池
- 主菜单流程：
  - 主菜单默认可见。
  - 关卡选择默认隐藏。
  - 设置页默认隐藏。
  - `Start Game` 可打开关卡选择。
  - 关卡选择中至少有一个关卡按钮。
  - `Settings` 可打开设置占位页。
  - 设置返回可回主菜单。
- 局内流程：
  - 关卡场景可从配置加载。
  - 玩家可生成。
  - 初始武器可添加。
  - `SpawnDirector` 存在。
  - `UpgradeManager` 存在。
  - HUD 存在。
  - 敌人可自动生成。
- 升级流程：
  - 经验满后触发升级请求。
  - 升级选择期间暂停场景树。
  - 三选一面板可见。
  - 点击奖励后恢复场景树。
  - 等级提升到下一等级。
  - 奖励会改变武器或被动状态。
- 结算流程：
  - Game Over 面板可见。
  - Confirm 按钮存在。

自动验收命令示例：

```powershell
& 'D:\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path . --scene res://tests/acceptance/GameplayAcceptanceRunner.tscn --fixed-fps 60 --quit-after 900
```

最近一次结果：

```text
ACCEPTANCE_RESULT: PASS
```

说明：

- 第一次尝试时曾把 `dotnet build` 和 Godot runner 并行执行，导致 Godot 早于临时 C# runner 编译完成，出现 `associated class could not be found` 并卡住。
- 之后改为先构建，再运行 Godot headless，并加上 `--quit-after` 兜底，验收通过。

---

### 临时资源清理

已完成。

- 将自动验收 runner 从 `tmp` 移到 `tests/acceptance`。
- 检查 `tmp/imagegen` 下生成图片是否被正式资源引用。
- 以下图片没有被引用，因此删除：
  - `enemy_slime_basic_idle_alpha.png`
  - `enemy_slime_move_strip_4f_alpha.png`
  - `hit_spark_strip_alpha.png`
  - `magic_projectile_basic_alpha.png`
- 同步删除对应 `.import` 文件。
- 删除临时验收日志。
- `.gitignore` 增加 `/tmp/`，避免后续误提交临时目录。

验证结果：

- 项目中不再引用 `res://tmp/imagegen`。
- `dotnet build` 通过。
- `tests/acceptance` 下的 headless 验收通过。

---

## 当前 Gameplay 1 状态

当前 Gameplay 1 的第一版闭环已经具备：

1. 主菜单。
2. 关卡选择。
3. 存活时间目标。
4. 自动武器攻击。
5. 时间驱动持续刷怪。
6. 击杀敌人掉落经验。
7. 玩家靠近拾取经验。
8. 经验满后显示三选一升级。
9. 奖励能实际改变武器或被动状态。
10. HUD 显示生命、经验、存活时间、武器列表和被动列表。
11. 玩家死亡后显示存活时间结算。
12. Confirm 后返回主菜单。

剩余主要工作已经从“Gameplay 1 基础闭环实现”转为：

- 数值手感继续调优。
- UI 视觉资源重做。
- 新增更多武器行为，尤其是非 `ProjectileEmitter` 类型。
- 新增更多敌人表现和关卡内容。
- 将自动验收 runner 进一步整理为长期可维护的测试入口。
