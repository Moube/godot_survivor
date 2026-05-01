using Godot;
using System.Collections.Generic;

public partial class SpawnDirector : Node
{
	private const float RetryDelayWhenAtEnemyLimitSeconds = 0.35f;
	private const float EnemySpawnSeparationPadding = 4.0f;

	private readonly RandomNumberGenerator _random = new();
	private readonly Dictionary<string, PackedScene> _enemySceneCache = new();

	private SpawnScheduleConfig _spawnSchedule;
	private Node2D _worldRoot;
	private Node2D _player;
	private Vector2 _spawnOrigin;
	private Vector2 _spawnAreaHalfExtents;
	private float _minSpawnDistanceFromPlayer;
	private double _spawnCooldownRemaining;
	private int _activeEntryIndex = -1;
	private bool _isRunning;

	public override void _Ready()
	{
		_random.Randomize();
	}

	public override void _Process(double delta)
	{
		if (!_isRunning || GameSession.Instance?.IsGameOver == true)
		{
			return;
		}

		if (_spawnSchedule is null || _worldRoot is null || _player is null || !IsInstanceValid(_player))
		{
			return;
		}

		SpawnScheduleEntryConfig activeEntry = GetActiveEntry(GameSession.Instance?.ElapsedRunTime ?? 0.0);
		if (activeEntry is null)
		{
			return;
		}

		if (_spawnCooldownRemaining > 0.0)
		{
			_spawnCooldownRemaining -= delta;
			return;
		}

		if (GetTree().GetNodesInGroup("enemy").Count >= activeEntry.MaxEnemyCount)
		{
			_spawnCooldownRemaining = RetryDelayWhenAtEnemyLimitSeconds;
			return;
		}

		SpawnBatch(activeEntry);
		_spawnCooldownRemaining = activeEntry.SpawnIntervalSeconds;
	}

	public void Initialize(
		SpawnScheduleConfig spawnSchedule,
		Node2D worldRoot,
		Node2D player,
		Vector2 spawnOrigin,
		Vector2 spawnAreaHalfExtents,
		float minSpawnDistanceFromPlayer)
	{
		_spawnSchedule = spawnSchedule;
		_worldRoot = worldRoot;
		_player = player;
		_spawnOrigin = spawnOrigin;
		_spawnAreaHalfExtents = spawnAreaHalfExtents;
		_minSpawnDistanceFromPlayer = Mathf.Max(0.0f, minSpawnDistanceFromPlayer);
		_spawnCooldownRemaining = 0.0;
		_activeEntryIndex = -1;
		_isRunning = _spawnSchedule is not null && _worldRoot is not null && _player is not null;

		if (_spawnSchedule is null)
		{
			GD.PushError("SpawnDirector cannot start because spawn schedule config is missing.");
		}
	}

	public void Stop()
	{
		_isRunning = false;
	}

	private SpawnScheduleEntryConfig GetActiveEntry(double elapsedRunTime)
	{
		if (_spawnSchedule.Entries.Count == 0)
		{
			return null;
		}

		int selectedIndex = 0;
		for (int i = 0; i < _spawnSchedule.Entries.Count; i++)
		{
			if (elapsedRunTime < _spawnSchedule.Entries[i].StartTimeSeconds)
			{
				break;
			}

			selectedIndex = i;
		}

		if (selectedIndex != _activeEntryIndex)
		{
			_activeEntryIndex = selectedIndex;
			_spawnCooldownRemaining = 0.0;
		}

		return _spawnSchedule.Entries[selectedIndex];
	}

	private void SpawnBatch(SpawnScheduleEntryConfig entry)
	{
		int remainingSlots = entry.MaxEnemyCount - GetTree().GetNodesInGroup("enemy").Count;
		int spawnCount = Mathf.Min(entry.SpawnCount, Mathf.Max(0, remainingSlots));
		for (int i = 0; i < spawnCount; i++)
		{
			EnemyConfig enemyConfig = PickEnemyConfig(entry);
			if (enemyConfig is null)
			{
				continue;
			}

			SpawnEnemy(enemyConfig);
		}
	}

	private EnemyConfig PickEnemyConfig(SpawnScheduleEntryConfig entry)
	{
		int totalWeight = 0;
		foreach (SpawnEnemyWeightConfig enemyWeight in entry.EnemyWeights)
		{
			totalWeight += Mathf.Max(0, enemyWeight.Weight);
		}

		if (totalWeight <= 0)
		{
			GD.PushError("Spawn schedule entry has no positive enemy weights.");
			return null;
		}

		int roll = _random.RandiRange(1, totalWeight);
		int accumulatedWeight = 0;
		foreach (SpawnEnemyWeightConfig enemyWeight in entry.EnemyWeights)
		{
			accumulatedWeight += Mathf.Max(0, enemyWeight.Weight);
			if (roll > accumulatedWeight)
			{
				continue;
			}

			return GameConfigManager.Instance?.GetEnemyConfig(enemyWeight.EnemyId);
		}

		return null;
	}

	private void SpawnEnemy(EnemyConfig enemyConfig)
	{
		PackedScene enemyScene = GetEnemyScene(enemyConfig);
		if (enemyScene is null)
		{
			return;
		}

		Node enemyInstance = enemyScene.Instantiate();
		if (enemyInstance is not EnemyBase enemy)
		{
			GD.PushError($"Enemy scene for '{enemyConfig.Id}' must instantiate an EnemyBase.");
			enemyInstance.QueueFree();
			return;
		}

		Vector2 spawnPosition = FindEnemySpawnPosition(enemyConfig);
		_worldRoot.AddChild(enemy);
		enemy.GlobalPosition = spawnPosition;
		enemy.ApplyConfig(enemyConfig);
	}

	private PackedScene GetEnemyScene(EnemyConfig enemyConfig)
	{
		if (_enemySceneCache.TryGetValue(enemyConfig.Id, out PackedScene cachedScene))
		{
			return cachedScene;
		}

		PackedScene scene = ResourceLoader.Load<PackedScene>(enemyConfig.ScenePath);
		if (scene is null)
		{
			GD.PushError($"Cannot load enemy scene for '{enemyConfig.Id}': {enemyConfig.ScenePath}");
			return null;
		}

		_enemySceneCache.Add(enemyConfig.Id, scene);
		return scene;
	}

	private Vector2 FindEnemySpawnPosition(EnemyConfig enemyConfig)
	{
		Vector2 fallback = _spawnOrigin + new Vector2(_spawnAreaHalfExtents.X, 0.0f);
		float collisionRadius = Mathf.Max(0.1f, enemyConfig?.CollisionRadius ?? 12.0f);

		for (int attempt = 0; attempt < 24; attempt++)
		{
			Vector2 candidate = _spawnOrigin + GetPerimeterSpawnOffset();
			if (candidate.DistanceTo(_player.GlobalPosition) >= _minSpawnDistanceFromPlayer
				&& IsEnemySpawnPositionClear(candidate, collisionRadius))
			{
				return candidate;
			}

			fallback = candidate;
		}

		return fallback;
	}

	private bool IsEnemySpawnPositionClear(Vector2 candidate, float collisionRadius)
	{
		foreach (Node node in GetTree().GetNodesInGroup("enemy"))
		{
			if (node is not EnemyBase enemy || !IsInstanceValid(enemy))
			{
				continue;
			}

			float minDistance = collisionRadius + Mathf.Max(0.1f, enemy.CollisionRadius) + EnemySpawnSeparationPadding;
			if (candidate.DistanceSquaredTo(enemy.GlobalPosition) < minDistance * minDistance)
			{
				return false;
			}
		}

		return true;
	}

	private Vector2 GetPerimeterSpawnOffset()
	{
		float horizontalX = _random.RandfRange(-_spawnAreaHalfExtents.X, _spawnAreaHalfExtents.X);
		float verticalY = _random.RandfRange(-_spawnAreaHalfExtents.Y, _spawnAreaHalfExtents.Y);

		return _random.RandiRange(0, 3) switch
		{
			0 => new Vector2(horizontalX, -_spawnAreaHalfExtents.Y),
			1 => new Vector2(horizontalX, _spawnAreaHalfExtents.Y),
			2 => new Vector2(-_spawnAreaHalfExtents.X, verticalY),
			_ => new Vector2(_spawnAreaHalfExtents.X, verticalY),
		};
	}
}
