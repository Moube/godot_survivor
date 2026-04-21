using Godot;

public partial class Hud : CanvasLayer
{
	private const string LevelScenePath = "res://scene/level/Level01.tscn";

	private ProgressBar _healthBar;
	private Label _scoreValueLabel;
	private Control _gameOverPanel;
	private Label _finalScoreLabel;
	private Button _restartButton;

	public override void _Ready()
	{
		_healthBar = GetNode<ProgressBar>("BottomCenter/HealthBar");
		_scoreValueLabel = GetNode<Label>("TopRight/ScorePanel/ScoreMargin/ScoreRow/ScoreValue");
		_gameOverPanel = GetNode<Control>("GameOverCenter/PanelContainer");
		_finalScoreLabel = GetNode<Label>("GameOverCenter/PanelContainer/VBoxContainer/GameOverMargin/Content/FinalScoreLabel");
		_restartButton = GetNode<Button>("GameOverCenter/PanelContainer/VBoxContainer/GameOverMargin/Content/RestartButton");
		_restartButton.Pressed += OnRestartButtonPressed;

		if (GameSession.Instance is null)
		{
			GD.PushError("GameSession autoload is unavailable for HUD.");
			return;
		}

		GameSession.Instance.ScoreChanged += OnScoreChanged;
		GameSession.Instance.PlayerHealthChanged += OnPlayerHealthChanged;
		GameSession.Instance.GameOver += OnGameOver;

		OnScoreChanged(GameSession.Instance.Score);
		OnPlayerHealthChanged(GameSession.Instance.CurrentPlayerHealth, GameSession.Instance.MaxPlayerHealth);
		SetGameOverVisible(GameSession.Instance.IsGameOver, GameSession.Instance.Score);
	}

	public override void _ExitTree()
	{
		if (GameSession.Instance is null)
		{
			return;
		}

		GameSession.Instance.ScoreChanged -= OnScoreChanged;
		GameSession.Instance.PlayerHealthChanged -= OnPlayerHealthChanged;
		GameSession.Instance.GameOver -= OnGameOver;
	}

	private void OnScoreChanged(int score)
	{
		_scoreValueLabel.Text = score.ToString();
	}

	private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
	{
		if (maxHealth <= 0)
		{
			_healthBar.MaxValue = 1;
			_healthBar.Value = 0;
			return;
		}

		_healthBar.MaxValue = maxHealth;
		_healthBar.Value = currentHealth;
	}

	private void OnGameOver(int finalScore)
	{
		SetGameOverVisible(true, finalScore);
	}

	private void SetGameOverVisible(bool isVisible, int finalScore)
	{
		_gameOverPanel.Visible = isVisible;
		_finalScoreLabel.Text = $"Final Score: {finalScore}";
	}

	private void OnRestartButtonPressed()
	{
		GameSession.Instance?.StartNewRun();
		GetTree().ChangeSceneToFile(LevelScenePath);
	}
}
