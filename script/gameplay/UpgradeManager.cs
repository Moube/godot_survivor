using Godot;
using System.Collections.Generic;

public sealed class UpgradeChoiceOption
{
	public UpgradeChoiceOption(UpgradeRewardConfig reward, string title, string typeLabel, string description)
	{
		Reward = reward;
		Title = title;
		TypeLabel = typeLabel;
		Description = description;
	}

	public UpgradeRewardConfig Reward { get; }

	public string Title { get; }

	public string TypeLabel { get; }

	public string Description { get; }
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
			UpgradeRewardType.NewWeapon => BuildWeaponOption(reward, "New Weapon", "Add this weapon to your run."),
			UpgradeRewardType.WeaponUpgrade => BuildWeaponOption(reward, "Weapon Upgrade", GetWeaponUpgradeDescription(reward.ContentId)),
			UpgradeRewardType.NewPassive => BuildPassiveOption(reward, "New Passive", "Gain this passive item."),
			UpgradeRewardType.PassiveUpgrade => BuildPassiveOption(reward, "Passive Upgrade", GetPassiveUpgradeDescription(reward.ContentId)),
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

		string description = string.IsNullOrWhiteSpace(weapon.Description) ? fallbackDescription : weapon.Description;
		if (reward.Type == UpgradeRewardType.WeaponUpgrade)
		{
			description = fallbackDescription;
		}

		return new UpgradeChoiceOption(reward, weapon.DisplayName, typeLabel, description);
	}

	private UpgradeChoiceOption BuildPassiveOption(UpgradeRewardConfig reward, string typeLabel, string fallbackDescription)
	{
		PassiveConfig passive = GameConfigManager.Instance?.GetPassiveConfig(reward.ContentId);
		if (passive is null)
		{
			return null;
		}

		string description = string.IsNullOrWhiteSpace(passive.Description) ? fallbackDescription : passive.Description;
		if (reward.Type == UpgradeRewardType.PassiveUpgrade)
		{
			description = fallbackDescription;
		}

		return new UpgradeChoiceOption(reward, passive.DisplayName, typeLabel, description);
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

		return $"Upgrade to Lv.{nextLevel}. Improves damage, cooldown, and later projectile count.";
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

		string effect = passiveConfig is null ? "Improves its stat bonus." : FormatPassiveEffect(passiveConfig);
		return $"Upgrade to Lv.{nextLevel}. {effect}";
	}

	private static string FormatPassiveEffect(PassiveConfig passive)
	{
		string value = passive.IsMultiplier
			? $"{passive.ValuePerLevel * 100.0f:0.#}% per level"
			: $"+{passive.ValuePerLevel:0.#} per level";

		return passive.StatType switch
		{
			PlayerStatType.MoveSpeed => $"Move speed {value}.",
			PlayerStatType.MaxHealth => $"Max health {value}.",
			PlayerStatType.PickupRange => $"Pickup range {value}.",
			PlayerStatType.WeaponDamageMultiplier => $"Weapon damage {value}.",
			PlayerStatType.WeaponCooldownMultiplier => $"Weapon cooldown {value}.",
			_ => $"Stat bonus {value}.",
		};
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
		_choicePanel.HideChoices();
		_currentOptions.Clear();
		_isChoosing = false;

		if (GameSession.Instance?.IsGameOver != true)
		{
			GetTree().Paused = _wasPausedBeforeChoice;
		}
	}
}
