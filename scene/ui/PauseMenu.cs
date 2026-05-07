using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	private const string MainScenePath = "res://scene/main/Main.tscn";
	private const string PanelTexturePath = "res://asset/art/ui/ui_hud_game_over_panel.png";
	private const string ButtonNormalTexturePath = "res://asset/art/ui/ui_hud_game_over_confirm_button_normal.png";
	private const string ButtonHoverTexturePath = "res://asset/art/ui/ui_hud_game_over_confirm_button_hover.png";
	private const string ButtonPressedTexturePath = "res://asset/art/ui/ui_hud_game_over_confirm_button_pressed.png";
	private const string ButtonDisabledTexturePath = "res://asset/art/ui/ui_hud_game_over_confirm_button_disabled.png";
	private const float PanelTextureMargin = 34.0f;
	private const float ButtonTextureMarginX = 34.0f;
	private const float ButtonTextureMarginY = 14.0f;
	private static readonly Vector2 MenuButtonSize = new(220.0f, 52.0f);

	public Func<bool> CanOpenMenu { get; set; }

	private Control _root;
	private PanelContainer _panel;
	private Label _titleLabel;
	private VBoxContainer _mainContent;
	private VBoxContainer _settingsContent;
	private Button _resumeButton;
	private Button _settingsButton;
	private Button _exitLevelButton;
	private Button _settingsBackButton;
	private bool _wasPausedBeforeOpen;

	public bool IsOpen => _root?.Visible == true;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Layer = 50;
		CreateMenu();
		ApplyTextureStyles();
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsPauseToggleEvent(@event))
		{
			return;
		}

		if (IsOpen)
		{
			GetViewport().SetInputAsHandled();
			ResumeGame();
			return;
		}

		if (CanOpenMenu?.Invoke() == false)
		{
			return;
		}

		GetViewport().SetInputAsHandled();
		ShowMenu();
	}

	public void CloseWithoutUnpausing()
	{
		HideMenu();
	}

	private void ShowMenu()
	{
		if (_root is null)
		{
			return;
		}

		_wasPausedBeforeOpen = GetTree().Paused;
		GetTree().Paused = true;
		ShowMainContent();
		_root.Visible = true;
		_root.MouseFilter = Control.MouseFilterEnum.Stop;
		_resumeButton?.GrabFocus();
	}

	private void HideMenu()
	{
		if (_root is null)
		{
			return;
		}

		_root.Visible = false;
		_root.MouseFilter = Control.MouseFilterEnum.Ignore;
	}

	private void ResumeGame()
	{
		HideMenu();
		GetTree().Paused = _wasPausedBeforeOpen;
	}

	private void OnSettingsPressed()
	{
		ShowSettingsContent();
	}

	private void OnSettingsBackPressed()
	{
		ShowMainContent();
		_resumeButton?.GrabFocus();
	}

	private void OnExitLevelPressed()
	{
		HideMenu();
		GetTree().Paused = false;
		CallDeferred(nameof(ReturnToMainScene));
	}

	private void ReturnToMainScene()
	{
		GetTree().ChangeSceneToFile(MainScenePath);
	}

	private void ShowMainContent()
	{
		if (_titleLabel != null)
		{
			_titleLabel.Text = "暂停";
		}

		if (_mainContent != null)
		{
			_mainContent.Visible = true;
		}

		if (_settingsContent != null)
		{
			_settingsContent.Visible = false;
		}
	}

	private void ShowSettingsContent()
	{
		if (_titleLabel != null)
		{
			_titleLabel.Text = "设置";
		}

		if (_mainContent != null)
		{
			_mainContent.Visible = false;
		}

		if (_settingsContent != null)
		{
			_settingsContent.Visible = true;
		}

		_settingsBackButton?.GrabFocus();
	}

	private static bool IsPauseToggleEvent(InputEvent @event)
	{
		return @event is InputEventKey keyEvent
			&& keyEvent.Pressed
			&& !keyEvent.Echo
			&& keyEvent.Keycode == Key.Escape;
	}

	private void CreateMenu()
	{
		_root = new Control
		{
			Name = "Root",
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ProcessMode = ProcessModeEnum.Always,
		};
		_root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_root);

		ColorRect overlay = new()
		{
			Name = "Overlay",
			Color = new Color(0.035f, 0.031f, 0.024f, 0.62f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(overlay);

		CenterContainer centerContainer = new()
		{
			Name = "CenterContainer",
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		centerContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(centerContainer);

		_panel = new PanelContainer
		{
			Name = "PanelContainer",
			CustomMinimumSize = new Vector2(340.0f, 0.0f),
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		centerContainer.AddChild(_panel);

		MarginContainer margin = new()
		{
			Name = "MarginContainer",
		};
		margin.AddThemeConstantOverride("margin_left", 26);
		margin.AddThemeConstantOverride("margin_top", 24);
		margin.AddThemeConstantOverride("margin_right", 26);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		_panel.AddChild(margin);

		VBoxContainer content = new()
		{
			Name = "Content",
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		content.AddThemeConstantOverride("separation", 14);
		margin.AddChild(content);

		_titleLabel = CreateTitleLabel("暂停");
		content.AddChild(_titleLabel);

		_mainContent = new VBoxContainer
		{
			Name = "MainContent",
		};
		_mainContent.AddThemeConstantOverride("separation", 12);
		content.AddChild(_mainContent);

		_resumeButton = CreateMenuButton("继续游戏");
		_settingsButton = CreateMenuButton("设置");
		_exitLevelButton = CreateMenuButton("退出关卡");
		_mainContent.AddChild(_resumeButton);
		_mainContent.AddChild(_settingsButton);
		_mainContent.AddChild(_exitLevelButton);

		_settingsContent = new VBoxContainer
		{
			Name = "SettingsContent",
			Visible = false,
		};
		_settingsContent.AddThemeConstantOverride("separation", 12);
		content.AddChild(_settingsContent);

		_settingsBackButton = CreateMenuButton("返回");
		_settingsContent.AddChild(_settingsBackButton);

		ConnectUiButtonSounds(_resumeButton);
		ConnectUiButtonSounds(_settingsButton);
		ConnectUiButtonSounds(_exitLevelButton);
		ConnectUiButtonSounds(_settingsBackButton);
		_resumeButton.Pressed += ResumeGame;
		_settingsButton.Pressed += OnSettingsPressed;
		_exitLevelButton.Pressed += OnExitLevelPressed;
		_settingsBackButton.Pressed += OnSettingsBackPressed;
	}

	private static Label CreateTitleLabel(string text)
	{
		Label label = new()
		{
			Name = "TitleLabel",
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		label.AddThemeFontSizeOverride("font_size", 28);
		label.AddThemeColorOverride("font_color", new Color(1.0f, 0.92f, 0.72f));
		label.AddThemeColorOverride("font_outline_color", new Color(0.12f, 0.05f, 0.02f, 0.9f));
		label.AddThemeConstantOverride("outline_size", 3);
		return label;
	}

	private static Button CreateMenuButton(string text)
	{
		return new Button
		{
			Text = text,
			CustomMinimumSize = MenuButtonSize,
		};
	}

	private static void ConnectUiButtonSounds(Button button)
	{
		if (button is null)
		{
			return;
		}

		button.ButtonDown += PlayUiClickSound;
		button.MouseEntered += PlayUiHoverSound;
	}

	private void ApplyTextureStyles()
	{
		ApplyPanelStyle(_panel, PanelTexturePath, PanelTextureMargin, PanelTextureMargin);
		ApplyButtonStyle(_resumeButton);
		ApplyButtonStyle(_settingsButton);
		ApplyButtonStyle(_exitLevelButton);
		ApplyButtonStyle(_settingsBackButton);
	}

	private static void ApplyPanelStyle(PanelContainer panel, string texturePath, float marginX, float marginY)
	{
		StyleBoxTexture style = CreateTextureStyle(texturePath, marginX, marginY);
		if (style is null)
		{
			return;
		}

		panel?.AddThemeStyleboxOverride("panel", style);
	}

	private static void ApplyButtonStyle(Button button)
	{
		if (button is null)
		{
			return;
		}

		StyleBoxTexture normalStyle = CreateTextureStyle(
			ButtonNormalTexturePath,
			ButtonTextureMarginX,
			ButtonTextureMarginY);
		if (normalStyle is null)
		{
			return;
		}

		StyleBoxTexture hoverStyle = CreateTextureStyle(
			ButtonHoverTexturePath,
			ButtonTextureMarginX,
			ButtonTextureMarginY);
		StyleBoxTexture pressedStyle = CreateTextureStyle(
			ButtonPressedTexturePath,
			ButtonTextureMarginX,
			ButtonTextureMarginY);
		StyleBoxTexture disabledStyle = CreateTextureStyle(
			ButtonDisabledTexturePath,
			ButtonTextureMarginX,
			ButtonTextureMarginY);

		button.CustomMinimumSize = MenuButtonSize;
		button.AddThemeStyleboxOverride("normal", normalStyle);
		button.AddThemeStyleboxOverride("hover", hoverStyle ?? normalStyle);
		button.AddThemeStyleboxOverride("pressed", pressedStyle ?? normalStyle);
		button.AddThemeStyleboxOverride("disabled", disabledStyle ?? normalStyle);
		button.AddThemeStyleboxOverride("focus", hoverStyle ?? normalStyle);
		button.AddThemeColorOverride("font_color", new Color(0.18f, 0.09f, 0.035f));
		button.AddThemeColorOverride("font_hover_color", new Color(0.13f, 0.065f, 0.025f));
		button.AddThemeColorOverride("font_pressed_color", new Color(0.95f, 0.82f, 0.58f));
		button.AddThemeColorOverride("font_disabled_color", new Color(0.36f, 0.31f, 0.25f));
		button.AddThemeConstantOverride("outline_size", 0);
	}

	private static StyleBoxTexture CreateTextureStyle(string texturePath, float marginX, float marginY)
	{
		Texture2D texture = ResourceLoader.Load<Texture2D>(texturePath);
		if (texture is null)
		{
			GD.PushWarning($"Unable to load pause menu texture: {texturePath}");
			return null;
		}

		StyleBoxTexture style = new()
		{
			Texture = texture,
		};
		style.SetTextureMargin(Side.Left, marginX);
		style.SetTextureMargin(Side.Right, marginX);
		style.SetTextureMargin(Side.Top, marginY);
		style.SetTextureMargin(Side.Bottom, marginY);
		style.SetContentMargin(Side.Left, 0.0f);
		style.SetContentMargin(Side.Right, 0.0f);
		style.SetContentMargin(Side.Top, 0.0f);
		style.SetContentMargin(Side.Bottom, 0.0f);
		return style;
	}

	private static void PlayUiClickSound()
	{
		AudioManager.Instance?.PlayUiClick();
	}

	private static void PlayUiHoverSound()
	{
		AudioManager.Instance?.PlayUiHover();
	}
}
