using Godot;

public partial class Player : CharacterBody2D
{
	[Export]
	public float MoveSpeed { get; set; } = 240.0f;

	[Export]
	public PackedScene BulletScene { get; set; }

	[Export]
	public float FireCooldownSeconds { get; set; } = 0.15f;

	private Marker2D _muzzle;
	private double _fireCooldownRemaining;

	public override void _Ready()
	{
		AddToGroup("player");
		_muzzle = GetNode<Marker2D>("Muzzle");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Velocity = inputDirection * MoveSpeed;
		MoveAndSlide();

		AimTowardMouse();
		UpdateFireCooldown(delta);
		TryShoot();
	}

	private void AimTowardMouse()
	{
		Vector2 aimDirection = GetGlobalMousePosition() - GlobalPosition;
		if (aimDirection == Vector2.Zero)
		{
			return;
		}

		Rotation = aimDirection.Angle();
	}

	private void UpdateFireCooldown(double delta)
	{
		if (_fireCooldownRemaining > 0.0)
		{
			_fireCooldownRemaining -= delta;
		}
	}

	private void TryShoot()
	{
		if (!Input.IsActionPressed("shoot") || _fireCooldownRemaining > 0.0)
		{
			return;
		}

		if (BulletScene is null)
		{
			GD.PushWarning("BulletScene is not assigned on Player.");
			return;
		}

		Node bulletInstance = BulletScene.Instantiate();
		if (bulletInstance is not Bullet bullet)
		{
			GD.PushError("BulletScene must instantiate a Bullet.");
			bulletInstance.QueueFree();
			return;
		}

		Vector2 direction = GetAimDirection();
		bullet.GlobalPosition = _muzzle.GlobalPosition;
		bullet.Initialize(direction);
		GetTree().CurrentScene.AddChild(bullet);

		_fireCooldownRemaining = FireCooldownSeconds;
	}

	private Vector2 GetAimDirection()
	{
		Vector2 direction = GetGlobalMousePosition() - GlobalPosition;
		return direction == Vector2.Zero ? Vector2.Right.Rotated(Rotation) : direction.Normalized();
	}
}
