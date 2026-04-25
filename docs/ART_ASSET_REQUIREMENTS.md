# 美术资源需求整理

## 文档目的

本文件用于整理当前项目在阶段 6 需要补充的美术资源，方便后续：

1. 使用 AI 工具批量生成占位素材
2. 从外部素材站点挑选可替代资源
3. 统一资源命名与导入目录

视觉风格统一遵循 [VISUAL_STYLE_GUIDE.md](/D:/project/godot/test_godot/docs/VISUAL_STYLE_GUIDE.md)。

当前项目已完成到阶段 6 前置玩法闭环，但场景中的视觉表现仍以 `Polygon2D` 和简单 UI 样式为主，正式贴图资源基本缺失。

参考实现位置：

- `scene/player/Player.tscn`
- `scene/enemy/Enemy.tscn`
- `scene/bullet/Bullet.tscn`
- `scene/level/Level01.tscn`
- `scene/ui/Hud.tscn`
- `docs/PROJECT_PLAN.md`

---

## 当前资源缺口概览

按优先级划分，当前美术资源可分为两类：

### 必须先补

这部分资源直接影响阶段 6 的演示效果，建议优先完成。

1. 玩家主体贴图
2. 敌人主体贴图
3. 子弹贴图
4. 命中闪光特效贴图
5. 敌人死亡消散特效贴图
6. 地面平铺贴图
7. 墙体/障碍物贴图
8. HUD 基础面板贴图

### 可后补

这部分资源不影响最小可演示闭环，但能明显提升观感。

1. 玩家受击反馈贴图
2. 敌人受击反馈贴图
3. 枪口火焰贴图
4. 屏幕受伤边缘贴图
5. 按钮状态贴图
6. HUD 装饰图标

---

## 详细资源清单

## 1. 玩家资源

### 1.1 玩家主体贴图

用途：

- 替换 `scene/player/Player.tscn` 中当前的方块占位外观
- 作为玩家在关卡中的主要视觉主体

最低需求：

- 1 张静态玩家贴图

推荐需求：

- 1 套四方向或八方向角色贴图
- 如做简单动画，可补充待机/移动帧

建议规格：

- 俯视角 2D 风格
- 单张尺寸可从 `32x32` 或 `48x48` 起步
- 轮廓要和敌人明显区分
- 朝向应便于表现鼠标瞄准

建议命名：

- `player_idle.png`
- `player_move_strip.png`

### 1.2 玩家受击反馈贴图

用途：

- 玩家受伤时闪红、闪白、描边或短暂叠加特效

最低需求：

- 可以暂时不做，先用代码调色替代

推荐需求：

- 1 张受击叠加贴图
- 或 1 组短帧受击闪烁效果

建议命名：

- `player_hit_overlay.png`

---

## 2. 敌人资源

### 2.1 敌人主体贴图

用途：

- 替换 `scene/enemy/Enemy.tscn` 中当前的红色方块占位

最低需求：

- 1 张基础敌人贴图

推荐需求：

- 1 套简单移动帧
- 与玩家有明显形状、配色和轮廓差异

建议规格：

- 与玩家保持相近尺寸，便于碰撞与视觉判断
- 颜色建议偏暖色或危险色

建议命名：

- `enemy_basic_idle.png`
- `enemy_basic_move_strip.png`

### 2.2 敌人受击反馈贴图

用途：

- 子弹命中敌人时提供更明显的反馈

最低需求：

- 可以先依赖命中闪光特效，不单独制作

推荐需求：

- 1 张白闪或红闪叠加贴图

建议命名：

- `enemy_hit_overlay.png`

### 2.3 敌人死亡消散特效贴图

用途：

- 敌人死亡时替代当前直接消失的表现

最低需求：

- 1 组 3 到 5 帧的小型消散或爆点序列

可选风格：

- 爆裂
- 烟雾
- 能量消散
- 碎片散开

建议命名：

- `enemy_death_strip.png`

---

## 3. 子弹与战斗特效资源

### 3.1 子弹贴图

用途：

- 替换 `scene/bullet/Bullet.tscn` 中当前的简易多边形

最低需求：

- 1 张小尺寸子弹贴图

建议规格：

- 细长、方向性明确
- 推荐尺寸 `8x8`、`12x12` 或 `16x16`
- 需要适配旋转朝向

建议命名：

- `bullet_basic.png`

### 3.2 命中闪光贴图

用途：

- 子弹命中敌人或墙体时显示 hit spark

最低需求：

- 1 组小型闪光贴图

推荐需求：

- 命中敌人和命中墙体共用一套
- 或分别制作两套不同视觉效果

建议规格：

- 尺寸小，持续时间短
- 对比度高，便于战斗中看清

建议命名：

- `hit_spark_strip.png`
- `hit_wall_spark_strip.png`

### 3.3 枪口火焰贴图

用途：

- 玩家射击时在 `Muzzle` 附近显示短促火焰特效

最低需求：

- 可以后补

推荐需求：

- 1 组 2 到 4 帧枪口火焰

建议命名：

- `muzzle_flash_strip.png`

---

## 4. 场景资源

## 4.1 地面平铺贴图

用途：

- 替换 `Level01` 当前代码绘制的网格背景
- 建立关卡的整体视觉基调

最低需求：

- 1 张可循环平铺的地板纹理

推荐风格方向：

- 草地与石砖地板

建议规格：

- 可平铺
- 推荐基础尺寸 `128x128` 或 `256x256`
- 明暗对比不要过高，避免干扰战斗识别
- 饱和度低于角色、敌人和战斗特效

建议命名：

- `floor_tile_stage2.png`

### 4.2 墙体贴图

用途：

- 替换关卡边界墙的纯色矩形表现

最低需求：

- 1 张墙体或边界块贴图

推荐需求：

- 1 套横向/纵向可复用贴图
- 或一张可平铺材质加一个统一边框做法

建议命名：

- `wall_tile_01.png`

### 4.3 障碍物贴图

用途：

- 替换 `CenterBlock`、`WideBlockLeft`、`WideBlockRight` 的纯色占位

最低需求：

- 2 类障碍物外观

推荐方向：

- 卡通木箱
- 奇幻石柱/遗迹方块

建议命名：

- `obstacle_block_large.png`
- `obstacle_block_wide.png`

---

## 5. UI 资源

## 5.1 HUD 面板贴图

用途：

- 提升 `Hud.tscn` 中分数面板、Game Over 面板和血条容器的观感

最低需求：

- 1 张通用 UI 面板底图

推荐需求：

- 一套风格统一的 panel / button / bar 资源

建议命名：

- `ui_panel_basic.png`

### 5.2 血条资源

用途：

- 替换当前 `ProgressBar` 纯色样式

最低需求：

- 1 张血条底板
- 1 张血条填充图

建议命名：

- `ui_health_bar_bg.png`
- `ui_health_bar_fill.png`

### 5.3 按钮资源

用途：

- 用于 `Restart` 按钮及后续菜单按钮

最低需求：

- 可以暂时保留默认 Godot 按钮

推荐需求：

- 按钮普通态
- 按钮悬停态
- 按钮按下态

建议命名：

- `ui_button_normal.png`
- `ui_button_hover.png`
- `ui_button_pressed.png`

### 5.4 HUD 装饰图标

用途：

- 提升分数与状态信息辨识度

推荐需求：

- 分数图标
- 击杀图标
- 心形或护甲图标

建议命名：

- `ui_icon_score.png`
- `ui_icon_kill.png`
- `ui_icon_health.png`

---

## 最小可演示资源包建议

如果当前目标只是让阶段 6 达到“可演示”而不是“正式美术完成”，建议先补齐以下 8 类：

1. `player_idle.png`
2. `enemy_basic_idle.png`
3. `bullet_basic.png`
4. `hit_spark_strip.png`
5. `enemy_death_strip.png`
6. `floor_tile_stage2.png`
7. `wall_tile_01.png`
8. `ui_panel_basic.png`

这套资源已经足够支撑：

1. 玩家、敌人、子弹不再是纯几何占位
2. 命中与死亡有最基础的视觉反馈
3. 场景背景和障碍不再过于简陋
4. HUD 与 Game Over 界面具备基本统一风格

---

## 建议目录结构

建议在项目中新增如下资源目录：

```text
res://asset/
  art/
    player/
    enemy/
    bullet/
    effects/
    level/
    ui/
```

对应示例：

```text
res://asset/art/player/player_idle.png
res://asset/art/enemy/enemy_basic_idle.png
res://asset/art/bullet/bullet_basic.png
res://asset/art/effects/hit_spark_strip.png
res://asset/art/effects/enemy_death_strip.png
res://asset/art/level/floor_tile_stage2.png
res://asset/art/level/wall_tile_01.png
res://asset/art/ui/ui_panel_basic.png
```

---

## 后续执行建议

推荐按下面顺序补资源：

1. 玩家、敌人、子弹主体贴图
2. 命中闪光与敌人死亡特效
3. 地面与墙体贴图
4. HUD 面板与血条资源
5. 枪口火焰、按钮状态、装饰图标等补强内容

如果后续准备使用 AI 生成素材，建议先统一一条美术方向，再批量出图。对于本项目，更适合以下风格之一：

- 明亮高饱和的可爱奇幻卡通风
- 俯视角
- 深色粗描边
- 角色与敌人轮廓清晰、颜色区分明确
- 场景与 UI 降低复杂度，服务战斗可读性

为保证后续 codex 批量生成稳定，补充以下约束：

- 第一轮资源优先静态单张，不依赖多方向动画
- 角色、敌人、子弹优先大色块和简化轮廓，少做复杂服装与小挂件
- 地面、墙体、障碍物细节从简，避免过强纹理和平铺接缝风险
- 命中、死亡、枪口等特效保持卡通化与高辨识度，不混入写实能量风格

避免不同资源来源风格差异过大，否则即使功能完整，演示观感也会比较散。
