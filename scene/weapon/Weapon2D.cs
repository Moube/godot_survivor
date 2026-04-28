using Godot;

public partial class Weapon2D : Node2D
{
	[Export]
	public bool AutoFireEnabled { get; set; } = true;

	public string WeaponId { get; private set; } = string.Empty;

	public int WeaponLevel { get; private set; } = 1;

	public WeaponBehaviorType BehaviorType { get; private set; } = WeaponBehaviorType.ProjectileEmitter;

	public WeaponConfig Config { get; private set; }

	protected PlayerStats PlayerStats { get; private set; }

	public void SetFireRequested(bool requested)
	{
		AutoFireEnabled = requested;
	}

	public bool InitializeFromConfig(WeaponConfig config, int level = 1, PlayerStats playerStats = null)
	{
		if (config is null)
		{
			GD.PushError($"{Name} cannot initialize from a null weapon config.");
			return false;
		}

		if (!CanApplyConfig(config))
		{
			GD.PushError($"{Name} cannot initialize weapon '{config.Id}' with behavior '{config.BehaviorType}'.");
			return false;
		}

		Config = config;
		WeaponId = config.Id;
		BehaviorType = config.BehaviorType;
		WeaponLevel = Mathf.Clamp(level, 1, Mathf.Max(1, config.MaxLevel));
		ApplyConfig(config);
		BindPlayerStats(playerStats);
		return true;
	}

	public void SetLevel(int level, WeaponConfig config = null)
	{
		if (config != null)
		{
			Config = config;
		}

		int maxLevel = Config?.MaxLevel ?? level;
		WeaponLevel = Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));

		if (Config != null)
		{
			ApplyConfig(Config);
		}
	}

	public bool TryUpgrade()
	{
		if (Config is null || WeaponLevel >= Mathf.Max(1, Config.MaxLevel))
		{
			return false;
		}

		SetLevel(WeaponLevel + 1);
		return true;
	}

	public void BindPlayerStats(PlayerStats playerStats)
	{
		if (PlayerStats != null)
		{
			PlayerStats.StatsChanged -= OnPlayerStatsChanged;
		}

		PlayerStats = playerStats;

		if (PlayerStats != null)
		{
			PlayerStats.StatsChanged += OnPlayerStatsChanged;
		}

		RefreshEffectiveStats();
	}

	public override void _ExitTree()
	{
		if (PlayerStats != null)
		{
			PlayerStats.StatsChanged -= OnPlayerStatsChanged;
			PlayerStats = null;
		}
	}

	protected virtual void ApplyConfig(WeaponConfig config)
	{
		RefreshEffectiveStats();
	}

	protected virtual bool CanApplyConfig(WeaponConfig config)
	{
		return true;
	}

	public virtual void SetInventorySlot(int slotIndex, int totalSlots)
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!AutoFireEnabled)
		{
			return;
		}

		UpdateWeapon(delta);
	}

	protected virtual void UpdateWeapon(double delta)
	{
	}

	protected virtual void RefreshEffectiveStats()
	{
	}

	private void OnPlayerStatsChanged()
	{
		RefreshEffectiveStats();
	}
}
