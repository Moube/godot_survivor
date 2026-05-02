using Godot;

public partial class HolyWaterFlameField : Node2D
{
	[Export]
	public NodePath SpritePath { get; set; } = new("Sprite2D");

	[Export(PropertyHint.File, "*.png")]
	public string TexturePath { get; set; } = "res://asset/art/effects/holy_water_flame_field_strip_6f.png";

	[Export]
	public int FrameCount { get; set; } = 6;

	[Export]
	public float AnimationFps { get; set; } = 10.0f;

	[Export]
	public float BaseRadius { get; set; } = 48.0f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float Alpha { get; set; } = 0.96f;

	private double _elapsed;
	private Sprite2D _sprite;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<Sprite2D>(SpritePath);
		ConfigureSprite();
		ApplyAlpha();
	}

	public override void _Process(double delta)
	{
		if (_sprite == null)
		{
			return;
		}

		_elapsed += delta;
		int frameCount = Mathf.Max(1, FrameCount);
		_sprite.Frame = (int)(_elapsed * Mathf.Max(0.0f, AnimationFps)) % frameCount;
	}

	public void SetRadius(float radius)
	{
		float safeBaseRadius = Mathf.Max(1.0f, BaseRadius);
		float scale = Mathf.Max(1.0f, radius) / safeBaseRadius;
		Scale = new Vector2(scale, scale);
	}

	private void ConfigureSprite()
	{
		if (_sprite == null)
		{
			return;
		}

		_sprite.Hframes = Mathf.Max(1, FrameCount);
		_sprite.Frame = 0;

		Texture2D texture = LoadTexture(TexturePath);
		if (texture != null)
		{
			_sprite.Texture = texture;
		}
	}

	private void ApplyAlpha()
	{
		Color modulate = Modulate;
		modulate.A = Mathf.Clamp(Alpha, 0.0f, 1.0f);
		Modulate = modulate;
	}

	private static Texture2D LoadTexture(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}

		if (FileAccess.FileExists(path))
		{
			Image image = Image.LoadFromFile(path);
			if (image != null && !image.IsEmpty())
			{
				return ImageTexture.CreateFromImage(image);
			}
		}

		return ResourceLoader.Load<Texture2D>(path);
	}
}
