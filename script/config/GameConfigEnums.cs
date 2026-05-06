public enum WeaponBehaviorType
{
	ProjectileEmitter,
	GroundArea,
	PlayerAura,
	OrbitingObject,
	AreaPulse,
	TargetedStrike,
	ChainDamage,
}

public enum ProjectileFireMode
{
	MouseDirection,
	NearestEnemy,
	RandomDirection,
	PlayerLastMoveDirection,
	ForwardSweep,
	MultipleNearest,
	FixedPattern,
}

public enum UpgradeRewardType
{
	NewWeapon,
	WeaponUpgrade,
	NewPassive,
	PassiveUpgrade,
}

public enum PlayerStatType
{
	MoveSpeed,
	MaxHealth,
	PickupRange,
	WeaponDamageMultiplier,
	WeaponCooldownMultiplier,
}
