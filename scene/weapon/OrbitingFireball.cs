using Godot;

public partial class OrbitingFireball : Area2D
{
	[Export]
	public NodePath SpritePath { get; set; } = new("Sprite2D");

	[Export]
	public NodePath CollisionShapePath { get; set; } = new("CollisionShape2D");

	[Export(PropertyHint.File, "*.png")]
	public string TexturePath { get; set; } = "res://asset/art/item/weapon_repulsion_fire_shield.png";

	[Export]
	public float CollisionRadius { get; set; } = 10.0f;

	[Export]
	public float VisualScale { get; set; } = 0.40f;

	[Export]
	public float VisualSpinDegreesPerSecond { get; set; } = 130.0f;

	[Export]
	public float PulseAmplitude { get; set; } = 0.08f;

	[Export]
	public float PulseSpeed { get; set; } = 7.5f;

	private Sprite2D _sprite;
	private CollisionShape2D _collisionShape;
	private double _elapsed;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<Sprite2D>(SpritePath);
		_collisionShape = GetNodeOrNull<CollisionShape2D>(CollisionShapePath);
		ApplyTexture();
		ApplyCollisionRadius();
		ApplyVisualScale();
	}

	public override void _Process(double delta)
	{
		_elapsed += delta;
		if (_sprite == null)
		{
			return;
		}

		if (!Mathf.IsZeroApprox(VisualSpinDegreesPerSecond))
		{
			_sprite.RotationDegrees += VisualSpinDegreesPerSecond * (float)delta;
		}

		ApplyVisualScale();
	}

	public void Configure(string texturePath, float collisionRadius, float visualScale, float visualSpinDegreesPerSecond)
	{
		if (!string.IsNullOrWhiteSpace(texturePath))
		{
			TexturePath = texturePath;
		}

		CollisionRadius = Mathf.Max(1.0f, collisionRadius);
		VisualScale = Mathf.Max(0.05f, visualScale);
		VisualSpinDegreesPerSecond = visualSpinDegreesPerSecond;
		ApplyTexture();
		ApplyCollisionRadius();
		ApplyVisualScale();
	}

	private void ApplyTexture()
	{
		_sprite ??= GetNodeOrNull<Sprite2D>(SpritePath);
		if (_sprite == null)
		{
			return;
		}

		Texture2D texture = LoadTexture(TexturePath);
		if (texture != null)
		{
			_sprite.Texture = texture;
		}
	}

	private void ApplyCollisionRadius()
	{
		_collisionShape ??= GetNodeOrNull<CollisionShape2D>(CollisionShapePath);
		if (_collisionShape?.Shape is not CircleShape2D circleShape)
		{
			return;
		}

		CircleShape2D editableShape = circleShape.Duplicate() as CircleShape2D;
		if (editableShape == null)
		{
			return;
		}

		editableShape.Radius = Mathf.Max(1.0f, CollisionRadius);
		_collisionShape.Shape = editableShape;
	}

	private void ApplyVisualScale()
	{
		_sprite ??= GetNodeOrNull<Sprite2D>(SpritePath);
		if (_sprite == null)
		{
			return;
		}

		float pulse = 1.0f + Mathf.Sin((float)_elapsed * Mathf.Max(0.0f, PulseSpeed)) * Mathf.Max(0.0f, PulseAmplitude);
		_sprite.Scale = Vector2.One * Mathf.Max(0.05f, VisualScale * pulse);
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
