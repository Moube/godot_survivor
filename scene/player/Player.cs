using Godot;

public partial class Player : CharacterBody2D
{
    [Export]
    public float MoveSpeed { get; set; } = 240.0f;

    public override void _Ready()
    {
        AddToGroup("player");
    }
}
