using Godot;

public partial class Enemy : CharacterBody2D
{
	[Export]
	public float MoveSpeed { get; set; } = 120.0f;

	[Export]
	public string TargetGroupName { get; set; } = "player";

	[Export]
	public float StopDistance { get; set; } = 12.0f;

	[Export]
	public int ContactDamage { get; set; } = 1;

	[Export]
	public float ContactDamageCooldownSeconds { get; set; } = 0.75f;

	[Export]
	public int ScoreReward { get; set; } = 100;

	private CharacterBody2D _target;
	private CombatComponent _combat;
	private double _contactDamageCooldownRemaining;

	public override void _Ready()
	{
		_combat = GetNode<CombatComponent>("CombatComponent");
		_combat.Died += OnDied;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_combat.IsDead)
		{
			Velocity = Vector2.Zero;
			return;
		}

		UpdateContactDamageCooldown(delta);
		_target ??= FindTarget();

		if (!IsInstanceValid(_target))
		{
			_target = FindTarget();
		}

		if (_target is null)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		Vector2 toTarget = _target.GlobalPosition - GlobalPosition;
		if (toTarget.LengthSquared() <= StopDistance * StopDistance)
		{
			Velocity = Vector2.Zero;
		}
		else
		{
			Velocity = toTarget.Normalized() * MoveSpeed;
			Rotation = toTarget.Angle();
		}

		MoveAndSlide();
		TryApplyContactDamage();
	}

	private void UpdateContactDamageCooldown(double delta)
	{
		if (_contactDamageCooldownRemaining > 0.0)
		{
			_contactDamageCooldownRemaining -= delta;
		}
	}

	private void TryApplyContactDamage()
	{
		if (_contactDamageCooldownRemaining > 0.0)
		{
			return;
		}

		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			KinematicCollision2D collision = GetSlideCollision(i);
			if (collision.GetCollider() is not Node collider)
			{
				continue;
			}

			CombatComponent targetCombat = collider.GetNodeOrNull<CombatComponent>("CombatComponent");
			if (targetCombat is null || targetCombat.IsDead)
			{
				continue;
			}

			if (!collider.IsInGroup(TargetGroupName))
			{
				continue;
			}

			if (targetCombat.ApplyDamage(ContactDamage))
			{
				_contactDamageCooldownRemaining = ContactDamageCooldownSeconds;
				return;
			}
		}
	}

	private CharacterBody2D FindTarget()
	{
		return GetTree().GetFirstNodeInGroup(TargetGroupName) as CharacterBody2D;
	}

	private void OnDied()
	{
		GameSession.Instance?.AddScore(ScoreReward);
		QueueFree();
	}
}
