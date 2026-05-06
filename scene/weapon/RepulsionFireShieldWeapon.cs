using Godot;
using System.Collections.Generic;

public partial class RepulsionFireShieldWeapon : Weapon2D
{
	[Export]
	public PackedScene FireballScene { get; set; }

	[Export]
	public float BaseOrbitRadius { get; set; } = 72.0f;

	[Export]
	public float OrbitRadiusPerLevel { get; set; } = 4.0f;

	[Export]
	public float BaseAngularSpeedDegrees { get; set; } = 150.0f;

	[Export]
	public float AngularSpeedGrowthPerLevel { get; set; } = 0.07f;

	[Export]
	public float OrbitStartAngleDegrees { get; set; } = -90.0f;

	[Export]
	public float FireballCollisionRadius { get; set; } = 10.0f;

	[Export]
	public float FireballVisualScale { get; set; } = 0.40f;

	[Export]
	public float FireballVisualSpinDegreesPerSecond { get; set; } = 130.0f;

	private readonly List<OrbitingFireball> _fireballs = new();
	private readonly Dictionary<ulong, double> _hitCooldownsByBodyId = new();
	private readonly List<ulong> _cooldownBodyIds = new();
	private readonly List<ulong> _expiredBodyIds = new();
	private float _angleDegrees;
	private float _inventorySlotAngleOffset;
	private int _baseDamage = 1;
	private int _baseFireballCount = 2;
	private float _baseHitCooldownSeconds = 0.50f;
	private int _effectiveDamage = 1;
	private int _effectiveFireballCount = 2;
	private float _effectiveHitCooldownSeconds = 0.50f;
	private float _effectiveOrbitRadius = 72.0f;
	private float _effectiveAngularSpeedDegrees = 150.0f;
	private string _visualTexturePath = string.Empty;

	public override void _Ready()
	{
		_angleDegrees = OrbitStartAngleDegrees + _inventorySlotAngleOffset;
		EnsureFireballCount();
		ConfigureFireballs();
		UpdateFireballPositions();
	}

	protected override bool CanApplyConfig(WeaponConfig config)
	{
		return config.BehaviorType == WeaponBehaviorType.OrbitingObject;
	}

	protected override void ApplyConfig(WeaponConfig config)
	{
		_baseDamage = Mathf.Max(1, config.Damage);
		_baseFireballCount = Mathf.Max(1, config.ProjectileCount);
		_baseHitCooldownSeconds = Mathf.Max(0.05f, config.FireCooldownSeconds);
		_visualTexturePath = string.IsNullOrWhiteSpace(config.WeaponTexturePath)
			? config.IconTexturePath
			: config.WeaponTexturePath;
		RefreshEffectiveStats();
	}

	protected override void RefreshEffectiveStats()
	{
		int levelIndex = Mathf.Max(0, WeaponLevel - 1);
		float damageMultiplier = PlayerStats?.WeaponDamageMultiplier ?? 1.0f;
		float cooldownMultiplier = PlayerStats?.WeaponCooldownMultiplier ?? 1.0f;

		_effectiveDamage = Mathf.Max(1, Mathf.RoundToInt((_baseDamage + levelIndex) * damageMultiplier));
		_effectiveFireballCount = Mathf.Max(1, _baseFireballCount + levelIndex / 2);
		_effectiveHitCooldownSeconds = Mathf.Max(
			0.08f,
			_baseHitCooldownSeconds * Mathf.Pow(0.96f, levelIndex) * cooldownMultiplier);
		_effectiveOrbitRadius = Mathf.Max(16.0f, BaseOrbitRadius + OrbitRadiusPerLevel * levelIndex);
		_effectiveAngularSpeedDegrees = BaseAngularSpeedDegrees * (1.0f + AngularSpeedGrowthPerLevel * levelIndex);

		EnsureFireballCount();
		ConfigureFireballs();
		UpdateFireballPositions();
	}

	public override void SetInventorySlot(int slotIndex, int totalSlots)
	{
		_inventorySlotAngleOffset = totalSlots <= 0 ? 0.0f : 360.0f * slotIndex / totalSlots;
		UpdateFireballPositions();
	}

	protected override void UpdateWeapon(double delta)
	{
		_angleDegrees = Mathf.PosMod(
			_angleDegrees + _effectiveAngularSpeedDegrees * (float)delta,
			360.0f);
		UpdateHitCooldowns(delta);
		UpdateFireballPositions();
		ApplyContactDamage();
	}

	private void EnsureFireballCount()
	{
		if (FireballScene == null)
		{
			if (_effectiveFireballCount > 0)
			{
				GD.PushWarning($"{Name} cannot create orbiting fireballs because FireballScene is not assigned.");
			}
			return;
		}

		while (_fireballs.Count < _effectiveFireballCount)
		{
			Node instance = FireballScene.Instantiate();
			if (instance is not OrbitingFireball fireball)
			{
				GD.PushError("FireballScene must instantiate an OrbitingFireball.");
				instance.QueueFree();
				return;
			}

			fireball.Name = $"Fireball{_fireballs.Count + 1}";
			AddChild(fireball);
			_fireballs.Add(fireball);
		}

		while (_fireballs.Count > _effectiveFireballCount)
		{
			int lastIndex = _fireballs.Count - 1;
			OrbitingFireball fireball = _fireballs[lastIndex];
			_fireballs.RemoveAt(lastIndex);
			if (IsInstanceValid(fireball))
			{
				fireball.QueueFree();
			}
		}
	}

	private void ConfigureFireballs()
	{
		foreach (OrbitingFireball fireball in _fireballs)
		{
			if (!IsInstanceValid(fireball))
			{
				continue;
			}

			fireball.Configure(
				_visualTexturePath,
				FireballCollisionRadius,
				FireballVisualScale,
				FireballVisualSpinDegreesPerSecond);
		}
	}

	private void UpdateFireballPositions()
	{
		int count = _fireballs.Count;
		if (count == 0)
		{
			return;
		}

		for (int i = 0; i < count; i++)
		{
			OrbitingFireball fireball = _fireballs[i];
			if (!IsInstanceValid(fireball))
			{
				continue;
			}

			float angleRadians = Mathf.DegToRad(_angleDegrees + _inventorySlotAngleOffset + 360.0f * i / count);
			fireball.Position = Vector2.Right.Rotated(angleRadians) * _effectiveOrbitRadius;
		}
	}

	private void ApplyContactDamage()
	{
		foreach (OrbitingFireball fireball in _fireballs)
		{
			if (!IsInstanceValid(fireball))
			{
				continue;
			}

			foreach (Node2D body in fireball.GetOverlappingBodies())
			{
				TryDamageBody(body);
			}
		}
	}

	private void TryDamageBody(Node2D body)
	{
		if (body == null || !IsInstanceValid(body))
		{
			return;
		}

		ulong bodyId = body.GetInstanceId();
		if (_hitCooldownsByBodyId.TryGetValue(bodyId, out double cooldown) && cooldown > 0.0)
		{
			return;
		}

		CombatComponent combat = body.GetNodeOrNull<CombatComponent>("CombatComponent");
		if (combat?.IsDead != false)
		{
			return;
		}

		if (combat.ApplyDamage(_effectiveDamage))
		{
			_hitCooldownsByBodyId[bodyId] = _effectiveHitCooldownSeconds;
		}
	}

	private void UpdateHitCooldowns(double delta)
	{
		_cooldownBodyIds.Clear();
		_expiredBodyIds.Clear();
		foreach (ulong bodyId in _hitCooldownsByBodyId.Keys)
		{
			_cooldownBodyIds.Add(bodyId);
		}

		foreach (ulong bodyId in _cooldownBodyIds)
		{
			if (!_hitCooldownsByBodyId.TryGetValue(bodyId, out double cooldown))
			{
				continue;
			}

			double remaining = cooldown - delta;
			if (remaining <= 0.0)
			{
				_expiredBodyIds.Add(bodyId);
			}
			else
			{
				_hitCooldownsByBodyId[bodyId] = remaining;
			}
		}

		foreach (ulong bodyId in _expiredBodyIds)
		{
			_hitCooldownsByBodyId.Remove(bodyId);
		}
	}
}
