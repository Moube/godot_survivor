using Godot;

public partial class HolyWaterArea : Area2D
{
	[Export]
	public int Damage { get; set; } = 1;

	[Export]
	public float Radius { get; set; } = 48.0f;

	[Export]
	public float DurationSeconds { get; set; } = 3.2f;

	[Export]
	public float DamageIntervalSeconds { get; set; } = 0.55f;

	[Export]
	public NodePath CollisionShapePath { get; set; } = new("CollisionShape2D");

	[Export]
	public NodePath VisualRootPath { get; set; } = new("VisualRoot");

	[Export]
	public NodePath FlameFieldPath { get; set; } = new("VisualRoot/FlameField");

	[Export]
	public int GroundEffectZIndex { get; set; } = -9;

	private const float VisualBaseRadius = 48.0f;
	private double _remainingDuration;
	private double _damageIntervalRemaining;
	private Node2D _visualRoot;
	private HolyWaterFlameField _flameField;

	public void Initialize(int damage, float radius, float durationSeconds, float damageIntervalSeconds)
	{
		Damage = Mathf.Max(1, damage);
		Radius = Mathf.Max(4.0f, radius);
		DurationSeconds = Mathf.Max(0.1f, durationSeconds);
		DamageIntervalSeconds = Mathf.Max(0.05f, damageIntervalSeconds);
		_remainingDuration = DurationSeconds;
		_damageIntervalRemaining = 0.0;
		ApplyRadius();
	}

	public override void _Ready()
	{
		ZIndex = GroundEffectZIndex;
		_visualRoot = GetNodeOrNull<Node2D>(VisualRootPath);
		_flameField = GetNodeOrNull<HolyWaterFlameField>(FlameFieldPath);
		_remainingDuration = DurationSeconds;
		_damageIntervalRemaining = 0.0;
		ApplyRadius();
	}

	public override void _PhysicsProcess(double delta)
	{
		_remainingDuration -= delta;
		_damageIntervalRemaining -= delta;

		if (_damageIntervalRemaining <= 0.0)
		{
			ApplyDamageTick();
			_damageIntervalRemaining = DamageIntervalSeconds;
		}

		UpdateVisualFade();

		if (_remainingDuration <= 0.0)
		{
			QueueFree();
		}
	}

	private void ApplyDamageTick()
	{
		foreach (Node2D body in GetOverlappingBodies())
		{
			CombatComponent combat = body.GetNodeOrNull<CombatComponent>("CombatComponent");
			if (combat?.IsDead == false)
			{
				combat.ApplyDamage(Damage);
			}
		}
	}

	private void ApplyRadius()
	{
		CollisionShape2D collisionShape = GetNodeOrNull<CollisionShape2D>(CollisionShapePath);
		if (collisionShape?.Shape is CircleShape2D circle)
		{
			circle.Radius = Radius;
		}

		_visualRoot ??= GetNodeOrNull<Node2D>(VisualRootPath);
		if (_visualRoot != null)
		{
			float visualScale = Radius / VisualBaseRadius;
			_visualRoot.Scale = new Vector2(visualScale, visualScale);
		}

		_flameField ??= GetNodeOrNull<HolyWaterFlameField>(FlameFieldPath);
		_flameField?.SetRadius(VisualBaseRadius);
	}

	private void UpdateVisualFade()
	{
		if (_visualRoot == null)
		{
			return;
		}

		float duration = Mathf.Max(0.01f, DurationSeconds);
		float normalizedRemaining = Mathf.Clamp((float)(_remainingDuration / duration), 0.0f, 1.0f);
		Color modulate = _visualRoot.Modulate;
		modulate.A = Mathf.Clamp(normalizedRemaining * 1.35f, 0.0f, 1.0f);
		_visualRoot.Modulate = modulate;
	}

}
