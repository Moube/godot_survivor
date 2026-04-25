using Godot;

public partial class DropShadow2D : Sprite2D
{
	[Export]
	public NodePath SourceSpritePath { get; set; } = new("../Sprite2D");

	[Export]
	public Vector2 GroundOffset { get; set; } = new(6.0f, 2.0f);

	[Export]
	public Vector2 ScaleMultiplier { get; set; } = new(1.35f, 0.22f);

	[Export]
	public bool FollowSourceScale { get; set; } = true;

	[Export]
	public Vector2I ShadowCanvasSize { get; set; } = new(64, 32);

	[Export(PropertyHint.Range, "-89,89,0.1")]
	public float RotationDegreesOffset { get; set; }

	[Export(PropertyHint.Range, "-2,2,0.01")]
	public float SkewAmount { get; set; } = -0.3f;

	[Export]
	public Color ShadowColor { get; set; } = new(0.14f, 0.16f, 0.13f, 0.38f);

	[Export]
	public Vector2 ContactCenter { get; set; } = new(0.22f, 0.52f);

	[Export]
	public Vector2 ContactRadius { get; set; } = new(0.16f, 0.12f);

	[Export(PropertyHint.Range, "-180,180,0.1")]
	public float ContactAngleDegrees { get; set; }

	[Export(PropertyHint.Range, "0,2,0.01")]
	public float ContactStrength { get; set; } = 1.0f;

	[Export]
	public Vector2 CastCenter { get; set; } = new(0.62f, 0.58f);

	[Export]
	public Vector2 CastRadius { get; set; } = new(0.46f, 0.28f);

	[Export(PropertyHint.Range, "-180,180,0.1")]
	public float CastAngleDegrees { get; set; } = 20.0f;

	[Export(PropertyHint.Range, "0,2,0.01")]
	public float CastStrength { get; set; } = 0.72f;

	[Export(PropertyHint.Range, "0.01,0.95,0.01")]
	public float ProceduralSoftness { get; set; } = 0.42f;

	private Sprite2D _sourceSprite;
	private ShaderMaterial _shaderMaterial;
	private ImageTexture _proceduralTexture;
	private Vector2I _proceduralTextureSize;

	public override void _Ready()
	{
		Centered = false;
		ZAsRelative = true;
		ZIndex = -1;
		if (Material is ShaderMaterial material)
		{
			_shaderMaterial = (ShaderMaterial)material.Duplicate();
			Material = _shaderMaterial;
		}
		SyncShadow();
	}

	public override void _Process(double delta)
	{
		SyncShadow();
	}

	private void SyncShadow()
	{
		_sourceSprite = ResolveSourceSprite();
		if (_sourceSprite == null || _sourceSprite.Texture == null || _shaderMaterial == null || _shaderMaterial.Shader == null)
		{
			Visible = false;
			if (_shaderMaterial == null || _shaderMaterial.Shader == null)
			{
				Material = null;
			}
			return;
		}

		Visible = _sourceSprite.Visible;
		Vector2 frameSize = SyncShadowTexture();
		Vector2 sourceScale = FollowSourceScale
			? new Vector2(Mathf.Abs(_sourceSprite.Scale.X), Mathf.Abs(_sourceSprite.Scale.Y))
			: Vector2.One;
		Scale = new Vector2(
			sourceScale.X * ScaleMultiplier.X,
			sourceScale.Y * ScaleMultiplier.Y);
		RotationDegrees = RotationDegreesOffset;
		Skew = SkewAmount;

		Position = new Vector2(
			GroundOffset.X - frameSize.X * Scale.X * 0.5f,
			GroundOffset.Y - frameSize.Y * Scale.Y);

		ApplyMaterialParameters();
	}

	private Vector2 SyncShadowTexture()
	{
		TextureFilter = _sourceSprite.TextureFilter;
		TextureRepeat = _sourceSprite.TextureRepeat;
		Texture = GetProceduralTexture();
		Hframes = 1;
		Vframes = 1;
		Frame = 0;
		RegionEnabled = false;
		FlipH = false;
		FlipV = false;
		return new Vector2(
			Mathf.Max(1, ShadowCanvasSize.X),
			Mathf.Max(1, ShadowCanvasSize.Y));
	}

	private Texture2D GetProceduralTexture()
	{
		Vector2I size = new(
			Mathf.Max(1, ShadowCanvasSize.X),
			Mathf.Max(1, ShadowCanvasSize.Y));

		if (_proceduralTexture != null && _proceduralTextureSize == size)
		{
			return _proceduralTexture;
		}

		Image image = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
		image.Fill(Colors.White);
		_proceduralTexture = ImageTexture.CreateFromImage(image);
		_proceduralTextureSize = size;
		return _proceduralTexture;
	}

	private void ApplyMaterialParameters()
	{
		if (_shaderMaterial == null)
		{
			return;
		}

		_shaderMaterial.SetShaderParameter("shadow_color", ShadowColor);
		_shaderMaterial.SetShaderParameter("contact_center", ContactCenter);
		_shaderMaterial.SetShaderParameter("contact_radius", ContactRadius);
		_shaderMaterial.SetShaderParameter("contact_angle", Mathf.DegToRad(ContactAngleDegrees));
		_shaderMaterial.SetShaderParameter("contact_strength", ContactStrength);
		_shaderMaterial.SetShaderParameter("cast_center", CastCenter);
		_shaderMaterial.SetShaderParameter("cast_radius", CastRadius);
		_shaderMaterial.SetShaderParameter("cast_angle", Mathf.DegToRad(CastAngleDegrees));
		_shaderMaterial.SetShaderParameter("cast_strength", CastStrength);
		_shaderMaterial.SetShaderParameter("procedural_softness", ProceduralSoftness);
	}

	private Sprite2D ResolveSourceSprite()
	{
		if (!GodotObject.IsInstanceValid(_sourceSprite))
		{
			_sourceSprite = null;
		}

		if (_sourceSprite != null)
		{
			return _sourceSprite;
		}

		if (!SourceSpritePath.IsEmpty)
		{
			_sourceSprite = GetNodeOrNull<Sprite2D>(SourceSpritePath);
		}

		if (_sourceSprite == null)
		{
			_sourceSprite = GetParent()?.GetNodeOrNull<Sprite2D>("Sprite2D");
		}

		return _sourceSprite;
	}
}
