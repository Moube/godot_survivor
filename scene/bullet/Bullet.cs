using Godot;

public partial class Bullet : Area2D
{
    [Export]
    public float Speed { get; set; } = 640.0f;

    [Export]
    public float LifetimeSeconds { get; set; } = 1.5f;

    public Vector2 Direction { get; private set; } = Vector2.Right;

    public void Initialize(Vector2 direction)
    {
        Direction = direction.Normalized();
        Rotation = Direction.Angle();
    }
}
