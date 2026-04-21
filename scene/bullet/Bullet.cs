using Godot;

public partial class Bullet : Area2D
{
	[Export]
	public float Speed { get; set; } = 640.0f;

	[Export]
	public float LifetimeSeconds { get; set; } = 1.5f;

	[Export]
	public int Damage { get; set; } = 1;

	public Vector2 Direction { get; private set; } = Vector2.Right;

	private double _remainingLifetime;

	public void Initialize(Vector2 direction)
	{
		Direction = direction == Vector2.Zero ? Vector2.Right : direction.Normalized();
		Rotation = Direction.Angle();
	}

	public override void _Ready()
	{
		_remainingLifetime = LifetimeSeconds;
		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		GlobalPosition += Direction * Speed * (float)delta;

		_remainingLifetime -= delta;
		if (_remainingLifetime <= 0.0)
		{
			QueueFree();
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body == this)
		{
			return;
		}

		CombatComponent targetCombat = body.GetNodeOrNull<CombatComponent>("CombatComponent");
		targetCombat?.ApplyDamage(Damage);
		QueueFree();
	}
}
