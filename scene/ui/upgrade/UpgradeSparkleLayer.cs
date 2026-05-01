using Godot;
using System.Collections.Generic;

public partial class UpgradeSparkleLayer : Control
{
	[Export]
	public float MinAlpha { get; set; } = 0.28f;

	[Export]
	public float MaxAlpha { get; set; } = 1.0f;

	[Export]
	public float MinScale { get; set; } = 0.86f;

	[Export]
	public float MaxScale { get; set; } = 1.1f;

	[Export]
	public float BasePeriodSeconds { get; set; } = 1.15f;

	[Export]
	public float PeriodStepSeconds { get; set; } = 0.13f;

	private readonly List<TextureRect> _sparkles = new();
	private double _elapsedSeconds;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		CollectSparkles();
	}

	public override void _Process(double delta)
	{
		if (_sparkles.Count == 0)
		{
			return;
		}

		_elapsedSeconds += delta;

		for (int i = 0; i < _sparkles.Count; i++)
		{
			TextureRect sparkle = _sparkles[i];
			float period = Mathf.Max(0.1f, BasePeriodSeconds + PeriodStepSeconds * (i % 5));
			float phase = i * 0.173f + (i % 3) * 0.29f;
			float wave = Mathf.Sin((float)((_elapsedSeconds / period + phase) * Mathf.Tau)) * 0.5f + 0.5f;
			float easedWave = wave * wave * (3.0f - 2.0f * wave);
			float alpha = Mathf.Lerp(MinAlpha, MaxAlpha, easedWave);
			float scale = Mathf.Lerp(MinScale, MaxScale, easedWave);

			sparkle.Modulate = new Color(1.0f, 1.0f, 1.0f, alpha);
			sparkle.Scale = new Vector2(scale, scale);
		}
	}

	private void CollectSparkles()
	{
		_sparkles.Clear();

		foreach (Node child in GetChildren())
		{
			if (child is not TextureRect sparkle)
			{
				continue;
			}

			sparkle.MouseFilter = MouseFilterEnum.Ignore;
			sparkle.PivotOffset = sparkle.Size * 0.5f;
			_sparkles.Add(sparkle);
		}
	}
}
