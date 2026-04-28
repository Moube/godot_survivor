using Godot;
using System.Collections.Generic;

public partial class PlayerStats : Node
{
	[Signal]
	public delegate void StatsChangedEventHandler();

	[Export]
	public int BaseMaxHealth { get; private set; } = 5;

	[Export]
	public float BaseMoveSpeed { get; private set; } = 240.0f;

	[Export]
	public float BasePickupRange { get; private set; } = 48.0f;

	public int MaxHealth { get; private set; } = 5;

	public float MoveSpeed { get; private set; } = 240.0f;

	public float PickupRange { get; private set; } = 48.0f;

	public float WeaponDamageMultiplier { get; private set; } = 1.0f;

	public float WeaponCooldownMultiplier { get; private set; } = 1.0f;

	private readonly Dictionary<PlayerStatType, float> _flatBonuses = new();
	private readonly Dictionary<PlayerStatType, float> _multiplierBonuses = new();

	public override void _Ready()
	{
		Recalculate();
	}

	public void ResetBaseStats(int maxHealth, float moveSpeed, float pickupRange)
	{
		BaseMaxHealth = Mathf.Max(1, maxHealth);
		BaseMoveSpeed = Mathf.Max(1.0f, moveSpeed);
		BasePickupRange = Mathf.Max(1.0f, pickupRange);
		ClearPassiveBonuses();
	}

	public void ApplyPassiveBonuses(IEnumerable<PassiveInventoryEntry> passives)
	{
		_flatBonuses.Clear();
		_multiplierBonuses.Clear();

		if (passives != null)
		{
			foreach (PassiveInventoryEntry passive in passives)
			{
				AddPassiveBonus(passive);
			}
		}

		Recalculate();
	}

	public void ClearPassiveBonuses()
	{
		_flatBonuses.Clear();
		_multiplierBonuses.Clear();
		Recalculate();
	}

	private void AddPassiveBonus(PassiveInventoryEntry passive)
	{
		if (passive?.Config is null || passive.Level <= 0)
		{
			return;
		}

		float value = passive.Config.ValuePerLevel * passive.Level;
		Dictionary<PlayerStatType, float> target = passive.Config.IsMultiplier ? _multiplierBonuses : _flatBonuses;
		target.TryGetValue(passive.Config.StatType, out float currentValue);
		target[passive.Config.StatType] = currentValue + value;
	}

	private void Recalculate()
	{
		MoveSpeed = Mathf.Max(1.0f, ApplyBonuses(PlayerStatType.MoveSpeed, BaseMoveSpeed));
		PickupRange = Mathf.Max(1.0f, ApplyBonuses(PlayerStatType.PickupRange, BasePickupRange));
		MaxHealth = Mathf.Max(1, Mathf.RoundToInt(ApplyBonuses(PlayerStatType.MaxHealth, BaseMaxHealth)));
		WeaponDamageMultiplier = Mathf.Max(0.1f, ApplyBonuses(PlayerStatType.WeaponDamageMultiplier, 1.0f));
		WeaponCooldownMultiplier = Mathf.Max(0.15f, ApplyBonuses(PlayerStatType.WeaponCooldownMultiplier, 1.0f));
		EmitSignal(SignalName.StatsChanged);
	}

	private float ApplyBonuses(PlayerStatType statType, float baseValue)
	{
		_flatBonuses.TryGetValue(statType, out float flatBonus);
		_multiplierBonuses.TryGetValue(statType, out float multiplierBonus);
		return baseValue + flatBonus + (baseValue * multiplierBonus);
	}
}
