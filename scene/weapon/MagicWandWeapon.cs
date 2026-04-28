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
	private float _orbitSlotOffsetRadians;

	public override void _Ready()
	{
		base._Ready();
		_orbitAngle = GetOrbitStartAngleRadians();
		UpdateOrbitPosition();
	}

	public override void SetInventorySlot(int slotIndex, int totalSlots)
	{
		int safeTotalSlots = Mathf.Max(1, totalSlots);
		int safeSlotIndex = Mathf.Clamp(slotIndex, 0, safeTotalSlots - 1);
		_orbitSlotOffsetRadians = Mathf.Tau * safeSlotIndex / safeTotalSlots;

		if (!IsNodeReady())
		{
			return;
		}

		_orbitAngle = GetOrbitStartAngleRadians();
		UpdateOrbitPosition();
	}

	protected override void OnEmitterUpdate(double delta)
	{
		_orbitAngle += Mathf.DegToRad(OrbitAngularSpeedDegrees) * (float)delta;
		UpdateOrbitPosition();
	}

	private float GetOrbitStartAngleRadians()
	{
		return Mathf.DegToRad(OrbitStartAngleDegrees) + _orbitSlotOffsetRadians;
	}

	private void UpdateOrbitPosition()
	{
		Position = new Vector2(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle)) * OrbitRadius;
	}
}
