using Godot;

public partial class Weapon2D : Node2D
{
	[Export]
	public bool AutoFireEnabled { get; set; } = true;

	public string WeaponId { get; private set; } = string.Empty;

	public int WeaponLevel { get; private set; } = 1;

	public WeaponBehaviorType BehaviorType { get; private set; } = WeaponBehaviorType.ProjectileEmitter;

	public void SetFireRequested(bool requested)
	{
		AutoFireEnabled = requested;
	}

	public bool InitializeFromConfig(WeaponConfig config, int level = 1)
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

		WeaponId = config.Id;
		BehaviorType = config.BehaviorType;
		WeaponLevel = Mathf.Clamp(level, 1, Mathf.Max(1, config.MaxLevel));
		ApplyConfig(config);
		return true;
	}

	public void SetLevel(int level, WeaponConfig config = null)
	{
		int maxLevel = config?.MaxLevel ?? level;
		WeaponLevel = Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));

		if (config != null)
		{
			ApplyConfig(config);
		}
	}

	protected virtual void ApplyConfig(WeaponConfig config)
	{
	}

	protected virtual bool CanApplyConfig(WeaponConfig config)
	{
		return true;
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
}
