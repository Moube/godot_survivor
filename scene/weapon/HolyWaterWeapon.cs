using Godot;
using System.Collections.Generic;

public partial class HolyWaterWeapon : Weapon2D
{
	[Export]
	public PackedScene AreaScene { get; set; }

	[Export]
	public PackedScene FlaskDropScene { get; set; }

	[Export]
	public float AreaRadius { get; set; } = 48.0f;

	[Export]
	public float AreaDurationSeconds { get; set; } = 3.2f;

	[Export]
	public float DamageIntervalSeconds { get; set; } = 0.55f;

	[Export]
	public float TargetSearchRadius { get; set; } = 720.0f;

	[Export]
	public float FallbackSpawnDistance { get; set; } = 120.0f;

	[Export]
	public float MultiAreaOffsetRadius { get; set; } = 34.0f;

	private const float AimEpsilonSquared = 0.0001f;

	private readonly RandomNumberGenerator _random = new();
	private double _cooldownRemaining;
	private float _baseCooldownSeconds = 2.8f;
	private int _baseDamage = 1;
	private int _baseAreaCount = 1;
	private float _effectiveCooldownSeconds = 2.8f;
	private int _effectiveDamage = 1;
	private int _effectiveAreaCount = 1;
	private float _effectiveDurationSeconds = 3.2f;
	private Player _owningPlayer;

	public override void _Ready()
	{
		_random.Randomize();
		_owningPlayer = FindOwningPlayer();
		RefreshEffectiveStats();
	}

	protected override bool CanApplyConfig(WeaponConfig config)
	{
		return config.BehaviorType == WeaponBehaviorType.GroundArea;
	}

	protected override void ApplyConfig(WeaponConfig config)
	{
		_baseCooldownSeconds = Mathf.Max(0.05f, config.FireCooldownSeconds);
		_baseDamage = Mathf.Max(1, config.Damage);
		_baseAreaCount = Mathf.Max(1, config.ProjectileCount);
		RefreshEffectiveStats();
	}

	protected override void RefreshEffectiveStats()
	{
		int levelIndex = Mathf.Max(0, WeaponLevel - 1);
		float damageMultiplier = PlayerStats?.WeaponDamageMultiplier ?? 1.0f;
		float cooldownMultiplier = PlayerStats?.WeaponCooldownMultiplier ?? 1.0f;

		_effectiveDamage = Mathf.Max(1, Mathf.RoundToInt((_baseDamage + levelIndex) * damageMultiplier));
		_effectiveAreaCount = Mathf.Max(1, _baseAreaCount + (levelIndex / 3));
		_effectiveCooldownSeconds = Mathf.Max(
			0.1f,
			_baseCooldownSeconds * Mathf.Pow(0.92f, levelIndex) * cooldownMultiplier);
		_effectiveDurationSeconds = AreaDurationSeconds + levelIndex * 0.15f;
	}

	protected override void UpdateWeapon(double delta)
	{
		if (_cooldownRemaining > 0.0)
		{
			_cooldownRemaining -= delta;
			return;
		}

		SpawnFlaskDrops();
		_cooldownRemaining = _effectiveCooldownSeconds;
	}

	private void SpawnFlaskDrops()
	{
		if (AreaScene == null)
		{
			GD.PushWarning($"{Name} cannot spawn holy water because AreaScene is not assigned.");
			return;
		}

		if (FlaskDropScene == null)
		{
			GD.PushWarning($"{Name} cannot spawn holy water because FlaskDropScene is not assigned.");
			return;
		}

		List<Vector2> spawnPositions = ResolveSpawnPositions();
		foreach (Vector2 spawnPosition in spawnPositions)
		{
			SpawnFlaskDrop(spawnPosition);
		}
	}

	private List<Vector2> ResolveSpawnPositions()
	{
		List<Node2D> targets = FindEnemyTargets();
		List<Vector2> positions = new();

		int areaCount = Mathf.Max(1, _effectiveAreaCount);
		for (int i = 0; i < areaCount; i++)
		{
			if (i < targets.Count)
			{
				positions.Add(targets[i].GlobalPosition);
			}
			else
			{
				positions.Add(GetFallbackSpawnPosition(i, areaCount));
			}
		}

		return positions;
	}

	private List<Node2D> FindEnemyTargets()
	{
		List<Node2D> targets = new();
		SceneTree tree = GetTree();
		if (tree == null)
		{
			return targets;
		}

		Vector2 origin = GlobalPosition;
		float maxDistanceSquared = TargetSearchRadius * TargetSearchRadius;
		foreach (Node node in tree.GetNodesInGroup("enemy"))
		{
			if (node is not Node2D enemy || !IsInstanceValid(enemy))
			{
				continue;
			}

			CombatComponent combat = enemy.GetNodeOrNull<CombatComponent>("CombatComponent");
			if (combat?.IsDead == true)
			{
				continue;
			}

			float distanceSquared = origin.DistanceSquaredTo(enemy.GlobalPosition);
			if (distanceSquared > maxDistanceSquared)
			{
				continue;
			}

			targets.Add(enemy);
		}

		targets.Sort((left, right) =>
			origin.DistanceSquaredTo(left.GlobalPosition).CompareTo(origin.DistanceSquaredTo(right.GlobalPosition)));
		return targets;
	}

	private Vector2 GetFallbackSpawnPosition(int index, int totalCount)
	{
		Vector2 direction = _owningPlayer?.LastMoveDirection ?? Vector2.Right;
		if (direction.LengthSquared() <= AimEpsilonSquared)
		{
			direction = Vector2.Right;
		}

		Vector2 center = GlobalPosition + direction.Normalized() * FallbackSpawnDistance;
		if (totalCount <= 1)
		{
			return center;
		}

		float angle = Mathf.Tau * index / totalCount + _random.RandfRange(-0.15f, 0.15f);
		return center + Vector2.Right.Rotated(angle) * MultiAreaOffsetRadius;
	}

	private void SpawnFlaskDrop(Vector2 position)
	{
		Node instance = FlaskDropScene.Instantiate();
		if (instance is not HolyWaterFlaskDrop flaskDrop)
		{
			GD.PushError("Holy water FlaskDropScene must instantiate a HolyWaterFlaskDrop.");
			instance.QueueFree();
			return;
		}

		Node parent = WorldNodeUtilities.ResolveRuntimeVisualParent(this);
		parent.AddChild(flaskDrop);
		flaskDrop.GlobalPosition = position;
		flaskDrop.AreaScene = AreaScene;
		flaskDrop.Initialize(_effectiveDamage, AreaRadius, _effectiveDurationSeconds, DamageIntervalSeconds);
	}

	private Player FindOwningPlayer()
	{
		Node current = this;
		while (current != null)
		{
			if (current is Player player)
			{
				return player;
			}

			current = current.GetParent();
		}

		return null;
	}
}
