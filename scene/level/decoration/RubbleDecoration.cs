using Godot;

public partial class RubbleDecoration : MapDecorationBase
{
	protected override string DefaultTexturePath => "res://asset/art/level/decoration_rubble.png";

	protected override float DefaultVisualScale => 1.0f;

	public override void _Ready()
	{
		base._Ready();
		ConfigureStoneSprites();
	}

	private void ConfigureStoneSprites()
	{
		Texture2D stoneTexture = Texture ?? GD.Load<Texture2D>(DefaultTexturePath);
		if (stoneTexture is null || Sprite is null)
		{
			return;
		}

		ConfigureSprite(Sprite, stoneTexture);
		ConfigureSprite(EnsureSprite("StoneMedium", new Vector2(-19.0f, -1.0f), 0.25f), stoneTexture);
		ConfigureSprite(EnsureSprite("StoneSmall", new Vector2(19.0f, -2.0f), 0.15f), stoneTexture);
	}

	private Sprite2D EnsureSprite(string nodeName, Vector2 defaultPosition, float defaultScale)
	{
		Sprite2D sprite = GetNodeOrNull<Sprite2D>(nodeName);
		if (sprite != null)
		{
			return sprite;
		}

		sprite = new Sprite2D
		{
			Name = nodeName,
			Position = defaultPosition,
			Scale = Vector2.One * defaultScale,
		};
		AddChild(sprite);
		return sprite;
	}

	private static void ConfigureSprite(Sprite2D sprite, Texture2D texture)
	{
		sprite.Texture = texture;
		sprite.Centered = true;
	}
}
