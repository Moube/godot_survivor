# 音效资源需求整理

## 文档目的

本文件用于整理当前项目所需的音效资源，方便后续：

1. 先补齐最小可演示所需音效
2. 使用 AI 工具生成定制音效
3. 从免费音效网站下载可替代资源
4. 统一音效命名、分类与导入目录

当前项目已经具备基础玩法闭环，但声音反馈尚未正式接入。根据现有脚本与开发计划，阶段 6 的重点之一就是补齐基础音效反馈。

参考实现位置：

- `scene/player/Player.cs`
- `scene/enemy/Enemy.cs`
- `scene/bullet/Bullet.cs`
- `scene/level/Level01.cs`
- `script/common/CombatComponent.cs`
- `script/common/GameSession.cs`
- `docs/PROJECT_PLAN.md`

---

## 当前音效缺口概览

按优先级划分，当前音效资源可分为两类：

### 必须先补

1. 玩家射击音效
2. 子弹命中敌人音效
3. 子弹命中墙体/障碍物音效
4. 敌人死亡音效
5. 玩家受伤音效
6. Game Over 音效
7. UI 按钮点击音效

### 可后补

1. 敌人生成音效
2. 得分提升提示音
3. Restart 确认音效
4. 低血量警告循环音
5. 关卡环境氛围循环音

---

## 详细音效清单

## 1. 战斗核心音效

### 1.1 玩家射击音效

用途：

- 玩家按住或点击射击时播放
- 对应 `Player.TryShoot()`

目标表现：

- 短促
- 清晰
- 不拖尾过长
- 连续射击时不会糊成一片

建议方向：

- 科幻脉冲枪
- 轻型手枪
- 训练场能量射击

建议命名：

- `sfx_player_shoot.wav`
- `sfx_player_shoot_01.wav`
- `sfx_player_shoot_02.wav`

推荐补充：

- 至少准备 2 到 3 个轻微变化版本，避免重复感过强

### 1.2 子弹命中敌人音效

用途：

- 子弹命中带 `CombatComponent` 的敌人时播放
- 对应 `Bullet.OnBodyEntered()`

目标表现：

- 短促、有反馈感
- 能和射击声区分开

建议方向：

- 轻微金属/能量命中
- 肉感较弱的 arcade hit
- 干脆的命中 click / pop

建议命名：

- `sfx_bullet_hit_enemy.wav`
- `sfx_bullet_hit_enemy_01.wav`

### 1.3 子弹命中墙体/障碍物音效

用途：

- 子弹打到墙体、边界或障碍物时播放

目标表现：

- 比命中敌人更偏硬质或摩擦感

建议方向：

- 金属碰撞
- 石块/训练板命中
- 能量弹撞击护板

建议命名：

- `sfx_bullet_hit_wall.wav`
- `sfx_bullet_hit_obstacle.wav`

### 1.4 敌人死亡音效

用途：

- 敌人 `Died` 后播放
- 对应 `Enemy.OnDied()`

目标表现：

- 明确告诉玩家“击杀成功”
- 长度不要过长，避免战斗中堆叠太乱

建议方向：

- 小型爆裂
- 能量瓦解
- 机械单位失效

建议命名：

- `sfx_enemy_die.wav`
- `sfx_enemy_die_01.wav`

---

## 2. 玩家状态音效

### 2.1 玩家受伤音效

用途：

- 玩家被敌人接触伤害时播放
- 对应 `Enemy.TryApplyContactDamage()` 和 `CombatComponent.ApplyDamage()`

目标表现：

- 明显，但不要刺耳
- 能和敌人死亡、命中声区分

建议方向：

- 轻度受击
- 护甲破损
- 科幻角色受伤提示

建议命名：

- `sfx_player_hurt.wav`
- `sfx_player_hurt_01.wav`

### 2.2 低血量警告循环音

用途：

- 玩家生命值过低时循环或间歇播放

优先级：

- 可后补

建议方向：

- 电子警报
- 心跳感脉冲
- HUD 提示滴答声

建议命名：

- `sfx_low_hp_loop.ogg`
- `sfx_low_hp_warning.wav`

### 2.3 Game Over 音效

用途：

- 玩家死亡并触发 `GameSession.TriggerGameOver()` 时播放

目标表现：

- 明确表达本局结束
- 时长可略长于普通 SFX

建议方向：

- 短促失败提示
- 电子系统停机
- arcade fail sting

建议命名：

- `sfx_game_over.wav`

---

## 3. UI 与流程音效

### 3.1 按钮点击音效

用途：

- 主菜单 `Start`
- Game Over 面板 `Restart`
- 未来其他 UI 操作

目标表现：

- 轻快
- 不喧宾夺主

建议方向：

- UI click
- soft tick
- synth button

建议命名：

- `sfx_ui_click.wav`
- `sfx_ui_click_soft.wav`

### 3.2 Restart 确认音效

用途：

- 点击重新开始后播放

优先级：

- 可与按钮点击音效共用
- 也可后补独立版本

建议命名：

- `sfx_restart.wav`

### 3.3 得分提升提示音

用途：

- 击杀加分后播放
- 对应 `GameSession.AddScore()`

优先级：

- 可后补

建议方向：

- 轻量 arcade score up
- 短促上扬提示音

建议命名：

- `sfx_score_up.wav`

---

## 4. 环境与氛围音效

### 4.1 敌人生成音效

用途：

- 敌人刷出时给一点存在感
- 对应 `Level01.SpawnEnemy()`

优先级：

- 可后补

建议方向：

- 传送出现
- 能量聚合
- 轻量机械激活

建议命名：

- `sfx_enemy_spawn.wav`

### 4.2 关卡环境循环音

用途：

- 填补完全静音造成的空洞感

优先级：

- 可后补，但对演示体验帮助很大

建议方向：

- 训练场电流底噪
- 科幻设施环境声
- 轻度风扇/设备 humming

建议命名：

- `amb_level_loop.ogg`

推荐格式：

- 循环环境音优先考虑 `ogg`

---

## 最小可演示音效包建议

如果当前目标只是让项目达到“可演示”，建议先补齐以下 7 个音效：

1. `sfx_player_shoot.wav`
2. `sfx_bullet_hit_enemy.wav`
3. `sfx_bullet_hit_wall.wav`
4. `sfx_enemy_die.wav`
5. `sfx_player_hurt.wav`
6. `sfx_game_over.wav`
7. `sfx_ui_click.wav`

这套已经足够支撑：

1. 射击有反馈
2. 命中有区分
3. 击杀有确认感
4. 玩家受伤和失败状态更清楚
5. UI 操作不再完全无声

---

## 建议目录结构

建议在项目中新增如下目录：

```text
res://asset/
  audio/
    sfx/
      combat/
      player/
      enemy/
      ui/
    ambience/
```

对应示例：

```text
res://asset/audio/sfx/combat/sfx_player_shoot.wav
res://asset/audio/sfx/combat/sfx_bullet_hit_enemy.wav
res://asset/audio/sfx/combat/sfx_bullet_hit_wall.wav
res://asset/audio/sfx/enemy/sfx_enemy_die.wav
res://asset/audio/sfx/player/sfx_player_hurt.wav
res://asset/audio/sfx/ui/sfx_ui_click.wav
res://asset/audio/sfx/ui/sfx_game_over.wav
res://asset/audio/ambience/amb_level_loop.ogg
```

---

## AI 音效生成工具建议

以下工具更适合当前这种小型 Godot demo 的 SFX 补充工作流。

### 1. ElevenLabs Sound Effects

适合用途：

- 短音效
- 游戏交互音
- 命中、射击、UI click
- 可控时长的效果音

我查到的官方能力说明：

- 支持用文本生成高质量音效
- 可以指定时长
- 支持循环音效
- 输出支持 MP3，非循环效果支持 48kHz WAV

适合本项目的原因：

- 很适合补 `shoot`、`hit`、`enemy_die`、`ui_click`
- 提示词简单直接，适合快速迭代

官方文档：

- <https://elevenlabs.io/docs/overview/capabilities/sound-effects>

### 2. Stable Audio

适合用途：

- 环境氛围
- 较长的音频内容
- 科幻场景底噪
- 较有质感的品牌化声音

我查到的官方说明：

- Stability AI 当前主推的是 `Stable Audio 2.5`
- 页面强调其面向高质量音频生产和更强控制能力

适合本项目的原因：

- 更适合 `amb_level_loop`
- 也适合做较长的 spawn / energy / system ambience

官方页面：

- <https://stability.ai/stable-audio>

### 3. Adobe Express Sound Effects

适合用途：

- 轻量快速补素材
- 非音频专业流程下的简单编辑与拼接

说明：

- 我更建议把它作为补充选项，而不是本项目主力工具
- 如果你只想快速拿一个能用的短音效，它的门槛较低

官方页面：

- <https://www.adobe.com/express/feature/audio/sound-effects>

---

## 免费音效网站建议

下面这些站点适合当前项目快速补音效，但授权条件并不完全相同，下载前仍需逐条确认。

### 1. Freesound

优点：

- 资源量很大
- 能找到很多细分声音
- 社区上传内容丰富

注意事项：

- 站内素材许可不统一
- 常见有 `CC0`、`CC BY`、`CC BY-NC`
- 官方 FAQ 提供了署名示例

适合本项目的用途：

- 找特定命中声
- 找机械音、环境底噪
- 找较冷门的小效果音

官方页面：

- <https://freesound.org/help/faq/>

### 2. Pixabay

优点：

- 上手简单
- 免费使用
- 官方许可摘要明确写到可免费使用、可修改、通常不要求署名

我查到的许可摘要重点：

- 可免费使用内容
- 可不署名使用
- 可修改和改编
- 但不能原样单独转售或分发

适合本项目的用途：

- 先快速拿一套 demo 音效
- 找 UI、环境、通用命中类声音

官方页面：

- <https://pixabay.com/sound-effects/>
- <https://pixabay.com/ro/service/license-summary/>

### 3. Mixkit

优点：

- 站内分类清晰
- 有专门的 `Sound Effects` 分类
- 非常适合快速挑现成音效

注意事项：

- 具体使用边界仍以其站点 License 为准
- 使用前建议再看一次当前条款

适合本项目的用途：

- UI click
- 通用 combat hit
- ambience 与 transition

官方页面：

- <https://mixkit.co/sound-effects/>
- <https://mixkit.co/license/>

### 4. ZapSplat

优点：

- 库很大
- 游戏开发里很常用
- 很多通用 SFX 都能找到

注意事项：

- 使用必须遵循其 Standard License
- 免费与付费使用条件可能不同
- 使用前必须复核当前授权条款

适合本项目的用途：

- 战斗 one-shot
- UI 音效
- 环境与机械音

官方页面：

- <https://www.zapsplat.com/>
- <https://www.zapsplat.com/license-type/standard-license/page/2/>

### 5. Kenney

优点：

- 非常适合游戏开发
- 风格统一
- 音效与美术资源都适合 demo 阶段快速使用

我查到的官方信息：

- Kenney 的常见资源页会直接标注 `Creative Commons CC0`
- 例如 `Digital Audio`、`Interface Sounds` 页面都明确标出 `License | Creative Commons CC0`

适合本项目的用途：

- UI 按钮音
- 通用游戏交互音
- 快速拿一套可商用占位资源

官方页面：

- <https://kenney.nl/assets/digital-audio>
- <https://kenney.nl/assets/interface-sounds>
- <https://kenney.nl/support>

---

## 使用建议

对当前项目，最省时间的执行顺序建议如下：

1. 先从 `Pixabay`、`Kenney`、`Mixkit` 找一套能跑的临时音效
2. 缺少关键声音时，再用 `ElevenLabs` 定制补洞
3. 如果需要更完整的环境氛围，再用 `Stable Audio` 生成循环 ambience
4. 导入 Godot 后统一调整音量、音高和总线分组

如果后续正式商用或公开发布，建议你在最终打包前再做一次统一授权复查，尤其是：

1. Freesound 的单条素材许可
2. ZapSplat 当前账户类型与使用条件
3. Mixkit 当前 license 页面限制

---

## 后续可继续补的文档

如果后续要继续整理，可以继续新增：

1. `AUDIO_PROMPTS_FOR_AI_GENERATION.md`
2. `ASSET_IMPORT_GUIDE.md`
3. `GODOT_AUDIO_INTEGRATION_PLAN.md`

其中最有价值的下一份文档，通常是把本项目每个音效对应的 AI 提示词也整理出来，直接可用于生成。

---

## 参考来源

以下信息已基于对应官网页面整理：

- ElevenLabs Sound Effects docs
  - <https://elevenlabs.io/docs/overview/capabilities/sound-effects>
- Stable Audio
  - <https://stability.ai/stable-audio>
- Adobe Express Sound Effects
  - <https://www.adobe.com/express/feature/audio/sound-effects>
- Freesound FAQ
  - <https://freesound.org/help/faq/>
- Pixabay License Summary
  - <https://pixabay.com/ro/service/license-summary/>
- Mixkit License
  - <https://mixkit.co/license/>
- ZapSplat Standard License
  - <https://www.zapsplat.com/license-type/standard-license/page/2/>
- Kenney Support
  - <https://kenney.nl/support>
- Kenney Digital Audio
  - <https://kenney.nl/assets/digital-audio>
- Kenney Interface Sounds
  - <https://kenney.nl/assets/interface-sounds>
