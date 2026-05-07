using Godot;
using System.Collections.Generic;

public partial class SpawnDirector : Node
{
	private const float RetryDelayWhenAtEnemyLimitSeconds = 0.35f;
	private const float EnemySpawnSeparationPadding = 4.0f;
	private const int SpawnPositionAttemptCount = 48;
	private const uint WorldPhysicsLayerMask = 1u << 0;

	private readonly RandomNumberGenerator _random = new();
	private readonly Dictionary<string, PackedScene> _enemySceneCache = new();

	private SpawnScheduleConfig _spawnSchedule;
	private Node2D _worldRoot;
	private Node2D _player;
	private Vector2 _spawnOrigin;
	private Vector2 _spawnAreaHalfExtents;
	private Rect2 _spawnBounds;
	private float _minSpawnDistanceFromPlayer;
	private float _despawnDistanceFromPlayer;
	private float _startupEnemySafeDurationRemaining;
	private double _spawnCooldownRemaining;
	private int _activeEntryIndex = -1;
	private bool _hasSpawnBounds;
	private bool _spawnAroundPlayer;
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

		DespawnEnemiesOutsidePlayerRange();
		ApplyStartupEnemySafety((float)delta);

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
		Initialize(
			spawnSchedule,
			worldRoot,
			player,
			spawnOrigin,
			spawnAreaHalfExtents,
			minSpawnDistanceFromPlayer,
			0.0f,
			new Rect2(),
			false,
			0.0f,
			0.0f);
	}

	public void Initialize(
		SpawnScheduleConfig spawnSchedule,
		Node2D worldRoot,
		Node2D player,
		Vector2 spawnOrigin,
		Vector2 spawnAreaHalfExtents,
		float minSpawnDistanceFromPlayer,
		float despawnDistanceFromPlayer,
		Rect2 spawnBounds,
		bool spawnAroundPlayer,
		float initialSpawnDelaySeconds,
		float startupEnemySafeDurationSeconds)
	{
		_spawnSchedule = spawnSchedule;
		_worldRoot = worldRoot;
		_player = player;
		_spawnOrigin = spawnOrigin;
		_spawnAreaHalfExtents = spawnAreaHalfExtents;
		_spawnBounds = spawnBounds;
		_hasSpawnBounds = spawnBounds.Size.X > 0.0f && spawnBounds.Size.Y > 0.0f;
		_minSpawnDistanceFromPlayer = Mathf.Max(0.0f, minSpawnDistanceFromPlayer);
		_despawnDistanceFromPlayer = Mathf.Max(0.0f, despawnDistanceFromPlayer);
		_startupEnemySafeDurationRemaining = Mathf.Max(0.0f, startupEnemySafeDurationSeconds);
		_spawnAroundPlayer = spawnAroundPlayer;
		_spawnCooldownRemaining = Mathf.Max(0.0f, initialSpawnDelaySeconds);
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
			bool isInitialEntrySelection = _activeEntryIndex < 0;
			_activeEntryIndex = selectedIndex;
			if (!isInitialEntrySelection)
			{
				_spawnCooldownRemaining = 0.0;
			}
		}

		return _spawnSchedule.Entries[selectedIndex];
	}

	private void SpawnBatch(SpawnScheduleEntryConfig entry)
	{
		int remainingSlots = entry.MaxEnemyCount - GetTree().GetNodesInGroup("enemy").Count;
		int spawnCount = Mathf.Min(entry.SpawnCount, Mathf.Max(0, remainingSlots));
		HashSet<string> selectedScenePaths = new();
		for (int i = 0; i < spawnCount; i++)
		{
			EnemyConfig enemyConfig = PickEnemyConfig(entry, selectedScenePaths) ?? PickEnemyConfig(entry);
			if (enemyConfig is null)
			{
				continue;
			}

			selectedScenePaths.Add(enemyConfig.ScenePath);
			SpawnEnemy(enemyConfig);
		}
	}

	private EnemyConfig PickEnemyConfig(SpawnScheduleEntryConfig entry, HashSet<string> excludedScenePaths = null)
	{
		int totalWeight = 0;
		foreach (SpawnEnemyWeightConfig enemyWeight in entry.EnemyWeights)
		{
			EnemyConfig enemyConfig = GameConfigManager.Instance?.GetEnemyConfig(enemyWeight.EnemyId);
			if (enemyConfig is null || excludedScenePaths?.Contains(enemyConfig.ScenePath) == true)
			{
				continue;
			}

			totalWeight += Mathf.Max(0, enemyWeight.Weight);
		}

		if (totalWeight <= 0)
		{
			if (excludedScenePaths is null)
			{
				GD.PushError("Spawn schedule entry has no positive enemy weights.");
			}

			return null;
		}

		int roll = _random.RandiRange(1, totalWeight);
		int accumulatedWeight = 0;
		foreach (SpawnEnemyWeightConfig enemyWeight in entry.EnemyWeights)
		{
			EnemyConfig enemyConfig = GameConfigManager.Instance?.GetEnemyConfig(enemyWeight.EnemyId);
			if (enemyConfig is null || excludedScenePaths?.Contains(enemyConfig.ScenePath) == true)
			{
				continue;
			}

			accumulatedWeight += Mathf.Max(0, enemyWeight.Weight);
			if (roll > accumulatedWeight)
			{
				continue;
			}

			return enemyConfig;
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

		if (!TryFindEnemySpawnPosition(enemyConfig, out Vector2 spawnPosition))
		{
			enemy.QueueFree();
			return;
		}

		enemy.Position = _worldRoot.ToLocal(spawnPosition);
		_worldRoot.AddChild(enemy);
		if (!IsEnemyOutsidePlayerSafeDistance(enemy.GlobalPosition))
		{
			enemy.QueueFree();
			return;
		}

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

	private bool TryFindEnemySpawnPosition(EnemyConfig enemyConfig, out Vector2 spawnPosition)
	{
		Vector2 spawnCenter = GetSpawnCenter();
		float collisionRadius = Mathf.Max(0.1f, enemyConfig?.CollisionRadius ?? 12.0f);

		for (int attempt = 0; attempt < SpawnPositionAttemptCount; attempt++)
		{
			Vector2 candidate = ClampToSpawnBounds(spawnCenter + GetPerimeterSpawnOffset(), collisionRadius);
			if (IsEnemySpawnPositionValid(candidate, collisionRadius))
			{
				spawnPosition = candidate;
				return true;
			}
		}

		foreach (Vector2 fallbackOffset in GetFallbackSpawnOffsets())
		{
			Vector2 candidate = ClampToSpawnBounds(spawnCenter + fallbackOffset, collisionRadius);
			if (IsEnemySpawnPositionValid(candidate, collisionRadius))
			{
				spawnPosition = candidate;
				return true;
			}
		}

		spawnPosition = Vector2.Zero;
		return false;
	}

	private IEnumerable<Vector2> GetFallbackSpawnOffsets()
	{
		float x = Mathf.Max(_spawnAreaHalfExtents.X, _minSpawnDistanceFromPlayer);
		float y = Mathf.Max(_spawnAreaHalfExtents.Y, _minSpawnDistanceFromPlayer);
		yield return new Vector2(x, 0.0f);
		yield return new Vector2(-x, 0.0f);
		yield return new Vector2(0.0f, y);
		yield return new Vector2(0.0f, -y);
		yield return new Vector2(x, y);
		yield return new Vector2(-x, y);
		yield return new Vector2(x, -y);
		yield return new Vector2(-x, -y);
	}

	private bool IsEnemySpawnPositionValid(Vector2 candidate, float collisionRadius)
	{
		if (!IsEnemyOutsidePlayerSafeDistance(candidate))
		{
			return false;
		}

		return IsEnemySpawnPositionClear(candidate, collisionRadius);
	}

	private bool IsEnemyOutsidePlayerSafeDistance(Vector2 position)
	{
		if (_player is null || !IsInstanceValid(_player))
		{
			return true;
		}

		float minDistance = Mathf.Max(0.0f, _minSpawnDistanceFromPlayer);
		return position.DistanceSquaredTo(_player.GlobalPosition) >= minDistance * minDistance;
	}

	private void DespawnEnemiesOutsidePlayerRange()
	{
		if (_despawnDistanceFromPlayer <= 0.0f || _player is null || !IsInstanceValid(_player))
		{
			return;
		}

		float despawnDistanceSquared = _despawnDistanceFromPlayer * _despawnDistanceFromPlayer;
		Vector2 playerPosition = _player.GlobalPosition;
		foreach (Node node in GetTree().GetNodesInGroup("enemy"))
		{
			if (node is not EnemyBase enemy || !IsInstanceValid(enemy))
			{
				continue;
			}

			if (enemy.GlobalPosition.DistanceSquaredTo(playerPosition) <= despawnDistanceSquared)
			{
				continue;
			}

			enemy.QueueFree();
		}
	}

	private void ApplyStartupEnemySafety(float delta)
	{
		if (_startupEnemySafeDurationRemaining <= 0.0f || _player is null || !IsInstanceValid(_player))
		{
			return;
		}

		_startupEnemySafeDurationRemaining -= Mathf.Max(0.0f, delta);
		foreach (Node node in GetTree().GetNodesInGroup("enemy"))
		{
			if (node is not EnemyBase enemy || !IsInstanceValid(enemy))
			{
				continue;
			}

			if (IsEnemyOutsidePlayerSafeDistance(enemy.GlobalPosition))
			{
				continue;
			}

			enemy.QueueFree();
		}
	}

	private Vector2 GetSpawnCenter()
	{
		return _spawnAroundPlayer && _player != null && IsInstanceValid(_player)
			? _player.GlobalPosition
			: _spawnOrigin;
	}

	private Vector2 ClampToSpawnBounds(Vector2 position, float clearance)
	{
		if (!_hasSpawnBounds)
		{
			return position;
		}

		float minX = _spawnBounds.Position.X + clearance;
		float maxX = _spawnBounds.End.X - clearance;
		float minY = _spawnBounds.Position.Y + clearance;
		float maxY = _spawnBounds.End.Y - clearance;

		if (minX > maxX)
		{
			float centerX = _spawnBounds.Position.X + _spawnBounds.Size.X * 0.5f;
			minX = centerX;
			maxX = centerX;
		}

		if (minY > maxY)
		{
			float centerY = _spawnBounds.Position.Y + _spawnBounds.Size.Y * 0.5f;
			minY = centerY;
			maxY = centerY;
		}

		return new Vector2(
			Mathf.Clamp(position.X, minX, maxX),
			Mathf.Clamp(position.Y, minY, maxY));
	}

	private bool IsEnemySpawnPositionClear(Vector2 candidate, float collisionRadius)
	{
		if (!IsEnemySpawnPositionClearOfWorld(candidate, collisionRadius))
		{
			return false;
		}

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

	private bool IsEnemySpawnPositionClearOfWorld(Vector2 candidate, float collisionRadius)
	{
		PhysicsDirectSpaceState2D spaceState = _worldRoot?.GetWorld2D()?.DirectSpaceState;
		if (spaceState is null)
		{
			return true;
		}

		CircleShape2D shape = new()
		{
			Radius = Mathf.Max(0.1f, collisionRadius),
		};
		PhysicsShapeQueryParameters2D query = new()
		{
			Shape = shape,
			Transform = new Transform2D(0.0f, candidate),
			CollisionMask = WorldPhysicsLayerMask,
			CollideWithBodies = true,
			CollideWithAreas = false,
		};

		return spaceState.IntersectShape(query, 1).Count == 0;
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
