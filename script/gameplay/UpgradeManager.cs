using Godot;
using System.Collections.Generic;
using System.Globalization;

public sealed class UpgradeChoiceOption
{
	public UpgradeChoiceOption(UpgradeRewardConfig reward, string title, string typeLabel, string description, string iconTexturePath)
	{
		Reward = reward;
		Title = title;
		TypeLabel = typeLabel;
		Description = description;
		IconTexturePath = iconTexturePath;
	}

	public UpgradeRewardConfig Reward { get; }

	public string Title { get; }

	public string TypeLabel { get; }

	public string Description { get; }

	public string IconTexturePath { get; }
}

public partial class UpgradeManager : Node
{
	private const string DefaultUpgradeChoicePanelPath = "res://scene/ui/upgrade/UpgradeChoicePanel.tscn";
	private const int ChoiceCount = 3;

	[Export(PropertyHint.File, "*.tscn")]
	public string UpgradeChoicePanelPath { get; set; } = DefaultUpgradeChoicePanelPath;

	private readonly RandomNumberGenerator _random = new();
	private readonly List<UpgradeChoiceOption> _currentOptions = new();

	private Player _player;
	private WeaponInventory _weaponInventory;
	private PassiveInventory _passiveInventory;
	private LevelConfig _levelConfig;
	private UpgradePoolConfig _upgradePool;
	private UpgradeChoicePanel _choicePanel;
	private bool _isChoosing;
	private bool _wasPausedBeforeChoice;

	public override void _Ready()
	{
		_random.Randomize();
		ProcessMode = ProcessModeEnum.Always;

		if (GameSettings.Instance != null)
		{
			GameSettings.Instance.LanguageChanged += OnLanguageChanged;
		}
	}

	public override void _ExitTree()
	{
		if (ExperienceController.Instance != null)
		{
			ExperienceController.Instance.LevelUpRequested -= OnLevelUpRequested;
		}

		if (_choicePanel != null)
		{
			_choicePanel.OptionSelected -= OnOptionSelected;
		}

		if (GameSettings.Instance != null)
		{
			GameSettings.Instance.LanguageChanged -= OnLanguageChanged;
		}
	}

	public void Initialize(LevelConfig levelConfig, Player player, CanvasLayer uiRoot)
	{
		_levelConfig = levelConfig;
		_player = player;
		_weaponInventory = player?.WeaponInventory;
		_passiveInventory = player?.PassiveInventory;

		if (_levelConfig is null)
		{
			GD.PushError("UpgradeManager cannot initialize without level config.");
			return;
		}

		_upgradePool = GameConfigManager.Instance?.GetUpgradePoolConfig(_levelConfig.UpgradePoolId);
		if (_upgradePool is null)
		{
			GD.PushError($"UpgradeManager cannot find upgrade pool '{_levelConfig.UpgradePoolId}'.");
			return;
		}

		if (_weaponInventory is null || _passiveInventory is null)
		{
			GD.PushError("UpgradeManager cannot initialize because player inventories are missing.");
			return;
		}

		if (!EnsureChoicePanel(uiRoot))
		{
			return;
		}

		if (ExperienceController.Instance != null)
		{
			ExperienceController.Instance.LevelUpRequested -= OnLevelUpRequested;
			ExperienceController.Instance.LevelUpRequested += OnLevelUpRequested;
		}
	}

	private bool EnsureChoicePanel(CanvasLayer uiRoot)
	{
		if (_choicePanel != null && IsInstanceValid(_choicePanel))
		{
			return true;
		}

		PackedScene panelScene = ResourceLoader.Load<PackedScene>(UpgradeChoicePanelPath);
		if (panelScene is null)
		{
			GD.PushError($"UpgradeManager cannot load upgrade choice panel: {UpgradeChoicePanelPath}");
			return false;
		}

		Node panelInstance = panelScene.Instantiate();
		if (panelInstance is not UpgradeChoicePanel panel)
		{
			GD.PushError("UpgradeChoicePanel scene must instantiate an UpgradeChoicePanel.");
			panelInstance.QueueFree();
			return false;
		}

		_choicePanel = panel;
		_choicePanel.ProcessMode = ProcessModeEnum.Always;
		_choicePanel.OptionSelected += OnOptionSelected;
		Node parent = uiRoot ?? GetTree().CurrentScene ?? GetTree().Root;
		parent.AddChild(_choicePanel);
		_choicePanel.HideChoices();
		return true;
	}

	private void OnLevelUpRequested(int level)
	{
		if (_isChoosing || GameSession.Instance?.IsGameOver == true)
		{
			return;
		}

		_currentOptions.Clear();
		_currentOptions.AddRange(BuildChoices());

		if (_currentOptions.Count == 0)
		{
			GD.PushWarning("No eligible upgrade rewards are available.");
			ExperienceController.Instance?.CompletePendingLevelUp();
			return;
		}

		_isChoosing = true;
		_wasPausedBeforeChoice = GetTree().Paused;
		AudioManager.Instance?.PlayLevelUp();
		GetTree().Paused = true;
		_choicePanel.ShowChoices(_currentOptions);
	}

	private List<UpgradeChoiceOption> BuildChoices()
	{
		List<UpgradeRewardConfig> eligibleRewards = BuildEligibleRewards();
		List<UpgradeChoiceOption> choices = new();

		while (choices.Count < ChoiceCount && eligibleRewards.Count > 0)
		{
			UpgradeRewardConfig reward = DrawWeightedReward(eligibleRewards);
			if (reward is null)
			{
				break;
			}

			eligibleRewards.Remove(reward);
			UpgradeChoiceOption option = BuildChoiceOption(reward);
			if (option != null)
			{
				choices.Add(option);
			}
		}

		return choices;
	}

	private List<UpgradeRewardConfig> BuildEligibleRewards()
	{
		List<UpgradeRewardConfig> eligibleRewards = new();
		foreach (UpgradeRewardConfig reward in _upgradePool.Rewards)
		{
			if (reward is null || reward.Weight <= 0 || !IsRewardEligible(reward))
			{
				continue;
			}

			eligibleRewards.Add(reward);
		}

		return eligibleRewards;
	}

	private bool IsRewardEligible(UpgradeRewardConfig reward)
	{
		return reward.Type switch
		{
			UpgradeRewardType.NewWeapon => _weaponInventory.CanAddWeapon(reward.ContentId),
			UpgradeRewardType.WeaponUpgrade => _weaponInventory.CanUpgradeWeapon(reward.ContentId),
			UpgradeRewardType.NewPassive => _passiveInventory.CanAddPassive(reward.ContentId),
			UpgradeRewardType.PassiveUpgrade => _passiveInventory.CanUpgradePassive(reward.ContentId),
			_ => false,
		};
	}

	private UpgradeRewardConfig DrawWeightedReward(List<UpgradeRewardConfig> rewards)
	{
		int totalWeight = 0;
		foreach (UpgradeRewardConfig reward in rewards)
		{
			totalWeight += Mathf.Max(0, reward.Weight);
		}

		if (totalWeight <= 0)
		{
			return null;
		}

		int roll = _random.RandiRange(1, totalWeight);
		int accumulatedWeight = 0;
		foreach (UpgradeRewardConfig reward in rewards)
		{
			accumulatedWeight += Mathf.Max(0, reward.Weight);
			if (roll <= accumulatedWeight)
			{
				return reward;
			}
		}

		return null;
	}

	private UpgradeChoiceOption BuildChoiceOption(UpgradeRewardConfig reward)
	{
		return reward.Type switch
		{
			UpgradeRewardType.NewWeapon => BuildWeaponOption(reward, GameText.Tr("ui.upgrade.new_weapon"), GameText.Tr("ui.upgrade.add_weapon")),
			UpgradeRewardType.WeaponUpgrade => BuildWeaponOption(reward, GameText.Tr("ui.upgrade.weapon_upgrade"), GetWeaponUpgradeDescription(reward.ContentId)),
			UpgradeRewardType.NewPassive => BuildPassiveOption(reward, GameText.Tr("ui.upgrade.new_passive"), GameText.Tr("ui.upgrade.gain_passive")),
			UpgradeRewardType.PassiveUpgrade => BuildPassiveOption(reward, GameText.Tr("ui.upgrade.passive_upgrade"), GetPassiveUpgradeDescription(reward.ContentId)),
			_ => null,
		};
	}

	private UpgradeChoiceOption BuildWeaponOption(UpgradeRewardConfig reward, string typeLabel, string fallbackDescription)
	{
		WeaponConfig weapon = GameConfigManager.Instance?.GetWeaponConfig(reward.ContentId);
		if (weapon is null)
		{
			return null;
		}

		string description = GameText.ConfigDescription("weapon", weapon.Id, string.IsNullOrWhiteSpace(weapon.Description) ? fallbackDescription : weapon.Description);
		if (reward.Type == UpgradeRewardType.WeaponUpgrade)
		{
			description = fallbackDescription;
		}

		return new UpgradeChoiceOption(reward, GameText.ConfigName("weapon", weapon.Id, weapon.DisplayName), typeLabel, description, weapon.IconTexturePath);
	}

	private UpgradeChoiceOption BuildPassiveOption(UpgradeRewardConfig reward, string typeLabel, string fallbackDescription)
	{
		PassiveConfig passive = GameConfigManager.Instance?.GetPassiveConfig(reward.ContentId);
		if (passive is null)
		{
			return null;
		}

		string description = GameText.ConfigDescription("passive", passive.Id, string.IsNullOrWhiteSpace(passive.Description) ? fallbackDescription : passive.Description);
		if (reward.Type == UpgradeRewardType.PassiveUpgrade)
		{
			description = fallbackDescription;
		}

		return new UpgradeChoiceOption(reward, GameText.ConfigName("passive", passive.Id, passive.DisplayName), typeLabel, description, passive.IconTexturePath);
	}

	private string GetWeaponUpgradeDescription(string weaponId)
	{
		int nextLevel = 2;
		foreach (WeaponInventoryEntry weapon in _weaponInventory.Weapons)
		{
			if (weapon.WeaponId == weaponId)
			{
				nextLevel = weapon.Level + 1;
				break;
			}
		}

		return GameText.Format("ui.upgrade.weapon_upgrade_description", nextLevel);
	}

	private string GetPassiveUpgradeDescription(string passiveId)
	{
		PassiveConfig passiveConfig = GameConfigManager.Instance?.GetPassiveConfig(passiveId);
		int nextLevel = 2;
		foreach (PassiveInventoryEntry passive in _passiveInventory.Passives)
		{
			if (passive.PassiveId == passiveId)
			{
				nextLevel = passive.Level + 1;
				break;
			}
		}

		string effect = passiveConfig is null
			? GameText.Tr("ui.upgrade.effect.generic_improvement")
			: FormatPassiveEffect(passiveConfig);
		return GameText.Format("ui.upgrade.passive_upgrade_description", nextLevel, effect);
	}

	private static string FormatPassiveEffect(PassiveConfig passive)
	{
		string rawValue = passive.IsMultiplier
			? (passive.ValuePerLevel * 100.0f).ToString("0.#", CultureInfo.InvariantCulture)
			: passive.ValuePerLevel.ToString("0.#", CultureInfo.InvariantCulture);
		string value = passive.IsMultiplier
			? GameText.Format("ui.upgrade.effect.percent_per_level", rawValue)
			: GameText.Format("ui.upgrade.effect.flat_per_level", rawValue);

		return passive.StatType switch
		{
			PlayerStatType.MoveSpeed => GameText.Format("ui.upgrade.effect.move_speed", value),
			PlayerStatType.MaxHealth => GameText.Format("ui.upgrade.effect.max_health", value),
			PlayerStatType.PickupRange => GameText.Format("ui.upgrade.effect.pickup_range", value),
			PlayerStatType.WeaponDamageMultiplier => GameText.Format("ui.upgrade.effect.weapon_damage", value),
			PlayerStatType.WeaponCooldownMultiplier => GameText.Format("ui.upgrade.effect.weapon_cooldown", value),
			_ => GameText.Format("ui.upgrade.effect.stat_bonus", value),
		};
	}

	private void OnLanguageChanged(GameLanguage language)
	{
		if (!_isChoosing || _choicePanel is null || _currentOptions.Count == 0)
		{
			return;
		}

		List<UpgradeRewardConfig> rewards = new();
		foreach (UpgradeChoiceOption option in _currentOptions)
		{
			if (option?.Reward != null)
			{
				rewards.Add(option.Reward);
			}
		}

		_currentOptions.Clear();
		foreach (UpgradeRewardConfig reward in rewards)
		{
			UpgradeChoiceOption option = BuildChoiceOption(reward);
			if (option != null)
			{
				_currentOptions.Add(option);
			}
		}

		_choicePanel.ShowChoices(_currentOptions);
	}

	private void OnOptionSelected(int optionIndex)
	{
		if (!_isChoosing || optionIndex < 0 || optionIndex >= _currentOptions.Count)
		{
			return;
		}

		UpgradeChoiceOption option = _currentOptions[optionIndex];
		if (!ApplyReward(option.Reward))
		{
			GD.PushWarning($"Failed to apply upgrade reward '{option.Reward.ContentId}'.");
			return;
		}

		ExperienceController.Instance?.CompletePendingLevelUp();
		CloseChoice();
	}

	private bool ApplyReward(UpgradeRewardConfig reward)
	{
		return reward.Type switch
		{
			UpgradeRewardType.NewWeapon => _weaponInventory.AddWeapon(reward.ContentId),
			UpgradeRewardType.WeaponUpgrade => _weaponInventory.UpgradeWeapon(reward.ContentId),
			UpgradeRewardType.NewPassive => _passiveInventory.AddPassive(reward.ContentId),
			UpgradeRewardType.PassiveUpgrade => _passiveInventory.UpgradePassive(reward.ContentId),
			_ => false,
		};
	}

	private void CloseChoice()
	{
		_choicePanel.HideChoices(animate: true);
		_currentOptions.Clear();
		_isChoosing = false;

		if (GameSession.Instance?.IsGameOver != true)
		{
			GetTree().Paused = _wasPausedBeforeChoice;
		}
	}
}
