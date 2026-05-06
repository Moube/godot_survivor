using Godot;

public abstract partial class MapDecorationBase : Node2D
{
	private const int DefaultGroundDecorationZIndex = -9;
	private const string DecorationGroupName = "map_decoration";

	[Export]
	public Texture2D Texture { get; set; }

	[Export]
	public float VisualScale { get; set; } = 0.0f;

	[Export]
	public Vector2 SpriteOffset { get; set; } = Vector2.Zero;

	[Export]
	public int GroundZIndex { get; set; } = DefaultGroundDecorationZIndex;

	protected virtual string DefaultTexturePath => string.Empty;

	protected virtual float DefaultVisualScale => 1.0f;

	protected Sprite2D Sprite { get; private set; }

	private bool _createdSprite;

	public override void _Ready()
	{
		ConfigureGroundOnlySorting();
		EnsureSprite();
		ApplyVisual();
		AddToGroup(DecorationGroupName);
	}

	private void ConfigureGroundOnlySorting()
	{
		ZIndex = GroundZIndex;
		YSortEnabled = false;
	}

	private void EnsureSprite()
	{
		Sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (Sprite != null)
		{
			return;
		}

		Sprite = new Sprite2D
		{
			Name = "Sprite2D",
		};
		_createdSprite = true;
		AddChild(Sprite);
	}

	private void ApplyVisual()
	{
		Texture2D resolvedTexture = Texture ?? LoadDefaultTexture();
		if (resolvedTexture is null)
		{
			GD.PushWarning($"{Name} cannot find a decoration texture.");
			return;
		}

		Sprite.Texture = resolvedTexture;
		Sprite.Centered = true;
		if (_createdSprite || SpriteOffset != Vector2.Zero)
		{
			Sprite.Position = SpriteOffset;
		}

		if (_createdSprite || VisualScale > 0.0f)
		{
			Sprite.Scale = Vector2.One * GetResolvedVisualScale();
		}
	}

	private Texture2D LoadDefaultTexture()
	{
		if (string.IsNullOrWhiteSpace(DefaultTexturePath))
		{
			return null;
		}

		Texture2D loadedTexture = GD.Load<Texture2D>(DefaultTexturePath);
		if (loadedTexture is null)
		{
			GD.PushWarning($"{Name} cannot load decoration texture: {DefaultTexturePath}");
		}

		return loadedTexture;
	}

	private float GetResolvedVisualScale()
	{
		if (VisualScale > 0.0f)
		{
			return VisualScale;
		}

		return Mathf.Max(0.01f, DefaultVisualScale);
	}
}
