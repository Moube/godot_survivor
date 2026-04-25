using Godot;

public partial class Player : CharacterBody2D
{
	[Signal]
	public delegate void DiedEventHandler();

	[Export]
	public float MoveSpeed { get; set; } = 240.0f;

	[Export]
	public PackedScene BulletScene { get; set; }

	[Export]
	public float FireCooldownSeconds { get; set; } = 0.15f;

	[Export]
	public float WalkAnimationFps { get; set; } = 5.0f;

	private Sprite2D _sprite;
	private Marker2D _muzzle;
	private CombatComponent _combat;
	private double _fireCooldownRemaining;
	private double _walkAnimationTime;
	private bool _isDead;
	private const float MuzzleDistance = 22.0f;
	private const int IdleFrame = 0;
	private const int SpriteSheetFrameCount = 3;
	private static readonly int[] WalkFrameSequence = { 1, 0, 2, 0 };

	public override void _Ready()
	{
		AddToGroup("player");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_sprite.Hframes = SpriteSheetFrameCount;
		_sprite.Frame = IdleFrame;
		_muzzle = GetNode<Marker2D>("Muzzle");
		_combat = GetNode<CombatComponent>("CombatComponent");
		_combat.Died += OnDied;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead)
		{
			Velocity = Vector2.Zero;
			return;
		}

		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Velocity = inputDirection * MoveSpeed;
		MoveAndSlide();
		UpdateWalkAnimation(delta, inputDirection);

		AimTowardMouse();
		UpdateFireCooldown(delta);
		TryShoot();
	}

	private void UpdateWalkAnimation(double delta, Vector2 inputDirection)
	{
		if (inputDirection == Vector2.Zero)
		{
			_walkAnimationTime = 0.0;
			_sprite.Frame = IdleFrame;
			return;
		}

		_walkAnimationTime += delta;
		int sequenceIndex = (int)(_walkAnimationTime * WalkAnimationFps) % WalkFrameSequence.Length;
		_sprite.Frame = WalkFrameSequence[sequenceIndex];

		if (!Mathf.IsZeroApprox(inputDirection.X))
		{
			_sprite.FlipH = inputDirection.X > 0.0f;
		}
	}

	private void AimTowardMouse()
	{
		Vector2 aimDirection = GetGlobalMousePosition() - GlobalPosition;
		if (aimDirection == Vector2.Zero)
		{
			return;
		}

		_muzzle.Position = aimDirection.Normalized() * MuzzleDistance;
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
		return direction == Vector2.Zero ? Vector2.Right : direction.Normalized();
	}

	private void OnDied()
	{
		_isDead = true;
		EmitSignal(SignalName.Died);
	}
}
