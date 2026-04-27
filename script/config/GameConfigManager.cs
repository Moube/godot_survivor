using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public partial class GameConfigManager : Node
{
	private const string TableDelimiter = "\t";
	private const string WeaponConfigPath = "res://asset/config/weapons.tsv";
	private const string PassiveConfigPath = "res://asset/config/passives.tsv";
	private const string EnemyConfigPath = "res://asset/config/enemies.tsv";
	private const string LevelConfigPath = "res://asset/config/levels.tsv";
	private const string SpawnScheduleConfigPath = "res://asset/config/spawn_schedules.tsv";
	private const string SpawnScheduleEntryConfigPath = "res://asset/config/spawn_schedule_entries.tsv";
	private const string SpawnEnemyWeightConfigPath = "res://asset/config/spawn_enemy_weights.tsv";
	private const string UpgradePoolConfigPath = "res://asset/config/upgrade_pools.tsv";
	private const string UpgradeRewardConfigPath = "res://asset/config/upgrade_rewards.tsv";
	private const string ExperienceCurveConfigPath = "res://asset/config/experience_curves.tsv";
	private const string ExperienceCurveLevelConfigPath = "res://asset/config/experience_curve_levels.tsv";

	private readonly Dictionary<string, WeaponConfig> _weaponConfigs = new(StringComparer.Ordinal);
	private readonly Dictionary<string, PassiveConfig> _passiveConfigs = new(StringComparer.Ordinal);
	private readonly Dictionary<string, EnemyConfig> _enemyConfigs = new(StringComparer.Ordinal);
	private readonly Dictionary<string, LevelConfig> _levelConfigs = new(StringComparer.Ordinal);
	private readonly Dictionary<string, SpawnScheduleConfig> _spawnScheduleConfigs = new(StringComparer.Ordinal);
	private readonly Dictionary<string, UpgradePoolConfig> _upgradePoolConfigs = new(StringComparer.Ordinal);
	private readonly Dictionary<string, ExperienceCurveConfig> _experienceCurveConfigs = new(StringComparer.Ordinal);
	private readonly List<LevelConfig> _sortedLevelConfigs = new();

	public static GameConfigManager Instance { get; private set; }

	public bool IsLoaded { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		ReloadConfigs();
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void ReloadConfigs()
	{
		ClearCaches();

		RegisterConfigs(LoadWeaponConfigs(), _weaponConfigs, "weapon");
		RegisterConfigs(LoadPassiveConfigs(), _passiveConfigs, "passive");
		RegisterConfigs(LoadEnemyConfigs(), _enemyConfigs, "enemy");
		RegisterConfigs(LoadLevelConfigs(), _levelConfigs, "level");
		RegisterConfigs(LoadSpawnScheduleConfigs(), _spawnScheduleConfigs, "spawn schedule");
		RegisterConfigs(LoadUpgradePoolConfigs(), _upgradePoolConfigs, "upgrade pool");
		RegisterConfigs(LoadExperienceCurveConfigs(), _experienceCurveConfigs, "experience curve");

		_sortedLevelConfigs.AddRange(_levelConfigs.Values.OrderBy(config => config.SortOrder).ThenBy(config => config.Id));
		ValidateConfigs();
		IsLoaded = true;
	}

	public WeaponConfig GetWeaponConfig(string id)
	{
		return GetById(_weaponConfigs, id, "weapon");
	}

	public PassiveConfig GetPassiveConfig(string id)
	{
		return GetById(_passiveConfigs, id, "passive");
	}

	public EnemyConfig GetEnemyConfig(string id)
	{
		return GetById(_enemyConfigs, id, "enemy");
	}

	public LevelConfig GetLevelConfig(string id)
	{
		return GetById(_levelConfigs, id, "level");
	}

	public SpawnScheduleConfig GetSpawnScheduleConfig(string id)
	{
		return GetById(_spawnScheduleConfigs, id, "spawn schedule");
	}

	public UpgradePoolConfig GetUpgradePoolConfig(string id)
	{
		return GetById(_upgradePoolConfigs, id, "upgrade pool");
	}

	public ExperienceCurveConfig GetExperienceCurveConfig(string id)
	{
		return GetById(_experienceCurveConfigs, id, "experience curve");
	}

	public IReadOnlyList<LevelConfig> GetAllLevelConfigs()
	{
		return _sortedLevelConfigs.AsReadOnly();
	}

	private void ClearCaches()
	{
		_weaponConfigs.Clear();
		_passiveConfigs.Clear();
		_enemyConfigs.Clear();
		_levelConfigs.Clear();
		_spawnScheduleConfigs.Clear();
		_upgradePoolConfigs.Clear();
		_experienceCurveConfigs.Clear();
		_sortedLevelConfigs.Clear();
		IsLoaded = false;
	}

	private static List<WeaponConfig> LoadWeaponConfigs()
	{
		List<WeaponConfig> configs = new();
		foreach (CsvRow row in ReadCsvRows(WeaponConfigPath))
		{
			string context = BuildContext("weapon", row);
			configs.Add(new WeaponConfig
			{
				Id = GetString(row, "id", context),
				DisplayName = GetString(row, "display_name", context),
				Description = GetString(row, "description", context, required: false),
				ScenePath = GetString(row, "scene_path", context),
				BehaviorType = GetEnum(row, "behavior_type", WeaponBehaviorType.ProjectileEmitter, context),
				BulletScenePath = GetString(row, "bullet_scene_path", context),
				ProjectileFireMode = GetEnum(row, "projectile_fire_mode", ProjectileFireMode.MouseDirection, context),
				FireCooldownSeconds = GetFloat(row, "fire_cooldown_seconds", 0.5f, context),
				Damage = GetInt(row, "damage", 1, context),
				ProjectileCount = GetInt(row, "projectile_count", 1, context),
				MaxLevel = GetInt(row, "max_level", 5, context),
			});
		}

		return configs;
	}

	private static List<PassiveConfig> LoadPassiveConfigs()
	{
		List<PassiveConfig> configs = new();
		foreach (CsvRow row in ReadCsvRows(PassiveConfigPath))
		{
			string context = BuildContext("passive", row);
			configs.Add(new PassiveConfig
			{
				Id = GetString(row, "id", context),
				DisplayName = GetString(row, "display_name", context),
				Description = GetString(row, "description", context, required: false),
				StatType = GetEnum(row, "stat_type", PlayerStatType.MoveSpeed, context),
				ValuePerLevel = GetFloat(row, "value_per_level", 0.0f, context),
				MaxLevel = GetInt(row, "max_level", 5, context),
				IsMultiplier = GetBool(row, "is_multiplier", false, context),
			});
		}

		return configs;
	}

	private static List<EnemyConfig> LoadEnemyConfigs()
	{
		List<EnemyConfig> configs = new();
		foreach (CsvRow row in ReadCsvRows(EnemyConfigPath))
		{
			string context = BuildContext("enemy", row);
			configs.Add(new EnemyConfig
			{
				Id = GetString(row, "id", context),
				DisplayName = GetString(row, "display_name", context),
				Description = GetString(row, "description", context, required: false),
				ScenePath = GetString(row, "scene_path", context),
				MaxHealth = GetInt(row, "max_health", 1, context),
				MoveSpeed = GetFloat(row, "move_speed", 50.0f, context),
				ContactDamage = GetInt(row, "contact_damage", 1, context),
				ContactDamageCooldownSeconds = GetFloat(row, "contact_damage_cooldown_seconds", 0.75f, context),
				ExperienceValue = GetInt(row, "experience_value", 1, context),
				VisualScale = GetFloat(row, "visual_scale", 1.0f, context),
			});
		}

		return configs;
	}

	private static List<LevelConfig> LoadLevelConfigs()
	{
		List<LevelConfig> configs = new();
		foreach (CsvRow row in ReadCsvRows(LevelConfigPath))
		{
			string context = BuildContext("level", row);
			configs.Add(new LevelConfig
			{
				Id = GetString(row, "id", context),
				DisplayName = GetString(row, "display_name", context),
				Description = GetString(row, "description", context, required: false),
				ScenePath = GetString(row, "scene_path", context),
				SortOrder = GetInt(row, "sort_order", 0, context),
				InitialPlayerMaxHealth = GetInt(row, "initial_player_max_health", 5, context),
				InitialPlayerMoveSpeed = GetFloat(row, "initial_player_move_speed", 240.0f, context),
				InitialPickupRange = GetFloat(row, "initial_pickup_range", 48.0f, context),
				InitialWeaponId = GetString(row, "initial_weapon_id", context),
				SpawnScheduleId = GetString(row, "spawn_schedule_id", context),
				UpgradePoolId = GetString(row, "upgrade_pool_id", context),
				ExperienceCurveId = GetString(row, "experience_curve_id", context),
			});
		}

		return configs;
	}

	private static List<SpawnScheduleConfig> LoadSpawnScheduleConfigs()
	{
		Dictionary<string, SpawnScheduleConfig> schedules = new(StringComparer.Ordinal);
		Dictionary<string, SpawnScheduleEntryConfig> entriesById = new(StringComparer.Ordinal);

		foreach (CsvRow row in ReadCsvRows(SpawnScheduleConfigPath))
		{
			string context = BuildContext("spawn schedule", row);
			string id = GetString(row, "id", context);
			if (string.IsNullOrEmpty(id))
			{
				continue;
			}

			if (schedules.ContainsKey(id))
			{
				GD.PushError($"Duplicate spawn schedule config ID before registration: {id}");
				continue;
			}

			schedules.Add(id, new SpawnScheduleConfig
			{
				Id = id,
				DisplayName = GetString(row, "display_name", context),
				Description = GetString(row, "description", context, required: false),
			});
		}

		foreach (CsvRow row in ReadCsvRows(SpawnScheduleEntryConfigPath))
		{
			string context = BuildContext("spawn schedule entry", row);
			string entryId = GetString(row, "id", context);
			string scheduleId = GetString(row, "spawn_schedule_id", context);
			if (string.IsNullOrEmpty(entryId) || string.IsNullOrEmpty(scheduleId))
			{
				continue;
			}

			if (!schedules.TryGetValue(scheduleId, out SpawnScheduleConfig schedule))
			{
				GD.PushError($"Spawn schedule entry '{entryId}' references missing schedule '{scheduleId}'.");
				continue;
			}

			if (entriesById.ContainsKey(entryId))
			{
				GD.PushError($"Duplicate spawn schedule entry ID: {entryId}");
				continue;
			}

			SpawnScheduleEntryConfig entry = new()
			{
				StartTimeSeconds = GetFloat(row, "start_time_seconds", 0.0f, context),
				SpawnIntervalSeconds = GetFloat(row, "spawn_interval_seconds", 1.0f, context),
				SpawnCount = GetInt(row, "spawn_count", 1, context),
				MaxEnemyCount = GetInt(row, "max_enemy_count", 10, context),
			};
			schedule.Entries.Add(entry);
			entriesById.Add(entryId, entry);
		}

		foreach (CsvRow row in ReadCsvRows(SpawnEnemyWeightConfigPath))
		{
			string context = BuildContext("spawn enemy weight", row);
			string entryId = GetString(row, "spawn_schedule_entry_id", context);
			if (string.IsNullOrEmpty(entryId))
			{
				continue;
			}

			if (!entriesById.TryGetValue(entryId, out SpawnScheduleEntryConfig entry))
			{
				GD.PushError($"Spawn enemy weight references missing entry '{entryId}'.");
				continue;
			}

			entry.EnemyWeights.Add(new SpawnEnemyWeightConfig
			{
				EnemyId = GetString(row, "enemy_id", context),
				Weight = GetInt(row, "weight", 1, context),
			});
		}

		foreach (SpawnScheduleConfig schedule in schedules.Values)
		{
			schedule.Entries.Sort((left, right) => left.StartTimeSeconds.CompareTo(right.StartTimeSeconds));
		}

		return schedules.Values.ToList();
	}

	private static List<UpgradePoolConfig> LoadUpgradePoolConfigs()
	{
		Dictionary<string, UpgradePoolConfig> pools = new(StringComparer.Ordinal);

		foreach (CsvRow row in ReadCsvRows(UpgradePoolConfigPath))
		{
			string context = BuildContext("upgrade pool", row);
			string id = GetString(row, "id", context);
			if (string.IsNullOrEmpty(id))
			{
				continue;
			}

			if (pools.ContainsKey(id))
			{
				GD.PushError($"Duplicate upgrade pool config ID before registration: {id}");
				continue;
			}

			pools.Add(id, new UpgradePoolConfig
			{
				Id = id,
				DisplayName = GetString(row, "display_name", context),
				Description = GetString(row, "description", context, required: false),
			});
		}

		foreach (CsvRow row in ReadCsvRows(UpgradeRewardConfigPath))
		{
			string context = BuildContext("upgrade reward", row);
			string poolId = GetString(row, "upgrade_pool_id", context);
			if (string.IsNullOrEmpty(poolId))
			{
				continue;
			}

			if (!pools.TryGetValue(poolId, out UpgradePoolConfig pool))
			{
				GD.PushError($"Upgrade reward references missing pool '{poolId}'.");
				continue;
			}

			pool.Rewards.Add(new UpgradeRewardConfig
			{
				Type = GetEnum(row, "type", UpgradeRewardType.NewWeapon, context),
				ContentId = GetString(row, "content_id", context),
				Weight = GetInt(row, "weight", 1, context),
			});
		}

		return pools.Values.ToList();
	}

	private static List<ExperienceCurveConfig> LoadExperienceCurveConfigs()
	{
		Dictionary<string, ExperienceCurveConfig> curves = new(StringComparer.Ordinal);
		Dictionary<string, List<(int Level, int RequiredExperience)>> levelsByCurve = new(StringComparer.Ordinal);

		foreach (CsvRow row in ReadCsvRows(ExperienceCurveConfigPath))
		{
			string context = BuildContext("experience curve", row);
			string id = GetString(row, "id", context);
			if (string.IsNullOrEmpty(id))
			{
				continue;
			}

			if (curves.ContainsKey(id))
			{
				GD.PushError($"Duplicate experience curve config ID before registration: {id}");
				continue;
			}

			curves.Add(id, new ExperienceCurveConfig
			{
				Id = id,
				DisplayName = GetString(row, "display_name", context),
				Description = GetString(row, "description", context, required: false),
			});
			levelsByCurve.Add(id, new List<(int Level, int RequiredExperience)>());
		}

		foreach (CsvRow row in ReadCsvRows(ExperienceCurveLevelConfigPath))
		{
			string context = BuildContext("experience curve level", row);
			string curveId = GetString(row, "experience_curve_id", context);
			if (string.IsNullOrEmpty(curveId))
			{
				continue;
			}

			if (!levelsByCurve.TryGetValue(curveId, out List<(int Level, int RequiredExperience)> levels))
			{
				GD.PushError($"Experience curve level references missing curve '{curveId}'.");
				continue;
			}

			levels.Add((
				GetInt(row, "level", 1, context),
				GetInt(row, "required_experience", 1, context)));
		}

		foreach ((string curveId, List<(int Level, int RequiredExperience)> levels) in levelsByCurve)
		{
			levels.Sort((left, right) => left.Level.CompareTo(right.Level));
			curves[curveId].RequiredExperienceByLevel.AddRange(levels.Select(level => level.RequiredExperience));
		}

		return curves.Values.ToList();
	}

	private static List<CsvRow> ReadCsvRows(string path)
	{
		List<CsvRow> rows = new();
		if (!FileAccess.FileExists(path))
		{
			GD.PushError($"Config table does not exist: {path}");
			return rows;
		}

		using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file is null)
		{
			GD.PushError($"Unable to open config table: {path}");
			return rows;
		}

		string[] headers = ReadHeader(file, path);
		if (headers.Length == 0)
		{
			return rows;
		}

		int rowNumber = 1;
		while (!file.EofReached())
		{
			rowNumber++;
			string[] values = file.GetCsvLine(TableDelimiter);
			if (IsEmptyCsvLine(values) || IsCommentCsvLine(values))
			{
				continue;
			}

			Dictionary<string, string> valuesByHeader = new(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < headers.Length; i++)
			{
				string value = i < values.Length ? values[i].Trim() : string.Empty;
				valuesByHeader[headers[i]] = value;
			}

			rows.Add(new CsvRow(path, rowNumber, valuesByHeader));
		}

		return rows;
	}

	private static string[] ReadHeader(FileAccess file, string path)
	{
		int rowNumber = 0;
		while (!file.EofReached())
		{
			rowNumber++;
			string[] headers = file.GetCsvLine(TableDelimiter);
			if (IsEmptyCsvLine(headers) || IsCommentCsvLine(headers))
			{
				continue;
			}

			for (int i = 0; i < headers.Length; i++)
			{
				headers[i] = NormalizeColumnName(headers[i]);
			}

			if (headers.Any(string.IsNullOrEmpty))
			{
				GD.PushError($"Config table {path} header row {rowNumber} contains an empty column name.");
				return Array.Empty<string>();
			}

			if (headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Length)
			{
				GD.PushError($"Config table {path} header row {rowNumber} contains duplicate column names.");
				return Array.Empty<string>();
			}

			return headers;
		}

		GD.PushError($"Config table {path} has no header row.");
		return Array.Empty<string>();
	}

	private static bool IsEmptyCsvLine(string[] values)
	{
		return values.Length == 0 || values.All(string.IsNullOrWhiteSpace);
	}

	private static bool IsCommentCsvLine(string[] values)
	{
		return values.Length > 0 && values[0].TrimStart().StartsWith("#", StringComparison.Ordinal);
	}

	private static string NormalizeColumnName(string value)
	{
		return value.Trim().TrimStart('\uFEFF');
	}

	private static string BuildContext(string label, CsvRow row)
	{
		string id = row.TryGetValue("id", out string value) ? value : string.Empty;
		if (string.IsNullOrEmpty(id))
		{
			id = row.TryGetValue("content_id", out value) ? value : string.Empty;
		}

		if (string.IsNullOrEmpty(id))
		{
			id = row.TryGetValue("spawn_schedule_entry_id", out value) ? value : string.Empty;
		}

		return string.IsNullOrEmpty(id)
			? $"{label} at {row.Path}:{row.RowNumber}"
			: $"{label} '{id}' at {row.Path}:{row.RowNumber}";
	}

	private static string GetString(CsvRow row, string columnName, string context, bool required = true)
	{
		if (!row.TryGetValue(columnName, out string value))
		{
			GD.PushError($"{context} is missing required column '{columnName}'.");
			return string.Empty;
		}

		if (required && string.IsNullOrWhiteSpace(value))
		{
			GD.PushError($"{context} has empty value for '{columnName}'.");
		}

		return value.Trim();
	}

	private static int GetInt(CsvRow row, string columnName, int fallback, string context)
	{
		string value = GetString(row, columnName, context);
		if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
		{
			return parsed;
		}

		GD.PushError($"{context} has invalid integer for '{columnName}': {value}");
		return fallback;
	}

	private static float GetFloat(CsvRow row, string columnName, float fallback, string context)
	{
		string value = GetString(row, columnName, context);
		if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
		{
			return parsed;
		}

		GD.PushError($"{context} has invalid float for '{columnName}': {value}");
		return fallback;
	}

	private static bool GetBool(CsvRow row, string columnName, bool fallback, string context)
	{
		string value = GetString(row, columnName, context);
		if (bool.TryParse(value, out bool parsed))
		{
			return parsed;
		}

		if (value == "1")
		{
			return true;
		}

		if (value == "0")
		{
			return false;
		}

		GD.PushError($"{context} has invalid bool for '{columnName}': {value}");
		return fallback;
	}

	private static TEnum GetEnum<TEnum>(CsvRow row, string columnName, TEnum fallback, string context)
		where TEnum : struct, Enum
	{
		string value = GetString(row, columnName, context);
		if (Enum.TryParse(value, ignoreCase: true, out TEnum parsed))
		{
			return parsed;
		}

		GD.PushError($"{context} has invalid {typeof(TEnum).Name} for '{columnName}': {value}");
		return fallback;
	}

	private static void RegisterConfigs<TConfig>(
		IEnumerable<TConfig> configs,
		Dictionary<string, TConfig> target,
		string label)
		where TConfig : class, IGameConfig
	{
		foreach (TConfig config in configs)
		{
			if (config is null)
			{
				GD.PushError($"Found null {label} config.");
				continue;
			}

			config.Id = NormalizeId(config.Id);
			if (string.IsNullOrEmpty(config.Id))
			{
				GD.PushError($"Found {label} config with empty ID.");
				continue;
			}

			if (target.ContainsKey(config.Id))
			{
				GD.PushError($"Duplicate {label} config ID: {config.Id}");
				continue;
			}

			target.Add(config.Id, config);
		}
	}

	private static string NormalizeId(string id)
	{
		return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
	}

	private static TConfig GetById<TConfig>(
		IReadOnlyDictionary<string, TConfig> configs,
		string id,
		string label)
		where TConfig : class
	{
		string normalizedId = NormalizeId(id);
		if (string.IsNullOrEmpty(normalizedId))
		{
			GD.PushWarning($"Cannot get {label} config because ID is empty.");
			return null;
		}

		if (configs.TryGetValue(normalizedId, out TConfig config))
		{
			return config;
		}

		GD.PushWarning($"Missing {label} config: {normalizedId}");
		return null;
	}

	private void ValidateConfigs()
	{
		foreach (WeaponConfig weapon in _weaponConfigs.Values)
		{
			ValidateWeaponConfig(weapon);
		}

		foreach (PassiveConfig passive in _passiveConfigs.Values)
		{
			ValidatePassiveConfig(passive);
		}

		foreach (EnemyConfig enemy in _enemyConfigs.Values)
		{
			ValidateEnemyConfig(enemy);
		}

		foreach (ExperienceCurveConfig curve in _experienceCurveConfigs.Values)
		{
			ValidateExperienceCurveConfig(curve);
		}

		foreach (SpawnScheduleConfig spawnSchedule in _spawnScheduleConfigs.Values)
		{
			ValidateSpawnScheduleConfig(spawnSchedule);
		}

		foreach (UpgradePoolConfig upgradePool in _upgradePoolConfigs.Values)
		{
			ValidateUpgradePoolConfig(upgradePool);
		}

		foreach (LevelConfig level in _levelConfigs.Values)
		{
			ValidateLevelConfig(level);
		}
	}

	private static void ValidateWeaponConfig(WeaponConfig weapon)
	{
		ValidateScenePath(weapon.ScenePath, $"weapon '{weapon.Id}' scene");
		ValidatePositive(weapon.FireCooldownSeconds, $"weapon '{weapon.Id}' fire cooldown");
		ValidatePositive(weapon.Damage, $"weapon '{weapon.Id}' damage");
		ValidatePositive(weapon.MaxLevel, $"weapon '{weapon.Id}' max level");

		if (weapon.BehaviorType == WeaponBehaviorType.ProjectileEmitter)
		{
			ValidateScenePath(weapon.BulletScenePath, $"weapon '{weapon.Id}' bullet scene");
			ValidatePositive(weapon.ProjectileCount, $"weapon '{weapon.Id}' projectile count");
		}
	}

	private static void ValidatePassiveConfig(PassiveConfig passive)
	{
		ValidatePositive(passive.MaxLevel, $"passive '{passive.Id}' max level");
	}

	private static void ValidateEnemyConfig(EnemyConfig enemy)
	{
		ValidateScenePath(enemy.ScenePath, $"enemy '{enemy.Id}' scene");
		ValidatePositive(enemy.MaxHealth, $"enemy '{enemy.Id}' max health");
		ValidatePositive(enemy.MoveSpeed, $"enemy '{enemy.Id}' move speed");
		ValidatePositive(enemy.ContactDamage, $"enemy '{enemy.Id}' contact damage");
		ValidatePositive(enemy.ContactDamageCooldownSeconds, $"enemy '{enemy.Id}' contact damage cooldown");
		ValidatePositive(enemy.ExperienceValue, $"enemy '{enemy.Id}' experience value");
		ValidatePositive(enemy.VisualScale, $"enemy '{enemy.Id}' visual scale");
	}

	private static void ValidateExperienceCurveConfig(ExperienceCurveConfig curve)
	{
		if (curve.RequiredExperienceByLevel.Count == 0)
		{
			GD.PushError($"Experience curve '{curve.Id}' has no level requirements.");
			return;
		}

		for (int i = 0; i < curve.RequiredExperienceByLevel.Count; i++)
		{
			ValidatePositive(curve.RequiredExperienceByLevel[i], $"experience curve '{curve.Id}' level {i + 1} requirement");
		}
	}

	private void ValidateSpawnScheduleConfig(SpawnScheduleConfig spawnSchedule)
	{
		if (spawnSchedule.Entries.Count == 0)
		{
			GD.PushError($"Spawn schedule '{spawnSchedule.Id}' has no entries.");
			return;
		}

		float previousStartTime = -1.0f;
		for (int i = 0; i < spawnSchedule.Entries.Count; i++)
		{
			SpawnScheduleEntryConfig entry = spawnSchedule.Entries[i];
			if (entry.StartTimeSeconds < previousStartTime)
			{
				GD.PushError($"Spawn schedule '{spawnSchedule.Id}' entry {i} starts before the previous entry.");
			}

			previousStartTime = entry.StartTimeSeconds;
			ValidatePositive(entry.SpawnIntervalSeconds, $"spawn schedule '{spawnSchedule.Id}' entry {i} interval");
			ValidatePositive(entry.SpawnCount, $"spawn schedule '{spawnSchedule.Id}' entry {i} spawn count");
			ValidatePositive(entry.MaxEnemyCount, $"spawn schedule '{spawnSchedule.Id}' entry {i} max enemy count");

			if (entry.EnemyWeights.Count == 0)
			{
				GD.PushError($"Spawn schedule '{spawnSchedule.Id}' entry {i} has no enemy weights.");
				continue;
			}

			foreach (SpawnEnemyWeightConfig enemyWeight in entry.EnemyWeights)
			{
				ValidatePositive(enemyWeight.Weight, $"spawn schedule '{spawnSchedule.Id}' enemy weight '{enemyWeight.EnemyId}'");
				ValidateReferenceExists(_enemyConfigs, enemyWeight.EnemyId, $"spawn schedule '{spawnSchedule.Id}' enemy");
			}
		}
	}

	private void ValidateUpgradePoolConfig(UpgradePoolConfig upgradePool)
	{
		if (upgradePool.Rewards.Count == 0)
		{
			GD.PushError($"Upgrade pool '{upgradePool.Id}' has no rewards.");
			return;
		}

		foreach (UpgradeRewardConfig reward in upgradePool.Rewards)
		{
			ValidatePositive(reward.Weight, $"upgrade pool '{upgradePool.Id}' reward weight '{reward.ContentId}'");

			switch (reward.Type)
			{
				case UpgradeRewardType.NewWeapon:
				case UpgradeRewardType.WeaponUpgrade:
					ValidateReferenceExists(_weaponConfigs, reward.ContentId, $"upgrade pool '{upgradePool.Id}' weapon reward");
					break;

				case UpgradeRewardType.NewPassive:
				case UpgradeRewardType.PassiveUpgrade:
					ValidateReferenceExists(_passiveConfigs, reward.ContentId, $"upgrade pool '{upgradePool.Id}' passive reward");
					break;

				default:
					GD.PushError($"Upgrade pool '{upgradePool.Id}' has unsupported reward type: {reward.Type}");
					break;
			}
		}
	}

	private void ValidateLevelConfig(LevelConfig level)
	{
		ValidateScenePath(level.ScenePath, $"level '{level.Id}' scene");
		ValidatePositive(level.InitialPlayerMaxHealth, $"level '{level.Id}' initial player max health");
		ValidatePositive(level.InitialPlayerMoveSpeed, $"level '{level.Id}' initial player move speed");
		ValidatePositive(level.InitialPickupRange, $"level '{level.Id}' initial pickup range");
		ValidateReferenceExists(_weaponConfigs, level.InitialWeaponId, $"level '{level.Id}' initial weapon");
		ValidateReferenceExists(_spawnScheduleConfigs, level.SpawnScheduleId, $"level '{level.Id}' spawn schedule");
		ValidateReferenceExists(_upgradePoolConfigs, level.UpgradePoolId, $"level '{level.Id}' upgrade pool");
		ValidateReferenceExists(_experienceCurveConfigs, level.ExperienceCurveId, $"level '{level.Id}' experience curve");
	}

	private static void ValidateReferenceExists<TConfig>(
		IReadOnlyDictionary<string, TConfig> configs,
		string id,
		string context)
	{
		string normalizedId = NormalizeId(id);
		if (string.IsNullOrEmpty(normalizedId))
		{
			GD.PushError($"{context} reference is empty.");
			return;
		}

		if (!configs.ContainsKey(normalizedId))
		{
			GD.PushError($"{context} references missing config '{normalizedId}'.");
		}
	}

	private static void ValidateScenePath(string scenePath, string context)
	{
		if (string.IsNullOrWhiteSpace(scenePath))
		{
			GD.PushError($"{context} path is empty.");
			return;
		}

		if (!ResourceLoader.Exists(scenePath, "PackedScene"))
		{
			GD.PushError($"{context} path does not exist or is not a PackedScene: {scenePath}");
		}
	}

	private static void ValidatePositive(float value, string context)
	{
		if (value <= 0.0f)
		{
			GD.PushError($"{context} must be greater than 0.");
		}
	}

	private static void ValidatePositive(int value, string context)
	{
		if (value <= 0)
		{
			GD.PushError($"{context} must be greater than 0.");
		}
	}

	private readonly record struct CsvRow(
		string Path,
		int RowNumber,
		Dictionary<string, string> Values)
	{
		public bool TryGetValue(string columnName, out string value)
		{
			return Values.TryGetValue(columnName, out value);
		}
	}
}
