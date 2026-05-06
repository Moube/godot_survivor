using Godot;

public partial class StoneEnemy : EnemyBase
{
	protected override void ConfigureEnemyDefaults()
	{
		MoveSpeed = 34.0f;
		MoveAnimationFps = 4.0f;
	}

	protected override void ConfigureDropShadow(DropShadow2D dropShadow)
	{
		dropShadow.GroundOffset = new Vector2(6.0f, 22.0f);
		dropShadow.ScaleMultiplier = new Vector2(1.02f, 0.88f);
		dropShadow.FollowSourceScale = false;
		dropShadow.ShadowCanvasSize = new Vector2I(50, 18);
		dropShadow.SkewAmount = 0.0f;
		dropShadow.ShadowColor = new Color(0.0588235f, 0.054902f, 0.0470588f, 0.48f);
		dropShadow.ContactCenter = new Vector2(0.42f, 0.60f);
		dropShadow.ContactRadius = new Vector2(0.36f, 0.21f);
		dropShadow.ContactStrength = 1.0f;
		dropShadow.CastCenter = new Vector2(0.54f, 0.60f);
		dropShadow.CastRadius = new Vector2(0.44f, 0.24f);
		dropShadow.CastAngleDegrees = 14.0f;
		dropShadow.CastStrength = 0.54f;
		dropShadow.ProceduralSoftness = 0.60f;
	}
}
