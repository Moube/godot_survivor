using Godot;
using System;
using System.Collections.Generic;

public partial class Hud : CanvasLayer
{
	private const string MainScenePath = "res://scene/main/Main.tscn";
	private const string HudTimerPanelTexturePath = "res://asset/art/ui/ui_hud_timer_panel.png";
	private const string HudInventoryPanelTexturePath = "res://asset/art/ui/ui_hud_inventory_panel.png";
	private const string HudGameOverPanelTexturePath = "res://asset/art/ui/ui_hud_game_over_panel.png";
	private const string HudSlotEmptyTexturePath = "res://asset/art/ui/ui_hud_slot_empty.png";
	private const string HudExperienceBarBackgroundTexturePath = "res://asset/art/ui/ui_hud_experience_bar_bg.png";
	private const string HudExperienceBarFillTexturePath = "res://asset/art/ui/ui_hud_experience_bar_fill.png";
	private const string HudHealthBarBackgroundTexturePath = "res://asset/art/ui/ui_hud_health_bar_bg.png";
	private const string HudHealthBarFillTexturePath = "res://asset/art/ui/ui_hud_health_bar_fill.png";
	private const int InventorySlotCount = 4;
	private const float InventoryIconSize = 44.0f;

	private ProgressBar _healthBar;
	private ProgressBar _experienceBar;
	private Label _runTimeLabel;
	private PanelContainer _runTimePanel;
	private PanelContainer _leftInventoryPanel;
	private PanelContainer _rightInventoryPanel;
	private PanelContainer _gameOverPanel;
	private Label _finalSurvivalTimeLabel;
	private Button _confirmButton;
	private PanelContainer[] _weaponSlotPanels;
	private PanelContainer[] _passiveSlotPanels;
	private TextureRect[] _weaponSlotIcons;
	private TextureRect[] _passiveSlotIcons;
	private Label[] _weaponSlotLevelLabels;
	private Label[] _passiveSlotLevelLabels;
	private StyleBoxTexture _slotEmptyStyle;
	private WeaponInventory _boundWeaponInventory;
	private PassiveInventory _boundPassiveInventory;
	private readonly Dictionary<string, Texture2D> _iconCache = new(StringComparer.Ordinal);

	public override void _Ready()
	{
		_runTimePanel = GetNode<PanelContainer>("TopCenter/RunTimePanel");
		_healthBar = GetNode<ProgressBar>("BottomCenter/VitalsStack/HealthBar");
		_experienceBar = GetNode<ProgressBar>("BottomCenter/VitalsStack/ExperienceBar");
		_runTimeLabel = GetNode<Label>("TopCenter/RunTimePanel/RunTimeMargin/RunTimeLabel");
		_leftInventoryPanel = GetNode<PanelContainer>("LeftInventoryPanel");
		_rightInventoryPanel = GetNode<PanelContainer>("RightInventoryPanel");
		_gameOverPanel = GetNode<PanelContainer>("GameOverCenter/PanelContainer");
		_finalSurvivalTimeLabel = GetNode<Label>("GameOverCenter/PanelContainer/VBoxContainer/GameOverMargin/Content/FinalSurvivalTimeLabel");
		_confirmButton = GetNode<Button>("GameOverCenter/PanelContainer/VBoxContainer/GameOverMargin/Content/ConfirmButton");
		_weaponSlotPanels = LoadSlotPanels("LeftInventoryPanel/MarginContainer/Content/WeaponSlots", "WeaponSlot");
		_passiveSlotPanels = LoadSlotPanels("RightInventoryPanel/MarginContainer/Content/PassiveSlots", "PassiveSlot");
		_weaponSlotIcons = LoadSlotIcons("LeftInventoryPanel/MarginContainer/Content/WeaponSlots", "WeaponSlot");
		_passiveSlotIcons = LoadSlotIcons("RightInventoryPanel/MarginContainer/Content/PassiveSlots", "PassiveSlot");
		HideSlotTextLabels("LeftInventoryPanel/MarginContainer/Content/WeaponSlots", "WeaponSlot");
		HideSlotTextLabels("RightInventoryPanel/MarginContainer/Content/PassiveSlots", "PassiveSlot");
		_weaponSlotLevelLabels = CreateSlotLevelLabels(_weaponSlotIcons);
		_passiveSlotLevelLabels = CreateSlotLevelLabels(_passiveSlotIcons);
		ApplyHudTextureStyles();
		_confirmButton.Pressed += OnConfirmButtonPressed;

		if (GameSession.Instance is null)
		{
			GD.PushError("GameSession autoload is unavailable for HUD.");
			return;
		}

		GameSession.Instance.RunTimeChanged += OnRunTimeChanged;
		GameSession.Instance.PlayerHealthChanged += OnPlayerHealthChanged;
		GameSession.Instance.GameOver += OnGameOver;

		if (ExperienceController.Instance != null)
		{
			ExperienceController.Instance.ExperienceChanged += OnExperienceChanged;
			OnExperienceChanged(
				ExperienceController.Instance.CurrentExperience,
				ExperienceController.Instance.RequiredExperience,
				ExperienceController.Instance.Level);
		}

		OnRunTimeChanged(GameSession.Instance.ElapsedRunTime);
		OnPlayerHealthChanged(GameSession.Instance.CurrentPlayerHealth, GameSession.Instance.MaxPlayerHealth);
		SetGameOverVisible(GameSession.Instance.IsGameOver, GameSession.Instance.FinalSurvivalTime);
		RefreshInventoryHud();
	}

	public override void _ExitTree()
	{
		UnbindInventorySignals();

		if (GameSession.Instance != null)
		{
			GameSession.Instance.RunTimeChanged -= OnRunTimeChanged;
			GameSession.Instance.PlayerHealthChanged -= OnPlayerHealthChanged;
			GameSession.Instance.GameOver -= OnGameOver;
		}

		if (ExperienceController.Instance != null)
		{
			ExperienceController.Instance.ExperienceChanged -= OnExperienceChanged;
		}
	}

	public void BindPlayer(Player player)
	{
		UnbindInventorySignals();

		_boundWeaponInventory = player?.WeaponInventory;
		_boundPassiveInventory = player?.PassiveInventory;

		if (_boundWeaponInventory != null)
		{
			_boundWeaponInventory.InventoryChanged += RefreshInventoryHud;
		}

		if (_boundPassiveInventory != null)
		{
			_boundPassiveInventory.InventoryChanged += RefreshInventoryHud;
		}

		RefreshInventoryHud();
	}

	private void OnRunTimeChanged(double elapsedRunTime)
	{
		_runTimeLabel.Text = FormatRunTime(elapsedRunTime);
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

	private void OnExperienceChanged(int currentExperience, int requiredExperience, int level)
	{
		_experienceBar.MaxValue = Mathf.Max(1, requiredExperience);
		_experienceBar.Value = Mathf.Clamp(currentExperience, 0, requiredExperience);
	}

	private void OnGameOver(double finalSurvivalTime)
	{
		SetGameOverVisible(true, finalSurvivalTime);
	}

	private void SetGameOverVisible(bool isVisible, double finalSurvivalTime)
	{
		_gameOverPanel.Visible = isVisible;
		_finalSurvivalTimeLabel.Text = $"Survived {FormatRunTime(finalSurvivalTime)}";
	}

	private void OnConfirmButtonPressed()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(MainScenePath);
	}

	private void RefreshInventoryHud()
	{
		RefreshWeaponSlots();
		RefreshPassiveSlots();
	}

	private void RefreshWeaponSlots()
	{
		if (_weaponSlotIcons is null)
		{
			return;
		}

		for (int i = 0; i < _weaponSlotIcons.Length; i++)
		{
			if (_boundWeaponInventory != null && i < _boundWeaponInventory.Weapons.Count)
			{
				WeaponInventoryEntry weapon = _boundWeaponInventory.Weapons[i];
				SetInventorySlot(_weaponSlotIcons, _weaponSlotLevelLabels, _weaponSlotPanels, i, weapon.Config?.IconTexturePath, weapon.Level, weapon.DisplayName);
			}
			else
			{
				SetInventorySlot(_weaponSlotIcons, _weaponSlotLevelLabels, _weaponSlotPanels, i, string.Empty, 0, string.Empty);
			}
		}
	}

	private void RefreshPassiveSlots()
	{
		if (_passiveSlotIcons is null)
		{
			return;
		}

		for (int i = 0; i < _passiveSlotIcons.Length; i++)
		{
			if (_boundPassiveInventory != null && i < _boundPassiveInventory.Passives.Count)
			{
				PassiveInventoryEntry passive = _boundPassiveInventory.Passives[i];
				SetInventorySlot(_passiveSlotIcons, _passiveSlotLevelLabels, _passiveSlotPanels, i, passive.Config?.IconTexturePath, passive.Level, passive.DisplayName);
			}
			else
			{
				SetInventorySlot(_passiveSlotIcons, _passiveSlotLevelLabels, _passiveSlotPanels, i, string.Empty, 0, string.Empty);
			}
		}
	}

	private PanelContainer[] LoadSlotPanels(string rootPath, string slotNamePrefix)
	{
		PanelContainer[] panels = new PanelContainer[InventorySlotCount];
		for (int i = 0; i < panels.Length; i++)
		{
			string path = $"{rootPath}/{slotNamePrefix}{i + 1}";
			panels[i] = GetNodeOrNull<PanelContainer>(path);
		}

		return panels;
	}

	private TextureRect[] LoadSlotIcons(string rootPath, string slotNamePrefix)
	{
		TextureRect[] icons = new TextureRect[InventorySlotCount];
		for (int i = 0; i < icons.Length; i++)
		{
			string path = $"{rootPath}/{slotNamePrefix}{i + 1}/SlotMargin/SlotContent/IconSlot";
			icons[i] = GetNodeOrNull<TextureRect>(path);
		}

		return icons;
	}

	private void HideSlotTextLabels(string rootPath, string slotNamePrefix)
	{
		for (int i = 0; i < InventorySlotCount; i++)
		{
			string path = $"{rootPath}/{slotNamePrefix}{i + 1}/SlotMargin/SlotContent/TextLabel";
			Label label = GetNodeOrNull<Label>(path);
			if (label != null)
			{
				label.Visible = false;
			}
		}
	}

	private Label[] CreateSlotLevelLabels(TextureRect[] icons)
	{
		if (icons is null)
		{
			return null;
		}

		Label[] labels = new Label[icons.Length];
		for (int i = 0; i < icons.Length; i++)
		{
			TextureRect icon = icons[i];
			if (icon is null)
			{
				continue;
			}

			icon.CustomMinimumSize = new Vector2(InventoryIconSize, InventoryIconSize);
			icon.MouseFilter = Control.MouseFilterEnum.Pass;

			Label label = icon.GetNodeOrNull<Label>("LevelLabel");
			if (label is null)
			{
				label = new Label { Name = "LevelLabel" };
				icon.AddChild(label);
			}

			label.Visible = false;
			label.MouseFilter = Control.MouseFilterEnum.Ignore;
			label.ZIndex = 1;
			label.AnchorLeft = 1.0f;
			label.AnchorRight = 1.0f;
			label.OffsetLeft = -InventoryIconSize;
			label.OffsetTop = 0.0f;
			label.OffsetRight = 0.0f;
			label.OffsetBottom = 16.0f;
			label.HorizontalAlignment = HorizontalAlignment.Right;
			label.VerticalAlignment = VerticalAlignment.Top;
			label.AddThemeFontSizeOverride("font_size", 11);
			label.AddThemeColorOverride("font_color", new Color(1.0f, 0.94f, 0.36f));
			label.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.04f, 0.03f));
			label.AddThemeConstantOverride("outline_size", 2);
			labels[i] = label;
		}

		return labels;
	}

	private void SetInventorySlot(TextureRect[] icons, Label[] levelLabels, PanelContainer[] slotPanels, int index, string texturePath, int level, string tooltipText)
	{
		if (icons is null || index < 0 || index >= icons.Length || icons[index] is null)
		{
			return;
		}

		bool hasItem = !string.IsNullOrWhiteSpace(texturePath);
		Texture2D texture = LoadIconTexture(texturePath);
		icons[index].Texture = texture;
		icons[index].Visible = true;
		icons[index].TooltipText = tooltipText ?? string.Empty;
		ApplySlotPanelStyle(slotPanels, index, hasItem);

		if (levelLabels is null || index >= levelLabels.Length || levelLabels[index] is null)
		{
			return;
		}

		levelLabels[index].Visible = level > 0;
		levelLabels[index].Text = level > 0 ? $"Lv.{level}" : string.Empty;
		levelLabels[index].TooltipText = tooltipText ?? string.Empty;
	}

	private Texture2D LoadIconTexture(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}

		if (_iconCache.TryGetValue(path, out Texture2D cachedTexture))
		{
			return cachedTexture;
		}

		Texture2D texture = ResourceLoader.Load<Texture2D>(path);
		if (texture == null)
		{
			GD.PushWarning($"Unable to load HUD icon texture: {path}");
		}

		_iconCache[path] = texture;
		return texture;
	}

	private void ApplyHudTextureStyles()
	{
		ApplyPanelStyle(_runTimePanel, HudTimerPanelTexturePath, 32.0f, 18.0f);
		ApplyPanelStyle(_leftInventoryPanel, HudInventoryPanelTexturePath, 24.0f, 20.0f);
		ApplyPanelStyle(_rightInventoryPanel, HudInventoryPanelTexturePath, 24.0f, 20.0f);
		ApplyPanelStyle(_gameOverPanel, HudGameOverPanelTexturePath, 34.0f, 34.0f);
		ApplyProgressBarStyle(_experienceBar, HudExperienceBarBackgroundTexturePath, HudExperienceBarFillTexturePath, 16.0f, 3.0f);
		ApplyProgressBarStyle(_healthBar, HudHealthBarBackgroundTexturePath, HudHealthBarFillTexturePath, 18.0f, 5.0f);

		_slotEmptyStyle = CreateTextureStyle(HudSlotEmptyTexturePath, 11.0f, 11.0f);
		ApplySlotPanelStyles(_weaponSlotPanels, _slotEmptyStyle);
		ApplySlotPanelStyles(_passiveSlotPanels, _slotEmptyStyle);
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

	private static void ApplyProgressBarStyle(ProgressBar progressBar, string backgroundTexturePath, string fillTexturePath, float marginX, float marginY)
	{
		if (progressBar is null)
		{
			return;
		}

		StyleBoxTexture backgroundStyle = CreateTextureStyle(backgroundTexturePath, marginX, marginY);
		StyleBoxTexture fillStyle = CreateTextureStyle(fillTexturePath, marginX, marginY);
		if (backgroundStyle != null)
		{
			progressBar.AddThemeStyleboxOverride("background", backgroundStyle);
		}

		if (fillStyle != null)
		{
			progressBar.AddThemeStyleboxOverride("fill", fillStyle);
		}
	}

	private void ApplySlotPanelStyle(PanelContainer[] slotPanels, int index, bool hasItem)
	{
		if (slotPanels is null || index < 0 || index >= slotPanels.Length || slotPanels[index] is null)
		{
			return;
		}

		if (_slotEmptyStyle is null)
		{
			return;
		}

		slotPanels[index].AddThemeStyleboxOverride("panel", _slotEmptyStyle);
	}

	private static void ApplySlotPanelStyles(PanelContainer[] slotPanels, StyleBoxTexture style)
	{
		if (slotPanels is null || style is null)
		{
			return;
		}

		foreach (PanelContainer slotPanel in slotPanels)
		{
			slotPanel?.AddThemeStyleboxOverride("panel", style);
		}
	}

	private static StyleBoxTexture CreateTextureStyle(string texturePath, float marginX, float marginY)
	{
		Texture2D texture = ResourceLoader.Load<Texture2D>(texturePath);
		if (texture is null)
		{
			GD.PushWarning($"Unable to load HUD UI texture: {texturePath}");
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

	private void UnbindInventorySignals()
	{
		if (_boundWeaponInventory != null)
		{
			_boundWeaponInventory.InventoryChanged -= RefreshInventoryHud;
			_boundWeaponInventory = null;
		}

		if (_boundPassiveInventory != null)
		{
			_boundPassiveInventory.InventoryChanged -= RefreshInventoryHud;
			_boundPassiveInventory = null;
		}
	}

	private static string FormatRunTime(double elapsedSeconds)
	{
		int totalSeconds = Math.Max(0, (int)Math.Floor(elapsedSeconds));
		int minutes = totalSeconds / 60;
		int seconds = totalSeconds % 60;
		return $"{minutes:00}:{seconds:00}";
	}
}
