using Godot;

public partial class MonsterArtReviewLevel : Node2D
{
	private const string MoveTargetGroupName = "monster_review_move_target";
	private const string EnemyConfigId = "bat_preview";
	private const double DeathHitDelaySeconds = 0.65;
	private const double DeathCycleSeconds = 2.6;

	private Node2D _world;
	private CharacterBody2D _moveTarget;
	private EnemyBase _movingEnemy;
	private EnemyBase _deathEnemy;
	private EnemyConfig _enemyConfig;
	private double _elapsedSeconds;
	private double _deathElapsedSeconds;
	private bool _deathDamageApplied;

	public override void _Ready()
	{
		GameSession.Instance?.StartNewRun();
		_world = GetNode<Node2D>("World");
		_enemyConfig = GameConfigManager.Instance?.GetEnemyConfig(EnemyConfigId);

		CreateMoveTarget();
		SpawnMovingPreview();
		SpawnDeathPreview();
	}

	public override void _Process(double delta)
	{
		_elapsedSeconds += delta;
		UpdateMoveTarget();
		UpdateMovingPreview();
		UpdateDeathPreview(delta);
	}

	private void CreateMoveTarget()
	{
		_moveTarget = new CharacterBody2D
		{
			Name = "BatMoveTarget",
			CollisionLayer = 0,
			CollisionMask = 0,
		};
		_moveTarget.AddToGroup(MoveTargetGroupName);
		_world.AddChild(_moveTarget);
		UpdateMoveTarget();
	}

	private void UpdateMoveTarget()
	{
		if (_moveTarget is null || !IsInstanceValid(_moveTarget))
		{
			return;
		}

		float x = Mathf.Sin((float)_elapsedSeconds * 0.75f) * 260.0f;
		float y = -118.0f + Mathf.Cos((float)_elapsedSeconds * 0.5f) * 32.0f;
		_moveTarget.GlobalPosition = new Vector2(x, y);
	}

	private void UpdateMovingPreview()
	{
		if (_movingEnemy != null && IsInstanceValid(_movingEnemy))
		{
			return;
		}

		SpawnMovingPreview();
	}

	private void UpdateDeathPreview(double delta)
	{
		_deathElapsedSeconds += delta;
		if (!_deathDamageApplied && _deathElapsedSeconds >= DeathHitDelaySeconds)
		{
			ApplyDeathPreviewDamage();
			_deathDamageApplied = true;
		}

		if (_deathElapsedSeconds < DeathCycleSeconds)
		{
			return;
		}

		if (_deathEnemy != null && IsInstanceValid(_deathEnemy))
		{
			_deathEnemy.QueueFree();
		}

		ClearDroppedExperience();
		SpawnDeathPreview();
	}

	private void SpawnMovingPreview()
	{
		_movingEnemy = SpawnPreviewEnemy(new Vector2(-260.0f, -118.0f));
		if (_movingEnemy is null)
		{
			return;
		}

		_movingEnemy.TargetGroupName = MoveTargetGroupName;
	}

	private void SpawnDeathPreview()
	{
		_deathElapsedSeconds = 0.0;
		_deathDamageApplied = false;
		_deathEnemy = SpawnPreviewEnemy(new Vector2(0.0f, 116.0f));
		if (_deathEnemy is null)
		{
			return;
		}

		_deathEnemy.TargetGroupName = "monster_review_no_target";
		_deathEnemy.MoveSpeed = 0.1f;
	}

	private EnemyBase SpawnPreviewEnemy(Vector2 position)
	{
		if (_enemyConfig is null)
		{
			GD.PushError($"{Name} cannot spawn preview enemy because enemy config '{EnemyConfigId}' is missing.");
			return null;
		}

		PackedScene scene = ResourceLoader.Load<PackedScene>(_enemyConfig.ScenePath);
		if (scene is null)
		{
			GD.PushError($"{Name} cannot load enemy scene: {_enemyConfig.ScenePath}");
			return null;
		}

		Node instance = scene.Instantiate();
		if (instance is not EnemyBase enemy)
		{
			GD.PushError($"{Name} enemy preview scene must instantiate an EnemyBase.");
			instance.QueueFree();
			return null;
		}

		_world.AddChild(enemy);
		enemy.GlobalPosition = position;
		enemy.ApplyConfig(_enemyConfig);
		return enemy;
	}

	private void ApplyDeathPreviewDamage()
	{
		if (_deathEnemy is null || !IsInstanceValid(_deathEnemy))
		{
			return;
		}

		CombatComponent combat = _deathEnemy.GetNodeOrNull<CombatComponent>("CombatComponent");
		combat?.ApplyDamage(999);
	}

	private void ClearDroppedExperience()
	{
		if (_world is null)
		{
			return;
		}

		foreach (Node child in _world.GetChildren())
		{
			if (child is ExperienceGem)
			{
				child.QueueFree();
			}
		}
	}
}
