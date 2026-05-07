using Godot;
using System.Collections.Generic;

public partial class SoulChainWeapon : Weapon2D
{
	[Export]
	public PackedScene ChainEffectScene { get; set; }

	[Export]
	public float TargetSearchRadius { get; set; } = 760.0f;

	[Export]
	public float JumpRadius { get; set; } = 178.0f;

	[Export]
	public float StepDelaySeconds { get; set; } = 0.09f;

	[Export]
	public float EffectLingerSeconds { get; set; } = 0.22f;

	[Export]
	public float EmptyTargetRetrySeconds { get; set; } = 0.24f;

	private readonly List<Node2D> _chainTargets = new();
	private readonly HashSet<ulong> _selectedTargetIds = new();
	private double _cooldownRemaining;
	private float _baseCooldownSeconds = 1.65f;
	private int _baseDamage = 2;
	private int _baseTargetCount = 3;
	private float _effectiveCooldownSeconds = 1.65f;
	private int _effectiveDamage = 2;
	private int _effectiveTargetCount = 3;
	private float _effectiveJumpRadius = 178.0f;

	protected override bool CanApplyConfig(WeaponConfig config)
	{
		return config.BehaviorType == WeaponBehaviorType.ChainDamage;
	}

	protected override void ApplyConfig(WeaponConfig config)
	{
		_baseCooldownSeconds = Mathf.Max(0.05f, config.FireCooldownSeconds);
		_baseDamage = Mathf.Max(1, config.Damage);
		_baseTargetCount = Mathf.Max(1, config.ProjectileCount);
		RefreshEffectiveStats();
	}

	protected override void RefreshEffectiveStats()
	{
		int levelIndex = Mathf.Max(0, WeaponLevel - 1);
		float damageMultiplier = PlayerStats?.WeaponDamageMultiplier ?? 1.0f;
		float cooldownMultiplier = PlayerStats?.WeaponCooldownMultiplier ?? 1.0f;

		_effectiveDamage = Mathf.Max(1, Mathf.RoundToInt((_baseDamage + levelIndex) * damageMultiplier));
		_effectiveTargetCount = Mathf.Max(1, _baseTargetCount + levelIndex / 2);
		_effectiveCooldownSeconds = Mathf.Max(
			0.15f,
			_baseCooldownSeconds * Mathf.Pow(0.92f, levelIndex) * cooldownMultiplier);
		_effectiveJumpRadius = Mathf.Max(32.0f, JumpRadius + levelIndex * 10.0f);
	}

	protected override void UpdateWeapon(double delta)
	{
		if (_cooldownRemaining > 0.0)
		{
			_cooldownRemaining -= delta;
			return;
		}

		if (!TrySpawnChain())
		{
			_cooldownRemaining = Mathf.Min(EmptyTargetRetrySeconds, _effectiveCooldownSeconds);
			return;
		}

		_cooldownRemaining = _effectiveCooldownSeconds;
	}

	private bool TrySpawnChain()
	{
		if (ChainEffectScene == null)
		{
			GD.PushWarning($"{Name} cannot create soul chain because ChainEffectScene is not assigned.");
			return false;
		}

		ResolveChainTargets();
		if (_chainTargets.Count == 0)
		{
			return false;
		}

		AudioManager.Instance?.PlaySoulChainSpell(this);
		Node instance = ChainEffectScene.Instantiate();
		if (instance is not SoulChainEffect effect)
		{
			GD.PushError("Soul chain ChainEffectScene must instantiate a SoulChainEffect.");
			instance.QueueFree();
			return false;
		}

		Node parent = WorldNodeUtilities.ResolveRuntimeVisualParent(this);
		parent.AddChild(effect);
		effect.GlobalPosition = GlobalPosition;
		effect.Initialize(GlobalPosition, _chainTargets, _effectiveDamage, StepDelaySeconds, EffectLingerSeconds);
		return true;
	}

	private void ResolveChainTargets()
	{
		_chainTargets.Clear();
		_selectedTargetIds.Clear();

		Node2D currentTarget = FindNearestEnemy(GlobalPosition, TargetSearchRadius);
		while (currentTarget != null && _chainTargets.Count < _effectiveTargetCount)
		{
			_chainTargets.Add(currentTarget);
			_selectedTargetIds.Add(currentTarget.GetInstanceId());
			currentTarget = FindNearestEnemy(currentTarget.GlobalPosition, _effectiveJumpRadius);
		}
	}

	private Node2D FindNearestEnemy(Vector2 origin, float radius)
	{
		SceneTree tree = GetTree();
		if (tree == null)
		{
			return null;
		}

		Node2D nearestEnemy = null;
		float nearestDistanceSquared = radius * radius;
		foreach (Node node in tree.GetNodesInGroup("enemy"))
		{
			if (node is not Node2D enemy || !IsInstanceValid(enemy))
			{
				continue;
			}

			if (_selectedTargetIds.Contains(enemy.GetInstanceId()))
			{
				continue;
			}

			CombatComponent combat = enemy.GetNodeOrNull<CombatComponent>("CombatComponent");
			if (combat?.IsDead != false)
			{
				continue;
			}

			float distanceSquared = origin.DistanceSquaredTo(enemy.GlobalPosition);
			if (distanceSquared > nearestDistanceSquared)
			{
				continue;
			}

			nearestEnemy = enemy;
			nearestDistanceSquared = distanceSquared;
		}

		return nearestEnemy;
	}
}
