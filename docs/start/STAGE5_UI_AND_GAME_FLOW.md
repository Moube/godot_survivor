# 阶段5：UI 与游戏流程完成记录

## 阶段目标

本阶段的目标是把阶段4已经完成的战斗闭环，接入到正式的界面与流程表现中，形成可验证的演示流程：

1. 在游戏过程中显示 HUD
2. HUD 能展示玩家生命状态与当前分数
3. 玩家死亡后显示正式的 Game Over 界面
4. 提供重新开始按钮，能够直接重开当前关卡
5. 完成“开始菜单 -> 游戏中 -> Game Over -> 重开”的最小流程闭环

## 本阶段完成内容

### 1. 新增独立 HUD 场景

本阶段没有把界面逻辑继续堆进 `Level01`，而是新增了独立的 HUD 场景：

- `scene/ui/Hud.tscn`
- `scene/ui/Hud.cs`

HUD 当前包含三部分：

- 右上角分数显示
- 屏幕下方中间的玩家血条
- 居中的 Game Over 面板

其中玩家血条采用 `ProgressBar` 表现：

- 位置位于屏幕底部中央
- 使用红色填充
- 不显示具体数值，只显示剩余比例

这与当前阶段的演示目标一致，即先完成清晰的状态可视化，而不在此阶段引入更复杂的 HUD 动效、图标系统或主题资源。

### 2. HUD 改为由 `GameSession` 信号驱动

为了避免 HUD 直接依赖 `Level01` 中的具体节点组织方式，本阶段扩展了：

- `script/common/GameSession.cs`

新增了与玩家生命值相关的状态与信号：

- `CurrentPlayerHealth`
- `MaxPlayerHealth`
- `PlayerHealthChanged`
- `SetPlayerHealth(int currentHealth, int maxHealth)`
- `ClearPlayerHealth()`

整理后的职责划分如下：

- `GameSession`
  - 持有本局分数、Game Over 状态、玩家当前生命值
  - 通过信号向 HUD 广播状态变化
- `Hud`
  - 只负责订阅信号并刷新界面显示
- `Level01`
  - 在生成玩家后，把玩家生命值同步给 `GameSession`
  - 在玩家受伤或死亡时继续更新局内状态

这样做的好处是：

- HUD 不需要知道玩家节点具体挂在什么位置
- 后续如果扩展新关卡，HUD 仍可以复用
- UI 刷新逻辑从关卡控制逻辑中分离，结构更清晰

### 3. 关卡接入正式 HUD

`scene/level/Level01.tscn` 当前已经实例化 `Hud.tscn`，使 HUD 在进入关卡后自动显示。

同时，`scene/level/Level01.cs` 新增了玩家生命同步逻辑：

- 在玩家生成后读取其 `CombatComponent`
- 把初始生命值同步到 `GameSession`
- 在 `Damaged` 信号触发时更新 HUD 所需生命状态

这样在当前阶段中，玩家一进入关卡就能看到正确的初始血条；之后每次受伤，HUD 也会实时变化。

### 4. 正式接入 Game Over UI

在阶段4中，Game Over 仍然是最小实现，主要依赖：

- 暂停 `SceneTree`
- 日志输出结果

本阶段在 HUD 中新增了正式的 Game Over 面板，用于在死亡后可视化反馈当前局结束。

当前 Game Over UI 包含：

- 标题 `Game Over`
- 最终分数 `Final Score`
- `Restart` 按钮

HUD 会监听 `GameSession.GameOver` 信号，在玩家死亡后自动显示该面板。  
由于 HUD 所在 `CanvasLayer` 设置为可在暂停状态下继续工作，因此即使 `SceneTree` 已暂停，Game Over 面板仍可正常显示并响应按钮点击。

### 5. 完成重开流程闭环

本阶段把阶段计划中的“重开”补齐为完整闭环：

- 玩家点击主菜单 `Start`
- `Main` 调用 `GameSession.StartNewRun()`
- 进入 `Level01`
- 游戏进行中通过 HUD 显示分数与血条
- 玩家死亡后触发 `GameSession.TriggerGameOver()`
- HUD 显示 Game Over 面板
- 玩家点击 `Restart`
- 再次调用 `GameSession.StartNewRun()`
- 重新加载 `Level01`

这样当前项目已经具备最基本的演示流程，不再停留在“能跑逻辑”的阶段，而是完成了可操作、可重开的单局展示闭环。

## 验收结果

本阶段已经完成并通过以下验收项：

1. HUD 会在进入关卡后正常显示
2. 分数会随击杀变化并实时刷新
3. 玩家血条会随受伤变化并实时刷新
4. 玩家死亡后会显示正式的 Game Over 界面
5. 在暂停后的 Game Over 状态下，`Restart` 按钮仍可点击
6. 点击重开后可以重新开始一局
7. 主菜单到局内、局内到结束、结束到重开的流程可以完整闭环

## 本阶段产出文件

- `script/common/GameSession.cs`
- `scene/level/Level01.cs`
- `scene/level/Level01.tscn`
- `scene/ui/Hud.cs`
- `scene/ui/Hud.tscn`

## 当前边界

本阶段完成的是“最小可演示 UI 与流程闭环”，但仍然保留了明确边界，没有提前扩展为完整 UI 系统：

- HUD 当前只包含基础血条与分数，不包含技能栏、武器栏或波次信息
- 玩家血条当前采用简单进度条表现，没有加入受击闪烁、平滑补间或动画
- Game Over 界面当前只提供最终分数与重开入口，没有返回主菜单或结算统计页
- 主菜单仍保持最小实现，没有继续扩展设置页、帮助页或角色选择
- UI 风格当前以可读性和流程验证为主，尚未进入最终美术打磨阶段

这些内容更适合在后续阶段结合演示效果与整体美术风格继续推进，而不是在阶段5一次性做成复杂 UI 框架。

## 进入下一阶段前的状态

当前项目已经具备进入阶段6所需的基础：

1. 局内核心玩法闭环已存在
2. HUD 与 Game Over 已完成可视化接入
3. 分数、生命值与流程状态已有统一的信号驱动更新方式
4. 玩家可以完成开始、战斗、死亡和重开的一整轮体验

也就是说，当前项目已经从“最小战斗闭环”推进到了“最小可演示 Demo 闭环”。  
下一步应优先进入阶段6，对表现层进行补强，例如音效、命中特效、死亡反馈、镜头震动和数值调优，而不是继续扩展新的系统复杂度。
