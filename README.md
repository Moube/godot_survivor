# Arcane Survivor

Arcane Survivor 是一个基于 Godot 4.6.2 .NET 与 C# ，完全由AI主导开发的 2D 轻量幸存者玩法项目。
项目需求，代码实现，美术资源均由AI生成，人工负责验收功能/美术以及搜集一些免费音效。
用于个人验证与学习AI独立开发游戏。

## 开发环境

- Godot `4.6.2` .NET / Mono 版本
- .NET SDK `8.x`
- C#
- Codex

项目的 C# 工程使用 `Godot.NET.Sdk/4.6.2`，目标框架为 `net8.0`。

## 运行项目

1. 安装 Godot 4.6.2 .NET 版本和 .NET 8 SDK。
2. 使用 Godot 打开仓库根目录。
3. 在 Godot 编辑器中执行 C# Build。
4. 运行项目，入口场景由 `project.godot` 指向 `scene/main/Main.tscn`。

也可以在命令行先检查 C# 工程：

```powershell
dotnet build godot_survivor.sln
```

如果本机没有还原过 `Godot.NET.Sdk/4.6.2`，第一次构建需要能访问 NuGet。

## 项目结构

```text
asset/
  art/                 美术资源
  audio/               音效与音乐
  config/              TSV 配置数据
docs/                  开发计划、需求和推进记录
scene/
  main/                主菜单入口
  level/               关卡与关卡基类
  player/              玩家
  enemy/               敌人
  weapon/              武器运行时
  bullet/              投射物
  pickup/              拾取物
  ui/                  HUD、暂停、设置、升级 UI
script/
  common/              通用状态、设置、文本和工具
  config/              配置模型与加载入口
  gameplay/            经验、升级、武器/被动持有和刷怪逻辑
  audio/               音频管理
```

## 操作方式


| 操作    | 按键/方式           |
| ----- | --------------- |
| 移动    | `W` `A` `S` `D` |
| 瞄准    | 鼠标位置            |
| 暂停/继续 | `Esc`           |
| 菜单操作  | 鼠标点击            |

## Windows 导出

当前仓库包含 `Windows Desktop` 导出预设。正式导出前请确认本机已经安装 Godot 4.6.2 对应的 Export Templates。

1. 打开 `Project > Export`。
2. 选择 `Windows Desktop` 预设。
3. 确认 `Resources > Filters to export non-resource files/folders` 包含 `asset/config/*.tsv`。
4. 导出路径可使用 `build/windows/ArcaneSurvivor.exe`。
5. 点击 `Export Project`。
6. 发布正式版时不要勾选 `Export With Debug`。

`asset/config/*.tsv` 必须进入导出包，否则关卡、武器、刷怪和升级配置会在发布版中缺失。

