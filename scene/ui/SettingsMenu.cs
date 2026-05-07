using Godot;
using System;

public partial class SettingsMenu : Control
{
	private enum SettingsPage
	{
		Game,
		Audio,
	}

	private const string BackgroundTexturePath = "res://asset/art/ui/settings/ui_settings_background_plain.png";
	private const string ButtonNormalTexturePath = "res://asset/art/ui/settings/ui_settings_button_normal.png";
	private const string ButtonHoverTexturePath = "res://asset/art/ui/settings/ui_settings_button_hover.png";
	private const string ButtonPressedTexturePath = "res://asset/art/ui/settings/ui_settings_button_pressed.png";
	private const string ButtonDisabledTexturePath = "res://asset/art/ui/settings/ui_settings_button_disabled.png";
	private const string TabSelectedTexturePath = "res://asset/art/ui/settings/ui_settings_tab_selected.png";
	private const string DropdownTexturePath = "res://asset/art/ui/settings/ui_settings_dropdown.png";
	private const string DropdownHoverTexturePath = "res://asset/art/ui/settings/ui_settings_dropdown_hover.png";
	private const string DropdownPressedTexturePath = "res://asset/art/ui/settings/ui_settings_dropdown_pressed.png";
	private const string DropdownListPanelTexturePath = "res://asset/art/ui/settings/ui_settings_dropdown_list_panel.png";
	private const string DropdownListHoverTexturePath = "res://asset/art/ui/settings/ui_settings_dropdown_list_hover.png";
	private const string SliderTrackTexturePath = "res://asset/art/ui/settings/ui_settings_slider_track.png";
	private const string SliderFillTexturePath = "res://asset/art/ui/settings/ui_settings_slider_fill.png";
	private const string SliderGrabberTexturePath = "res://asset/art/ui/settings/ui_settings_slider_grabber.png";
	private const float ButtonTextureMarginX = 34.0f;
	private const float ButtonTextureMarginY = 14.0f;
	private const float DropdownTextureMarginX = 34.0f;
	private const float DropdownTextureMarginY = 14.0f;
	private const float DropdownListPanelTextureMarginX = 20.0f;
	private const float DropdownListPanelTextureMarginY = 18.0f;
	private const float DropdownListHoverTextureMarginX = 18.0f;
	private const float DropdownListHoverTextureMarginY = 12.0f;
	private const float SliderTextureMarginX = 8.0f;
	private const float SliderTextureMarginY = 4.0f;
	private static readonly Vector2 TabButtonSize = new(220.0f, 52.0f);
	private static readonly Vector2 BackButtonSize = new(220.0f, 52.0f);
	private static readonly Vector2 SettingRowSize = new(800.0f, 66.0f);
	private static readonly Vector2 AudioRowSize = new(800.0f, 66.0f);

	private Button _gameTabButton;
	private Button _audioTabButton;
	private Button _backButton;
	private VBoxContainer _gamePage;
	private VBoxContainer _audioPage;
	private Label _languageLabel;
	private OptionButton _languageOption;
	private Label _masterVolumeLabel;
	private Label _sfxVolumeLabel;
	private Label _musicVolumeLabel;
	private Label _masterVolumeValueLabel;
	private Label _sfxVolumeValueLabel;
	private Label _musicVolumeValueLabel;
	private HSlider _masterVolumeSlider;
	private HSlider _sfxVolumeSlider;
	private HSlider _musicVolumeSlider;
	private bool _isRefreshing;
	private Action _closedCallback;
	private SettingsPage _currentPage = SettingsPage.Game;

	public bool IsOpen => Visible;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		ForceFullScreenLayout();
		MouseFilter = MouseFilterEnum.Ignore;
		Visible = false;

		CreateMenu();
		ConnectSignals();
		RefreshFromSettings();
		ApplyLocalizedText();
		ShowPage(SettingsPage.Game);
	}

	public override void _Process(double delta)
	{
		if (Visible)
		{
			ForceFullScreenLayout();
		}
	}

	public override void _ExitTree()
	{
		if (GameSettings.Instance != null)
		{
			GameSettings.Instance.LanguageChanged -= OnLanguageChanged;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsOpen || !IsBackEvent(@event))
		{
			return;
		}

		GetViewport().SetInputAsHandled();
		Close();
	}

	public void Open(Action closedCallback = null)
	{
		_closedCallback = closedCallback;
		ForceFullScreenLayout();
		RefreshFromSettings();
		ApplyLocalizedText();
		Visible = true;
		MouseFilter = MouseFilterEnum.Stop;
		ShowPage(SettingsPage.Game);
		_gameTabButton?.GrabFocus();
	}

	public void Close()
	{
		CloseInternal(invokeCallback: true);
	}

	public void CloseWithoutCallback()
	{
		CloseInternal(invokeCallback: false);
	}

	private void CloseInternal(bool invokeCallback)
	{
		Visible = false;
		MouseFilter = MouseFilterEnum.Ignore;

		Action callback = _closedCallback;
		_closedCallback = null;
		if (invokeCallback)
		{
			callback?.Invoke();
		}
	}

	private void ForceFullScreenLayout()
	{
		Vector2 viewportSize = GetViewportRect().Size;
		if (viewportSize.X <= 1.0f || viewportSize.Y <= 1.0f)
		{
			return;
		}

		AnchorLeft = 0.0f;
		AnchorTop = 0.0f;
		AnchorRight = 0.0f;
		AnchorBottom = 0.0f;
		OffsetLeft = 0.0f;
		OffsetTop = 0.0f;
		OffsetRight = viewportSize.X;
		OffsetBottom = viewportSize.Y;
		Position = Vector2.Zero;
		Size = viewportSize;
	}

	private void CreateMenu()
	{
		ColorRect overlay = new()
		{
			Name = "Overlay",
			Color = new Color(0.035f, 0.031f, 0.024f, 0.86f),
			MouseFilter = MouseFilterEnum.Stop,
		};
		overlay.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(overlay);

		TextureRect backgroundTexture = new()
		{
			Name = "BackgroundTexture",
			Texture = ResourceLoader.Load<Texture2D>(BackgroundTexturePath),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		backgroundTexture.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(backgroundTexture);

		MarginContainer screenMargin = new()
		{
			Name = "ScreenMargin",
			MouseFilter = MouseFilterEnum.Ignore,
		};
		screenMargin.SetAnchorsPreset(LayoutPreset.FullRect);
		screenMargin.AddThemeConstantOverride("margin_left", 72);
		screenMargin.AddThemeConstantOverride("margin_top", 56);
		screenMargin.AddThemeConstantOverride("margin_right", 72);
		screenMargin.AddThemeConstantOverride("margin_bottom", 62);
		AddChild(screenMargin);

		PanelContainer panel = new()
		{
			Name = "PanelContainer",
			MouseFilter = MouseFilterEnum.Stop,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
		screenMargin.AddChild(panel);

		MarginContainer panelMargin = new()
		{
			Name = "PanelMargin",
		};
		panelMargin.AddThemeConstantOverride("margin_left", 78);
		panelMargin.AddThemeConstantOverride("margin_top", 42);
		panelMargin.AddThemeConstantOverride("margin_right", 78);
		panelMargin.AddThemeConstantOverride("margin_bottom", 54);
		panel.AddChild(panelMargin);

		VBoxContainer layout = new()
		{
			Name = "Layout",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		layout.AddThemeConstantOverride("separation", 16);
		panelMargin.AddChild(layout);

		HBoxContainer tabRow = new()
		{
			Name = "TabRow",
			Alignment = BoxContainer.AlignmentMode.Begin,
			CustomMinimumSize = new Vector2(0.0f, 58.0f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		tabRow.AddThemeConstantOverride("separation", 14);
		layout.AddChild(tabRow);

		_gameTabButton = CreateButton("GameTabButton", TabButtonSize, toggleMode: true);
		_audioTabButton = CreateButton("AudioTabButton", TabButtonSize, toggleMode: true);
		ApplyTabSelectedStyle(_gameTabButton);
		ApplyTabSelectedStyle(_audioTabButton);
		tabRow.AddChild(_gameTabButton);
		tabRow.AddChild(_audioTabButton);

		PanelContainer pagePanel = new()
		{
			Name = "PagePanel",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Pass,
		};
		pagePanel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
		layout.AddChild(pagePanel);

		MarginContainer pageMargin = new()
		{
			Name = "PageMargin",
		};
		pageMargin.AddThemeConstantOverride("margin_left", 86);
		pageMargin.AddThemeConstantOverride("margin_top", 42);
		pageMargin.AddThemeConstantOverride("margin_right", 86);
		pageMargin.AddThemeConstantOverride("margin_bottom", 66);
		pagePanel.AddChild(pageMargin);

		Control pageRoot = new()
		{
			Name = "PageRoot",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		pageMargin.AddChild(pageRoot);

		_gamePage = new VBoxContainer
		{
			Name = "GamePage",
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		_gamePage.SetAnchorsPreset(LayoutPreset.FullRect);
		_gamePage.AddThemeConstantOverride("separation", 26);
		pageRoot.AddChild(_gamePage);

		_audioPage = new VBoxContainer
		{
			Name = "AudioPage",
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		_audioPage.SetAnchorsPreset(LayoutPreset.FullRect);
		_audioPage.AddThemeConstantOverride("separation", 24);
		pageRoot.AddChild(_audioPage);

		HBoxContainer footer = new()
		{
			Name = "Footer",
			Alignment = BoxContainer.AlignmentMode.End,
			CustomMinimumSize = new Vector2(0.0f, 64.0f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		layout.AddChild(footer);

		_backButton = CreateButton("BackButton", BackButtonSize);
		footer.AddChild(_backButton);

		CreateGamePage();
		CreateAudioPage();
	}

	private void CreateGamePage()
	{
		_languageOption = new OptionButton
		{
			Name = "LanguageOption",
			CustomMinimumSize = new Vector2(360.0f, 54.0f),
			SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
			Alignment = HorizontalAlignment.Center,
		};
		ApplyPopupControlStyle(_languageOption);
		_languageLabel = AddControlRow(_gamePage, _languageOption);
	}

	private void CreateAudioPage()
	{
		_masterVolumeSlider = CreateVolumeSlider("MasterVolumeSlider");
		_masterVolumeValueLabel = CreateValueLabel();
		_masterVolumeLabel = AddSliderRow(_audioPage, _masterVolumeSlider, _masterVolumeValueLabel);

		_sfxVolumeSlider = CreateVolumeSlider("SfxVolumeSlider");
		_sfxVolumeValueLabel = CreateValueLabel();
		_sfxVolumeLabel = AddSliderRow(_audioPage, _sfxVolumeSlider, _sfxVolumeValueLabel);

		_musicVolumeSlider = CreateVolumeSlider("MusicVolumeSlider");
		_musicVolumeValueLabel = CreateValueLabel();
		_musicVolumeLabel = AddSliderRow(_audioPage, _musicVolumeSlider, _musicVolumeValueLabel);
	}

	private void ConnectSignals()
	{
		_backButton.Pressed += Close;
		_gameTabButton.Pressed += () => ShowPage(SettingsPage.Game);
		_audioTabButton.Pressed += () => ShowPage(SettingsPage.Audio);
		_languageOption.ItemSelected += OnLanguageSelected;
		_masterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
		_sfxVolumeSlider.ValueChanged += OnSfxVolumeChanged;
		_musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;

		ConnectUiButtonSounds(_backButton);
		ConnectUiButtonSounds(_gameTabButton);
		ConnectUiButtonSounds(_audioTabButton);

		if (GameSettings.Instance != null)
		{
			GameSettings.Instance.LanguageChanged += OnLanguageChanged;
		}
	}

	private void ShowPage(SettingsPage page)
	{
		_currentPage = page;
		if (_gamePage != null)
		{
			_gamePage.Visible = page == SettingsPage.Game;
		}

		if (_audioPage != null)
		{
			_audioPage.Visible = page == SettingsPage.Audio;
		}

		SyncTabPressedState();
	}

	private void SyncTabPressedState()
	{
		if (_gameTabButton != null)
		{
			bool selected = _currentPage == SettingsPage.Game;
			_gameTabButton.SetPressedNoSignal(selected);
			ApplyTabVisualState(_gameTabButton, selected);
		}

		if (_audioTabButton != null)
		{
			bool selected = _currentPage == SettingsPage.Audio;
			_audioTabButton.SetPressedNoSignal(selected);
			ApplyTabVisualState(_audioTabButton, selected);
		}
	}

	private void RefreshFromSettings()
	{
		_isRefreshing = true;
		GameSettings settings = GameSettings.Instance;
		GameLanguage language = settings?.CurrentLanguage ?? GameLanguage.Chinese;
		float masterVolume = settings?.MasterVolume ?? 1.0f;
		float sfxVolume = settings?.SfxVolume ?? 1.0f;
		float musicVolume = settings?.MusicVolume ?? 1.0f;

		_languageOption.Clear();
		_languageOption.AddItem(GameText.Tr("ui.settings.language.chinese"), (int)GameLanguage.Chinese);
		_languageOption.AddItem(GameText.Tr("ui.settings.language.english"), (int)GameLanguage.English);
		SelectLanguageOption(language);

		_masterVolumeSlider.Value = masterVolume * 100.0f;
		_sfxVolumeSlider.Value = sfxVolume * 100.0f;
		_musicVolumeSlider.Value = musicVolume * 100.0f;
		UpdateVolumeValueLabels();
		_isRefreshing = false;
	}

	private void ApplyLocalizedText()
	{
		if (_backButton != null)
		{
			_backButton.Text = GameText.Tr("ui.common.back");
		}

		if (_gameTabButton != null)
		{
			_gameTabButton.Text = GameText.Tr("ui.settings.tab.game");
		}

		if (_audioTabButton != null)
		{
			_audioTabButton.Text = GameText.Tr("ui.settings.tab.audio");
		}

		if (_languageLabel != null)
		{
			_languageLabel.Text = GameText.Tr("ui.settings.language");
		}

		if (_masterVolumeLabel != null)
		{
			_masterVolumeLabel.Text = GameText.Tr("ui.settings.master_volume");
		}

		if (_sfxVolumeLabel != null)
		{
			_sfxVolumeLabel.Text = GameText.Tr("ui.settings.sfx_volume");
		}

		if (_musicVolumeLabel != null)
		{
			_musicVolumeLabel.Text = GameText.Tr("ui.settings.music_volume");
		}
	}

	private void OnLanguageChanged(GameLanguage language)
	{
		RefreshFromSettings();
		ApplyLocalizedText();
	}

	private void OnLanguageSelected(long itemIndex)
	{
		if (_isRefreshing || _languageOption == null)
		{
			return;
		}

		int id = _languageOption.GetItemId((int)itemIndex);
		GameSettings.Instance?.SetLanguage((GameLanguage)id);
	}

	private void OnMasterVolumeChanged(double value)
	{
		if (_isRefreshing)
		{
			return;
		}

		GameSettings.Instance?.SetMasterVolume((float)value / 100.0f);
		UpdateVolumeValueLabels();
	}

	private void OnSfxVolumeChanged(double value)
	{
		if (_isRefreshing)
		{
			return;
		}

		GameSettings.Instance?.SetSfxVolume((float)value / 100.0f);
		UpdateVolumeValueLabels();
	}

	private void OnMusicVolumeChanged(double value)
	{
		if (_isRefreshing)
		{
			return;
		}

		GameSettings.Instance?.SetMusicVolume((float)value / 100.0f);
		UpdateVolumeValueLabels();
	}

	private void SelectLanguageOption(GameLanguage language)
	{
		for (int i = 0; i < _languageOption.ItemCount; i++)
		{
			if (_languageOption.GetItemId(i) == (int)language)
			{
				_languageOption.Select(i);
				return;
			}
		}

		_languageOption.Select(0);
	}

	private void UpdateVolumeValueLabels()
	{
		SetValueLabel(_masterVolumeValueLabel, _masterVolumeSlider);
		SetValueLabel(_sfxVolumeValueLabel, _sfxVolumeSlider);
		SetValueLabel(_musicVolumeValueLabel, _musicVolumeSlider);
	}

	private static void SetValueLabel(Label label, Slider slider)
	{
		if (label == null || slider == null)
		{
			return;
		}

		label.Text = $"{Mathf.RoundToInt((float)slider.Value)}%";
	}

	private static Label AddControlRow(VBoxContainer parent, Control control)
	{
		HBoxContainer row = CreateRow(SettingRowSize);
		Label label = CreateSettingLabel();
		Control spacer = new()
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		row.AddChild(label);
		row.AddChild(spacer);
		row.AddChild(control);
		parent.AddChild(row);
		return label;
	}

	private static Label AddSliderRow(VBoxContainer parent, HSlider slider, Label valueLabel)
	{
		HBoxContainer row = CreateRow(AudioRowSize);
		Label label = CreateSettingLabel();
		row.AddChild(label);
		row.AddChild(slider);
		row.AddChild(valueLabel);
		parent.AddChild(row);
		return label;
	}

	private static HBoxContainer CreateRow(Vector2 minimumSize)
	{
		HBoxContainer row = new()
		{
			CustomMinimumSize = minimumSize,
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
		};
		row.AddThemeConstantOverride("separation", 22);
		return row;
	}

	private static Label CreateSettingLabel()
	{
		Label label = new()
		{
			CustomMinimumSize = new Vector2(240.0f, 0.0f),
			VerticalAlignment = VerticalAlignment.Center,
		};
		label.AddThemeFontSizeOverride("font_size", 28);
		label.AddThemeColorOverride("font_color", new Color(1.0f, 0.91f, 0.70f));
		label.AddThemeColorOverride("font_outline_color", new Color(0.13f, 0.06f, 0.02f, 0.92f));
		label.AddThemeConstantOverride("outline_size", 3);
		return label;
	}

	private static Label CreateValueLabel()
	{
		Label label = new()
		{
			CustomMinimumSize = new Vector2(78.0f, 0.0f),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
		};
		label.AddThemeFontSizeOverride("font_size", 24);
		label.AddThemeColorOverride("font_color", new Color(0.97f, 0.88f, 0.68f));
		label.AddThemeColorOverride("font_outline_color", new Color(0.13f, 0.06f, 0.02f, 0.92f));
		label.AddThemeConstantOverride("outline_size", 2);
		return label;
	}

	private static HSlider CreateVolumeSlider(string name)
	{
		HSlider slider = new()
		{
			Name = name,
			MinValue = 0.0,
			MaxValue = 100.0,
			Step = 1.0,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(400.0f, 46.0f),
		};
		ApplySliderStyle(slider);
		return slider;
	}

	private static Button CreateButton(string name, Vector2 minimumSize, bool toggleMode = false)
	{
		Button button = new()
		{
			Name = name,
			CustomMinimumSize = minimumSize,
			ToggleMode = toggleMode,
		};
		ApplyButtonStyle(button);
		return button;
	}

	private static void ApplyButtonStyle(Button button)
	{
		StyleBox normalStyle = (StyleBox)CreateTextureStyle(ButtonNormalTexturePath, ButtonTextureMarginX, ButtonTextureMarginY)
			?? CreateButtonStyle(new Color(0.36f, 0.24f, 0.11f, 0.96f), new Color(0.84f, 0.62f, 0.30f, 0.55f));
		StyleBox hoverStyle = (StyleBox)CreateTextureStyle(ButtonHoverTexturePath, ButtonTextureMarginX, ButtonTextureMarginY)
			?? CreateButtonStyle(new Color(0.48f, 0.31f, 0.12f, 0.98f), new Color(1.0f, 0.76f, 0.34f, 0.76f));
		StyleBox pressedStyle = (StyleBox)CreateTextureStyle(ButtonPressedTexturePath, ButtonTextureMarginX, ButtonTextureMarginY)
			?? CreateButtonStyle(new Color(0.19f, 0.12f, 0.06f, 0.98f), new Color(1.0f, 0.82f, 0.42f, 0.88f));
		StyleBox disabledStyle = (StyleBox)CreateTextureStyle(ButtonDisabledTexturePath, ButtonTextureMarginX, ButtonTextureMarginY)
			?? normalStyle;

		button.AddThemeStyleboxOverride("normal", normalStyle);
		button.AddThemeStyleboxOverride("hover", hoverStyle);
		button.AddThemeStyleboxOverride("pressed", pressedStyle);
		button.AddThemeStyleboxOverride("disabled", disabledStyle);
		button.AddThemeStyleboxOverride("focus", hoverStyle);
		button.AddThemeColorOverride("font_color", new Color(1.0f, 0.92f, 0.74f));
		button.AddThemeColorOverride("font_hover_color", new Color(1.0f, 0.98f, 0.84f));
		button.AddThemeColorOverride("font_pressed_color", new Color(1.0f, 0.82f, 0.38f));
		button.AddThemeColorOverride("font_outline_color", new Color(0.11f, 0.05f, 0.02f, 0.9f));
		button.AddThemeFontSizeOverride("font_size", 22);
		button.AddThemeConstantOverride("outline_size", 2);
	}

	private static void ApplyTabSelectedStyle(Button button)
	{
		ApplyTabVisualState(button, selected: false);
	}

	private static void ApplyTabVisualState(Button button, bool selected)
	{
		if (!selected)
		{
			ApplyButtonStyle(button);
			return;
		}

		StyleBox selectedStyle = (StyleBox)CreateTextureStyle(TabSelectedTexturePath, ButtonTextureMarginX, ButtonTextureMarginY)
			?? CreateButtonStyle(new Color(0.20f, 0.11f, 0.04f, 0.98f), new Color(0.92f, 0.61f, 0.25f, 0.88f));

		button.AddThemeStyleboxOverride("normal", selectedStyle);
		button.AddThemeStyleboxOverride("hover", selectedStyle);
		button.AddThemeStyleboxOverride("pressed", selectedStyle);
		button.AddThemeStyleboxOverride("hover_pressed", selectedStyle);
		button.AddThemeStyleboxOverride("focus", selectedStyle);
		button.AddThemeColorOverride("font_color", new Color(1.0f, 0.87f, 0.45f));
		button.AddThemeColorOverride("font_hover_color", new Color(1.0f, 0.92f, 0.58f));
		button.AddThemeColorOverride("font_pressed_color", new Color(1.0f, 0.87f, 0.45f));
		button.AddThemeColorOverride("font_hover_pressed_color", new Color(1.0f, 0.92f, 0.58f));
	}

	private static void ApplyPopupControlStyle(Control control)
	{
		StyleBox normalStyle = (StyleBox)CreateTextureStyle(DropdownTexturePath, DropdownTextureMarginX, DropdownTextureMarginY)
			?? CreateButtonStyle(new Color(0.13f, 0.15f, 0.13f, 0.96f), new Color(0.64f, 0.51f, 0.28f, 0.64f));
		StyleBox hoverStyle = (StyleBox)CreateTextureStyle(DropdownHoverTexturePath, DropdownTextureMarginX, DropdownTextureMarginY)
			?? normalStyle;
		StyleBox pressedStyle = (StyleBox)CreateTextureStyle(DropdownPressedTexturePath, DropdownTextureMarginX, DropdownTextureMarginY)
			?? hoverStyle;
		control.AddThemeStyleboxOverride("normal", normalStyle);
		control.AddThemeStyleboxOverride("hover", hoverStyle);
		control.AddThemeStyleboxOverride("pressed", pressedStyle);
		control.AddThemeStyleboxOverride("hover_pressed", pressedStyle);
		control.AddThemeStyleboxOverride("focus", hoverStyle);

		control.AddThemeIconOverride("arrow", CreateTransparentIcon());
		control.AddThemeConstantOverride("arrow_margin", 0);
		control.AddThemeConstantOverride("h_separation", 0);
		control.AddThemeColorOverride("font_color", new Color(0.98f, 0.91f, 0.72f));
		control.AddThemeColorOverride("font_hover_color", new Color(1.0f, 0.96f, 0.78f));
		control.AddThemeColorOverride("font_focus_color", new Color(1.0f, 0.96f, 0.78f));
		control.AddThemeColorOverride("font_pressed_color", new Color(1.0f, 0.93f, 0.76f));
		control.AddThemeColorOverride("font_hover_pressed_color", new Color(1.0f, 0.96f, 0.80f));
		control.AddThemeColorOverride("font_outline_color", new Color(0.11f, 0.05f, 0.02f, 0.9f));
		control.AddThemeFontSizeOverride("font_size", 24);
		control.AddThemeConstantOverride("outline_size", 2);

		if (control is OptionButton optionButton)
		{
			optionButton.Alignment = HorizontalAlignment.Center;
			ApplyDropdownListStyle(optionButton.GetPopup());
		}
	}

	private static Texture2D CreateTransparentIcon()
	{
		Image image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
		image.Fill(new Color(1.0f, 1.0f, 1.0f, 0.0f));
		return ImageTexture.CreateFromImage(image);
	}

	private static void ApplyDropdownListStyle(PopupMenu popup)
	{
		if (popup is null)
		{
			return;
		}

		StyleBox panelStyle = CreateTextureStyle(
			DropdownListPanelTexturePath,
			DropdownListPanelTextureMarginX,
			DropdownListPanelTextureMarginY);
		StyleBox hoverStyle = CreateTextureStyle(
			DropdownListHoverTexturePath,
			DropdownListHoverTextureMarginX,
			DropdownListHoverTextureMarginY);

		if (panelStyle != null)
		{
			popup.AddThemeStyleboxOverride("panel", panelStyle);
		}

		if (hoverStyle != null)
		{
			popup.AddThemeStyleboxOverride("hover", hoverStyle);
		}

		popup.AddThemeColorOverride("font_color", new Color(1.0f, 0.91f, 0.70f));
		popup.AddThemeColorOverride("font_hover_color", new Color(1.0f, 0.97f, 0.79f));
		popup.AddThemeColorOverride("font_disabled_color", new Color(0.62f, 0.52f, 0.40f));
		popup.AddThemeColorOverride("font_outline_color", new Color(0.11f, 0.05f, 0.02f, 0.9f));
		popup.AddThemeFontSizeOverride("font_size", 22);
		popup.AddThemeConstantOverride("outline_size", 2);
		popup.AddThemeConstantOverride("item_start_padding", 22);
		popup.AddThemeConstantOverride("item_end_padding", 22);
		popup.AddThemeConstantOverride("v_separation", 6);
	}

	private static void ApplySliderStyle(HSlider slider)
	{
		StyleBoxTexture trackStyle = CreateTextureStyle(SliderTrackTexturePath, SliderTextureMarginX, SliderTextureMarginY);
		StyleBoxTexture fillStyle = CreateTextureStyle(SliderFillTexturePath, SliderTextureMarginX, SliderTextureMarginY);
		Texture2D grabberTexture = ResourceLoader.Load<Texture2D>(SliderGrabberTexturePath);

		if (trackStyle != null)
		{
			slider.AddThemeStyleboxOverride("slider", trackStyle);
		}

		if (fillStyle != null)
		{
			slider.AddThemeStyleboxOverride("grabber_area", fillStyle);
			slider.AddThemeStyleboxOverride("grabber_area_highlight", fillStyle);
		}

		if (grabberTexture != null)
		{
			slider.AddThemeIconOverride("grabber", grabberTexture);
			slider.AddThemeIconOverride("grabber_highlight", grabberTexture);
			slider.AddThemeIconOverride("grabber_disabled", grabberTexture);
		}
	}

	private static StyleBoxTexture CreateTextureStyle(string texturePath, float marginX, float marginY)
	{
		Texture2D texture = ResourceLoader.Load<Texture2D>(texturePath);
		if (texture is null)
		{
			GD.PushWarning($"Unable to load settings UI texture: {texturePath}");
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
		return style;
	}

	private static StyleBoxFlat CreateButtonStyle(Color color, Color borderColor)
	{
		StyleBoxFlat style = new()
		{
			BgColor = color,
			BorderColor = borderColor,
		};
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(6);
		return style;
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

	private static void PlayUiClickSound()
	{
		AudioManager.Instance?.PlayUiClick();
	}

	private static void PlayUiHoverSound()
	{
		AudioManager.Instance?.PlayUiHover();
	}

	private static bool IsBackEvent(InputEvent @event)
	{
		return @event is InputEventKey keyEvent
			&& keyEvent.Pressed
			&& !keyEvent.Echo
			&& keyEvent.Keycode == Key.Escape;
	}
}
