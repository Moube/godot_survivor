using Godot;
using System.Collections.Generic;

public partial class Bullet : Area2D
{
	[Export]
	public float Speed { get; set; } = 640.0f;

	[Export]
	public float LifetimeSeconds { get; set; } = 1.5f;

	[Export]
	public int Damage { get; set; } = 1;

	[Export]
	public int MaxHitCount { get; set; } = 1;

	[Export]
	public float VisualSpinDegreesPerSecond { get; set; }

	[Export]
	public PackedScene HitSparkScene { get; set; }

	[Export]
	public NodePath SpritePath { get; set; } = new("Sprite2D");

	public Vector2 Direction { get; private set; } = Vector2.Right;

	private double _remainingLifetime;
	private int _remainingHitCount;
	private Texture2D _visualTextureOverride;
	private Sprite2D _sprite;
	private readonly HashSet<ulong> _hitBodyIds = new();

	public void Initialize(Vector2 direction)
	{
		Direction = direction == Vector2.Zero ? Vector2.Right : direction.Normalized();
		Rotation = Direction.Angle();
	}

	public void SetVisualTexture(Texture2D texture)
	{
		_visualTextureOverride = texture;
		ApplyVisualTextureOverride();
	}

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<Sprite2D>(SpritePath);
		ApplyVisualTextureOverride();
		_remainingLifetime = LifetimeSeconds;
		_remainingHitCount = Mathf.Max(1, MaxHitCount);
		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		GlobalPosition += Direction * Speed * (float)delta;
		if (_sprite != null && !Mathf.IsZeroApprox(VisualSpinDegreesPerSecond))
		{
			_sprite.RotationDegrees += VisualSpinDegreesPerSecond * (float)delta;
		}

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

		ulong bodyId = body.GetInstanceId();
		if (_hitBodyIds.Contains(bodyId))
		{
			return;
		}

		CombatComponent targetCombat = body.GetNodeOrNull<CombatComponent>("CombatComponent");
		if (targetCombat is null)
		{
			SpawnHitSpark();
			QueueFree();
			return;
		}

		_hitBodyIds.Add(bodyId);
		targetCombat.ApplyDamage(Damage);
		SpawnHitSpark();

		_remainingHitCount--;
		if (_remainingHitCount <= 0)
		{
			QueueFree();
		}
	}

	private void SpawnHitSpark()
	{
		if (HitSparkScene is null)
		{
			return;
		}

		Node instance = HitSparkScene.Instantiate();
		if (instance is not Node2D hitSpark)
		{
			GD.PushWarning("HitSparkScene must instantiate a Node2D.");
			instance.QueueFree();
			return;
		}

		GetParent()?.AddChild(hitSpark);
		hitSpark.GlobalPosition = GlobalPosition;
		hitSpark.GlobalRotation = GlobalRotation;
	}

	private void ApplyVisualTextureOverride()
	{
		if (_visualTextureOverride == null)
		{
			return;
		}

		_sprite ??= GetNodeOrNull<Sprite2D>(SpritePath);
		if (_sprite != null)
		{
			_sprite.Texture = _visualTextureOverride;
		}
	}
}
