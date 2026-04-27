using Godot;
using System.Collections.Generic;

public partial class WeaponInventory : Node2D
{
	[Export]
	public int MaxWeaponCount { get; set; } = 4;

	private readonly List<Weapon2D> _weapons = new();

	public IReadOnlyList<Weapon2D> Weapons => _weapons;

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

		if (!weapon.InitializeFromConfig(config, level))
		{
			weapon.QueueFree();
			return false;
		}

		AddChild(weapon);
		_weapons.Add(weapon);
		return true;
	}

	public void ClearWeapons()
	{
		foreach (Weapon2D weapon in _weapons)
		{
			if (IsInstanceValid(weapon))
			{
				weapon.QueueFree();
			}
		}

		_weapons.Clear();
	}

	public bool HasWeapon(string weaponId)
	{
		foreach (Weapon2D weapon in _weapons)
		{
			if (weapon.WeaponId == weaponId)
			{
				return true;
			}
		}

		return false;
	}

	public void SetWeaponsEnabled(bool enabled)
	{
		foreach (Weapon2D weapon in _weapons)
		{
			if (IsInstanceValid(weapon))
			{
				weapon.AutoFireEnabled = enabled;
			}
		}
	}
}
