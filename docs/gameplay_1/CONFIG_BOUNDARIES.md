# Gameplay 1 配置边界

## 配置文件

第一版内容配置位于 `res://asset/config/`：

- `weapons.tsv`：武器 ID、名称、瞄准模式、冷却、伤害、弹体数量、场景路径。
- `passives.tsv`：被动 ID、名称、影响的玩家属性、每级数值、最大等级。
- `enemies.tsv`：敌人 ID、名称、场景路径、生命、速度、接触伤害、经验掉落值、视觉缩放。
- `levels.tsv`：关卡入口、初始玩家属性、初始武器、刷怪表、升级池、经验曲线。
- `spawn_schedules.tsv`：刷怪表元信息。
- `spawn_schedule_entries.tsv`：按存活时间切换的刷怪阶段、生成间隔、生成数量、场上上限。时间单位为秒，每一行从 `start_time_seconds` 开始生效，直到下一行开始。
- `spawn_enemy_weights.tsv`：每个刷怪阶段对应的敌人权重。
- `upgrade_pools.tsv`：升级池元信息。
- `upgrade_rewards.tsv`：升级候选奖励，包括新武器、武器升级、新被动、被动升级。
- `experience_curves.tsv`：经验曲线元信息。
- `experience_curve_levels.tsv`：每级升级所需经验。

## 访问入口

`GameConfigManager` 是唯一的共享内容配置入口，并作为 Autoload 注册。

第一版接口：

- `GetWeaponConfig(string id)`
- `GetPassiveConfig(string id)`
- `GetEnemyConfig(string id)`
- `GetLevelConfig(string id)`
- `GetSpawnScheduleConfig(string id)`
- `GetUpgradePoolConfig(string id)`
- `GetExperienceCurveConfig(string id)`
- `GetAllLevelConfigs()`

启动时会加载所有配置，并检查：

- 空 ID 和重复 ID。
- 关卡、武器、敌人的场景路径是否存在。
- 关卡引用的初始武器、刷怪表、升级池、经验曲线是否存在。
- 刷怪表引用的敌人是否存在。
- 升级池引用的武器或被动是否存在。
- 关键数值是否大于 0。

TSV 由 `Godot.FileAccess.GetCsvLine("\t")` 读取。表中复杂的一对多关系不要塞进单元格，而是拆成子表，例如刷怪阶段与敌人权重、升级池与奖励项。

## 运行时读取边界

- 主菜单和关卡选择只通过 `GetAllLevelConfigs()` 与 `GetLevelConfig()` 获取可进入关卡。
- 进入关卡时，关卡入口读取 `LevelConfig`，再把本局需要的配置 ID 交给对应系统。
- 武器系统只读取 `WeaponConfig`，不读取敌人、关卡选择或 HUD。
- 刷怪系统只读取 `SpawnScheduleConfig` 和 `EnemyConfig`。
- 升级系统读取 `UpgradePoolConfig`、`WeaponConfig`、`PassiveConfig` 和 `ExperienceCurveConfig`。
- HUD 只读取运行时状态，例如生命、经验、等级、已持有武器和被动，不直接读取内容配置。
- `GameConfigManager` 不记录本局状态，不生成敌人，不生成武器，不生成升级选项。

阶段 2 只建立配置入口与数据边界。后续阶段再按系统逐步把当前脚本里的临时导出参数迁移到配置读取。
