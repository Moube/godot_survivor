using Godot;
using System;

public enum GameLanguage
{
	Chinese = 0,
	English = 1,
}

public partial class GameSettings : Node
{
	public const string MasterBusName = "Master";
	public const string SfxBusName = "Sfx";
	public const string MusicBusName = "Music";

	private const string SettingsPath = "user://settings.cfg";
	private const string GameSection = "game";
	private const string AudioSection = "audio";
	private const string LanguageKey = "language";
	private const string MasterVolumeKey = "master_volume";
	private const string SfxVolumeKey = "sfx_volume";
	private const string MusicVolumeKey = "music_volume";
	private const string ChineseLanguageValue = "zh";
	private const string EnglishLanguageValue = "en";
	private const float DefaultVolume = 1.0f;
	private const float SilentVolumeDb = -80.0f;

	private GameLanguage _currentLanguage = GameLanguage.Chinese;
	private float _masterVolume = DefaultVolume;
	private float _sfxVolume = DefaultVolume;
	private float _musicVolume = DefaultVolume;

	public static GameSettings Instance { get; private set; }

	public event Action<GameLanguage> LanguageChanged;

	public event Action AudioVolumeChanged;

	public GameLanguage CurrentLanguage => _currentLanguage;

	public float MasterVolume => _masterVolume;

	public float SfxVolume => _sfxVolume;

	public float MusicVolume => _musicVolume;

	public override void _EnterTree()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Ready()
	{
		LoadSettings();
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
		_masterVolume = ReadVolume(config, MasterVolumeKey);
		_sfxVolume = ReadVolume(config, SfxVolumeKey);
		_musicVolume = ReadVolume(config, MusicVolumeKey);
	}

	private void SaveSettings()
	{
		ConfigFile config = new();
		config.SetValue(GameSection, LanguageKey, GetLanguageValue(_currentLanguage));
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

	private static string GetLanguageValue(GameLanguage language)
	{
		return language == GameLanguage.English ? EnglishLanguageValue : ChineseLanguageValue;
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
