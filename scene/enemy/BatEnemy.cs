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
		dropShadow.GroundOffset = new Vector2(6.0f, 17.0f);
		dropShadow.ScaleMultiplier = new Vector2(0.75f, 0.75f);
		dropShadow.FollowSourceScale = false;
		dropShadow.ShadowCanvasSize = new Vector2I(46, 16);
		dropShadow.SkewAmount = 0.0f;
		dropShadow.ShadowColor = new Color(0.0705882f, 0.054902f, 0.105882f, 0.24f);
		dropShadow.ContactCenter = new Vector2(0.36f, 0.58f);
		dropShadow.ContactRadius = new Vector2(0.28f, 0.16f);
		dropShadow.ContactStrength = 0.62f;
		dropShadow.CastCenter = new Vector2(0.58f, 0.58f);
		dropShadow.CastRadius = new Vector2(0.46f, 0.22f);
		dropShadow.CastAngleDegrees = 14.0f;
		dropShadow.CastStrength = 0.42f;
		dropShadow.ProceduralSoftness = 0.72f;
	}
}
