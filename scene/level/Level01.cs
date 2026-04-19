using Godot;

public partial class Level01 : Node2D
{
	[Export]
	public PackedScene PlayerScene { get; set; }

	[Export]
	public int GridSize { get; set; } = 64;

	[Export]
	public int GridExtent { get; set; } = 2048;

	public override void _Ready()
	{
		SpawnPlayer();
		QueueRedraw();
	}

	private void SpawnPlayer()
	{
		if (PlayerScene is null)
		{
			GD.PushError("PlayerScene is not assigned on Level01.");
			return;
		}

		Marker2D spawnPoint = GetNode<Marker2D>("PlayerSpawn");
		Node playerInstance = PlayerScene.Instantiate();

		if (playerInstance is not CharacterBody2D player)
		{
			GD.PushError("PlayerScene must instantiate a CharacterBody2D.");
			playerInstance.QueueFree();
			return;
		}

		player.GlobalPosition = spawnPoint.GlobalPosition;
		AddChild(player);
	}

	public override void _Draw()
	{
		Color minorLineColor = new(0.18f, 0.22f, 0.28f, 1.0f);
		Color majorLineColor = new(0.25f, 0.31f, 0.39f, 1.0f);
		Color axisColor = new(0.88f, 0.45f, 0.29f, 1.0f);

		for (int x = -GridExtent; x <= GridExtent; x += GridSize)
		{
			Color lineColor = x == 0 ? axisColor : (x % (GridSize * 4) == 0 ? majorLineColor : minorLineColor);
			DrawLine(new Vector2(x, -GridExtent), new Vector2(x, GridExtent), lineColor, 2.0f);
		}

		for (int y = -GridExtent; y <= GridExtent; y += GridSize)
		{
			Color lineColor = y == 0 ? axisColor : (y % (GridSize * 4) == 0 ? majorLineColor : minorLineColor);
			DrawLine(new Vector2(-GridExtent, y), new Vector2(GridExtent, y), lineColor, 2.0f);
		}
	}
}
