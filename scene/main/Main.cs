using Godot;

public partial class Main : Control
{
	private const string FirstLevelScenePath = "res://scene/level/Level01.tscn";

	public override void _Ready()
	{
		Button startButton = GetNode<Button>("CenterContainer/PanelContainer/VBoxContainer/StartButton");
		startButton.Pressed += OnStartButtonPressed;
	}

	private void OnStartButtonPressed()
	{
		GetTree().ChangeSceneToFile(FirstLevelScenePath);
	}
}
