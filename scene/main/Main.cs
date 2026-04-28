using Godot;

public partial class Main : Control
{
	private const string FallbackLevelScenePath = "res://scene/level/Level01.tscn";

	private Control _mainMenuPanel;
	private Control _levelSelectPanel;
	private Control _settingsPanel;
	private VBoxContainer _levelButtonList;
	private Button _startGameButton;
	private Button _settingsButton;
	private Button _quitButton;
	private Button _levelSelectBackButton;
	private Button _settingsBackButton;

	public override void _Ready()
	{
		GetTree().Paused = false;

		_mainMenuPanel = GetNode<Control>("MainMenuPanel");
		_levelSelectPanel = GetNode<Control>("LevelSelectPanel");
		_settingsPanel = GetNode<Control>("SettingsPanel");
		_levelButtonList = GetNode<VBoxContainer>("LevelSelectPanel/PanelContainer/MarginContainer/Content/LevelButtonList");
		_startGameButton = GetNode<Button>("MainMenuPanel/PanelContainer/MarginContainer/Content/StartGameButton");
		_settingsButton = GetNode<Button>("MainMenuPanel/PanelContainer/MarginContainer/Content/SettingsButton");
		_quitButton = GetNode<Button>("MainMenuPanel/PanelContainer/MarginContainer/Content/QuitButton");
		_levelSelectBackButton = GetNode<Button>("LevelSelectPanel/PanelContainer/MarginContainer/Content/BackButton");
		_settingsBackButton = GetNode<Button>("SettingsPanel/PanelContainer/MarginContainer/Content/BackButton");

		_startGameButton.Pressed += OnStartGamePressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_quitButton.Pressed += OnQuitPressed;
		_levelSelectBackButton.Pressed += ShowMainMenu;
		_settingsBackButton.Pressed += ShowMainMenu;

		PopulateLevelButtons();
		ShowMainMenu();
	}

	private void PopulateLevelButtons()
	{
		foreach (Node child in _levelButtonList.GetChildren())
		{
			_levelButtonList.RemoveChild(child);
			child.QueueFree();
		}

		if (GameConfigManager.Instance is null)
		{
			AddFallbackLevelButton();
			return;
		}

		bool hasLevel = false;
		foreach (LevelConfig level in GameConfigManager.Instance.GetAllLevelConfigs())
		{
			if (level is null)
			{
				continue;
			}

			hasLevel = true;
			AddLevelButton(level);
		}

		if (!hasLevel)
		{
			AddFallbackLevelButton();
		}
	}

	private void AddLevelButton(LevelConfig level)
	{
		string scenePath = string.IsNullOrWhiteSpace(level.ScenePath) ? FallbackLevelScenePath : level.ScenePath;
		string levelConfigId = level.Id;
		Button button = new()
		{
			Text = string.IsNullOrWhiteSpace(level.DisplayName) ? level.Id : level.DisplayName,
			CustomMinimumSize = new Vector2(280.0f, 42.0f),
		};
		button.Pressed += () => StartLevel(scenePath, levelConfigId);
		_levelButtonList.AddChild(button);
	}

	private void AddFallbackLevelButton()
	{
		Button button = new()
		{
			Text = "Level 01",
			CustomMinimumSize = new Vector2(280.0f, 42.0f),
		};
		button.Pressed += () => StartLevel(FallbackLevelScenePath, "level_01");
		_levelButtonList.AddChild(button);
	}

	private void OnStartGamePressed()
	{
		PopulateLevelButtons();
		ShowPanel(_levelSelectPanel);
	}

	private void OnSettingsPressed()
	{
		ShowPanel(_settingsPanel);
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}

	private void StartLevel(string scenePath, string levelConfigId)
	{
		GetTree().Paused = false;
		GameSession.Instance?.SelectLevelConfig(levelConfigId);
		GetTree().ChangeSceneToFile(string.IsNullOrWhiteSpace(scenePath) ? FallbackLevelScenePath : scenePath);
	}

	private void ShowMainMenu()
	{
		ShowPanel(_mainMenuPanel);
	}

	private void ShowPanel(Control activePanel)
	{
		_mainMenuPanel.Visible = activePanel == _mainMenuPanel;
		_levelSelectPanel.Visible = activePanel == _levelSelectPanel;
		_settingsPanel.Visible = activePanel == _settingsPanel;
	}
}
