using Godot;

public partial class MagicWandWeapon : Weapon2D
{
	[Export]
	public float OrbitRadius { get; set; } = 42.0f;

	[Export]
	public float OrbitAngularSpeedDegrees { get; set; } = 115.0f;

	[Export]
	public float OrbitStartAngleDegrees { get; set; } = -35.0f;

	private float _orbitAngle;

	public override void _Ready()
	{
		base._Ready();
		_orbitAngle = Mathf.DegToRad(OrbitStartAngleDegrees);
		UpdateOrbitPosition();
	}

	protected override void UpdateWeapon(double delta)
	{
		_orbitAngle += Mathf.DegToRad(OrbitAngularSpeedDegrees) * (float)delta;
		UpdateOrbitPosition();
		UpdateAimRotation();
	}

	protected override Vector2 GetProjectileDirection()
	{
		Vector2 origin = Muzzle?.GlobalPosition ?? GlobalPosition;
		Vector2 direction = GetGlobalMousePosition() - origin;
		return direction == Vector2.Zero ? Vector2.Right.Rotated(GlobalRotation) : direction.Normalized();
	}

	private void UpdateOrbitPosition()
	{
		Position = new Vector2(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle)) * OrbitRadius;
	}

	private void UpdateAimRotation()
	{
		Vector2 direction = GetGlobalMousePosition() - GlobalPosition;
		if (direction == Vector2.Zero)
		{
			return;
		}

		GlobalRotation = direction.Angle();
	}
}
