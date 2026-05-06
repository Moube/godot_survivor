using Godot;
using System.Collections.Generic;

public partial class SoulChainEffect : Node2D
{
	[Export]
	public NodePath ChainGlowPath { get; set; } = new("ChainGlow");

	[Export]
	public NodePath ChainCorePath { get; set; } = new("ChainCore");

	[Export]
	public NodePath MarkerRootPath { get; set; } = new("MarkerRoot");

	[Export]
	public NodePath LinkRootPath { get; set; } = new("LinkRoot");

	[Export]
	public float MarkerRadius { get; set; } = 8.0f;

	[Export]
	public float MarkerPulseScale { get; set; } = 0.28f;

	[Export]
	public float LinkSpacing { get; set; } = 16.0f;

	[Export]
	public float LinkRadiusX { get; set; } = 6.5f;

	[Export]
	public float LinkRadiusY { get; set; } = 3.2f;

	[Export]
	public float LinkLineWidth { get; set; } = 1.8f;

	private const int MarkerPointCount = 12;
	private const int LinkPointCount = 14;

	private readonly List<Node2D> _targets = new();
	private readonly List<Vector2> _localPoints = new();
	private readonly List<Polygon2D> _markers = new();
	private readonly List<Line2D> _chainLinks = new();
	private readonly List<int> _chainLinkSegmentIndexes = new();
	private Line2D _chainGlow;
	private Line2D _chainCore;
	private Node2D _markerRoot;
	private Node2D _linkRoot;
	private double _elapsed;
	private float _stepDelaySeconds = 0.09f;
	private float _lingerSeconds = 0.22f;
	private int _damage = 1;
	private int _revealedTargetCount;
	private bool _isInitialized;

	public void Initialize(
		Vector2 sourceGlobalPosition,
		IReadOnlyList<Node2D> targets,
		int damage,
		float stepDelaySeconds,
		float lingerSeconds)
	{
		GlobalPosition = sourceGlobalPosition;
		_targets.Clear();
		_localPoints.Clear();
		_localPoints.Add(Vector2.Zero);

		foreach (Node2D target in targets)
		{
			if (target == null || !IsInstanceValid(target))
			{
				continue;
			}

			CombatComponent combat = target.GetNodeOrNull<CombatComponent>("CombatComponent");
			if (combat?.IsDead != false)
			{
				continue;
			}

			_targets.Add(target);
			_localPoints.Add(target.GlobalPosition - sourceGlobalPosition);
		}

		_damage = Mathf.Max(1, damage);
		_stepDelaySeconds = Mathf.Max(0.01f, stepDelaySeconds);
		_lingerSeconds = Mathf.Max(0.05f, lingerSeconds);
		_elapsed = 0.0;
		_revealedTargetCount = 0;
		_isInitialized = true;

		ResolveNodes();
		RebuildMarkers();
		RebuildChainLinks();
		RevealAvailableTargets();
		UpdateVisuals();
	}

	public override void _Ready()
	{
		ResolveNodes();
		UpdateVisuals();
	}

	public override void _Process(double delta)
	{
		if (!_isInitialized)
		{
			return;
		}

		_elapsed += delta;
		RevealAvailableTargets();
		UpdateVisuals();

		double totalLifetime = _stepDelaySeconds * Mathf.Max(0, _targets.Count - 1) + _lingerSeconds;
		if (_elapsed >= totalLifetime)
		{
			QueueFree();
		}
	}

	private void RevealAvailableTargets()
	{
		int targetCount = _targets.Count;
		while (_revealedTargetCount < targetCount &&
			_elapsed + 0.0001 >= _revealedTargetCount * _stepDelaySeconds)
		{
			ApplyDamageToTarget(_revealedTargetCount);
			_revealedTargetCount++;
		}
	}

	private void ApplyDamageToTarget(int targetIndex)
	{
		if (targetIndex < 0 || targetIndex >= _targets.Count)
		{
			return;
		}

		Node2D target = _targets[targetIndex];
		if (target == null || !IsInstanceValid(target))
		{
			return;
		}

		CombatComponent combat = target.GetNodeOrNull<CombatComponent>("CombatComponent");
		if (combat?.IsDead == false)
		{
			combat.ApplyDamage(_damage);
		}
	}

	private void ResolveNodes()
	{
		_chainGlow = GetNodeOrNull<Line2D>(ChainGlowPath);
		_chainCore = GetNodeOrNull<Line2D>(ChainCorePath);
		_markerRoot = GetNodeOrNull<Node2D>(MarkerRootPath);
		_linkRoot = GetNodeOrNull<Node2D>(LinkRootPath);
	}

	private void RebuildMarkers()
	{
		if (_markerRoot == null)
		{
			return;
		}

		foreach (Polygon2D marker in _markers)
		{
			if (IsInstanceValid(marker))
			{
				marker.QueueFree();
			}
		}

		_markers.Clear();
		for (int i = 1; i < _localPoints.Count; i++)
		{
			Polygon2D marker = new()
			{
				Name = $"SoulMark{i}",
				Position = _localPoints[i],
				Polygon = BuildDiamondPoints(MarkerRadius),
				Color = new Color(0.18f, 0.95f, 0.82f, 0.78f),
				ZIndex = 1,
			};
			_markerRoot.AddChild(marker);
			_markers.Add(marker);
		}
	}

	private void RebuildChainLinks()
	{
		if (_linkRoot == null)
		{
			return;
		}

		foreach (Line2D chainLink in _chainLinks)
		{
			if (IsInstanceValid(chainLink))
			{
				chainLink.QueueFree();
			}
		}

		_chainLinks.Clear();
		_chainLinkSegmentIndexes.Clear();

		for (int segmentIndex = 1; segmentIndex < _localPoints.Count; segmentIndex++)
		{
			Vector2 start = _localPoints[segmentIndex - 1];
			Vector2 end = _localPoints[segmentIndex];
			Vector2 segment = end - start;
			float segmentLength = segment.Length();
			if (segmentLength <= 2.0f)
			{
				continue;
			}

			Vector2 direction = segment / segmentLength;
			int linkCount = Mathf.Max(1, Mathf.FloorToInt(segmentLength / Mathf.Max(4.0f, LinkSpacing)));
			float spacing = segmentLength / (linkCount + 1);
			for (int i = 0; i < linkCount; i++)
			{
				Line2D chainLink = new()
				{
					Name = $"ChainLink{segmentIndex}_{i + 1}",
					Points = BuildEllipseLinePoints(LinkRadiusX, LinkRadiusY, LinkPointCount),
					Width = LinkLineWidth,
					DefaultColor = new Color(0.12f, 0.95f, 0.88f, 0.58f),
					ZIndex = 1,
					Position = start + direction * spacing * (i + 1),
					Rotation = direction.Angle() + (i % 2 == 0 ? 0.0f : Mathf.Pi * 0.5f),
				};
				_linkRoot.AddChild(chainLink);
				_chainLinks.Add(chainLink);
				_chainLinkSegmentIndexes.Add(segmentIndex);
			}
		}
	}

	private void UpdateVisuals()
	{
		if (_localPoints.Count == 0)
		{
			return;
		}

		int visiblePointCount = Mathf.Clamp(_revealedTargetCount + 1, 1, _localPoints.Count);
		Vector2[] points = new Vector2[visiblePointCount];
		for (int i = 0; i < visiblePointCount; i++)
		{
			points[i] = _localPoints[i];
		}

		float alpha = CalculateAlpha();
		float pulse = 0.5f + 0.5f * Mathf.Sin((float)_elapsed * Mathf.Tau * 7.0f);

		if (_chainGlow != null)
		{
			_chainGlow.Points = points;
			_chainGlow.Width = 3.2f + pulse * 0.8f;
			_chainGlow.DefaultColor = new Color(0.0f, 0.78f, 0.88f, 0.18f * alpha);
		}

		if (_chainCore != null)
		{
			_chainCore.Points = points;
			_chainCore.Width = 0.9f + pulse * 0.25f;
			_chainCore.DefaultColor = new Color(0.74f, 1.0f, 0.86f, 0.38f * alpha);
		}

		UpdateChainLinks(alpha, pulse);
		for (int i = 0; i < _markers.Count; i++)
		{
			Polygon2D marker = _markers[i];
			if (!IsInstanceValid(marker))
			{
				continue;
			}

			bool visible = i < _revealedTargetCount;
			marker.Visible = visible;
			if (!visible)
			{
				continue;
			}

			marker.Scale = Vector2.One * (1.0f + pulse * MarkerPulseScale);
			Color color = marker.Color;
			color.A = 0.66f * alpha;
			marker.Color = color;
		}
	}

	private void UpdateChainLinks(float alpha, float pulse)
	{
		for (int i = 0; i < _chainLinks.Count; i++)
		{
			Line2D chainLink = _chainLinks[i];
			if (!IsInstanceValid(chainLink))
			{
				continue;
			}

			bool visible = _chainLinkSegmentIndexes[i] <= _revealedTargetCount;
			chainLink.Visible = visible;
			if (!visible)
			{
				continue;
			}

			float linkPulse = 0.94f + pulse * 0.12f;
			chainLink.Scale = Vector2.One * linkPulse;
			chainLink.Width = LinkLineWidth + pulse * 0.35f;
			chainLink.DefaultColor = new Color(0.10f, 0.92f, 0.86f, 0.66f * alpha);
		}
	}

	private float CalculateAlpha()
	{
		if (_targets.Count == 0)
		{
			return 0.0f;
		}

		double fadeStart = _stepDelaySeconds * Mathf.Max(0, _targets.Count - 1);
		if (_elapsed <= fadeStart)
		{
			return 1.0f;
		}

		float fadeProgress = Mathf.Clamp((float)((_elapsed - fadeStart) / _lingerSeconds), 0.0f, 1.0f);
		return 1.0f - fadeProgress;
	}

	private static Vector2[] BuildEllipseLinePoints(float radiusX, float radiusY, int pointCount)
	{
		int safePointCount = Mathf.Max(8, pointCount);
		Vector2[] points = new Vector2[safePointCount + 1];
		for (int i = 0; i <= safePointCount; i++)
		{
			float angle = Mathf.Tau * i / safePointCount;
			points[i] = new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
		}

		return points;
	}

	private static Vector2[] BuildDiamondPoints(float radius)
	{
		return new[]
		{
			new Vector2(0.0f, -radius),
			new Vector2(radius * 0.72f, 0.0f),
			new Vector2(0.0f, radius),
			new Vector2(-radius * 0.72f, 0.0f),
		};
	}
}
