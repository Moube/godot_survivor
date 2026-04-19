using Godot;

public partial class Enemy : CharacterBody2D
{
    [Export]
    public float MoveSpeed { get; set; } = 120.0f;
}
