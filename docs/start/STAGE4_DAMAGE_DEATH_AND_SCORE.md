# 阶段4：伤害、死亡与分数完成记录

## 阶段目标

本阶段的目标是建立最小可验收的战斗闭环：

1. 玩家子弹命中敌人后造成伤害
2. 敌人生命值归零后死亡并从场景移除
3. 敌人与玩家接触时对玩家造成伤害
4. 玩家生命值归零后触发 Game Over
5. 击杀敌人后能够累计分数
6. 将“局内状态”从关卡脚本中抽离，避免分数逻辑绑定单一关卡

## 本阶段完成内容

### 1. 引入最小战斗组件 `CombatComponent`

本阶段没有直接把完整的攻击、防御、伤害公式全部抽成复杂系统，而是采用折中方案：

- 新增 `script/common/CombatComponent.cs`
- 组件名称保持为较宽泛的 `CombatComponent`
- 当前只承载生命值、受击和死亡事件

该组件当前提供：

- `MaxHealth`
- `CurrentHealth`
- `IsDead`
- `ResetHealth()`
- `ApplyDamage(int amount)`
- `Damaged` 信号
- `Died` 信号

这样做的目的，是先解决阶段4真正需要的“受伤和死亡”问题，同时为后续扩展攻击、防御、伤害类型保留空间，但避免过早设计完整战斗框架。

### 2. 玩家与敌人都接入了 `CombatComponent`

`scene/player/Player.tscn` 和 `scene/enemy/Enemy.tscn` 均新增了子节点 `CombatComponent`，并分别配置了基础生命值：

- 玩家：`MaxHealth = 5`
- 敌人：`MaxHealth = 3`

对应脚本中也已接入死亡处理：

- `scene/player/Player.cs`
  - 监听 `CombatComponent.Died`
  - 死亡后停止移动与射击逻辑
  - 对外发出 `Died` 信号，交给关卡或全局状态处理后续流程

- `scene/enemy/Enemy.cs`
  - 监听 `CombatComponent.Died`
  - 死亡后加分并 `QueueFree()`

这样玩家和敌人在“能否受伤、是否死亡”这一层已经具备统一基础，但各自死亡后的具体行为仍然保留在自己的脚本中，职责划分更清晰。

### 3. 子弹命中敌人的伤害闭环完成

`scene/bullet/Bullet.cs` 已扩展为可造成伤害：

- 新增导出属性 `Damage`
- 在 `BodyEntered` 中检查碰到的对象是否带有 `CombatComponent`
- 若存在则调用 `ApplyDamage(Damage)`
- 无论是否命中可受伤目标，子弹都会在碰撞后销毁

当前这套逻辑满足阶段4最小需求：

- 玩家子弹能稳定命中敌人
- 敌人会正确扣血
- 敌人血量归零后死亡并移除

由于敌人的碰撞层与子弹掩码已经提前完成配置，因此这一部分无需额外改动碰撞逻辑即可生效。

### 4. 敌人接触伤害玩家的逻辑完成

`scene/enemy/Enemy.cs` 新增了接触伤害相关导出参数：

- `ContactDamage`
- `ContactDamageCooldownSeconds`

实现方式为：

- 敌人继续通过 `CharacterBody2D` 追踪玩家
- 在 `MoveAndSlide()` 后读取滑动碰撞结果
- 如果碰到的是 `player` 分组目标，并且目标上存在 `CombatComponent`
  - 则对玩家调用 `ApplyDamage(ContactDamage)`
- 使用冷却时间避免每一帧都造成伤害

这样当前阶段的玩家受伤行为满足以下要求：

- 敌人贴近玩家时会掉血
- 不会因为一帧多次碰撞而瞬间清空血量
- 不需要额外新增攻击动画或复杂攻击状态机

### 5. 关卡中的 Game Over 闭环完成

`scene/level/Level01.cs` 中接入了玩家死亡后的局内流程控制：

- 关卡在生成玩家后监听 `Player.Died`
- 玩家死亡时停止 `SpawnTimer`
- 调用全局局状态单例触发 Game Over

当前阶段的 Game Over 仍然是最小实现：

- 通过暂停 `SceneTree` 停止继续游戏
- 通过日志输出当前结果

这与阶段计划一致，即先完成逻辑闭环，再在阶段5对接正式 HUD 和 Game Over UI。

### 6. 分数与局状态从关卡脚本中抽离到 `Autoload`

在阶段4实现通过后，又做了一步结构整理，避免把“局内状态”绑死在 `Level01.cs` 中。

新增：

- `script/common/GameSession.cs`

并在 `project.godot` 中注册为 `Autoload`：

- 名称：`GameSession`
- 路径：`res://script/common/GameSession.cs`

当前 `GameSession` 负责：

- `Score`
- `IsGameOver`
- `StartNewRun()`
- `AddScore(int amount)`
- `TriggerGameOver()`
- `ScoreChanged` 信号
- `GameOver` 信号

整理后的职责划分如下：

- `Level01`
  - 负责本关玩家生成、敌人生成、出生区域、刷怪计时器
- `Enemy`
  - 负责自己死亡时调用 `GameSession.AddScore()`
- `Player`
  - 负责自己死亡时发出角色死亡信号
- `GameSession`
  - 负责整局分数和 Game Over 状态
- `Main`
  - 负责点击开始时调用 `GameSession.StartNewRun()`

这样后续即使新增 `Level02`、`BossLevel` 等场景，也可以共用同一套局状态逻辑，而不需要把分数重复写进每个关卡脚本。

### 7. 碰撞层、掩码与可复用墙体场景整理完成

为了支撑阶段4战斗碰撞，本阶段还整理了 2D Physics Layer 命名与场景配置。

当前层命名为：

1. `World`
2. `Player`
3. `Enemy`
4. `PlayerBullet`
5. `EnemyBullet`

当前主要对象配置为：

- `Player`
  - Layer: `Player`
  - Mask: `World`, `Enemy`
- `Enemy`
  - Layer: `Enemy`
  - Mask: `World`, `Player`
- `Bullet`
  - Layer: `PlayerBullet`
  - Mask: `World`, `Enemy`
- 墙和障碍物
  - Layer: `World`
  - Mask: `Player`, `Enemy`, `PlayerBullet`

同时新增了可复用的 `scene/level/WallBlock.tscn`：

- 统一封装墙体与障碍物的碰撞配置
- `Level01.tscn` 中现有墙和障碍已经替换为该场景实例

这样后续继续搭建关卡时，不需要为每个静态障碍重新手动配置碰撞层和掩码。

## 验收结果

本阶段已经完成并通过以下验收项：

1. 玩家子弹命中敌人后会正确扣血
2. 敌人生命值归零后会从场景移除
3. 敌人与玩家接触时会按间隔造成伤害
4. 玩家生命值归零后无法继续正常游戏
5. 击杀敌人后会累计分数
6. 分数与 Game Over 状态不再依赖单一关卡脚本

## 本阶段产出文件

- `script/common/CombatComponent.cs`
- `script/common/GameSession.cs`
- `scene/player/Player.tscn`
- `scene/player/Player.cs`
- `scene/enemy/Enemy.tscn`
- `scene/enemy/Enemy.cs`
- `scene/bullet/Bullet.cs`
- `scene/level/Level01.cs`
- `scene/level/WallBlock.tscn`
- `project.godot`

## 当前边界

本阶段完成的是“最小可玩的战斗闭环”，但还没有扩展到完整战斗系统：

- `CombatComponent` 当前只包含生命值、受击和死亡
- 尚未引入攻击力、护甲、防御、伤害类型等数值系统
- 分数和 Game Over 目前仍以日志和暂停树为主，尚未接入正式 UI
- 敌人接触伤害目前是最小实现，尚未加入攻击动画、前摇或击退
- 尚未加入命中特效、死亡特效和战斗音效

这些内容适合在后续阶段继续推进，而不是在阶段4一次性做成复杂架构。

## 进入下一阶段前的状态

当前项目已经具备阶段5所需的战斗基础，可以继续推进：

1. HUD 显示生命值与分数
2. 正式 Game Over 界面
3. 使用 `GameSession` 信号驱动 UI 刷新
4. 将阶段4中的日志输出替换为可视化反馈

也就是说，战斗底层最小闭环已经成立，下一步应重点补齐“UI 与游戏流程呈现”，而不是继续扩大战斗系统抽象。
