using Godot;

public partial class FlowerEnemy : EnemyBase
{
	protected override void ConfigureEnemyDefaults()
	{
		MoveSpeed = 42.0f;
		MoveAnimationFps = 5.0f;
	}

	protected override void ConfigureDropShadow(DropShadow2D dropShadow)
	{
		dropShadow.GroundOffset = new Vector2(6.0f, 23.0f);
		dropShadow.ScaleMultiplier = new Vector2(1.02f, 0.9f);
		dropShadow.FollowSourceScale = false;
		dropShadow.ShadowCanvasSize = new Vector2I(52, 20);
		dropShadow.SkewAmount = 0.0f;
		dropShadow.ShadowColor = new Color(0.0745098f, 0.0470588f, 0.0862745f, 0.42f);
		dropShadow.ContactCenter = new Vector2(0.42f, 0.60f);
		dropShadow.ContactRadius = new Vector2(0.32f, 0.20f);
		dropShadow.ContactStrength = 0.92f;
		dropShadow.CastCenter = new Vector2(0.58f, 0.60f);
		dropShadow.CastRadius = new Vector2(0.52f, 0.27f);
		dropShadow.CastAngleDegrees = 14.0f;
		dropShadow.CastStrength = 0.66f;
		dropShadow.ProceduralSoftness = 0.62f;
	}
}
