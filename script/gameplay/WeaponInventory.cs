using Godot;
using System.Collections.Generic;

public sealed class WeaponInventoryEntry
{
	public WeaponInventoryEntry(WeaponConfig config, Weapon2D runtimeInstance)
	{
		Config = config;
		RuntimeInstance = runtimeInstance;
		WeaponId = config?.Id ?? string.Empty;
	}

	public string WeaponId { get; }

	public WeaponConfig Config { get; }

	public Weapon2D RuntimeInstance { get; }

	public int Level => RuntimeInstance?.WeaponLevel ?? 1;

	public string DisplayName => GameText.ConfigName(
		"weapon",
		WeaponId,
		string.IsNullOrWhiteSpace(Config?.DisplayName) ? WeaponId : Config.DisplayName);

	public int MaxLevel => Mathf.Max(1, Config?.MaxLevel ?? Level);
}

public partial class WeaponInventory : Node2D
{
	[Signal]
	public delegate void InventoryChangedEventHandler();

	[Export]
	public int MaxWeaponCount { get; set; } = 4;

	private readonly List<WeaponInventoryEntry> _weapons = new();
	private readonly List<Weapon2D> _weaponInstances = new();
	private PlayerStats _playerStats;

	public IReadOnlyList<WeaponInventoryEntry> Weapons => _weapons;

	public IReadOnlyList<Weapon2D> WeaponInstances => _weaponInstances;

	public void Initialize(PlayerStats playerStats)
	{
		_playerStats = playerStats;
		foreach (WeaponInventoryEntry weapon in _weapons)
		{
			weapon.RuntimeInstance?.BindPlayerStats(_playerStats);
		}
	}

	public bool AddWeapon(string weaponId, int level = 1)
	{
		WeaponConfig config = GameConfigManager.Instance?.GetWeaponConfig(weaponId);
		return AddWeapon(config, level);
	}

	public bool AddWeapon(WeaponConfig config, int level = 1)
	{
		if (config is null)
		{
			return false;
		}

		if (_weapons.Count >= MaxWeaponCount)
		{
			GD.PushWarning($"Cannot add weapon '{config.Id}' because inventory is full.");
			return false;
		}

		if (HasWeapon(config.Id))
		{
			GD.PushWarning($"Cannot add duplicate weapon '{config.Id}'.");
			return false;
		}

		PackedScene weaponScene = ResourceLoader.Load<PackedScene>(config.ScenePath);
		if (weaponScene is null)
		{
			GD.PushError($"Cannot load weapon scene for '{config.Id}': {config.ScenePath}");
			return false;
		}

		Node instance = weaponScene.Instantiate();
		if (instance is not Weapon2D weapon)
		{
			GD.PushError($"Weapon scene for '{config.Id}' must instantiate a Weapon2D.");
			instance.QueueFree();
			return false;
		}

		if (!weapon.InitializeFromConfig(config, level, _playerStats))
		{
			weapon.QueueFree();
			return false;
		}

		AddChild(weapon);
		_weapons.Add(new WeaponInventoryEntry(config, weapon));
		_weaponInstances.Add(weapon);
		RefreshWeaponInventorySlots();
		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	public void ClearWeapons()
	{
		foreach (Weapon2D weapon in _weaponInstances)
		{
			if (IsInstanceValid(weapon))
			{
				weapon.QueueFree();
			}
		}

		_weapons.Clear();
		_weaponInstances.Clear();
		EmitSignal(SignalName.InventoryChanged);
	}

	public bool HasWeapon(string weaponId)
	{
		return FindWeapon(weaponId) != null;
	}

	public bool CanAddWeapon(string weaponId)
	{
		return !string.IsNullOrWhiteSpace(weaponId)
			&& _weapons.Count < MaxWeaponCount
			&& !HasWeapon(weaponId)
			&& GameConfigManager.Instance?.GetWeaponConfig(weaponId) != null;
	}

	public bool CanUpgradeWeapon(string weaponId)
	{
		WeaponInventoryEntry weapon = FindWeapon(weaponId);
		return weapon != null && weapon.Level < weapon.MaxLevel;
	}

	public bool UpgradeWeapon(string weaponId)
	{
		WeaponInventoryEntry weapon = FindWeapon(weaponId);
		if (weapon?.RuntimeInstance is null || !IsInstanceValid(weapon.RuntimeInstance))
		{
			return false;
		}

		if (!weapon.RuntimeInstance.TryUpgrade())
		{
			return false;
		}

		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	private WeaponInventoryEntry FindWeapon(string weaponId)
	{
		foreach (WeaponInventoryEntry weapon in _weapons)
		{
			if (weapon.WeaponId == weaponId)
			{
				return weapon;
			}
		}

		return null;
	}

	public void SetWeaponsEnabled(bool enabled)
	{
		foreach (Weapon2D weapon in _weaponInstances)
		{
			if (IsInstanceValid(weapon))
			{
				weapon.AutoFireEnabled = enabled;
			}
		}
	}

	private void RefreshWeaponInventorySlots()
	{
		int totalSlots = _weaponInstances.Count;
		for (int i = 0; i < _weaponInstances.Count; i++)
		{
			Weapon2D weapon = _weaponInstances[i];
			if (IsInstanceValid(weapon))
			{
				weapon.SetInventorySlot(i, totalSlots);
			}
		}
	}
}
