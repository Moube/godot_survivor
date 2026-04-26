using Godot;

public partial class Enemy : EnemyBase
{
	protected override void ConfigureEnemyDefaults()
	{
		MoveSpeed = 50.0f;
		MoveAnimationFps = 2.0f;
	}

	protected override void ConfigureDropShadow(DropShadow2D dropShadow)
	{
		dropShadow.GroundOffset = new Vector2(7.0f, 19.0f);
		dropShadow.ScaleMultiplier = Vector2.One;
		dropShadow.FollowSourceScale = false;
		dropShadow.ShadowCanvasSize = new Vector2I(50, 20);
		dropShadow.SkewAmount = 0.0f;
		dropShadow.ShadowColor = new Color(0.0705882f, 0.0862745f, 0.0431373f, 0.44f);
		dropShadow.ContactCenter = new Vector2(0.36f, 0.58f);
		dropShadow.ContactRadius = new Vector2(0.34f, 0.21f);
		dropShadow.ContactStrength = 0.95f;
		dropShadow.CastCenter = new Vector2(0.58f, 0.58f);
		dropShadow.CastRadius = new Vector2(0.5f, 0.28f);
		dropShadow.CastAngleDegrees = 14.0f;
		dropShadow.CastStrength = 0.7f;
		dropShadow.ProceduralSoftness = 0.6f;
	}
}
