using Godot;

public partial class ExperienceGem : Node2D
{
	private enum GemState
	{
		Launching,
		Grounded,
		Attracting,
	}

	[Export]
	public int ExperienceValue { get; set; } = 1;

	[Export]
	public float LaunchDurationSeconds { get; set; } = 0.42f;

	[Export]
	public float LaunchHeight { get; set; } = 34.0f;

	[Export]
	public float MaxLaunchDistance { get; set; } = 38.0f;

	[Export]
	public float AttractionSpeed { get; set; } = 360.0f;

	[Export]
	public float AttractionAcceleration { get; set; } = 1100.0f;

	[Export]
	public float CollectDistance { get; set; } = 8.0f;

	private readonly RandomNumberGenerator _random = new();

	private Node2D _visualRoot;
	private Sprite2D _textureVisual;
	private Polygon2D _polygonVisual;
	private Vector2 _launchStartPosition;
	private Vector2 _landingPosition;
	private double _launchElapsed;
	private float _currentAttractionSpeed;
	private Player _targetPlayer;
	private GemState _state = GemState.Launching;

	public override void _Ready()
	{
		_random.Randomize();
		_visualRoot = GetNode<Node2D>("VisualRoot");
		_textureVisual = GetNodeOrNull<Sprite2D>("VisualRoot/TextureSlot");
		_polygonVisual = GetNodeOrNull<Polygon2D>("VisualRoot/PolygonVisual");
		UpdateVisualState();
	}

	public override void _Process(double delta)
	{
		if (GameSession.Instance?.IsGameOver == true)
		{
			return;
		}

		switch (_state)
		{
			case GemState.Launching:
				UpdateLaunch(delta);
				break;

			case GemState.Grounded:
				TryStartAttraction();
				break;

			case GemState.Attracting:
				UpdateAttraction(delta);
				break;
		}
	}

	public void Initialize(int experienceValue)
	{
		ExperienceValue = Mathf.Max(1, experienceValue);
		_launchStartPosition = GlobalPosition;
		_landingPosition = _launchStartPosition + GetRandomLaunchOffset();
		_launchElapsed = 0.0;
		_currentAttractionSpeed = AttractionSpeed;
		_state = GemState.Launching;
	}

	private Vector2 GetRandomLaunchOffset()
	{
		float angle = _random.RandfRange(0.0f, Mathf.Tau);
		float distance = _random.RandfRange(MaxLaunchDistance * 0.35f, MaxLaunchDistance);
		return Vector2.Right.Rotated(angle) * distance;
	}

	private void UpdateLaunch(double delta)
	{
		_launchElapsed += delta;
		float progress = Mathf.Clamp((float)(_launchElapsed / Mathf.Max(0.01f, LaunchDurationSeconds)), 0.0f, 1.0f);
		GlobalPosition = _launchStartPosition.Lerp(_landingPosition, progress);

		float height = Mathf.Sin(progress * Mathf.Pi) * LaunchHeight;
		_visualRoot.Position = new Vector2(0.0f, -height);

		if (progress >= 1.0f)
		{
			GlobalPosition = _landingPosition;
			_visualRoot.Position = Vector2.Zero;
			_state = GemState.Grounded;
			UpdateVisualState();
		}
	}

	private void TryStartAttraction()
	{
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player is null || !IsInstanceValid(player) || player.IsDead)
		{
			return;
		}

		if (GlobalPosition.DistanceSquaredTo(player.GlobalPosition) > player.PickupRange * player.PickupRange)
		{
			return;
		}

		_targetPlayer = player;
		_currentAttractionSpeed = AttractionSpeed;
		_state = GemState.Attracting;
		UpdateVisualState();
	}

	private void UpdateAttraction(double delta)
	{
		if (_targetPlayer is null || !IsInstanceValid(_targetPlayer) || _targetPlayer.IsDead)
		{
			_targetPlayer = null;
			_state = GemState.Grounded;
			UpdateVisualState();
			return;
		}

		Vector2 targetPosition = _targetPlayer.GlobalPosition;
		Vector2 toTarget = targetPosition - GlobalPosition;
		float distance = toTarget.Length();
		if (distance <= CollectDistance)
		{
			Collect(targetPosition);
			return;
		}

		_currentAttractionSpeed += AttractionAcceleration * (float)delta;
		float step = Mathf.Min(distance, _currentAttractionSpeed * (float)delta);
		if (step >= distance - CollectDistance)
		{
			Collect(targetPosition);
			return;
		}

		GlobalPosition += toTarget.Normalized() * step;
	}

	private void Collect(Vector2 targetPosition)
	{
		GlobalPosition = targetPosition;
		AudioManager.Instance?.PlayExperiencePickup(this);
		ExperienceController.Instance?.AddExperience(ExperienceValue);
		QueueFree();
	}

	private void UpdateVisualState()
	{
		bool hasTextureVisual = _textureVisual?.Texture is not null;
		if (_textureVisual is not null)
		{
			_textureVisual.Visible = hasTextureVisual;
			_textureVisual.Modulate = _state switch
			{
				GemState.Launching => new Color(1.0f, 0.94f, 0.72f, 0.85f),
				GemState.Attracting => new Color(1.0f, 0.98f, 0.78f, 1.0f),
				_ => Colors.White,
			};
		}

		if (_polygonVisual is null)
		{
			return;
		}

		_polygonVisual.Visible = !hasTextureVisual;
		_polygonVisual.Color = _state switch
		{
			GemState.Launching => new Color(1.0f, 0.82f, 0.28f, 0.85f),
			GemState.Attracting => new Color(1.0f, 0.95f, 0.52f, 1.0f),
			_ => new Color(1.0f, 0.65f, 0.08f, 1.0f),
		};
	}
}
