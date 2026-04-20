using Godot;

public partial class Enemy : CharacterBody2D
{
    [Export]
    public float MoveSpeed { get; set; } = 120.0f;

    [Export]
    public string TargetGroupName { get; set; } = "player";

    [Export]
    public float StopDistance { get; set; } = 12.0f;

    private CharacterBody2D _target;

    public override void _PhysicsProcess(double delta)
    {
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
    }

    private CharacterBody2D FindTarget()
    {
        return GetTree().GetFirstNodeInGroup(TargetGroupName) as CharacterBody2D;
    }
}
