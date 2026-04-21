using Godot;

public partial class Level01 : Node2D
{
	[Export]
	public PackedScene PlayerScene { get; set; }

	[Export]
	public PackedScene EnemyScene { get; set; }

	[Export]
	public int GridSize { get; set; } = 64;

	[Export]
	public int GridExtent { get; set; } = 2048;

	[Export]
	public float EnemySpawnIntervalSeconds { get; set; } = 1.75f;

	[Export]
	public int MaxEnemyCount { get; set; } = 6;

	[Export]
	public float MinSpawnDistanceFromPlayer { get; set; } = 180.0f;

	[Export]
	public Vector2 SpawnAreaHalfExtents { get; set; } = new(420.0f, 260.0f);

	private readonly RandomNumberGenerator _random = new();

	private CharacterBody2D _player;
	private Node2D _enemiesRoot;
	private Timer _spawnTimer;

	public override void _Ready()
	{
		_random.Randomize();
		_enemiesRoot = GetNode<Node2D>("Enemies");
		_spawnTimer = GetNode<Timer>("SpawnTimer");
		_spawnTimer.Timeout += OnSpawnTimerTimeout;

		SpawnPlayer();
		ConfigureSpawnTimer();
		QueueRedraw();
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

		player.GlobalPosition = spawnPoint.GlobalPosition;
		AddChild(player);
		_player = player;

		if (player is Player playerNode)
		{
			playerNode.Died += OnPlayerDied;
		}
	}

	private void ConfigureSpawnTimer()
	{
		if (EnemyScene is null)
		{
			GD.PushWarning("EnemyScene is not assigned on Level01.");
			_spawnTimer.Stop();
			return;
		}

		_spawnTimer.WaitTime = EnemySpawnIntervalSeconds;
		_spawnTimer.Start();
		SpawnEnemy();
	}

	private void OnSpawnTimerTimeout()
	{
		SpawnEnemy();
	}

	private void SpawnEnemy()
	{
		if (GameSession.Instance?.IsGameOver == true || _player is null || !IsInstanceValid(_player))
		{
			return;
		}

		if (_enemiesRoot.GetChildCount() >= MaxEnemyCount)
		{
			return;
		}

		Node enemyInstance = EnemyScene.Instantiate();
		if (enemyInstance is not Enemy enemy)
		{
			GD.PushError("EnemyScene must instantiate an Enemy.");
			enemyInstance.QueueFree();
			return;
		}

		enemy.GlobalPosition = FindEnemySpawnPosition();
		_enemiesRoot.AddChild(enemy);
	}

	private Vector2 FindEnemySpawnPosition()
	{
		Vector2 fallback = GlobalPosition + new Vector2(SpawnAreaHalfExtents.X, 0.0f);

		for (int attempt = 0; attempt < 12; attempt++)
		{
			Vector2 candidate = GlobalPosition + GetPerimeterSpawnOffset();
			if (candidate.DistanceTo(_player.GlobalPosition) >= MinSpawnDistanceFromPlayer)
			{
				return candidate;
			}

			fallback = candidate;
		}

		return fallback;
	}

	private Vector2 GetPerimeterSpawnOffset()
	{
		float horizontalX = _random.RandfRange(-SpawnAreaHalfExtents.X, SpawnAreaHalfExtents.X);
		float verticalY = _random.RandfRange(-SpawnAreaHalfExtents.Y, SpawnAreaHalfExtents.Y);

		return _random.RandiRange(0, 3) switch
		{
			0 => new Vector2(horizontalX, -SpawnAreaHalfExtents.Y),
			1 => new Vector2(horizontalX, SpawnAreaHalfExtents.Y),
			2 => new Vector2(-SpawnAreaHalfExtents.X, verticalY),
			_ => new Vector2(SpawnAreaHalfExtents.X, verticalY),
		};
	}

	private void OnPlayerDied()
	{
		if (GameSession.Instance?.IsGameOver == true)
		{
			return;
		}

		_spawnTimer.Stop();
		GameSession.Instance?.TriggerGameOver();
	}

	public override void _Draw()
	{
		Color minorLineColor = new(0.18f, 0.22f, 0.28f, 1.0f);
		Color majorLineColor = new(0.25f, 0.31f, 0.39f, 1.0f);
		Color axisColor = new(0.88f, 0.45f, 0.29f, 1.0f);

		for (int x = -GridExtent; x <= GridExtent; x += GridSize)
		{
			Color lineColor = x == 0 ? axisColor : (x % (GridSize * 4) == 0 ? majorLineColor : minorLineColor);
			DrawLine(new Vector2(x, -GridExtent), new Vector2(x, GridExtent), lineColor, 2.0f);
		}

		for (int y = -GridExtent; y <= GridExtent; y += GridSize)
		{
			Color lineColor = y == 0 ? axisColor : (y % (GridSize * 4) == 0 ? majorLineColor : minorLineColor);
			DrawLine(new Vector2(-GridExtent, y), new Vector2(GridExtent, y), lineColor, 2.0f);
		}
	}
}
