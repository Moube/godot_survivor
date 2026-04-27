using Godot;

public partial class Level01 : Node2D
{
	[Export]
	public PackedScene PlayerScene { get; set; }

	[Export]
	public string LevelConfigId { get; set; } = "level_01";

	[Export]
	public int GridSize { get; set; } = 64;

	[Export]
	public int GridExtent { get; set; } = 2048;

	[Export]
	public float MinSpawnDistanceFromPlayer { get; set; } = 180.0f;

	[Export]
	public Vector2 SpawnAreaHalfExtents { get; set; } = new(420.0f, 260.0f);

	private Node2D _worldRoot;
	private CharacterBody2D _player;
	private CombatComponent _playerCombat;
	private SpawnDirector _spawnDirector;
	private LevelConfig _levelConfig;

	public override void _Ready()
	{
		_worldRoot = GetNode<Node2D>("World");
		_levelConfig = GameConfigManager.Instance?.GetLevelConfig(LevelConfigId);

		GameSession.Instance?.StartNewRun();
		ExperienceController.Instance?.StartNewRun(_levelConfig?.ExperienceCurveId ?? string.Empty);
		SpawnPlayer();
		StartSpawnDirector();
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		GameSession.Instance?.AdvanceRunTime(delta);
	}

	private void SpawnPlayer()
	{
		if (PlayerScene is null)
		{
			GD.PushError("PlayerScene is not assigned on Level01.");
			return;
		}

		Marker2D spawnPoint = GetNode<Marker2D>("PlayerSpawn");
		Node playerInstance = PlayerScene.Instantiate();

		if (playerInstance is not CharacterBody2D player)
		{
			GD.PushError("PlayerScene must instantiate a CharacterBody2D.");
			playerInstance.QueueFree();
			return;
		}

		_worldRoot.AddChild(player);
		player.GlobalPosition = spawnPoint.GlobalPosition;
		_player = player;

		if (player is Player playerNode)
		{
			playerNode.InitializeFromLevelConfig(_levelConfig);
			playerNode.Died += OnPlayerDied;
		}

		_playerCombat = player.GetNodeOrNull<CombatComponent>("CombatComponent");
		if (_playerCombat is null)
		{
			GD.PushError("Player is missing CombatComponent.");
			return;
		}

		_playerCombat.Damaged += OnPlayerDamaged;
		GameSession.Instance?.SetPlayerHealth(_playerCombat.CurrentHealth, _playerCombat.MaxHealth);
	}

	private void StartSpawnDirector()
	{
		if (_player is null || !IsInstanceValid(_player))
		{
			GD.PushError("Level01 cannot start SpawnDirector because player is missing.");
			return;
		}

		if (_levelConfig is null)
		{
			GD.PushError("Level01 cannot start SpawnDirector because level config is missing.");
			return;
		}

		SpawnScheduleConfig spawnSchedule = GameConfigManager.Instance?.GetSpawnScheduleConfig(_levelConfig.SpawnScheduleId);
		if (spawnSchedule is null)
		{
			GD.PushError($"Level01 cannot find spawn schedule '{_levelConfig.SpawnScheduleId}'.");
			return;
		}

		_spawnDirector = new SpawnDirector
		{
			Name = "SpawnDirector",
		};
		AddChild(_spawnDirector);
		_spawnDirector.Initialize(
			spawnSchedule,
			_worldRoot,
			_player,
			GlobalPosition,
			SpawnAreaHalfExtents,
			MinSpawnDistanceFromPlayer);
	}

	private void OnPlayerDied()
	{
		if (GameSession.Instance?.IsGameOver == true)
		{
			return;
		}

		_spawnDirector?.Stop();
		GameSession.Instance?.TriggerGameOver();
	}

	private void OnPlayerDamaged(int amount, int currentHealth, int maxHealth)
	{
		GameSession.Instance?.SetPlayerHealth(currentHealth, maxHealth);
	}

	public override void _Draw()
	{
	}
}
