using Godot;

public partial class CombatComponent : Node
{
	[Signal]
	public delegate void DamagedEventHandler(int amount, int currentHealth, int maxHealth);

	[Signal]
	public delegate void DiedEventHandler();

	[Export]
	public int MaxHealth { get; set; } = 3;

	public int CurrentHealth { get; private set; }

	public bool IsDead { get; private set; }

	public override void _Ready()
	{
		ResetHealth();
	}

	public void ResetHealth()
	{
		MaxHealth = Mathf.Max(1, MaxHealth);
		CurrentHealth = MaxHealth;
		IsDead = false;
	}

	public void SetMaxHealth(int maxHealth, bool healAddedHealth)
	{
		int previousMaxHealth = MaxHealth;
		int previousCurrentHealth = CurrentHealth;

		MaxHealth = Mathf.Max(1, maxHealth);
		if (IsDead)
		{
			CurrentHealth = 0;
			return;
		}

		int healthDelta = healAddedHealth ? Mathf.Max(0, MaxHealth - previousMaxHealth) : 0;
		CurrentHealth = Mathf.Clamp(previousCurrentHealth + healthDelta, 1, MaxHealth);
	}

	public bool ApplyDamage(int amount)
	{
		if (IsDead || amount <= 0)
		{
			return false;
		}

		CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
		EmitSignal(SignalName.Damaged, amount, CurrentHealth, MaxHealth);

		if (CurrentHealth > 0)
		{
			return true;
		}

		IsDead = true;
		EmitSignal(SignalName.Died);
		return true;
	}
}
