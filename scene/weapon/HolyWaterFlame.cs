using Godot;

public partial class HolyWaterFlame : Node2D
{
	[Export]
	public float BaseWidth { get; set; } = 10.0f;

	[Export]
	public float BaseHeight { get; set; } = 8.0f;

	[Export]
	public float MinHeightScale { get; set; } = 0.72f;

	[Export]
	public float MaxHeightScale { get; set; } = 1.12f;

	[Export]
	public float WidthPulseScale { get; set; } = 0.12f;

	[Export]
	public float SwayPixels { get; set; } = 1.3f;

	[Export]
	public float LoopSeconds { get; set; } = 0.75f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float PhaseOffset { get; set; }

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float AlphaMultiplier { get; set; } = 1.0f;

	[Export]
	public bool RandomizePhaseOnReady { get; set; }

	[Export]
	public Color OuterColor { get; set; } = new(0.05f, 0.52f, 1.0f, 0.58f);

	[Export]
	public Color InnerColor { get; set; } = new(0.38f, 0.92f, 1.0f, 0.72f);

	[Export]
	public Color CoreColor { get; set; } = new(0.86f, 1.0f, 1.0f, 0.70f);

	[Export]
	public Color GlowColor { get; set; } = new(0.0f, 0.42f, 1.0f, 0.22f);

	[Export]
	public NodePath OuterFlamePath { get; set; } = new("OuterFlame");

	[Export]
	public NodePath InnerFlamePath { get; set; } = new("InnerFlame");

	[Export]
	public NodePath CoreFlamePath { get; set; } = new("CoreFlame");

	[Export]
	public NodePath BaseGlowPath { get; set; } = new("BaseGlow");

	private double _elapsed;
	private Polygon2D _outerFlame;
	private Polygon2D _innerFlame;
	private Polygon2D _coreFlame;
	private Polygon2D _baseGlow;

	public void Configure(
		float baseWidth,
		float baseHeight,
		float phaseOffset,
		float loopSeconds,
		float minHeightScale,
		float maxHeightScale,
		float swayPixels,
		float widthPulseScale,
		float alphaMultiplier)
	{
		BaseWidth = Mathf.Max(2.0f, baseWidth);
		BaseHeight = Mathf.Max(2.0f, baseHeight);
		PhaseOffset = Mathf.PosMod(phaseOffset, 1.0f);
		LoopSeconds = Mathf.Max(0.05f, loopSeconds);
		MinHeightScale = Mathf.Max(0.1f, minHeightScale);
		MaxHeightScale = Mathf.Max(MinHeightScale, maxHeightScale);
		SwayPixels = Mathf.Max(0.0f, swayPixels);
		WidthPulseScale = Mathf.Max(0.0f, widthPulseScale);
		AlphaMultiplier = Mathf.Clamp(alphaMultiplier, 0.0f, 1.0f);
		UpdateShape();
	}

	public override void _Ready()
	{
		_outerFlame = GetNodeOrNull<Polygon2D>(OuterFlamePath);
		_innerFlame = GetNodeOrNull<Polygon2D>(InnerFlamePath);
		_coreFlame = GetNodeOrNull<Polygon2D>(CoreFlamePath);
		_baseGlow = GetNodeOrNull<Polygon2D>(BaseGlowPath);

		if (RandomizePhaseOnReady)
		{
			PhaseOffset = (float)GD.Randf();
		}

		UpdateShape();
	}

	public override void _Process(double delta)
	{
		_elapsed += delta;
		UpdateShape();
	}

	private void UpdateShape()
	{
		float loop = Mathf.Max(0.05f, LoopSeconds);
		float phase = Mathf.PosMod((float)(_elapsed / loop) + PhaseOffset, 1.0f);
		float heightWave = 0.5f + 0.5f * Mathf.Sin(phase * Mathf.Tau);
		float widthWave = Mathf.Sin((phase + 0.37f) * Mathf.Tau);
		float swayWave = Mathf.Sin((phase + 0.18f) * Mathf.Tau);
		float secondaryWave = 0.5f + 0.5f * Mathf.Sin((phase * 2.0f + 0.13f) * Mathf.Tau);

		float height = BaseHeight * Mathf.Lerp(MinHeightScale, MaxHeightScale, heightWave);
		float width = BaseWidth * (1.0f + WidthPulseScale * widthWave);
		float sway = SwayPixels * swayWave;
		float alpha = AlphaMultiplier * (0.82f + 0.18f * secondaryWave);

		if (_baseGlow != null)
		{
			_baseGlow.Polygon = BuildOvalPolygon(width * 0.62f, Mathf.Max(1.0f, BaseHeight * 0.16f), 10);
			_baseGlow.Color = WithAlpha(GlowColor, GlowColor.A * alpha);
		}

		if (_outerFlame != null)
		{
			_outerFlame.Polygon = BuildFlamePolygon(width, height, sway);
			_outerFlame.Color = WithAlpha(OuterColor, OuterColor.A * alpha);
		}

		if (_innerFlame != null)
		{
			_innerFlame.Polygon = BuildFlamePolygon(width * 0.58f, height * 0.72f, sway * 0.48f);
			_innerFlame.Color = WithAlpha(InnerColor, InnerColor.A * alpha);
		}

		if (_coreFlame != null)
		{
			_coreFlame.Polygon = BuildFlamePolygon(width * 0.30f, height * 0.44f, sway * 0.22f);
			_coreFlame.Color = WithAlpha(CoreColor, CoreColor.A * alpha);
		}
	}

	private static Vector2[] BuildFlamePolygon(float width, float height, float sway)
	{
		float halfWidth = Mathf.Max(1.0f, width * 0.5f);
		float midHeight = height * 0.38f;

		return new[]
		{
			new Vector2(-halfWidth, 0.0f),
			new Vector2(-halfWidth * 0.58f, -midHeight),
			new Vector2(sway, -height),
			new Vector2(halfWidth * 0.62f, -midHeight * 0.92f),
			new Vector2(halfWidth, 0.0f),
		};
	}

	private static Vector2[] BuildOvalPolygon(float radiusX, float radiusY, int pointCount)
	{
		int safePointCount = Mathf.Max(6, pointCount);
		Vector2[] points = new Vector2[safePointCount];
		for (int i = 0; i < safePointCount; i++)
		{
			float angle = Mathf.Tau * i / safePointCount;
			points[i] = new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY - radiusY * 0.28f);
		}

		return points;
	}

	private static Color WithAlpha(Color color, float alpha)
	{
		color.A = Mathf.Clamp(alpha, 0.0f, 1.0f);
		return color;
	}
}
