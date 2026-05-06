using Godot;
using System.Collections.Generic;

public partial class ThunderSigilWeapon : Weapon2D
{
	[Export]
	public PackedScene StrikeScene { get; set; }

	[Export]
	public float StrikeRadius { get; set; } = 42.0f;

	[Export]
	public float WarningDelaySeconds { get; set; } = 0.58f;

	[Export]
	public float ImpactDurationSeconds { get; set; } = 0.24f;

	[Export]
	public float TargetSearchRadius { get; set; } = 760.0f;

	[Export]
	public float FallbackSpawnDistance { get; set; } = 135.0f;

	[Export]
	public float MultiStrikeOffsetRadius { get; set; } = 42.0f;

	private const float AimEpsilonSquared = 0.0001f;

	private readonly RandomNumberGenerator _random = new();
	private double _cooldownRemaining;
	private float _baseCooldownSeconds = 2.05f;
	private int _baseDamage = 3;
	private int _baseStrikeCount = 1;
	private float _effectiveCooldownSeconds = 2.05f;
	private int _effectiveDamage = 3;
	private int _effectiveStrikeCount = 1;
	private float _effectiveStrikeRadius = 42.0f;
	private Player _owningPlayer;

	public override void _Ready()
	{
		_random.Randomize();
		_owningPlayer = FindOwningPlayer();
		RefreshEffectiveStats();
	}

	protected override bool CanApplyConfig(WeaponConfig config)
	{
		return config.BehaviorType == WeaponBehaviorType.TargetedStrike;
	}

	protected override void ApplyConfig(WeaponConfig config)
	{
		_baseCooldownSeconds = Mathf.Max(0.05f, config.FireCooldownSeconds);
		_baseDamage = Mathf.Max(1, config.Damage);
		_baseStrikeCount = Mathf.Max(1, config.ProjectileCount);
		RefreshEffectiveStats();
	}

	protected override void RefreshEffectiveStats()
	{
		int levelIndex = Mathf.Max(0, WeaponLevel - 1);
		float damageMultiplier = PlayerStats?.WeaponDamageMultiplier ?? 1.0f;
		float cooldownMultiplier = PlayerStats?.WeaponCooldownMultiplier ?? 1.0f;

		_effectiveDamage = Mathf.Max(1, Mathf.RoundToInt((_baseDamage + levelIndex) * damageMultiplier));
		_effectiveStrikeCount = Mathf.Max(1, _baseStrikeCount + levelIndex / 3);
		_effectiveCooldownSeconds = Mathf.Max(
			0.15f,
			_baseCooldownSeconds * Mathf.Pow(0.92f, levelIndex) * cooldownMultiplier);
		_effectiveStrikeRadius = StrikeRadius + levelIndex * 2.0f;
	}

	protected override void UpdateWeapon(double delta)
	{
		if (_cooldownRemaining > 0.0)
		{
			_cooldownRemaining -= delta;
			return;
		}

		SpawnStrikes();
		_cooldownRemaining = _effectiveCooldownSeconds;
	}

	private void SpawnStrikes()
	{
		if (StrikeScene == null)
		{
			GD.PushWarning($"{Name} cannot create thunder strikes because StrikeScene is not assigned.");
			return;
		}

		List<Vector2> positions = ResolveStrikePositions();
		foreach (Vector2 position in positions)
		{
			SpawnStrike(position);
		}
	}

	private List<Vector2> ResolveStrikePositions()
	{
		List<Node2D> targets = FindEnemyTargets();
		List<Vector2> positions = new();
		int strikeCount = Mathf.Max(1, _effectiveStrikeCount);

		for (int i = 0; i < strikeCount; i++)
		{
			if (i < targets.Count)
			{
				positions.Add(targets[i].GlobalPosition);
			}
			else
			{
				positions.Add(GetFallbackSpawnPosition(i, strikeCount));
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

		float angle = Mathf.Tau * index / totalCount + _random.RandfRange(-0.18f, 0.18f);
		return center + Vector2.Right.Rotated(angle) * MultiStrikeOffsetRadius;
	}

	private void SpawnStrike(Vector2 position)
	{
		Node instance = StrikeScene.Instantiate();
		if (instance is not ThunderStrike strike)
		{
			GD.PushError("StrikeScene must instantiate a ThunderStrike.");
			instance.QueueFree();
			return;
		}

		Node parent = WorldNodeUtilities.ResolveRuntimeVisualParent(this);
		parent.AddChild(strike);
		strike.GlobalPosition = position;
		strike.Initialize(_effectiveDamage, _effectiveStrikeRadius, WarningDelaySeconds, ImpactDurationSeconds);
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
