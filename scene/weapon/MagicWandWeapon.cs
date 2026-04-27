using Godot;

public partial class MagicWandWeapon : ProjectileEmitterWeapon2D
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

	protected override void OnEmitterUpdate(double delta)
	{
		_orbitAngle += Mathf.DegToRad(OrbitAngularSpeedDegrees) * (float)delta;
		UpdateOrbitPosition();
	}

	private void UpdateOrbitPosition()
	{
		Position = new Vector2(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle)) * OrbitRadius;
	}
}
