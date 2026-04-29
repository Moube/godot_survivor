using Godot;
using System;
using System.Collections.Generic;

public partial class Hud : CanvasLayer
{
	private const string MainScenePath = "res://scene/main/Main.tscn";

	private ProgressBar _healthBar;
	private ProgressBar _experienceBar;
	private Label _runTimeLabel;
	private Control _gameOverPanel;
	private Label _finalSurvivalTimeLabel;
	private Button _confirmButton;
	private Label[] _weaponSlotLabels;
	private Label[] _passiveSlotLabels;
	private TextureRect[] _weaponSlotIcons;
	private TextureRect[] _passiveSlotIcons;
	private WeaponInventory _boundWeaponInventory;
	private PassiveInventory _boundPassiveInventory;
	private readonly Dictionary<string, Texture2D> _iconCache = new(StringComparer.Ordinal);

	public override void _Ready()
	{
		_healthBar = GetNode<ProgressBar>("BottomCenter/VitalsStack/HealthBar");
		_experienceBar = GetNode<ProgressBar>("BottomCenter/VitalsStack/ExperienceBar");
		_runTimeLabel = GetNode<Label>("TopCenter/RunTimePanel/RunTimeMargin/RunTimeLabel");
		_gameOverPanel = GetNode<Control>("GameOverCenter/PanelContainer");
		_finalSurvivalTimeLabel = GetNode<Label>("GameOverCenter/PanelContainer/VBoxContainer/GameOverMargin/Content/FinalSurvivalTimeLabel");
		_confirmButton = GetNode<Button>("GameOverCenter/PanelContainer/VBoxContainer/GameOverMargin/Content/ConfirmButton");
		_weaponSlotLabels = LoadSlotLabels("LeftInventoryPanel/MarginContainer/Content/WeaponSlots", "WeaponSlot");
		_passiveSlotLabels = LoadSlotLabels("RightInventoryPanel/MarginContainer/Content/PassiveSlots", "PassiveSlot");
		_weaponSlotIcons = LoadSlotIcons("LeftInventoryPanel/MarginContainer/Content/WeaponSlots", "WeaponSlot");
		_passiveSlotIcons = LoadSlotIcons("RightInventoryPanel/MarginContainer/Content/PassiveSlots", "PassiveSlot");
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
		if (_weaponSlotLabels is null)
		{
			return;
		}

		for (int i = 0; i < _weaponSlotLabels.Length; i++)
		{
			if (_weaponSlotLabels[i] is null)
			{
				continue;
			}

			if (_boundWeaponInventory != null && i < _boundWeaponInventory.Weapons.Count)
			{
				WeaponInventoryEntry weapon = _boundWeaponInventory.Weapons[i];
				_weaponSlotLabels[i].Text = $"{weapon.DisplayName} Lv.{weapon.Level}";
				SetSlotIcon(_weaponSlotIcons, i, weapon.Config?.IconTexturePath);
			}
			else
			{
				_weaponSlotLabels[i].Text = "Empty";
				SetSlotIcon(_weaponSlotIcons, i, string.Empty);
			}
		}
	}

	private void RefreshPassiveSlots()
	{
		if (_passiveSlotLabels is null)
		{
			return;
		}

		for (int i = 0; i < _passiveSlotLabels.Length; i++)
		{
			if (_passiveSlotLabels[i] is null)
			{
				continue;
			}

			if (_boundPassiveInventory != null && i < _boundPassiveInventory.Passives.Count)
			{
				PassiveInventoryEntry passive = _boundPassiveInventory.Passives[i];
				_passiveSlotLabels[i].Text = $"{passive.DisplayName} Lv.{passive.Level}";
				SetSlotIcon(_passiveSlotIcons, i, passive.Config?.IconTexturePath);
			}
			else
			{
				_passiveSlotLabels[i].Text = "Empty";
				SetSlotIcon(_passiveSlotIcons, i, string.Empty);
			}
		}
	}

	private Label[] LoadSlotLabels(string rootPath, string slotNamePrefix)
	{
		Label[] labels = new Label[4];
		for (int i = 0; i < labels.Length; i++)
		{
			string path = $"{rootPath}/{slotNamePrefix}{i + 1}/SlotMargin/SlotContent/TextLabel";
			labels[i] = GetNodeOrNull<Label>(path);
		}

		return labels;
	}

	private TextureRect[] LoadSlotIcons(string rootPath, string slotNamePrefix)
	{
		TextureRect[] icons = new TextureRect[4];
		for (int i = 0; i < icons.Length; i++)
		{
			string path = $"{rootPath}/{slotNamePrefix}{i + 1}/SlotMargin/SlotContent/IconSlot";
			icons[i] = GetNodeOrNull<TextureRect>(path);
		}

		return icons;
	}

	private void SetSlotIcon(TextureRect[] icons, int index, string texturePath)
	{
		if (icons is null || index < 0 || index >= icons.Length || icons[index] is null)
		{
			return;
		}

		icons[index].Texture = LoadIconTexture(texturePath);
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
