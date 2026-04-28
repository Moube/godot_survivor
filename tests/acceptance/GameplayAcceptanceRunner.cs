using Godot;
using System;
using System.Threading.Tasks;

public partial class GameplayAcceptanceRunner : Node
{
	private int _failures;

	public override async void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		try
		{
			await Run();
		}
		catch (Exception exception)
		{
			Fail($"Unhandled exception: {exception}");
		}

		GD.Print(_failures == 0
			? "ACCEPTANCE_RESULT: PASS"
			: $"ACCEPTANCE_RESULT: FAIL ({_failures} failure(s))");
		GetTree().Paused = false;
		GetTree().Quit(_failures == 0 ? 0 : 1);
	}

	private async Task Run()
	{
		await WaitFrames(2);

		Check(GameSession.Instance != null, "GameSession autoload exists.");
		Check(GameConfigManager.Instance != null, "GameConfigManager autoload exists.");
		Check(ExperienceController.Instance != null, "ExperienceController autoload exists.");
		Check(GameConfigManager.Instance?.IsLoaded == true, "GameConfigManager loaded configs.");

		LevelConfig levelConfig = GameConfigManager.Instance?.GetLevelConfig("level_01");
		Check(levelConfig != null, "level_01 config exists.");
		Check(GameConfigManager.Instance?.GetAllLevelConfigs().Count > 0, "level list is available.");
		Check(GameConfigManager.Instance?.GetWeaponConfig("magic_wand") != null, "initial weapon config exists.");
		Check(GameConfigManager.Instance?.GetWeaponConfig("spark_orb") != null, "nearest-enemy weapon config exists.");
		Check(GameConfigManager.Instance?.GetWeaponConfig("chaos_missile") != null, "random-direction weapon config exists.");
		Check(GameConfigManager.Instance?.GetPassiveConfig("collector_talisman") != null, "passive config exists.");
		Check(GameConfigManager.Instance?.GetUpgradePoolConfig(levelConfig?.UpgradePoolId ?? string.Empty) != null, "upgrade pool exists.");

		await VerifyMainMenu();
		await VerifyLevelLoop(levelConfig);
	}

	private async Task VerifyMainMenu()
	{
		PackedScene mainScene = ResourceLoader.Load<PackedScene>("res://scene/main/Main.tscn");
		Check(mainScene != null, "main menu scene loads.");
		if (mainScene is null)
		{
			return;
		}

		Control main = mainScene.Instantiate<Control>();
		AddChild(main);
		await WaitFrames(2);

		Control mainMenuPanel = main.GetNodeOrNull<Control>("MainMenuPanel");
		Control levelSelectPanel = main.GetNodeOrNull<Control>("LevelSelectPanel");
		Control settingsPanel = main.GetNodeOrNull<Control>("SettingsPanel");
		Button startButton = main.GetNodeOrNull<Button>("MainMenuPanel/PanelContainer/MarginContainer/Content/StartGameButton");
		Button settingsButton = main.GetNodeOrNull<Button>("MainMenuPanel/PanelContainer/MarginContainer/Content/SettingsButton");
		Button settingsBackButton = main.GetNodeOrNull<Button>("SettingsPanel/PanelContainer/MarginContainer/Content/BackButton");
		VBoxContainer levelButtonList = main.GetNodeOrNull<VBoxContainer>("LevelSelectPanel/PanelContainer/MarginContainer/Content/LevelButtonList");

		Check(mainMenuPanel?.Visible == true, "main menu starts visible.");
		Check(levelSelectPanel?.Visible == false, "level select starts hidden.");
		Check(settingsPanel?.Visible == false, "settings starts hidden.");
		Check(startButton != null, "Start Game button exists.");
		Check(settingsButton != null, "Settings button exists.");

		startButton?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(1);
		Check(levelSelectPanel?.Visible == true, "Start Game opens level select.");
		Check(levelButtonList != null && levelButtonList.GetChildCount() > 0, "level select has at least one level button.");

		mainMenuPanel?.Show();
		levelSelectPanel?.Hide();
		settingsButton?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(1);
		Check(settingsPanel?.Visible == true, "Settings opens placeholder page.");

		settingsBackButton?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(1);
		Check(mainMenuPanel?.Visible == true, "settings back returns to main menu.");

		main.QueueFree();
		await WaitFrames(1);
	}

	private async Task VerifyLevelLoop(LevelConfig levelConfig)
	{
		Check(levelConfig != null, "level config is available before loading level.");
		if (levelConfig is null)
		{
			return;
		}

		GameSession.Instance.SelectLevelConfig(levelConfig.Id);
		PackedScene levelScene = ResourceLoader.Load<PackedScene>(levelConfig.ScenePath);
		Check(levelScene != null, "level scene loads from config.");
		if (levelScene is null)
		{
			return;
		}

		Node level = levelScene.Instantiate();
		AddChild(level);
		await WaitFrames(8);

		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		Check(player != null, "player spawned.");
		Check(player?.WeaponInventory?.Weapons.Count == 1, "initial weapon added.");
		Check(player?.PassiveInventory?.Passives.Count == 0, "passive inventory starts empty.");
		Check(level.GetNodeOrNull<Node>("SpawnDirector") != null, "SpawnDirector exists.");
		Check(level.GetNodeOrNull<Node>("UpgradeManager") != null, "UpgradeManager exists.");
		Check(level.GetNodeOrNull<CanvasLayer>("Hud") != null, "HUD exists.");

		await WaitFrames(30);
		Check(GetTree().GetNodesInGroup("enemy").Count > 0, "spawn director spawned enemies.");

		int requiredExperience = ExperienceController.Instance.RequiredExperience;
		ExperienceController.Instance.AddExperience(requiredExperience);
		await WaitFrames(2);

		Control upgradePanel = level.GetNodeOrNull<Control>("Hud/UpgradeChoicePanel");
		Check(ExperienceController.Instance.IsLevelUpPending, "experience full requests level up.");
		Check(GetTree().Paused, "upgrade choice pauses the scene tree.");
		Check(upgradePanel?.Visible == true, "upgrade panel visible.");

		Button selectButton = upgradePanel?.GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/Content/CardRow/Card1/CardMargin/CardContent/SelectButton");
		Check(selectButton != null, "first upgrade select button exists.");
		selectButton?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);

		Check(!GetTree().Paused, "selecting upgrade resumes the scene tree.");
		Check(ExperienceController.Instance.Level == 2, "selecting upgrade completes level up.");
		Check(upgradePanel?.Visible == false, "upgrade panel hidden after selection.");
		Check(player?.WeaponInventory?.Weapons.Count > 1 || player?.PassiveInventory?.Passives.Count > 0 || player?.WeaponInventory?.Weapons[0].Level > 1,
			"selected reward changed weapon or passive state.");

		GameSession.Instance.TriggerGameOver();
		await WaitFrames(2);
		Control gameOverPanel = level.GetNodeOrNull<Control>("Hud/GameOverCenter/PanelContainer");
		Button confirmButton = level.GetNodeOrNull<Button>("Hud/GameOverCenter/PanelContainer/VBoxContainer/GameOverMargin/Content/ConfirmButton");
		Check(gameOverPanel?.Visible == true, "game over panel visible.");
		Check(confirmButton != null, "game over confirm button exists.");

		GetTree().Paused = false;
		level.QueueFree();
		await WaitFrames(1);
	}

	private async Task WaitFrames(int frameCount)
	{
		for (int i = 0; i < frameCount; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		if (condition)
		{
			GD.Print($"PASS: {message}");
			return;
		}

		Fail(message);
	}

	private void Fail(string message)
	{
		_failures++;
		GD.PushError($"FAIL: {message}");
	}
}
