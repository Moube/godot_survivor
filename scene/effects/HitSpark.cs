using Godot;

public partial class HitSpark : Node2D
{
	[Export]
	public NodePath SpritePath { get; set; } = new("Sprite2D");

	[Export]
	public float AnimationFps { get; set; } = 22.0f;

	[Export]
	public int FrameCount { get; set; } = 4;

	private Sprite2D _sprite;
	private double _animationTime;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<Sprite2D>(SpritePath);
		if (_sprite is null)
		{
			GD.PushWarning($"{Name} is missing Sprite2D at {SpritePath}.");
			QueueFree();
			return;
		}

		_sprite.Frame = 0;
	}

	public override void _Process(double delta)
	{
		if (_sprite is null)
		{
			return;
		}

		_animationTime += delta * Mathf.Max(0.1f, AnimationFps);
		int frame = (int)_animationTime;
		if (frame >= FrameCount)
		{
			QueueFree();
			return;
		}

		_sprite.Frame = frame;
	}
}
