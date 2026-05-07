using Godot;
using System;
using System.Collections.Generic;

public enum AudioCueId
{
	TitleStinger,
	UiClick,
	UiHover,
	GameplayMusic,
	WeaponFire,
	EnemyHit,
	GameOver,
	LevelUp,
	ExperiencePickup,
	SoulChainSpell,
	LightningSpell,
	HolyWaterBreak,
}

public partial class AudioManager : Node
{
	private const int DefaultSfxPoolSize = 12;
	private const int DefaultWorldSfxPoolSize = 24;

	private readonly Dictionary<AudioCueId, AudioCueDefinition> _cueDefinitions = new();
	private readonly Dictionary<AudioCueId, AudioStream> _streamCache = new();
	private readonly Dictionary<AudioCueId, double> _lastPlayedAtByCue = new();
	private readonly HashSet<AudioCueId> _missingCueWarnings = new();
	private readonly List<AudioStreamPlayer> _sfxPlayers = new();
	private readonly List<AudioStreamPlayer2D> _worldSfxPlayers = new();
	private AudioStreamPlayer _musicPlayer;
	private AudioCueId? _currentMusicCueId;
	private bool _isStoppingMusic;

	public static AudioManager Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Ready()
	{
		GameSettings.EnsureAudioBuses();
		GameSettings.Instance?.ApplyAllAudioVolumes();
		RegisterDefaultCues();
		_musicPlayer = CreateMusicPlayer();
		GetAvailableSfxPlayer();
		GetAvailableWorldSfxPlayer();
		WarmUpCue(AudioCueId.UiClick);
		WarmUpCue(AudioCueId.UiHover);
		WarmUpCue(AudioCueId.ExperiencePickup);
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void PlayTitleStinger()
	{
		PlaySfx(AudioCueId.TitleStinger);
	}

	public void PlayUiClick()
	{
		PlaySfx(AudioCueId.UiClick);
	}

	public void PlayUiHover()
	{
		PlaySfx(AudioCueId.UiHover);
	}

	public void PlayGameplayMusic()
	{
		PlayMusic(AudioCueId.GameplayMusic);
	}

	public void StopGameplayMusic()
	{
		if (_currentMusicCueId == AudioCueId.GameplayMusic)
		{
			StopMusic();
		}
	}

	public void PlayWeaponFire(Node2D source = null)
	{
		if (source != null && IsInstanceValid(source))
		{
			PlayWorldSfx(AudioCueId.WeaponFire, source.GlobalPosition);
			return;
		}

		PlaySfx(AudioCueId.WeaponFire);
	}

	public void PlayEnemyHit(Node2D source = null)
	{
		if (source != null && IsInstanceValid(source))
		{
			PlayWorldSfx(AudioCueId.EnemyHit, source.GlobalPosition);
			return;
		}

		PlaySfx(AudioCueId.EnemyHit);
	}

	public void PlayGameOver()
	{
		PlaySfx(AudioCueId.GameOver);
	}

	public void PlayLevelUp()
	{
		PlaySfx(AudioCueId.LevelUp);
	}

	public void PlayExperiencePickup(Node2D source = null)
	{
		if (source != null && IsInstanceValid(source))
		{
			PlayWorldSfx(AudioCueId.ExperiencePickup, source.GlobalPosition);
			return;
		}

		PlaySfx(AudioCueId.ExperiencePickup);
	}

	public void PlaySoulChainSpell(Node2D source = null)
	{
		if (source != null && IsInstanceValid(source))
		{
			PlayWorldSfx(AudioCueId.SoulChainSpell, source.GlobalPosition);
			return;
		}

		PlaySfx(AudioCueId.SoulChainSpell);
	}

	public void PlayLightningSpell(Node2D source = null)
	{
		if (source != null && IsInstanceValid(source))
		{
			PlayWorldSfx(AudioCueId.LightningSpell, source.GlobalPosition);
			return;
		}

		PlaySfx(AudioCueId.LightningSpell);
	}

	public void PlayHolyWaterBreak(Node2D source = null)
	{
		if (source != null && IsInstanceValid(source))
		{
			PlayWorldSfx(AudioCueId.HolyWaterBreak, source.GlobalPosition);
			return;
		}

		PlaySfx(AudioCueId.HolyWaterBreak);
	}

	public void StopMusic()
	{
		if (_musicPlayer == null)
		{
			return;
		}

		_isStoppingMusic = true;
		_musicPlayer.Stop();
		_isStoppingMusic = false;
		_currentMusicCueId = null;
	}

	private void RegisterDefaultCues()
	{
		_cueDefinitions.Clear();
		RegisterCue(AudioCueId.TitleStinger, new AudioCueDefinition(
			new[]
			{
				"res://asset/audio/ui/title_start.wav",
				"res://asset/audio/ui/title_start.ogg",
				"res://asset/audio/ui/title_start.mp3",
				"res://asset/audio/title_start.wav",
				"res://asset/audio/title_start.ogg",
				"res://asset/audio/title_start.mp3",
			},
			volumeDb: -2.0f,
			minIntervalSeconds: 0.25));

		RegisterCue(AudioCueId.UiClick, new AudioCueDefinition(
			new[]
			{
				"res://asset/audio/ui/ui_click.wav",
				"res://asset/audio/ui/ui_click.ogg",
				"res://asset/audio/ui/ui_click.mp3",
				"res://asset/audio/sfx/ui/ui_click.wav",
				"res://asset/audio/sfx/ui/ui_click.ogg",
				"res://asset/audio/sfx/ui/ui_click.mp3",
				"res://asset/audio/ui_click.wav",
				"res://asset/audio/ui_click.ogg",
				"res://asset/audio/ui_click.mp3",
			},
			volumeDb: 2.0f,
			minIntervalSeconds: 0.03,
			pitchMin: 0.98f,
			pitchMax: 1.02f));

		RegisterCue(AudioCueId.UiHover, new AudioCueDefinition(
			new[]
			{
				"res://asset/audio/ui/ui_hover.wav",
				"res://asset/audio/ui/ui_hover.ogg",
				"res://asset/audio/ui/ui_hover.mp3",
				"res://asset/audio/sfx/ui/ui_hover.wav",
				"res://asset/audio/sfx/ui/ui_hover.ogg",
				"res://asset/audio/sfx/ui/ui_hover.mp3",
				"res://asset/audio/ui_hover.wav",
				"res://asset/audio/ui_hover.ogg",
				"res://asset/audio/ui_hover.mp3",
			},
			volumeDb: 8.0f,
			minIntervalSeconds: 0.08,
			pitchMin: 0.99f,
			pitchMax: 1.03f));

		RegisterCue(AudioCueId.GameplayMusic, new AudioCueDefinition(
			new[]
			{
				"res://asset/audio/music/gameplay_music.ogg",
				"res://asset/audio/music/gameplay_music.wav",
				"res://asset/audio/music/gameplay_music.mp3",
				"res://asset/audio/ambience/gameplay_music.ogg",
				"res://asset/audio/ambience/gameplay_music.wav",
				"res://asset/audio/ambience/gameplay_music.mp3",
				"res://asset/audio/gameplay_music.ogg",
				"res://asset/audio/gameplay_music.wav",
				"res://asset/audio/gameplay_music.mp3",
			},
			volumeDb: -10.0f,
			isMusic: true,
			loop: true));

		RegisterCue(AudioCueId.WeaponFire, new AudioCueDefinition(
			new[]
			{
				"res://asset/audio/sfx/combat/weapon_fire.wav",
				"res://asset/audio/sfx/combat/weapon_fire.ogg",
				"res://asset/audio/sfx/combat/weapon_fire.mp3",
				"res://asset/audio/sfx/weapon_fire.wav",
				"res://asset/audio/sfx/weapon_fire.ogg",
				"res://asset/audio/sfx/weapon_fire.mp3",
				"res://asset/audio/weapon_fire.wav",
				"res://asset/audio/weapon_fire.ogg",
				"res://asset/audio/weapon_fire.mp3",
			},
			volumeDb: -2.0f,
			minIntervalSeconds: 0.04,
			pitchMin: 0.96f,
			pitchMax: 1.04f,
			maxDistance: 900.0f));

		RegisterCue(AudioCueId.EnemyHit, new AudioCueDefinition(
			new[]
			{
				"res://asset/audio/sfx/combat/enemy_hit.wav",
				"res://asset/audio/sfx/combat/enemy_hit.ogg",
				"res://asset/audio/sfx/combat/enemy_hit.mp3",
				"res://asset/audio/sfx/enemy/enemy_hit.wav",
				"res://asset/audio/sfx/enemy/enemy_hit.ogg",
				"res://asset/audio/sfx/enemy/enemy_hit.mp3",
				"res://asset/audio/enemy_hit.wav",
				"res://asset/audio/enemy_hit.ogg",
				"res://asset/audio/enemy_hit.mp3",
			},
			volumeDb: -9.0f,
			minIntervalSeconds: 0.055,
			pitchMin: 0.94f,
			pitchMax: 1.08f,
			maxDistance: 760.0f));

		RegisterCue(AudioCueId.GameOver, new AudioCueDefinition(
			new[]
			{
				"res://asset/audio/ui/game_over.wav",
				"res://asset/audio/ui/game_over.ogg",
				"res://asset/audio/ui/game_over.mp3",
				"res://asset/audio/sfx/ui/game_over.wav",
				"res://asset/audio/sfx/ui/game_over.ogg",
				"res://asset/audio/sfx/ui/game_over.mp3",
				"res://asset/audio/game_over.wav",
				"res://asset/audio/game_over.ogg",
				"res://asset/audio/game_over.mp3",
			},
			volumeDb: -3.0f,
			minIntervalSeconds: 0.25));

		RegisterCue(AudioCueId.LevelUp, new AudioCueDefinition(
			new[]
			{
				"res://asset/audio/ui/level_up.wav",
				"res://asset/audio/ui/level_up.ogg",
				"res://asset/audio/ui/level_up.mp3",
				"res://asset/audio/sfx/ui/level_up.wav",
				"res://asset/audio/sfx/ui/level_up.ogg",
				"res://asset/audio/sfx/ui/level_up.mp3",
				"res://asset/audio/level_up.wav",
				"res://asset/audio/level_up.ogg",
				"res://asset/audio/level_up.mp3",
			},
			volumeDb: -6.0f,
			minIntervalSeconds: 0.2,
			pitchMin: 0.98f,
			pitchMax: 1.02f));

		RegisterCue(AudioCueId.ExperiencePickup, new AudioCueDefinition(
			new[]
			{
				"res://asset/audio/pickup.wav",
				"res://asset/audio/pickup.ogg",
				"res://asset/audio/pickup.mp3",
				"res://asset/audio/sfx/pickup.wav",
				"res://asset/audio/sfx/pickup.ogg",
				"res://asset/audio/sfx/pickup.mp3",
				"res://asset/audio/sfx/ui/pickup.wav",
				"res://asset/audio/sfx/ui/pickup.ogg",
				"res://asset/audio/sfx/ui/pickup.mp3",
			},
			volumeDb: -1.0f,
			minIntervalSeconds: 0.035,
			pitchMin: 0.98f,
			pitchMax: 1.04f,
			maxDistance: 520.0f));

		RegisterCue(AudioCueId.SoulChainSpell, new AudioCueDefinition(
			new[]
			{
				"res://asset/audio/laser_spell.wav",
				"res://asset/audio/laser_spell.ogg",
				"res://asset/audio/laser_spell.mp3",
				"res://asset/audio/sfx/combat/laser_spell.wav",
				"res://asset/audio/sfx/combat/laser_spell.ogg",
				"res://asset/audio/sfx/combat/laser_spell.mp3",
			},
			volumeDb: -12.0f,
			minIntervalSeconds: 0.08,
			pitchMin: 0.97f,
			pitchMax: 1.03f,
			maxDistance: 900.0f));

		RegisterCue(AudioCueId.LightningSpell, new AudioCueDefinition(
			new[]
			{
				"res://asset/audio/lightning_spell.wav",
				"res://asset/audio/lightning_spell.ogg",
				"res://asset/audio/lightning_spell.mp3",
				"res://asset/audio/sfx/combat/lightning_spell.wav",
				"res://asset/audio/sfx/combat/lightning_spell.ogg",
				"res://asset/audio/sfx/combat/lightning_spell.mp3",
			},
			volumeDb: -6.0f,
			minIntervalSeconds: 0.08,
			pitchMin: 0.96f,
			pitchMax: 1.04f,
			maxDistance: 980.0f));

		RegisterCue(AudioCueId.HolyWaterBreak, new AudioCueDefinition(
			new[]
			{
				"res://asset/audio/break_glass.wav",
				"res://asset/audio/break_glass.ogg",
				"res://asset/audio/break_glass.mp3",
				"res://asset/audio/sfx/combat/break_glass.wav",
				"res://asset/audio/sfx/combat/break_glass.ogg",
				"res://asset/audio/sfx/combat/break_glass.mp3",
			},
			volumeDb: -3.0f,
			minIntervalSeconds: 0.06,
			pitchMin: 0.96f,
			pitchMax: 1.04f,
			maxDistance: 760.0f));
	}

	private void RegisterCue(AudioCueId cueId, AudioCueDefinition definition)
	{
		_cueDefinitions[cueId] = definition;
	}

	private void WarmUpCue(AudioCueId cueId)
	{
		TryResolveCue(cueId, out _, out _);
	}

	private void PlaySfx(AudioCueId cueId)
	{
		if (!TryResolveCue(cueId, out AudioCueDefinition definition, out AudioStream stream))
		{
			return;
		}

		if (!CanPlayCue(cueId, definition))
		{
			return;
		}

		AudioStreamPlayer player = GetAvailableSfxPlayer();
		ConfigurePlayer(player, stream, definition);
		player.Play();
		RememberCuePlayed(cueId);
	}

	private void PlayWorldSfx(AudioCueId cueId, Vector2 globalPosition)
	{
		if (!TryResolveCue(cueId, out AudioCueDefinition definition, out AudioStream stream))
		{
			return;
		}

		if (!CanPlayCue(cueId, definition))
		{
			return;
		}

		AudioStreamPlayer2D player = GetAvailableWorldSfxPlayer();
		ConfigureWorldPlayer(player, stream, definition, globalPosition);
		player.Play();
		RememberCuePlayed(cueId);
	}

	private void PlayMusic(AudioCueId cueId)
	{
		if (!TryResolveCue(cueId, out AudioCueDefinition definition, out AudioStream stream))
		{
			return;
		}

		_musicPlayer ??= CreateMusicPlayer();
		if (_currentMusicCueId == cueId && _musicPlayer.Playing)
		{
			return;
		}

		_musicPlayer.Stream = stream;
		_musicPlayer.VolumeDb = definition.VolumeDb;
		_musicPlayer.PitchScale = 1.0f;
		_musicPlayer.Bus = GameSettings.MusicBusName;
		_currentMusicCueId = cueId;
		_musicPlayer.Play();
		RememberCuePlayed(cueId);
	}

	private bool TryResolveCue(AudioCueId cueId, out AudioCueDefinition definition, out AudioStream stream)
	{
		stream = null;
		if (!_cueDefinitions.TryGetValue(cueId, out definition))
		{
			if (_missingCueWarnings.Add(cueId))
			{
				GD.PushWarning($"Audio cue '{cueId}' is not registered.");
			}

			return false;
		}

		if (_streamCache.TryGetValue(cueId, out stream))
		{
			return stream != null;
		}

		stream = LoadFirstAvailableStream(cueId, definition.Paths);
		_streamCache[cueId] = stream;
		return stream != null;
	}

	private AudioStream LoadFirstAvailableStream(AudioCueId cueId, IReadOnlyList<string> paths)
	{
		foreach (string path in paths)
		{
			if (string.IsNullOrWhiteSpace(path) || !FileAccess.FileExists(path))
			{
				continue;
			}

			AudioStream stream = ResourceLoader.Load<AudioStream>(path);
			if (stream != null)
			{
				return stream;
			}

			GD.PushWarning($"Audio cue '{cueId}' found a file but could not load it as audio: {path}");
		}

		if (_missingCueWarnings.Add(cueId))
		{
			GD.PushWarning($"Audio cue '{cueId}' has no available audio file. Checked: {string.Join(", ", paths)}");
		}

		return null;
	}

	private bool CanPlayCue(AudioCueId cueId, AudioCueDefinition definition)
	{
		if (definition.MinIntervalSeconds <= 0.0)
		{
			return true;
		}

		double now = Time.GetTicksMsec() / 1000.0;
		return !_lastPlayedAtByCue.TryGetValue(cueId, out double lastPlayedAt)
			|| now - lastPlayedAt >= definition.MinIntervalSeconds;
	}

	private void RememberCuePlayed(AudioCueId cueId)
	{
		_lastPlayedAtByCue[cueId] = Time.GetTicksMsec() / 1000.0;
	}

	private AudioStreamPlayer CreateMusicPlayer()
	{
		AudioStreamPlayer player = new()
		{
			Name = "MusicPlayer",
			ProcessMode = ProcessModeEnum.Always,
			Bus = GameSettings.MusicBusName,
		};
		player.Finished += OnMusicFinished;
		AddChild(player);
		return player;
	}

	private AudioStreamPlayer GetAvailableSfxPlayer()
	{
		foreach (AudioStreamPlayer player in _sfxPlayers)
		{
			if (!player.Playing)
			{
				return player;
			}
		}

		if (_sfxPlayers.Count < DefaultSfxPoolSize)
		{
			AudioStreamPlayer player = new()
			{
				Name = $"SfxPlayer{_sfxPlayers.Count + 1}",
				ProcessMode = ProcessModeEnum.Always,
				Bus = GameSettings.SfxBusName,
			};
			AddChild(player);
			_sfxPlayers.Add(player);
			return player;
		}

		AudioStreamPlayer fallback = _sfxPlayers[0];
		fallback.Stop();
		return fallback;
	}

	private AudioStreamPlayer2D GetAvailableWorldSfxPlayer()
	{
		foreach (AudioStreamPlayer2D player in _worldSfxPlayers)
		{
			if (!player.Playing)
			{
				return player;
			}
		}

		if (_worldSfxPlayers.Count < DefaultWorldSfxPoolSize)
		{
			AudioStreamPlayer2D player = new()
			{
				Name = $"WorldSfxPlayer{_worldSfxPlayers.Count + 1}",
				ProcessMode = ProcessModeEnum.Always,
				Bus = GameSettings.SfxBusName,
			};
			AddChild(player);
			_worldSfxPlayers.Add(player);
			return player;
		}

		AudioStreamPlayer2D fallback = _worldSfxPlayers[0];
		fallback.Stop();
		return fallback;
	}

	private static void ConfigurePlayer(AudioStreamPlayer player, AudioStream stream, AudioCueDefinition definition)
	{
		player.Stream = stream;
		player.VolumeDb = definition.VolumeDb;
		player.PitchScale = GetRandomPitchScale(definition);
		player.Bus = GameSettings.SfxBusName;
	}

	private static void ConfigureWorldPlayer(AudioStreamPlayer2D player, AudioStream stream, AudioCueDefinition definition, Vector2 globalPosition)
	{
		player.Stream = stream;
		player.GlobalPosition = globalPosition;
		player.VolumeDb = definition.VolumeDb;
		player.PitchScale = GetRandomPitchScale(definition);
		player.MaxDistance = definition.MaxDistance;
		player.Attenuation = definition.Attenuation;
		player.Bus = GameSettings.SfxBusName;
	}

	private static float GetRandomPitchScale(AudioCueDefinition definition)
	{
		if (Mathf.IsEqualApprox(definition.PitchMin, definition.PitchMax))
		{
			return Mathf.Max(0.01f, definition.PitchMin);
		}

		float min = Mathf.Min(definition.PitchMin, definition.PitchMax);
		float max = Mathf.Max(definition.PitchMin, definition.PitchMax);
		return Mathf.Clamp((float)GD.RandRange(min, max), 0.01f, 4.0f);
	}

	private void OnMusicFinished()
	{
		if (_isStoppingMusic || _currentMusicCueId == null)
		{
			return;
		}

		if (!_cueDefinitions.TryGetValue(_currentMusicCueId.Value, out AudioCueDefinition definition) || !definition.Loop)
		{
			_currentMusicCueId = null;
			return;
		}

		_musicPlayer?.Play();
	}

	private sealed class AudioCueDefinition
	{
		public AudioCueDefinition(
			IReadOnlyList<string> paths,
			float volumeDb = 0.0f,
			double minIntervalSeconds = 0.0,
			float pitchMin = 1.0f,
			float pitchMax = 1.0f,
			bool isMusic = false,
			bool loop = false,
			float maxDistance = 700.0f,
			float attenuation = 1.0f)
		{
			Paths = paths ?? Array.Empty<string>();
			VolumeDb = volumeDb;
			MinIntervalSeconds = Mathf.Max(0.0, minIntervalSeconds);
			PitchMin = Mathf.Max(0.01f, pitchMin);
			PitchMax = Mathf.Max(0.01f, pitchMax);
			IsMusic = isMusic;
			Loop = loop;
			MaxDistance = Mathf.Max(1.0f, maxDistance);
			Attenuation = Mathf.Max(0.0f, attenuation);
		}

		public IReadOnlyList<string> Paths { get; }

		public float VolumeDb { get; }

		public double MinIntervalSeconds { get; }

		public float PitchMin { get; }

		public float PitchMax { get; }

		public bool IsMusic { get; }

		public bool Loop { get; }

		public float MaxDistance { get; }

		public float Attenuation { get; }
	}
}
