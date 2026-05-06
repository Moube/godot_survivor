using Godot;

public partial class ThunderStrike : Node2D
{
	[Export]
	public int Damage { get; set; } = 3;

	[Export]
	public float Radius { get; set; } = 42.0f;

	[Export]
	public float WarningDelaySeconds { get; set; } = 0.58f;

	[Export]
	public float ImpactDurationSeconds { get; set; } = 0.24f;

	[Export]
	public float LightningSkyHeight { get; set; } = 760.0f;

	[Export]
	public NodePath WarningRootPath { get; set; } = new("WarningRoot");

	[Export]
	public NodePath WarningFillPath { get; set; } = new("WarningRoot/WarningFill");

	[Export]
	public NodePath WarningRimPath { get; set; } = new("WarningRoot/WarningRim");

	[Export]
	public NodePath WarningRunePath { get; set; } = new("WarningRoot/WarningRune");

	[Export]
	public NodePath ImpactRootPath { get; set; } = new("ImpactRoot");

	[Export]
	public NodePath GroundFlashPath { get; set; } = new("ImpactRoot/GroundFlash");

	[Export]
	public NodePath LightningBoltPath { get; set; } = new("ImpactRoot/LightningBolt");

	[Export]
	public NodePath LightningCorePath { get; set; } = new("ImpactRoot/LightningCore");

	[Export]
	public NodePath BranchLeftPath { get; set; } = new("ImpactRoot/BranchLeft");

	[Export]
	public NodePath BranchRightPath { get; set; } = new("ImpactRoot/BranchRight");

	private const int CirclePointCount = 24;

	private readonly RandomNumberGenerator _random = new();
	private double _elapsed;
	private bool _hasImpacted;
	private Node2D _warningRoot;
	private Node2D _impactRoot;
	private Polygon2D _warningFill;
	private Line2D _warningRim;
	private Line2D _warningRune;
	private Polygon2D _groundFlash;
	private Line2D _lightningBolt;
	private Line2D _lightningCore;
	private Line2D _branchLeft;
	private Line2D _branchRight;

	public void Initialize(int damage, float radius, float warningDelaySeconds, float impactDurationSeconds)
	{
		Damage = Mathf.Max(1, damage);
		Radius = Mathf.Max(4.0f, radius);
		WarningDelaySeconds = Mathf.Max(0.05f, warningDelaySeconds);
		ImpactDurationSeconds = Mathf.Max(0.05f, impactDurationSeconds);
		_elapsed = 0.0;
		_hasImpacted = false;
		ResolveNodes();
		ConfigureVisuals();
	}

	public override void _Ready()
	{
		_random.Randomize();
		ResolveNodes();
		ConfigureVisuals();
	}

	public override void _Process(double delta)
	{
		_elapsed += delta;

		if (!_hasImpacted)
		{
			UpdateWarningVisual();
			if (_elapsed >= WarningDelaySeconds)
			{
				Impact();
			}

			return;
		}

		UpdateImpactVisual();
		if (_elapsed >= WarningDelaySeconds + ImpactDurationSeconds)
		{
			QueueFree();
		}
	}

	private void Impact()
	{
		_hasImpacted = true;
		_warningRoot ??= GetNodeOrNull<Node2D>(WarningRootPath);
		_impactRoot ??= GetNodeOrNull<Node2D>(ImpactRootPath);

		if (_warningRoot != null)
		{
			_warningRoot.Visible = false;
		}

		if (_impactRoot != null)
		{
			_impactRoot.Visible = true;
		}

		ApplyImpactDamage();
		UpdateImpactVisual();
	}

	private void ApplyImpactDamage()
	{
		SceneTree tree = GetTree();
		if (tree == null)
		{
			return;
		}

		float radiusSquared = Radius * Radius;
		foreach (Node node in tree.GetNodesInGroup("enemy"))
		{
			if (node is not Node2D enemy || !IsInstanceValid(enemy))
			{
				continue;
			}

			if (GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition) > radiusSquared)
			{
				continue;
			}

			CombatComponent combat = enemy.GetNodeOrNull<CombatComponent>("CombatComponent");
			if (combat?.IsDead == false)
			{
				combat.ApplyDamage(Damage);
			}
		}
	}

	private void ResolveNodes()
	{
		_warningRoot = GetNodeOrNull<Node2D>(WarningRootPath);
		_impactRoot = GetNodeOrNull<Node2D>(ImpactRootPath);
		_warningFill = GetNodeOrNull<Polygon2D>(WarningFillPath);
		_warningRim = GetNodeOrNull<Line2D>(WarningRimPath);
		_warningRune = GetNodeOrNull<Line2D>(WarningRunePath);
		_groundFlash = GetNodeOrNull<Polygon2D>(GroundFlashPath);
		_lightningBolt = GetNodeOrNull<Line2D>(LightningBoltPath);
		_lightningCore = GetNodeOrNull<Line2D>(LightningCorePath);
		_branchLeft = GetNodeOrNull<Line2D>(BranchLeftPath);
		_branchRight = GetNodeOrNull<Line2D>(BranchRightPath);
	}

	private void ConfigureVisuals()
	{
		if (_warningFill != null)
		{
			_warningFill.Polygon = BuildCirclePoints(Radius, CirclePointCount);
		}

		if (_warningRim != null)
		{
			_warningRim.Points = BuildCircleLinePoints(Radius, CirclePointCount);
		}

		if (_warningRune != null)
		{
			float runeRadius = Radius * 0.52f;
			_warningRune.Points = new[]
			{
				new Vector2(-runeRadius * 0.34f, -runeRadius),
				new Vector2(runeRadius * 0.12f, -runeRadius * 0.18f),
				new Vector2(-runeRadius * 0.05f, -runeRadius * 0.18f),
				new Vector2(runeRadius * 0.28f, runeRadius),
			};
		}

		if (_groundFlash != null)
		{
			_groundFlash.Polygon = BuildCirclePoints(Radius * 0.72f, 16);
		}

		if (_impactRoot != null)
		{
			_impactRoot.Visible = _hasImpacted;
		}

		UpdateWarningVisual();
	}

	private void UpdateWarningVisual()
	{
		if (_warningRoot == null)
		{
			return;
		}

		float progress = Mathf.Clamp((float)(_elapsed / Mathf.Max(0.01f, WarningDelaySeconds)), 0.0f, 1.0f);
		float pulse = 0.5f + 0.5f * Mathf.Sin(progress * Mathf.Tau * 3.0f);
		_warningRoot.Scale = Vector2.One * Mathf.Lerp(0.82f, 1.0f, progress);
		Color modulate = _warningRoot.Modulate;
		modulate.A = 0.36f + 0.34f * pulse + 0.20f * progress;
		_warningRoot.Modulate = modulate;
	}

	private void UpdateImpactVisual()
	{
		float progress = Mathf.Clamp((float)((_elapsed - WarningDelaySeconds) / Mathf.Max(0.01f, ImpactDurationSeconds)), 0.0f, 1.0f);
		float alpha = Mathf.Clamp(1.0f - progress, 0.0f, 1.0f);

		if (_impactRoot != null)
		{
			Color modulate = _impactRoot.Modulate;
			modulate.A = alpha;
			_impactRoot.Modulate = modulate;
		}

		Vector2[] boltPoints = BuildLightningPoints();
		if (_lightningBolt != null)
		{
			_lightningBolt.Points = boltPoints;
		}

		if (_lightningCore != null)
		{
			_lightningCore.Points = boltPoints;
		}

		float branchHeight = -GetLightningSkyHeight() * 0.42f;
		if (_branchLeft != null)
		{
			_branchLeft.Points = new[]
			{
				new Vector2(0.0f, branchHeight),
				new Vector2(-Radius * 0.55f, branchHeight + Radius * 0.58f),
				new Vector2(-Radius * 0.82f, branchHeight + Radius * 1.05f),
			};
		}

		if (_branchRight != null)
		{
			_branchRight.Points = new[]
			{
				new Vector2(0.0f, branchHeight + Radius * 0.40f),
				new Vector2(Radius * 0.54f, branchHeight + Radius * 0.88f),
				new Vector2(Radius * 0.86f, branchHeight + Radius * 1.48f),
			};
		}
	}

	private Vector2[] BuildLightningPoints()
	{
		float top = -GetLightningSkyHeight();
		float bottom = Radius * 0.15f;
		return new[]
		{
			new Vector2(_random.RandfRange(-Radius * 0.22f, Radius * 0.22f), top),
			new Vector2(_random.RandfRange(-Radius * 0.42f, Radius * 0.42f), top * 0.70f),
			new Vector2(_random.RandfRange(-Radius * 0.36f, Radius * 0.36f), top * 0.42f),
			new Vector2(_random.RandfRange(-Radius * 0.26f, Radius * 0.26f), top * 0.16f),
			new Vector2(_random.RandfRange(-Radius * 0.16f, Radius * 0.16f), bottom),
		};
	}

	private float GetLightningSkyHeight()
	{
		return Mathf.Max(Radius * 3.5f, LightningSkyHeight);
	}

	private static Vector2[] BuildCirclePoints(float radius, int pointCount)
	{
		int safePointCount = Mathf.Max(8, pointCount);
		Vector2[] points = new Vector2[safePointCount];
		for (int i = 0; i < safePointCount; i++)
		{
			float angle = Mathf.Tau * i / safePointCount;
			points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
		}

		return points;
	}

	private static Vector2[] BuildCircleLinePoints(float radius, int pointCount)
	{
		int safePointCount = Mathf.Max(8, pointCount);
		Vector2[] points = new Vector2[safePointCount + 1];
		for (int i = 0; i <= safePointCount; i++)
		{
			float angle = Mathf.Tau * i / safePointCount;
			points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
		}

		return points;
	}
}
