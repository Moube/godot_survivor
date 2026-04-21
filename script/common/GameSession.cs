using Godot;

public partial class GameSession : Node
{
	[Signal]
	public delegate void ScoreChangedEventHandler(int score);

	[Signal]
	public delegate void GameOverEventHandler(int finalScore);

	public static GameSession Instance { get; private set; }

	public int Score { get; private set; }

	public bool IsGameOver { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void StartNewRun()
	{
		Score = 0;
		IsGameOver = false;
		GetTree().Paused = false;
		EmitSignal(SignalName.ScoreChanged, Score);
	}

	public void AddScore(int amount)
	{
		if (IsGameOver || amount == 0)
		{
			return;
		}

		Score += amount;
		EmitSignal(SignalName.ScoreChanged, Score);
		GD.Print($"Score: {Score}");
	}

	public void TriggerGameOver()
	{
		if (IsGameOver)
		{
			return;
		}

		IsGameOver = true;
		EmitSignal(SignalName.GameOver, Score);
		GetTree().Paused = true;
		GD.Print($"Game Over. Final Score: {Score}");
	}
}
