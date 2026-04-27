using System.Collections.Generic;

public interface IGameConfig
{
	string Id { get; set; }
	string DisplayName { get; set; }
	string Description { get; set; }
}

public sealed class WeaponConfig : IGameConfig
{
	public string Id { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string ScenePath { get; set; } = string.Empty;
	public string BulletScenePath { get; set; } = string.Empty;
	public WeaponAimMode AimMode { get; set; } = WeaponAimMode.MouseDirection;
	public float FireCooldownSeconds { get; set; } = 0.5f;
	public int Damage { get; set; } = 1;
	public int ProjectileCount { get; set; } = 1;
	public int MaxLevel { get; set; } = 5;
}

public sealed class PassiveConfig : IGameConfig
{
	public string Id { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public PlayerStatType StatType { get; set; } = PlayerStatType.MoveSpeed;
	public float ValuePerLevel { get; set; }
	public int MaxLevel { get; set; } = 5;
	public bool IsMultiplier { get; set; }
}

public sealed class EnemyConfig : IGameConfig
{
	public string Id { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string ScenePath { get; set; } = string.Empty;
	public int MaxHealth { get; set; } = 1;
	public float MoveSpeed { get; set; } = 50.0f;
	public int ContactDamage { get; set; } = 1;
	public float ContactDamageCooldownSeconds { get; set; } = 0.75f;
	public int ExperienceValue { get; set; } = 1;
}

public sealed class LevelConfig : IGameConfig
{
	public string Id { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string ScenePath { get; set; } = string.Empty;
	public int SortOrder { get; set; }
	public int InitialPlayerMaxHealth { get; set; } = 5;
	public float InitialPlayerMoveSpeed { get; set; } = 240.0f;
	public float InitialPickupRange { get; set; } = 48.0f;
	public string InitialWeaponId { get; set; } = string.Empty;
	public string SpawnScheduleId { get; set; } = string.Empty;
	public string UpgradePoolId { get; set; } = string.Empty;
	public string ExperienceCurveId { get; set; } = string.Empty;
}

public sealed class SpawnScheduleConfig : IGameConfig
{
	public string Id { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public List<SpawnScheduleEntryConfig> Entries { get; set; } = new();
}

public sealed class SpawnScheduleEntryConfig
{
	public float StartTimeSeconds { get; set; }
	public float SpawnIntervalSeconds { get; set; } = 1.0f;
	public int SpawnCount { get; set; } = 1;
	public int MaxEnemyCount { get; set; } = 10;
	public List<SpawnEnemyWeightConfig> EnemyWeights { get; set; } = new();
}

public sealed class SpawnEnemyWeightConfig
{
	public string EnemyId { get; set; } = string.Empty;
	public int Weight { get; set; } = 1;
}

public sealed class UpgradePoolConfig : IGameConfig
{
	public string Id { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public List<UpgradeRewardConfig> Rewards { get; set; } = new();
}

public sealed class UpgradeRewardConfig
{
	public UpgradeRewardType Type { get; set; } = UpgradeRewardType.NewWeapon;
	public string ContentId { get; set; } = string.Empty;
	public int Weight { get; set; } = 1;
}

public sealed class ExperienceCurveConfig : IGameConfig
{
	public string Id { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public List<int> RequiredExperienceByLevel { get; set; } = new();
}
