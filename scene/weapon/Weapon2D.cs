using Godot;

public partial class Weapon2D : Node2D
{
	private const string DefaultDropShadowScenePath = "res://scene/common/DropShadow2D.tscn";
	private const string DropShadowAnchorNodeName = "DropShadowAnchor";
	private const int IdleFrame = 0;

	[Export]
	public PackedScene BulletScene { get; set; }

	[Export]
	public NodePath SpritePath { get; set; } = new("Sprite2D");

	[Export]
	public NodePath MuzzlePath { get; set; } = new("Muzzle");

	[Export]
	public float FireCooldownSeconds { get; set; } = 0.15f;

	[Export]
	public float FireAnimationFps { get; set; } = 20.0f;

	[Export]
	public int FireAnimationFrameCount { get; set; } = 4;

	[Export]
	public int ProjectileSpawnFrame { get; set; } = 2;

	[ExportGroup("Drop Shadow")]
	[Export]
	public bool EnableDropShadow { get; set; }

	[Export(PropertyHint.File, "*.tscn")]
	public string DropShadowScenePath { get; set; } = DefaultDropShadowScenePath;

	[Export]
	public NodePath DropShadowSourceSpritePath { get; set; } = new("../../Sprite2D");

	[Export]
	public Vector2 DropShadowGroundOffset { get; set; } = new(0.0f, 12.0f);

	[Export]
	public Vector2 DropShadowScaleMultiplier { get; set; } = Vector2.One;

	[Export]
	public bool DropShadowFollowSourceScale { get; set; }

	[Export]
	public Vector2I DropShadowCanvasSize { get; set; } = new(48, 14);

	[Export(PropertyHint.Range, "-89,89,0.1")]
	public float DropShadowRotationDegreesOffset { get; set; }

	[Export(PropertyHint.Range, "-2,2,0.01")]
	public float DropShadowSkewAmount { get; set; }

	[Export]
	public Color DropShadowColor { get; set; } = new(0.101961f, 0.129412f, 0.0627451f, 0.26f);

	[Export]
	public Vector2 DropShadowContactCenter { get; set; } = new(0.34f, 0.56f);

	[Export]
	public Vector2 DropShadowContactRadius { get; set; } = new(0.28f, 0.2f);

	[Export(PropertyHint.Range, "-180,180,0.1")]
	public float DropShadowContactAngleDegrees { get; set; }

	[Export(PropertyHint.Range, "0,2,0.01")]
	public float DropShadowContactStrength { get; set; } = 0.8f;

	[Export]
	public Vector2 DropShadowCastCenter { get; set; } = new(0.58f, 0.58f);

	[Export]
	public Vector2 DropShadowCastRadius { get; set; } = new(0.42f, 0.22f);

	[Export(PropertyHint.Range, "-180,180,0.1")]
	public float DropShadowCastAngleDegrees { get; set; } = 12.0f;

	[Export]
	public bool DropShadowRotateWithWeapon { get; set; } = true;

	[Export(PropertyHint.Range, "0,2,0.01")]
	public float DropShadowCastStrength { get; set; } = 0.56f;

	[Export(PropertyHint.Range, "0.01,0.95,0.01")]
	public float DropShadowProceduralSoftness { get; set; } = 0.72f;

	[Export]
	public bool DropShadowZAsRelative { get; set; }

	[Export]
	public int DropShadowZIndex { get; set; } = -1;

	protected Sprite2D Sprite { get; private set; }
	protected Marker2D Muzzle { get; private set; }
	protected DropShadow2D DropShadow { get; private set; }

	private Node2D _dropShadowAnchor;
	private bool _fireRequested;
	private bool _isPlayingFireAnimation;
	private bool _projectileSpawnedThisCycle;
	private double _fireCooldownRemaining;
	private double _fireAnimationTime;

	public override void _Ready()
	{
		Sprite = GetNodeOrNull<Sprite2D>(SpritePath);
		Muzzle = GetNodeOrNull<Marker2D>(MuzzlePath);

		if (Sprite != null)
		{
			Sprite.Hframes = Mathf.Max(1, FireAnimationFrameCount);
			Sprite.Frame = IdleFrame;
		}

		SetupDropShadow();
	}

	public override void _PhysicsProcess(double delta)
	{
		UpdateWeapon(delta);
		UpdateDropShadowAnchor();
		UpdateFireCooldown(delta);
		UpdateFireAnimation(delta);
	}

	public void SetFireRequested(bool requested)
	{
		_fireRequested = requested;
	}

	protected virtual void UpdateWeapon(double delta)
	{
	}

	protected virtual Vector2 GetProjectileDirection()
	{
		return Vector2.Right.Rotated(GlobalRotation);
	}

	private void SetupDropShadow()
	{
		_dropShadowAnchor = GetNodeOrNull<Node2D>(DropShadowAnchorNodeName);
		DropShadow = _dropShadowAnchor?.GetNodeOrNull<DropShadow2D>("DropShadow2D")
			?? GetNodeOrNull<DropShadow2D>("DropShadow2D");

		if (!EnableDropShadow)
		{
			if (DropShadow != null)
			{
				DropShadow.Visible = false;
			}

			return;
		}

		_dropShadowAnchor ??= CreateDropShadowAnchor();

		if (DropShadow == null)
		{
			if (string.IsNullOrWhiteSpace(DropShadowScenePath))
			{
				GD.PushWarning($"{Name} cannot create a drop shadow because DropShadowScenePath is empty.");
				return;
			}

			PackedScene shadowScene = ResourceLoader.Load<PackedScene>(DropShadowScenePath);
			if (shadowScene == null)
			{
				GD.PushWarning($"{Name} cannot load drop shadow scene at {DropShadowScenePath}.");
				return;
			}

			DropShadow = shadowScene.Instantiate<DropShadow2D>();
			DropShadow.Name = "DropShadow2D";
			ConfigureDropShadow(DropShadow);
			_dropShadowAnchor.AddChild(DropShadow);
			ConfigureDropShadow(DropShadow);
			UpdateDropShadowAnchor();
			return;
		}

		ConfigureDropShadow(DropShadow);
		UpdateDropShadowAnchor();
	}

	private Node2D CreateDropShadowAnchor()
	{
		Node2D anchor = new()
		{
			Name = DropShadowAnchorNodeName,
			ZAsRelative = false,
			ZIndex = DropShadowZIndex,
		};
		AddChild(anchor);
		return anchor;
	}

	private void ConfigureDropShadow(DropShadow2D shadow)
	{
		shadow.SourceSpritePath = DropShadowSourceSpritePath;
		shadow.GroundOffset = DropShadowGroundOffset;
		shadow.ScaleMultiplier = DropShadowScaleMultiplier;
		shadow.FollowSourceScale = DropShadowFollowSourceScale;
		shadow.ShadowCanvasSize = DropShadowCanvasSize;
		shadow.RotationDegreesOffset = DropShadowRotationDegreesOffset;
		shadow.SkewAmount = DropShadowSkewAmount;
		shadow.ShadowColor = DropShadowColor;
		shadow.ContactCenter = DropShadowContactCenter;
		shadow.ContactRadius = DropShadowContactRadius;
		shadow.ContactStrength = DropShadowContactStrength;
		shadow.CastCenter = DropShadowCastCenter;
		shadow.CastRadius = DropShadowCastRadius;
		shadow.CastStrength = DropShadowCastStrength;
		shadow.ProceduralSoftness = DropShadowProceduralSoftness;
		shadow.ZAsRelative = DropShadowZAsRelative;
		shadow.ZIndex = DropShadowZIndex;
		shadow.Visible = true;
		UpdateDropShadowShapeRotation();
	}

	private void UpdateDropShadowAnchor()
	{
		if (_dropShadowAnchor == null)
		{
			return;
		}

		_dropShadowAnchor.GlobalPosition = GlobalPosition;
		_dropShadowAnchor.GlobalRotation = 0.0f;
		_dropShadowAnchor.GlobalScale = Vector2.One;
		UpdateDropShadowShapeRotation();
	}

	private void UpdateDropShadowShapeRotation()
	{
		if (DropShadow == null)
		{
			return;
		}

		float weaponAngleDegrees = DropShadowRotateWithWeapon ? Mathf.RadToDeg(GlobalRotation) : 0.0f;
		DropShadow.ContactAngleDegrees = DropShadowContactAngleDegrees + weaponAngleDegrees;
		DropShadow.CastAngleDegrees = DropShadowCastAngleDegrees + weaponAngleDegrees;
	}

	private void UpdateFireCooldown(double delta)
	{
		if (_fireCooldownRemaining > 0.0)
		{
			_fireCooldownRemaining -= delta;
		}
	}

	private void UpdateFireAnimation(double delta)
	{
		if (!_isPlayingFireAnimation)
		{
			if (_fireRequested && _fireCooldownRemaining <= 0.0)
			{
				StartFireAnimation();
			}
			else if (Sprite != null)
			{
				Sprite.Frame = IdleFrame;
			}

			return;
		}

		_fireAnimationTime += delta;
		int frameCount = Mathf.Max(1, FireAnimationFrameCount);
		int frame = Mathf.Clamp((int)(_fireAnimationTime * FireAnimationFps), 0, frameCount - 1);

		if (Sprite != null)
		{
			Sprite.Frame = frame;
		}

		if (!_projectileSpawnedThisCycle && frame >= ProjectileSpawnFrame)
		{
			SpawnProjectile();
			_projectileSpawnedThisCycle = true;
			_fireCooldownRemaining = FireCooldownSeconds;
		}

		double animationDuration = frameCount / Mathf.Max(1.0f, FireAnimationFps);
		if (_fireAnimationTime >= animationDuration)
		{
			_isPlayingFireAnimation = false;
			_fireAnimationTime = 0.0;

			if (Sprite != null)
			{
				Sprite.Frame = IdleFrame;
			}
		}
	}

	private void StartFireAnimation()
	{
		_isPlayingFireAnimation = true;
		_projectileSpawnedThisCycle = false;
		_fireAnimationTime = 0.0;

		if (Sprite != null)
		{
			Sprite.Frame = IdleFrame;
		}
	}

	private void SpawnProjectile()
	{
		if (BulletScene == null)
		{
			GD.PushWarning($"{Name} cannot fire because BulletScene is not assigned.");
			return;
		}

		if (Muzzle == null)
		{
			GD.PushWarning($"{Name} cannot fire because Muzzle is missing.");
			return;
		}

		Node bulletInstance = BulletScene.Instantiate();
		if (bulletInstance is not Bullet bullet)
		{
			GD.PushError("BulletScene must instantiate a Bullet.");
			bulletInstance.QueueFree();
			return;
		}

		Node parent = GetTree().CurrentScene ?? GetTree().Root;
		parent.AddChild(bullet);

		Vector2 direction = GetProjectileDirection();
		bullet.GlobalPosition = Muzzle.GlobalPosition;
		bullet.Initialize(direction);
	}
}
