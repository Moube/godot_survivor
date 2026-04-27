# 阶段0：环境与项目初始化说明

## 当前项目结构

当前仓库已经按分阶段 Shooter Demo 的方向整理出基础结构：

```text
res://
  asset/
  scene/
    main/
    player/
    bullet/
    enemy/
    level/
    ui/
  script/
    common/
```

这种结构将场景文件与对应脚本放在相邻位置，便于后续阶段持续扩展：

```text
res://scene/main/Main.tscn
res://scene/main/Main.cs
res://scene/player/Player.tscn
res://scene/player/Player.cs
```

后续新增的关卡场景也统一放入 `scene/level/`，例如：

```text
res://scene/level/Level01.tscn
res://scene/level/Level01.cs
```

## AI 已创建的脚本骨架

阶段0完成后，项目中具备以下基础 C# 脚本：

- `res://scene/main/Main.cs`
- `res://scene/player/Player.cs`
- `res://scene/bullet/Bullet.cs`
- `res://scene/enemy/Enemy.cs`

这些脚本在阶段0只承担“验证 Godot 能否正确加载并绑定 C#”的作用，不要求具备完整玩法逻辑。

## 需要在 Godot 中确认的项目设置

阶段0需要重点确认以下项目项：

1. `Project Settings > Application > Run > Main Scene`
   设为 `res://scene/main/Main.tscn`
2. `Project Settings > Dotnet > Assembly Name`
   保持为 `test_godot`
3. 如果 Godot 首次挂载 C# 脚本后提示生成工程文件
   执行 `Create C# Solution` 或按提示重新加载项目
4. `Main.tscn` 的场景根节点
   阶段0可以保持简单结构，后续再根据功能演进调整

## C# 初始化后会自动生成的文件

Godot 完成 C# 初始化后，项目中可能自动生成以下内容：

- `test_godot.csproj`
- `.godot/mono/`
- 相关缓存文件

这些文件属于正常生成结果，无需手动提前编写。

## 阶段0验收清单

1. 用 Godot 打开项目
2. 打开 `Main.tscn`
3. 确认 `Main` 已绑定 `Main.cs`
4. 运行项目
5. 确认没有 C# 编译错误

如果以上检查都通过，则阶段0可以视为完成。
