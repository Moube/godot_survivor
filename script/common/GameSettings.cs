using Godot;
using System;
using System.Collections.Generic;

public enum GameLanguage
{
	Chinese = 0,
	English = 1,
}

public enum GameWindowMode
{
	Windowed = 0,
	Fullscreen = 1,
}

public partial class GameSettings : Node
{
	public const string MasterBusName = "Master";
	public const string SfxBusName = "Sfx";
	public const string MusicBusName = "Music";

	private const string SettingsPath = "user://settings.cfg";
	private const string GameSection = "game";
	private const string AudioSection = "audio";
	private const string DisplaySection = "display";
	private const string LanguageKey = "language";
	private const string MasterVolumeKey = "master_volume";
	private const string SfxVolumeKey = "sfx_volume";
	private const string MusicVolumeKey = "music_volume";
	private const string WindowModeKey = "window_mode";
	private const string ResolutionWidthKey = "resolution_width";
	private const string ResolutionHeightKey = "resolution_height";
	private const string ChineseLanguageValue = "zh";
	private const string EnglishLanguageValue = "en";
	private const string WindowedModeValue = "windowed";
	private const string FullscreenModeValue = "fullscreen";
	private const float DefaultVolume = 1.0f;
	private const float SilentVolumeDb = -80.0f;
	private const int MinWindowWidth = 640;
	private const int MinWindowHeight = 360;
	private static readonly Vector2I[] ResolutionOptions =
	{
		new(1280, 720),
		new(1600, 900),
		new(1920, 1080),
		new(2560, 1440),
		new(3840, 2160),
	};

	private GameLanguage _currentLanguage = GameLanguage.Chinese;
	private GameWindowMode _windowMode = GameWindowMode.Windowed;
	private Vector2I _windowResolution = new(1280, 720);
	private float _masterVolume = DefaultVolume;
	private float _sfxVolume = DefaultVolume;
	private float _musicVolume = DefaultVolume;

	public static GameSettings Instance { get; private set; }

	public static IReadOnlyList<Vector2I> SupportedWindowResolutions => ResolutionOptions;

	public event Action<GameLanguage> LanguageChanged;

	public event Action AudioVolumeChanged;

	public GameLanguage CurrentLanguage => _currentLanguage;

	public GameWindowMode WindowMode => _windowMode;

	public Vector2I WindowResolution => _windowResolution;

	public float MasterVolume => _masterVolume;

	public float SfxVolume => _sfxVolume;

	public float MusicVolume => _musicVolume;

	public override void _EnterTree()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
		ApplyWindowContentScaling();
	}

	public override void _Ready()
	{
		LoadSettings();
		ApplyDisplaySettings();
		ApplyAllAudioVolumes();
		ApplyEngineLocale();
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void SetLanguage(GameLanguage language)
	{
		if (_currentLanguage == language)
		{
			return;
		}

		_currentLanguage = language;
		ApplyEngineLocale();
		SaveSettings();
		LanguageChanged?.Invoke(_currentLanguage);
	}

	public void SetWindowMode(GameWindowMode mode)
	{
		GameWindowMode normalizedMode = NormalizeWindowMode(mode);
		if (_windowMode == normalizedMode)
		{
			return;
		}

		_windowMode = normalizedMode;
		ApplyDisplaySettings();
		SaveSettings();
	}

	public void SetWindowResolution(Vector2I resolution)
	{
		Vector2I normalizedResolution = NormalizeWindowResolution(resolution, GetDefaultWindowResolution());
		if (_windowResolution == normalizedResolution)
		{
			return;
		}

		_windowResolution = normalizedResolution;
		ApplyDisplaySettings();
		SaveSettings();
	}

	public void SetMasterVolume(float value)
	{
		float clampedValue = ClampVolume(value);
		if (Mathf.IsEqualApprox(_masterVolume, clampedValue))
		{
			return;
		}

		_masterVolume = clampedValue;
		ApplyAllAudioVolumes();
		SaveSettings();
		AudioVolumeChanged?.Invoke();
	}

	public void SetSfxVolume(float value)
	{
		float clampedValue = ClampVolume(value);
		if (Mathf.IsEqualApprox(_sfxVolume, clampedValue))
		{
			return;
		}

		_sfxVolume = clampedValue;
		ApplyAllAudioVolumes();
		SaveSettings();
		AudioVolumeChanged?.Invoke();
	}

	public void SetMusicVolume(float value)
	{
		float clampedValue = ClampVolume(value);
		if (Mathf.IsEqualApprox(_musicVolume, clampedValue))
		{
			return;
		}

		_musicVolume = clampedValue;
		ApplyAllAudioVolumes();
		SaveSettings();
		AudioVolumeChanged?.Invoke();
	}

	public void ApplyAllAudioVolumes()
	{
		EnsureAudioBuses();
		ApplyBusVolume(MasterBusName, _masterVolume);
		ApplyBusVolume(SfxBusName, _sfxVolume);
		ApplyBusVolume(MusicBusName, _musicVolume);
	}

	public static void EnsureAudioBuses()
	{
		EnsureAudioBus(SfxBusName, MasterBusName);
		EnsureAudioBus(MusicBusName, MasterBusName);
	}

	private void LoadSettings()
	{
		ConfigFile config = new();
		Error loadError = config.Load(SettingsPath);
		if (loadError != Error.Ok && loadError != Error.FileNotFound)
		{
			GD.PushWarning($"Unable to load settings file '{SettingsPath}': {loadError}");
		}

		string languageValue = ReadString(config, GameSection, LanguageKey, ChineseLanguageValue);
		_currentLanguage = ParseLanguage(languageValue);
		string windowModeValue = ReadString(config, DisplaySection, WindowModeKey, WindowedModeValue);
		_windowMode = ParseWindowMode(windowModeValue);
		_windowResolution = ReadWindowResolution(config, GetDefaultWindowResolution());
		_masterVolume = ReadVolume(config, MasterVolumeKey);
		_sfxVolume = ReadVolume(config, SfxVolumeKey);
		_musicVolume = ReadVolume(config, MusicVolumeKey);
	}

	private void SaveSettings()
	{
		ConfigFile config = new();
		config.SetValue(GameSection, LanguageKey, GetLanguageValue(_currentLanguage));
		config.SetValue(DisplaySection, WindowModeKey, GetWindowModeValue(_windowMode));
		config.SetValue(DisplaySection, ResolutionWidthKey, _windowResolution.X);
		config.SetValue(DisplaySection, ResolutionHeightKey, _windowResolution.Y);
		config.SetValue(AudioSection, MasterVolumeKey, _masterVolume);
		config.SetValue(AudioSection, SfxVolumeKey, _sfxVolume);
		config.SetValue(AudioSection, MusicVolumeKey, _musicVolume);

		Error saveError = config.Save(SettingsPath);
		if (saveError != Error.Ok)
		{
			GD.PushWarning($"Unable to save settings file '{SettingsPath}': {saveError}");
		}
	}

	private void ApplyEngineLocale()
	{
		TranslationServer.SetLocale(_currentLanguage == GameLanguage.English ? "en" : "zh_CN");
	}

	private void ApplyDisplaySettings()
	{
		ApplyWindowContentScaling();
		Window rootWindow = GetTree()?.Root;
		if (rootWindow is null || IsHeadlessDisplay() || IsEmbeddedEditorWindow(rootWindow))
		{
			return;
		}

		if (_windowMode == GameWindowMode.Fullscreen)
		{
			rootWindow.Mode = Window.ModeEnum.Fullscreen;
			return;
		}

		rootWindow.Mode = Window.ModeEnum.Windowed;
		rootWindow.Borderless = false;
		rootWindow.Unresizable = true;
		rootWindow.Size = _windowResolution;
		CenterWindow(rootWindow, _windowResolution);
	}

	private void ApplyWindowContentScaling()
	{
		Window rootWindow = GetTree()?.Root;
		if (rootWindow is null)
		{
			return;
		}

		rootWindow.ContentScaleSize = GetProjectViewportSize();
		rootWindow.ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
		rootWindow.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
		rootWindow.ContentScaleStretch = Window.ContentScaleStretchEnum.Fractional;
	}

	private static Vector2I GetProjectViewportSize()
	{
		int width = ReadProjectSettingInt("display/window/size/viewport_width", 1280);
		int height = ReadProjectSettingInt("display/window/size/viewport_height", 720);
		return new Vector2I(Mathf.Max(1, width), Mathf.Max(1, height));
	}

	private static Vector2I GetDefaultWindowResolution()
	{
		int overrideWidth = ReadProjectSettingInt("display/window/size/window_width_override", 0);
		int overrideHeight = ReadProjectSettingInt("display/window/size/window_height_override", 0);
		if (overrideWidth > 0 && overrideHeight > 0)
		{
			return NormalizeWindowResolution(new Vector2I(overrideWidth, overrideHeight), GetProjectViewportSize());
		}

		return GetProjectViewportSize();
	}

	private static int ReadProjectSettingInt(string settingName, int fallback)
	{
		Variant value = ProjectSettings.GetSetting(settingName, fallback);
		return value.VariantType switch
		{
			Variant.Type.Int => value.AsInt32(),
			Variant.Type.Float => Mathf.RoundToInt(value.AsSingle()),
			Variant.Type.String => int.TryParse(value.AsString(), out int parsed) ? parsed : fallback,
			_ => fallback,
		};
	}

	private static void CenterWindow(Window rootWindow, Vector2I resolution)
	{
		Rect2I usableRect = DisplayServer.ScreenGetUsableRect(rootWindow.CurrentScreen);
		if (usableRect.Size.X <= 0 || usableRect.Size.Y <= 0)
		{
			return;
		}

		Vector2I offset = new(
			Mathf.Max(0, (usableRect.Size.X - resolution.X) / 2),
			Mathf.Max(0, (usableRect.Size.Y - resolution.Y) / 2));
		rootWindow.Position = usableRect.Position + offset;
	}

	private static bool IsHeadlessDisplay()
	{
		return string.Equals(DisplayServer.GetName(), "headless", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsEmbeddedEditorWindow(Window rootWindow)
	{
		return Engine.IsEmbeddedInEditor() || rootWindow.IsEmbedded();
	}

	private static Vector2I ReadWindowResolution(ConfigFile config, Vector2I fallback)
	{
		int width = ReadInt(config, DisplaySection, ResolutionWidthKey, fallback.X);
		int height = ReadInt(config, DisplaySection, ResolutionHeightKey, fallback.Y);
		return NormalizeWindowResolution(new Vector2I(width, height), fallback);
	}

	private static int ReadInt(ConfigFile config, string section, string key, int fallback)
	{
		Variant value = config.GetValue(section, key, fallback);
		return value.VariantType switch
		{
			Variant.Type.Int => value.AsInt32(),
			Variant.Type.Float => Mathf.RoundToInt(value.AsSingle()),
			Variant.Type.String => int.TryParse(value.AsString(), out int parsed) ? parsed : fallback,
			_ => fallback,
		};
	}

	private static Vector2I NormalizeWindowResolution(Vector2I resolution, Vector2I fallback)
	{
		if (resolution.X < MinWindowWidth || resolution.Y < MinWindowHeight)
		{
			return fallback;
		}

		return resolution;
	}

	private float ReadVolume(ConfigFile config, string key)
	{
		Variant value = config.GetValue(AudioSection, key, DefaultVolume);
		if (value.VariantType == Variant.Type.Float)
		{
			return ClampVolume(value.AsSingle());
		}

		if (value.VariantType == Variant.Type.Int)
		{
			return ClampVolume(value.AsInt32());
		}

		if (float.TryParse(value.AsString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed))
		{
			return ClampVolume(parsed);
		}

		return DefaultVolume;
	}

	private static string ReadString(ConfigFile config, string section, string key, string fallback)
	{
		Variant value = config.GetValue(section, key, fallback);
		string text = value.AsString();
		return string.IsNullOrWhiteSpace(text) ? fallback : text;
	}

	private static GameLanguage ParseLanguage(string value)
	{
		return value?.StripEdges().ToLowerInvariant() switch
		{
			EnglishLanguageValue or "english" or "en_us" => GameLanguage.English,
			ChineseLanguageValue or "chinese" or "zh_cn" => GameLanguage.Chinese,
			_ => GameLanguage.Chinese,
		};
	}

	private static GameWindowMode ParseWindowMode(string value)
	{
		return value?.StripEdges().ToLowerInvariant() switch
		{
			FullscreenModeValue or "full_screen" => GameWindowMode.Fullscreen,
			_ => GameWindowMode.Windowed,
		};
	}

	private static GameWindowMode NormalizeWindowMode(GameWindowMode mode)
	{
		return mode == GameWindowMode.Fullscreen ? GameWindowMode.Fullscreen : GameWindowMode.Windowed;
	}

	private static string GetLanguageValue(GameLanguage language)
	{
		return language == GameLanguage.English ? EnglishLanguageValue : ChineseLanguageValue;
	}

	private static string GetWindowModeValue(GameWindowMode mode)
	{
		return mode == GameWindowMode.Fullscreen ? FullscreenModeValue : WindowedModeValue;
	}

	private static float ClampVolume(float value)
	{
		return Mathf.Clamp(value, 0.0f, 1.0f);
	}

	private static void EnsureAudioBus(string busName, string sendName)
	{
		if (AudioServer.GetBusIndex(busName) >= 0)
		{
			return;
		}

		int busIndex = AudioServer.BusCount;
		AudioServer.AddBus(busIndex);
		AudioServer.SetBusName(busIndex, busName);
		AudioServer.SetBusSend(busIndex, sendName);
	}

	private static void ApplyBusVolume(string busName, float volume)
	{
		int busIndex = AudioServer.GetBusIndex(busName);
		if (busIndex < 0)
		{
			return;
		}

		float clampedVolume = ClampVolume(volume);
		bool isMuted = clampedVolume <= 0.0001f;
		AudioServer.SetBusMute(busIndex, isMuted);
		AudioServer.SetBusVolumeDb(busIndex, isMuted ? SilentVolumeDb : Mathf.LinearToDb(clampedVolume));
	}
}
