using Godot;

public abstract partial class SurvivorLevelBase : PausableLevelBase
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

	[Export]
	public float EnemyDespawnDistanceFromPlayer { get; set; } = 0.0f;

	[Export]
	public bool SpawnEnemiesAroundPlayer { get; set; } = false;

	[Export]
	public float InitialSpawnDelaySeconds { get; set; } = 0.0f;

	[Export]
	public float StartupEnemySafeDurationSeconds { get; set; } = 0.0f;

	protected Node2D WorldRoot => _worldRoot;

	protected CharacterBody2D PlayerBody => _player;

	protected Player PlayerNode => _playerNode;

	protected LevelConfig LevelConfig => _levelConfig;

	private Node2D _worldRoot;
	private CharacterBody2D _player;
	private CombatComponent _playerCombat;
	private SpawnDirector _spawnDirector;
	private UpgradeManager _upgradeManager;
	private LevelConfig _levelConfig;
	private Hud _hud;
	private Player _playerNode;

	public override void _EnterTree()
	{
		base._EnterTree();
		GameSession.Instance?.StartNewRun();
	}

	public override void _Ready()
	{
		_worldRoot = GetNode<Node2D>("World");
		ConfigureWorldSorting();
		_hud = GetNodeOrNull<Hud>("Hud");
		ClearExistingEnemies();
		ConfigureLevelBeforePlayerSpawn();
		LoadLevelConfig();

		ExperienceController.Instance?.StartNewRun(_levelConfig?.ExperienceCurveId ?? string.Empty);
		SpawnPlayer();
		StartSpawnDirector();
		StartUpgradeManager();
		QueueRedraw();
		AudioManager.Instance?.PlayGameplayMusic();
	}

	public override void _ExitTree()
	{
		AudioManager.Instance?.StopGameplayMusic();
	}

	public override void _Process(double delta)
	{
		GameSession.Instance?.AdvanceRunTime(delta);
	}

	protected virtual void ConfigureLevelBeforePlayerSpawn()
	{
	}

	protected virtual void OnPlayerSpawned(CharacterBody2D player)
	{
	}

	protected virtual Rect2 GetSpawnBounds()
	{
		return new Rect2();
	}

	protected virtual Vector2 GetSpawnOrigin()
	{
		return GlobalPosition;
	}

	protected override bool CanOpenPauseMenu()
	{
		return GameSession.Instance?.IsGameOver != true && !IsUpgradeChoiceVisible();
	}

	protected void AddYSortedWorldChild(Node2D child, Vector2 globalPosition)
	{
		if (child is null)
		{
			return;
		}

		if (_worldRoot is null)
		{
			GD.PushWarning($"{Name} cannot add '{child.Name}' because World is missing.");
			return;
		}

		_worldRoot.AddChild(child);
		child.GlobalPosition = globalPosition;
	}

	protected void ClearYSortedWorldChildrenInGroup(string groupName)
	{
		if (string.IsNullOrWhiteSpace(groupName))
		{
			return;
		}

		foreach (Node node in GetTree().GetNodesInGroup(groupName))
		{
			if (node.GetParent() != _worldRoot)
			{
				continue;
			}

			node.QueueFree();
		}
	}

	private void ConfigureWorldSorting()
	{
		if (_worldRoot is null)
		{
			return;
		}

		_worldRoot.YSortEnabled = true;
	}

	private void LoadLevelConfig()
	{
		string selectedLevelConfigId = GameSession.Instance?.SelectedLevelConfigId;
		string levelConfigId = string.IsNullOrWhiteSpace(selectedLevelConfigId) ? LevelConfigId : selectedLevelConfigId;
		_levelConfig = GameConfigManager.Instance?.GetLevelConfig(levelConfigId);
	}

	private void SpawnPlayer()
	{
		if (PlayerScene is null)
		{
			GD.PushError($"{Name} PlayerScene is not assigned.");
			return;
		}

		Marker2D spawnPoint = GetNode<Marker2D>("PlayerSpawn");
		Node playerInstance = PlayerScene.Instantiate();

		if (playerInstance is not CharacterBody2D player)
		{
			GD.PushError($"{Name} PlayerScene must instantiate a CharacterBody2D.");
			playerInstance.QueueFree();
			return;
		}

		AddYSortedWorldChild(player, spawnPoint.GlobalPosition);
		_player = player;
		OnPlayerSpawned(player);

		if (player is Player playerNode)
		{
			_playerNode = playerNode;
			playerNode.InitializeFromLevelConfig(_levelConfig);
			playerNode.Died += OnPlayerDied;
			_hud?.BindPlayer(playerNode);
		}

		_playerCombat = player.GetNodeOrNull<CombatComponent>("CombatComponent");
		if (_playerCombat is null)
		{
			GD.PushError($"{Name} player is missing CombatComponent.");
			return;
		}

		_playerCombat.Damaged += OnPlayerDamaged;
		GameSession.Instance?.SetPlayerHealth(_playerCombat.CurrentHealth, _playerCombat.MaxHealth);
	}

	private void StartSpawnDirector()
	{
		if (_player is null || !IsInstanceValid(_player))
		{
			GD.PushError($"{Name} cannot start SpawnDirector because player is missing.");
			return;
		}

		if (_levelConfig is null)
		{
			GD.PushError($"{Name} cannot start SpawnDirector because level config is missing.");
			return;
		}

		SpawnScheduleConfig spawnSchedule = GameConfigManager.Instance?.GetSpawnScheduleConfig(_levelConfig.SpawnScheduleId);
		if (spawnSchedule is null)
		{
			GD.PushError($"{Name} cannot find spawn schedule '{_levelConfig.SpawnScheduleId}'.");
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
			GetSpawnOrigin(),
			SpawnAreaHalfExtents,
			MinSpawnDistanceFromPlayer,
			EnemyDespawnDistanceFromPlayer,
			GetSpawnBounds(),
			SpawnEnemiesAroundPlayer,
			InitialSpawnDelaySeconds,
			StartupEnemySafeDurationSeconds);
	}

	private void StartUpgradeManager()
	{
		if (_playerNode is null || !IsInstanceValid(_playerNode))
		{
			GD.PushError($"{Name} cannot start UpgradeManager because player is missing.");
			return;
		}

		if (_levelConfig is null)
		{
			GD.PushError($"{Name} cannot start UpgradeManager because level config is missing.");
			return;
		}

		if (_hud is null)
		{
			GD.PushError($"{Name} cannot start UpgradeManager because HUD is missing.");
			return;
		}

		_upgradeManager = new UpgradeManager
		{
			Name = "UpgradeManager",
			ProcessMode = ProcessModeEnum.Always,
		};
		AddChild(_upgradeManager);
		_upgradeManager.Initialize(_levelConfig, _playerNode, _hud);
	}

	private void OnPlayerDied()
	{
		if (GameSession.Instance?.IsGameOver == true)
		{
			return;
		}

		_spawnDirector?.Stop();
		ClosePauseMenuWithoutUnpausing();
		GameSession.Instance?.TriggerGameOver();
	}

	private void OnPlayerDamaged(int amount, int currentHealth, int maxHealth)
	{
		GameSession.Instance?.SetPlayerHealth(currentHealth, maxHealth);
	}

	private void ClearExistingEnemies()
	{
		foreach (Node node in GetTree().GetNodesInGroup("enemy"))
		{
			node.QueueFree();
		}
	}

	private bool IsUpgradeChoiceVisible()
	{
		Control upgradeChoicePanel = GetNodeOrNull<Control>("Hud/UpgradeChoicePanel");
		return upgradeChoicePanel?.Visible == true;
	}

	public override void _Draw()
	{
	}
}
