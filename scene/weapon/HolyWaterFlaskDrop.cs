using Godot;

public partial class HolyWaterFlaskDrop : Node2D
{
	[Export]
	public PackedScene AreaScene { get; set; }

	[Export]
	public NodePath VisualRootPath { get; set; } = new("VisualRoot");

	[Export]
	public NodePath BottleSpritePath { get; set; } = new("VisualRoot/BottleSprite");

	[Export(PropertyHint.File, "*.png")]
	public string BottleTexturePath { get; set; } = "res://asset/art/item/weapon_holy_water.png";

	[Export]
	public float DropHeight { get; set; } = 820.0f;

	[Export]
	public float DropDurationSeconds { get; set; } = 0.64f;

	[Export]
	public float RotationDegreesPerSecond { get; set; } = 720.0f;

	private int _damage = 1;
	private float _radius = 48.0f;
	private float _areaDurationSeconds = 3.2f;
	private float _damageIntervalSeconds = 0.55f;
	private double _elapsed;
	private Node2D _visualRoot;
	private Sprite2D _bottleSprite;
	private bool _hasLanded;

	public void Initialize(int damage, float radius, float areaDurationSeconds, float damageIntervalSeconds)
	{
		_damage = Mathf.Max(1, damage);
		_radius = Mathf.Max(4.0f, radius);
		_areaDurationSeconds = Mathf.Max(0.1f, areaDurationSeconds);
		_damageIntervalSeconds = Mathf.Max(0.05f, damageIntervalSeconds);
		_elapsed = 0.0;
		_hasLanded = false;
		ApplyDropStart();
	}

	public override void _Ready()
	{
		_visualRoot = GetNodeOrNull<Node2D>(VisualRootPath);
		_bottleSprite = GetNodeOrNull<Sprite2D>(BottleSpritePath);
		ConfigureBottleSprite();
		ApplyDropStart();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_hasLanded)
		{
			return;
		}

		_elapsed += delta;
		float rawProgress = (float)(_elapsed / Mathf.Max(0.01f, DropDurationSeconds));
		if (rawProgress >= 1.0f)
		{
			Land();
			return;
		}

		float t = Mathf.Clamp(rawProgress, 0.0f, 1.0f);
		float eased = 1.0f - Mathf.Pow(1.0f - t, 2.0f);

		if (_visualRoot != null)
		{
			_visualRoot.Position = new Vector2(0.0f, -DropHeight * (1.0f - eased));
			_visualRoot.RotationDegrees += RotationDegreesPerSecond * (float)delta;
			float squash = t > 0.88f ? 1.0f + (t - 0.88f) * 0.9f : 1.0f;
			_visualRoot.Scale = new Vector2(1.0f + (squash - 1.0f) * 0.35f, 1.0f / squash);
		}
	}

	private void ApplyDropStart()
	{
		_visualRoot ??= GetNodeOrNull<Node2D>(VisualRootPath);
		if (_visualRoot == null)
		{
			return;
		}

		_visualRoot.Position = new Vector2(0.0f, -DropHeight);
		_visualRoot.RotationDegrees = 0.0f;
		_visualRoot.Scale = Vector2.One;
		_visualRoot.Visible = true;
	}

	private void Land()
	{
		if (_hasLanded)
		{
			return;
		}

		_hasLanded = true;
		SetPhysicsProcess(false);
		if (_visualRoot != null)
		{
			_visualRoot.Visible = false;
		}

		AudioManager.Instance?.PlayHolyWaterBreak(this);
		if (AreaScene == null)
		{
			GD.PushWarning($"{Name} cannot create holy water area because AreaScene is not assigned.");
			QueueFree();
			return;
		}

		Node instance = AreaScene.Instantiate();
		if (instance is not HolyWaterArea area)
		{
			GD.PushError("Holy water AreaScene must instantiate a HolyWaterArea.");
			instance.QueueFree();
			QueueFree();
			return;
		}

		GetParent()?.AddChild(area);
		area.GlobalPosition = GlobalPosition;
		area.Initialize(_damage, _radius, _areaDurationSeconds, _damageIntervalSeconds);
		QueueFree();
	}

	private void ConfigureBottleSprite()
	{
		if (_bottleSprite == null)
		{
			return;
		}

		Texture2D texture = LoadTexture(BottleTexturePath);
		if (texture != null)
		{
			_bottleSprite.Texture = texture;
		}
	}

	private static Texture2D LoadTexture(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}

		Texture2D texture = ResourceLoader.Load<Texture2D>(path);
		if (texture != null)
		{
			return texture;
		}

		if (!FileAccess.FileExists(path))
		{
			return null;
		}

		Image image = Image.LoadFromFile(path);
		return image == null || image.IsEmpty() ? null : ImageTexture.CreateFromImage(image);
	}
}
