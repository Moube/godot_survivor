using Godot;

public partial class Player : CharacterBody2D
{
	[Signal]
	public delegate void DiedEventHandler();

	[Export]
	public float MoveSpeed { get; set; } = 240.0f;

	[Export]
	public float WalkAnimationFps { get; set; } = 5.0f;

	[Export]
	public float PickupRange { get; set; } = 48.0f;

	public bool IsDead => _isDead;

	public WeaponInventory WeaponInventory => _weaponInventory;

	public PassiveInventory PassiveInventory => _passiveInventory;

	public PlayerStats PlayerStats => _playerStats;

	private Sprite2D _sprite;
	private WeaponInventory _weaponInventory;
	private PassiveInventory _passiveInventory;
	private PlayerStats _playerStats;
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
		_playerStats = GetNode<PlayerStats>("PlayerStats");
		_weaponInventory = GetNode<WeaponInventory>("WeaponInventory");
		_passiveInventory = GetNode<PassiveInventory>("PassiveInventory");
		_combat = GetNode<CombatComponent>("CombatComponent");
		_combat.Died += OnDied;
		_playerStats.StatsChanged += OnPlayerStatsChanged;
		_weaponInventory.Initialize(_playerStats);
		_passiveInventory.Initialize(_playerStats);
	}

	public override void _ExitTree()
	{
		if (_playerStats != null)
		{
			_playerStats.StatsChanged -= OnPlayerStatsChanged;
		}
	}

	public void InitializeFromLevelConfig(LevelConfig levelConfig)
	{
		if (levelConfig is null)
		{
			GD.PushWarning("Player cannot initialize from a null level config.");
			return;
		}

		_playerStats.ResetBaseStats(
			levelConfig.InitialPlayerMaxHealth,
			levelConfig.InitialPlayerMoveSpeed,
			levelConfig.InitialPickupRange);
		OnPlayerStatsChanged();

		if (_combat != null)
		{
			_combat.ResetHealth();
			_isDead = false;
		}

		_passiveInventory?.ClearPassives();
		_weaponInventory?.ClearWeapons();
		if (!string.IsNullOrWhiteSpace(levelConfig.InitialWeaponId))
		{
			_weaponInventory?.AddWeapon(levelConfig.InitialWeaponId);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead)
		{
			Velocity = Vector2.Zero;
			_weaponInventory?.SetWeaponsEnabled(false);
			return;
		}

		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Velocity = inputDirection * MoveSpeed;
		MoveAndSlide();
		UpdateWalkAnimation(delta, inputDirection);
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

	private void OnDied()
	{
		_isDead = true;
		_weaponInventory?.SetWeaponsEnabled(false);
		EmitSignal(SignalName.Died);
	}

	private void OnPlayerStatsChanged()
	{
		if (_playerStats is null)
		{
			return;
		}

		MoveSpeed = _playerStats.MoveSpeed;
		PickupRange = _playerStats.PickupRange;

		if (_combat != null)
		{
			_combat.SetMaxHealth(_playerStats.MaxHealth, healAddedHealth: true);
			GameSession.Instance?.SetPlayerHealth(_combat.CurrentHealth, _combat.MaxHealth);
		}
	}
}
