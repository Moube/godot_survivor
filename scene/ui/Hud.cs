using Godot;
using System;

public partial class Hud : CanvasLayer
{
	private const string LevelScenePath = "res://scene/level/Level01.tscn";

	private ProgressBar _healthBar;
	private ProgressBar _experienceBar;
	private Label _runTimeLabel;
	private Control _gameOverPanel;
	private Label _finalSurvivalTimeLabel;
	private Button _restartButton;
	private Label[] _weaponSlotLabels;
	private Label[] _passiveSlotLabels;
	private WeaponInventory _boundWeaponInventory;
	private PassiveInventory _boundPassiveInventory;

	public override void _Ready()
	{
		_healthBar = GetNode<ProgressBar>("BottomCenter/VitalsStack/HealthBar");
		_experienceBar = GetNode<ProgressBar>("BottomCenter/VitalsStack/ExperienceBar");
		_runTimeLabel = GetNode<Label>("TopCenter/RunTimePanel/RunTimeMargin/RunTimeLabel");
		_gameOverPanel = GetNode<Control>("GameOverCenter/PanelContainer");
		_finalSurvivalTimeLabel = GetNode<Label>("GameOverCenter/PanelContainer/VBoxContainer/GameOverMargin/Content/FinalSurvivalTimeLabel");
		_restartButton = GetNode<Button>("GameOverCenter/PanelContainer/VBoxContainer/GameOverMargin/Content/RestartButton");
		_weaponSlotLabels = LoadSlotLabels("LeftInventoryPanel/MarginContainer/Content/WeaponSlots", "WeaponSlot");
		_passiveSlotLabels = LoadSlotLabels("RightInventoryPanel/MarginContainer/Content/PassiveSlots", "PassiveSlot");
		_restartButton.Pressed += OnRestartButtonPressed;

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

	private void OnRestartButtonPressed()
	{
		GameSession.Instance?.StartNewRun();
		GetTree().ChangeSceneToFile(LevelScenePath);
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
			}
			else
			{
				_weaponSlotLabels[i].Text = "Empty";
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
			}
			else
			{
				_passiveSlotLabels[i].Text = "Empty";
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
