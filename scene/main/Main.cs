using Godot;

public partial class Main : Control
{
	private const string FallbackLevelScenePath = "res://scene/level/Level01.tscn";
	private const string MainMenuPanelTexturePath = "res://asset/art/ui/ui_main_menu_panel.png";
	private const string ButtonNormalTexturePath = "res://asset/art/ui/ui_main_menu_button_normal.png";
	private const string ButtonHoverTexturePath = "res://asset/art/ui/ui_main_menu_button_hover.png";
	private const string ButtonPressedTexturePath = "res://asset/art/ui/ui_main_menu_button_pressed.png";
	private const string ButtonDisabledTexturePath = "res://asset/art/ui/ui_main_menu_button_disabled.png";
	private const string LevelSelectPanelTexturePath = "res://asset/art/ui/ui_level_select_panel.png";
	private const string LevelSelectButtonNormalTexturePath = "res://asset/art/ui/ui_level_select_button_normal.png";
	private const string LevelSelectButtonHoverTexturePath = "res://asset/art/ui/ui_level_select_button_hover.png";
	private const string LevelSelectButtonPressedTexturePath = "res://asset/art/ui/ui_level_select_button_pressed.png";
	private const string LevelSelectButtonDisabledTexturePath = "res://asset/art/ui/ui_level_select_button_disabled.png";
	private const float MainMenuPanelTextureMarginX = 24.0f;
	private const float MainMenuPanelTextureMarginY = 24.0f;
	private const float MenuButtonTextureMarginX = 36.0f;
	private const float MenuButtonTextureMarginY = 12.0f;
	private const float LevelSelectPanelTextureMarginX = 52.0f;
	private const float LevelSelectPanelTextureMarginY = 52.0f;
	private const float LevelSelectButtonTextureMarginX = 34.0f;
	private const float LevelSelectButtonTextureMarginY = 14.0f;
	private static readonly Vector2 LevelSelectButtonSize = new(220.0f, 52.0f);

	private Control _mainMenuPanel;
	private PanelContainer _mainMenuPanelContainer;
	private Control _levelSelectPanel;
	private PanelContainer _levelSelectPanelContainer;
	private Control _settingsPanel;
	private GridContainer _levelButtonGrid;
	private Button _startGameButton;
	private Button _settingsButton;
	private Button _quitButton;
	private Button _levelSelectBackButton;
	private Button _settingsBackButton;
	private StyleBoxTexture _buttonNormalStyle;
	private StyleBoxTexture _buttonHoverStyle;
	private StyleBoxTexture _buttonPressedStyle;
	private StyleBoxTexture _buttonDisabledStyle;
	private StyleBoxTexture _levelButtonNormalStyle;
	private StyleBoxTexture _levelButtonHoverStyle;
	private StyleBoxTexture _levelButtonPressedStyle;
	private StyleBoxTexture _levelButtonDisabledStyle;

	public override void _Ready()
	{
		GetTree().Paused = false;

		_mainMenuPanel = GetNode<Control>("MainMenuPanel");
		_mainMenuPanelContainer = GetNode<PanelContainer>("MainMenuPanel/PanelContainer");
		_levelSelectPanel = GetNode<Control>("LevelSelectPanel");
		_levelSelectPanelContainer = GetNode<PanelContainer>("LevelSelectPanel/PanelContainer");
		_settingsPanel = GetNode<Control>("SettingsPanel");
		_levelButtonGrid = GetNode<GridContainer>("LevelSelectPanel/PanelContainer/MarginContainer/Content/LevelScrollContainer/LevelButtonGrid");
		_startGameButton = GetNode<Button>("MainMenuPanel/PanelContainer/PanelLayout/ButtonList/StartGameButton");
		_settingsButton = GetNode<Button>("MainMenuPanel/PanelContainer/PanelLayout/ButtonList/SettingsButton");
		_quitButton = GetNode<Button>("MainMenuPanel/PanelContainer/PanelLayout/ButtonList/QuitButton");
		_levelSelectBackButton = GetNode<Button>("LevelSelectPanel/PanelContainer/MarginContainer/Content/BackButtonCenter/BackButton");
		_settingsBackButton = GetNode<Button>("SettingsPanel/PanelContainer/MarginContainer/Content/BackButton");

		_startGameButton.Pressed += OnStartGamePressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_quitButton.Pressed += OnQuitPressed;
		_levelSelectBackButton.Pressed += ShowMainMenu;
		_settingsBackButton.Pressed += ShowMainMenu;

		ApplyMainMenuPanelStyle();
		LoadMenuButtonStyles();
		ApplyMenuButtonStyle(_startGameButton);
		ApplyMenuButtonStyle(_settingsButton);
		ApplyMenuButtonStyle(_quitButton);
		ApplyMenuButtonStyle(_settingsBackButton);
		ApplyLevelSelectPanelStyle();
		LoadLevelSelectButtonStyles();
		ApplyLevelSelectButtonStyle(_levelSelectBackButton);

		PopulateLevelButtons();
		ShowMainMenu();
	}

	private void PopulateLevelButtons()
	{
		foreach (Node child in _levelButtonGrid.GetChildren())
		{
			_levelButtonGrid.RemoveChild(child);
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
			CustomMinimumSize = LevelSelectButtonSize,
		};
		ApplyLevelSelectButtonStyle(button);
		button.Pressed += () => StartLevel(scenePath, levelConfigId);
		_levelButtonGrid.AddChild(button);
	}

	private void AddFallbackLevelButton()
	{
		Button button = new()
		{
			Text = "Level 01",
			CustomMinimumSize = LevelSelectButtonSize,
		};
		ApplyLevelSelectButtonStyle(button);
		button.Pressed += () => StartLevel(FallbackLevelScenePath, "level_01");
		_levelButtonGrid.AddChild(button);
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

	private void LoadMenuButtonStyles()
	{
		_buttonNormalStyle = CreateMenuButtonStyle(ButtonNormalTexturePath);
		_buttonHoverStyle = CreateMenuButtonStyle(ButtonHoverTexturePath);
		_buttonPressedStyle = CreateMenuButtonStyle(ButtonPressedTexturePath);
		_buttonDisabledStyle = CreateMenuButtonStyle(ButtonDisabledTexturePath);
	}

	private void LoadLevelSelectButtonStyles()
	{
		_levelButtonNormalStyle = CreateLevelSelectButtonStyle(LevelSelectButtonNormalTexturePath);
		_levelButtonHoverStyle = CreateLevelSelectButtonStyle(LevelSelectButtonHoverTexturePath);
		_levelButtonPressedStyle = CreateLevelSelectButtonStyle(LevelSelectButtonPressedTexturePath);
		_levelButtonDisabledStyle = CreateLevelSelectButtonStyle(LevelSelectButtonDisabledTexturePath);
	}

	private void ApplyMainMenuPanelStyle()
	{
		StyleBoxTexture panelStyle = CreateTextureStyle(MainMenuPanelTexturePath, MainMenuPanelTextureMarginX, MainMenuPanelTextureMarginY);
		if (panelStyle is null)
		{
			return;
		}

		_mainMenuPanelContainer.AddThemeStyleboxOverride("panel", panelStyle);
	}

	private void ApplyLevelSelectPanelStyle()
	{
		StyleBoxTexture panelStyle = CreateTextureStyle(LevelSelectPanelTexturePath, LevelSelectPanelTextureMarginX, LevelSelectPanelTextureMarginY);
		if (panelStyle is null)
		{
			return;
		}

		_levelSelectPanelContainer.AddThemeStyleboxOverride("panel", panelStyle);
	}

	private static StyleBoxTexture CreateMenuButtonStyle(string texturePath)
	{
		return CreateTextureStyle(texturePath, MenuButtonTextureMarginX, MenuButtonTextureMarginY);
	}

	private static StyleBoxTexture CreateLevelSelectButtonStyle(string texturePath)
	{
		return CreateTextureStyle(texturePath, LevelSelectButtonTextureMarginX, LevelSelectButtonTextureMarginY);
	}

	private static StyleBoxTexture CreateTextureStyle(string texturePath, float textureMarginX, float textureMarginY)
	{
		Texture2D texture = ResourceLoader.Load<Texture2D>(texturePath);
		if (texture is null)
		{
			GD.PushWarning($"Unable to load UI texture: {texturePath}");
			return null;
		}

		StyleBoxTexture style = new()
		{
			Texture = texture,
		};
		style.SetTextureMargin(Side.Left, textureMarginX);
		style.SetTextureMargin(Side.Right, textureMarginX);
		style.SetTextureMargin(Side.Top, textureMarginY);
		style.SetTextureMargin(Side.Bottom, textureMarginY);
		return style;
	}

	private void ApplyMenuButtonStyle(Button button)
	{
		if (button is null || _buttonNormalStyle is null)
		{
			return;
		}

		button.AddThemeStyleboxOverride("normal", _buttonNormalStyle);
		button.AddThemeStyleboxOverride("hover", _buttonHoverStyle ?? _buttonNormalStyle);
		button.AddThemeStyleboxOverride("pressed", _buttonPressedStyle ?? _buttonNormalStyle);
		button.AddThemeStyleboxOverride("disabled", _buttonDisabledStyle ?? _buttonNormalStyle);
		button.AddThemeStyleboxOverride("focus", _buttonHoverStyle ?? _buttonNormalStyle);
		button.AddThemeColorOverride("font_color", new Color(0.20f, 0.10f, 0.04f));
		button.AddThemeColorOverride("font_hover_color", new Color(0.16f, 0.08f, 0.03f));
		button.AddThemeColorOverride("font_pressed_color", new Color(0.11f, 0.06f, 0.02f));
		button.AddThemeColorOverride("font_disabled_color", new Color(0.34f, 0.30f, 0.25f));
		button.AddThemeConstantOverride("outline_size", 0);
	}

	private void ApplyLevelSelectButtonStyle(Button button)
	{
		if (button is null || _levelButtonNormalStyle is null)
		{
			return;
		}

		button.AddThemeStyleboxOverride("normal", _levelButtonNormalStyle);
		button.AddThemeStyleboxOverride("hover", _levelButtonHoverStyle ?? _levelButtonNormalStyle);
		button.AddThemeStyleboxOverride("pressed", _levelButtonPressedStyle ?? _levelButtonNormalStyle);
		button.AddThemeStyleboxOverride("disabled", _levelButtonDisabledStyle ?? _levelButtonNormalStyle);
		button.AddThemeStyleboxOverride("focus", _levelButtonHoverStyle ?? _levelButtonNormalStyle);
		button.AddThemeColorOverride("font_color", new Color(0.18f, 0.09f, 0.035f));
		button.AddThemeColorOverride("font_hover_color", new Color(0.13f, 0.065f, 0.025f));
		button.AddThemeColorOverride("font_pressed_color", new Color(0.09f, 0.045f, 0.015f));
		button.AddThemeColorOverride("font_disabled_color", new Color(0.36f, 0.31f, 0.25f));
		button.AddThemeConstantOverride("outline_size", 0);
	}
}
