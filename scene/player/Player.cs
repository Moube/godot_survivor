using Godot;

public partial class Player : CharacterBody2D
{
	[Signal]
	public delegate void DiedEventHandler();

	[Export]
	public float MoveSpeed { get; set; } = 240.0f;

	[Export]
	public float WalkAnimationFps { get; set; } = 5.0f;

	private Sprite2D _sprite;
	private Weapon2D _weapon;
	private CombatComponent _combat;
	private double _walkAnimationTime;
	private bool _isDead;
	private const int IdleFrame = 0;
	private const int SpriteSheetFrameCount = 3;
	private static readonly int[] WalkFrameSequence = { 1, 0, 2, 0 };

	public override void _Ready()
	{
		AddToGroup("player");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_sprite.Hframes = SpriteSheetFrameCount;
		_sprite.Frame = IdleFrame;
		_weapon = GetNodeOrNull<Weapon2D>("MagicWandWeapon");
		_combat = GetNode<CombatComponent>("CombatComponent");
		_combat.Died += OnDied;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead)
		{
			Velocity = Vector2.Zero;
			_weapon?.SetFireRequested(false);
			return;
		}

		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Velocity = inputDirection * MoveSpeed;
		MoveAndSlide();
		UpdateWalkAnimation(delta, inputDirection);
		UpdateWeaponInput();
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

	private void UpdateWeaponInput()
	{
		_weapon?.SetFireRequested(Input.IsActionPressed("shoot"));
	}

	private void OnDied()
	{
		_isDead = true;
		_weapon?.SetFireRequested(false);
		EmitSignal(SignalName.Died);
	}
}
