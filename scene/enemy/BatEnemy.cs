using Godot;

public partial class BatEnemy : EnemyBase
{
	protected override void ConfigureEnemyDefaults()
	{
		MoveSpeed = 54.0f;
		MoveAnimationFps = 7.0f;
	}

	protected override void ConfigureDropShadow(DropShadow2D dropShadow)
	{
		dropShadow.GroundOffset = new Vector2(4.0f, 18.0f);
		dropShadow.ScaleMultiplier = new Vector2(0.82f, 0.82f);
		dropShadow.FollowSourceScale = false;
		dropShadow.ShadowCanvasSize = new Vector2I(36, 28);
		dropShadow.SkewAmount = 0.0f;
		dropShadow.ShadowColor = new Color(0.0705882f, 0.054902f, 0.105882f, 0.36f);
		dropShadow.ContactCenter = new Vector2(0.5f, 0.54f);
		dropShadow.ContactRadius = new Vector2(0.30f, 0.30f);
		dropShadow.ContactStrength = 0.84f;
		dropShadow.CastCenter = new Vector2(0.5f, 0.54f);
		dropShadow.CastRadius = new Vector2(0.34f, 0.34f);
		dropShadow.CastAngleDegrees = 0.0f;
		dropShadow.CastStrength = 0.34f;
		dropShadow.ProceduralSoftness = 0.70f;
	}
}
