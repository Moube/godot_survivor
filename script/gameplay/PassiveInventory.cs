using Godot;
using System.Collections.Generic;

public sealed class PassiveInventoryEntry
{
	public PassiveInventoryEntry(PassiveConfig config, int level)
	{
		Config = config;
		PassiveId = config?.Id ?? string.Empty;
		Level = level;
	}

	public string PassiveId { get; }

	public PassiveConfig Config { get; }

	public int Level { get; set; }

	public string DisplayName => string.IsNullOrWhiteSpace(Config?.DisplayName) ? PassiveId : Config.DisplayName;

	public int MaxLevel => Mathf.Max(1, Config?.MaxLevel ?? Level);
}

public partial class PassiveInventory : Node
{
	[Signal]
	public delegate void InventoryChangedEventHandler();

	[Export]
	public int MaxPassiveCount { get; set; } = 4;

	private readonly List<PassiveInventoryEntry> _passives = new();
	private PlayerStats _playerStats;

	public IReadOnlyList<PassiveInventoryEntry> Passives => _passives;

	public void Initialize(PlayerStats playerStats)
	{
		_playerStats = playerStats;
		RebuildPlayerStats();
	}

	public bool AddPassive(string passiveId, int level = 1)
	{
		PassiveConfig config = GameConfigManager.Instance?.GetPassiveConfig(passiveId);
		return AddPassive(config, level);
	}

	public bool AddPassive(PassiveConfig config, int level = 1)
	{
		if (config is null)
		{
			return false;
		}

		if (_passives.Count >= MaxPassiveCount)
		{
			GD.PushWarning($"Cannot add passive '{config.Id}' because inventory is full.");
			return false;
		}

		if (HasPassive(config.Id))
		{
			GD.PushWarning($"Cannot add duplicate passive '{config.Id}'.");
			return false;
		}

		int clampedLevel = Mathf.Clamp(level, 1, Mathf.Max(1, config.MaxLevel));
		_passives.Add(new PassiveInventoryEntry(config, clampedLevel));
		RebuildPlayerStats();
		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	public bool UpgradePassive(string passiveId)
	{
		PassiveInventoryEntry passive = FindPassive(passiveId);
		if (passive is null)
		{
			return false;
		}

		if (passive.Level >= passive.MaxLevel)
		{
			return false;
		}

		passive.Level++;
		RebuildPlayerStats();
		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	public void ClearPassives()
	{
		if (_passives.Count == 0)
		{
			RebuildPlayerStats();
			return;
		}

		_passives.Clear();
		RebuildPlayerStats();
		EmitSignal(SignalName.InventoryChanged);
	}

	public bool HasPassive(string passiveId)
	{
		return FindPassive(passiveId) != null;
	}

	public bool CanAddPassive(string passiveId)
	{
		return !string.IsNullOrWhiteSpace(passiveId)
			&& _passives.Count < MaxPassiveCount
			&& !HasPassive(passiveId)
			&& GameConfigManager.Instance?.GetPassiveConfig(passiveId) != null;
	}

	public bool CanUpgradePassive(string passiveId)
	{
		PassiveInventoryEntry passive = FindPassive(passiveId);
		return passive != null && passive.Level < passive.MaxLevel;
	}

	private PassiveInventoryEntry FindPassive(string passiveId)
	{
		foreach (PassiveInventoryEntry passive in _passives)
		{
			if (passive.PassiveId == passiveId)
			{
				return passive;
			}
		}

		return null;
	}

	private void RebuildPlayerStats()
	{
		_playerStats?.ApplyPassiveBonuses(_passives);
	}
}
