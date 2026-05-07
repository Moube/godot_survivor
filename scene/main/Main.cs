using Godot;
using System.Collections.Generic;

public partial class Main : Control
{
	private const string FallbackLevelScenePath = "res://scene/level/FormalSurvivorLevel.tscn";
	private const string GrassTexturePath = "res://asset/art/level/floor_tile.png";
	private const string SlimeTexturePath = "res://asset/art/enemy/enemy_slime_move_strip_4f.png";
	private const string DropShadowScenePath = "res://scene/common/DropShadow2D.tscn";
	private const string ObstaclePillarScenePath = "res://scene/level/ObstaclePillar.tscn";
	private const string AmbientSlimeEnemyConfigId = "slime_small";
	private const string MainMenuPanelTexturePath = "res://asset/art/ui/ui_main_menu_panel.png";
	private const string ButtonNormalTexturePath = "res://asset/art/ui/ui_main_menu_button_normal.png";
	private const string ButtonHoverTexturePath = "res://asset/art/ui/ui_main_menu_button_hover.png";
	private const string ButtonPressedTexturePath = "res://asset/art/ui/ui_main_menu_button_pressed.png";
	private const string ButtonDisabledTexturePath = "res://asset/art/ui/ui_main_menu_button_disabled.png";
	private const string LevelSelectPanelTexturePath = "res://asset/art/ui/ui_level_select_panel.png";
	private const string LevelSelectButtonNormalTexturePath = "res://asset/art/ui/ui_level_select_button_normal.png";
	private const string LevelSelectButtonHoverTexturePath = "res://asset/art/ui/ui_level_select_button_hover.png";
	private const string LevelSelectButtonPressedTexturePath = "res://asset/art/ui/ui_level_select_button_pressed.png";
	private const string LevelSelectButtonDisabledTexturePath = "res://asset/art/ui/ui_level_select_button_disabled.png";
	private const float MainMenuPanelTextureMarginX = 24.0f;
	private const float MainMenuPanelTextureMarginY = 24.0f;
	private const float MenuButtonTextureMarginX = 36.0f;
	private const float MenuButtonTextureMarginY = 12.0f;
	private const float LevelSelectPanelTextureMarginX = 52.0f;
	private const float LevelSelectPanelTextureMarginY = 52.0f;
	private const float LevelSelectButtonTextureMarginX = 34.0f;
	private const float LevelSelectButtonTextureMarginY = 14.0f;
	private const int AmbientSlimeCount = 7;
	private const int SlimeMoveAnimationFrameCount = 4;
	private const float SlimeMoveAnimationFps = 2.0f;
	private const float SlimeViewportMargin = 72.0f;
	private const float SlimeSceneSpriteBaseScale = 0.38f;
	private const float SlimeMinMoveDistance = 260.0f;
	private const float SlimeMaxMoveDistance = 760.0f;
	private const float SlimeMinMoveDuration = 14.0f;
	private const float SlimeMaxMoveDuration = 28.0f;
	private const float SlimeMinIdleDuration = 0.35f;
	private const float SlimeMaxIdleDuration = 1.4f;
	private const float SlimeSeparationPadding = 8.0f;
	private const int SlimeSeparationIterations = 2;
	private const float AmbientObstacleCollisionRadius = 42.0f;
	private static readonly Vector2 LevelSelectButtonSize = new(220.0f, 52.0f);
	private static readonly Vector2[] AmbientObstacleNormalizedPositions =
	{
		new(0.964f, 0.078f),
		new(0.263f, 0.361f),
		new(0.098f, 0.542f),
		new(0.515f, 0.771f),
		new(0.821f, 0.917f),
	};

	private readonly RandomNumberGenerator _rng = new();
	private readonly List<AmbientSlime> _ambientSlimes = new();
	private readonly List<AmbientObstacle> _ambientObstacles = new();
	private Control _mainMenuPanel;
	private PanelContainer _mainMenuPanelContainer;
	private Control _levelSelectPanel;
	private PanelContainer _levelSelectPanelContainer;
	private GridContainer _levelButtonGrid;
	private Label _mainMenuTitleLabel;
	private Label _levelSelectTitleLabel;
	private Button _startGameButton;
	private Button _settingsButton;
	private Button _quitButton;
	private Button _levelSelectBackButton;
	private SettingsMenu _settingsMenu;
	private StyleBoxTexture _buttonNormalStyle;
	private StyleBoxTexture _buttonHoverStyle;
	private StyleBoxTexture _buttonPressedStyle;
	private StyleBoxTexture _buttonDisabledStyle;
	private StyleBoxTexture _levelButtonNormalStyle;
	private StyleBoxTexture _levelButtonHoverStyle;
	private StyleBoxTexture _levelButtonPressedStyle;
	private StyleBoxTexture _levelButtonDisabledStyle;
	private Node2D _ambientLayer;
	private Polygon2D _grassFloor;
	private Texture2D _slimeTexture;
	private PackedScene _dropShadowScene;
	private PackedScene _obstaclePillarScene;
	private Vector2 _lastAmbientViewportSize;
	private float _ambientSlimeBaseCollisionRadius = 12.0f;

	public override void _Ready()
	{
		GetTree().Paused = false;
		_rng.Randomize();
		CreateAmbientBackground();

		_mainMenuPanel = GetNode<Control>("MainMenuPanel");
		_mainMenuPanelContainer = GetNode<PanelContainer>("MainMenuPanel/PanelContainer");
		_levelSelectPanel = GetNode<Control>("LevelSelectPanel");
		_levelSelectPanelContainer = GetNode<PanelContainer>("LevelSelectPanel/PanelContainer");
		_levelButtonGrid = GetNode<GridContainer>("LevelSelectPanel/PanelContainer/MarginContainer/Content/LevelScrollContainer/LevelButtonGrid");
		_mainMenuTitleLabel = GetNode<Label>("MainMenuPanel/TitleLabel");
		_levelSelectTitleLabel = GetNode<Label>("LevelSelectPanel/PanelContainer/MarginContainer/Content/TitleLabel");
		_startGameButton = GetNode<Button>("MainMenuPanel/PanelContainer/PanelLayout/ButtonList/StartGameButton");
		_settingsButton = GetNode<Button>("MainMenuPanel/PanelContainer/PanelLayout/ButtonList/SettingsButton");
		_quitButton = GetNode<Button>("MainMenuPanel/PanelContainer/PanelLayout/ButtonList/QuitButton");
		_levelSelectBackButton = GetNode<Button>("LevelSelectPanel/PanelContainer/MarginContainer/Content/BackButtonCenter/BackButton");
		_settingsMenu = new SettingsMenu
		{
			Name = "SettingsMenu",
		};
		AddChild(_settingsMenu);

		ConnectUiClickSound(_startGameButton);
		ConnectUiClickSound(_settingsButton);
		ConnectUiClickSound(_quitButton);
		ConnectUiClickSound(_levelSelectBackButton);
		_startGameButton.Pressed += OnStartGamePressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_quitButton.Pressed += OnQuitPressed;
		_levelSelectBackButton.Pressed += OnLevelSelectBackPressed;

		ApplyMainMenuPanelStyle();
		LoadMenuButtonStyles();
		ApplyMenuButtonStyle(_startGameButton);
		ApplyMenuButtonStyle(_settingsButton);
		ApplyMenuButtonStyle(_quitButton);
		ApplyLevelSelectPanelStyle();
		LoadLevelSelectButtonStyles();
		ApplyLevelSelectButtonStyle(_levelSelectBackButton);

		if (GameSettings.Instance != null)
		{
			GameSettings.Instance.LanguageChanged += OnLanguageChanged;
		}

		ApplyLocalizedText();
		PopulateLevelButtons();
		ShowMainMenu();
	}

	public override void _ExitTree()
	{
		if (GameSettings.Instance != null)
		{
			GameSettings.Instance.LanguageChanged -= OnLanguageChanged;
		}
	}

	public override void _Process(double delta)
	{
		UpdateAmbientViewportLayout(false);
		UpdateAmbientSlimes(delta);
	}

	private void PopulateLevelButtons()
	{
		foreach (Node child in _levelButtonGrid.GetChildren())
		{
			_levelButtonGrid.RemoveChild(child);
			child.QueueFree();
		}

		if (GameConfigManager.Instance is null)
		{
			AddFallbackLevelButton();
			return;
		}

		bool hasLevel = false;
		foreach (LevelConfig level in GameConfigManager.Instance.GetAllLevelConfigs())
		{
			if (level is null)
			{
				continue;
			}

			hasLevel = true;
			AddLevelButton(level);
		}

		if (!hasLevel)
		{
			AddFallbackLevelButton();
		}
	}

	private void AddLevelButton(LevelConfig level)
	{
		string scenePath = string.IsNullOrWhiteSpace(level.ScenePath) ? FallbackLevelScenePath : level.ScenePath;
		string levelConfigId = level.Id;
		Button button = new()
		{
			Text = GameText.ConfigName("level", level.Id, string.IsNullOrWhiteSpace(level.DisplayName) ? level.Id : level.DisplayName),
			CustomMinimumSize = LevelSelectButtonSize,
		};
		ApplyLevelSelectButtonStyle(button);
		ConnectUiClickSound(button);
		button.Pressed += () => OnLevelButtonPressed(scenePath, levelConfigId);
		_levelButtonGrid.AddChild(button);
	}

	private void AddFallbackLevelButton()
	{
		Button button = new()
		{
			Text = GameText.ConfigName("level", "formal_survivor_01", "正式关卡"),
			CustomMinimumSize = LevelSelectButtonSize,
		};
		ApplyLevelSelectButtonStyle(button);
		ConnectUiClickSound(button);
		button.Pressed += () => OnLevelButtonPressed(FallbackLevelScenePath, "formal_survivor_01");
		_levelButtonGrid.AddChild(button);
	}

	private void OnStartGamePressed()
	{
		PopulateLevelButtons();
		ShowPanel(_levelSelectPanel);
	}

	private void OnSettingsPressed()
	{
		_settingsMenu?.Open(() => _settingsButton?.GrabFocus());
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}

	private void OnLevelSelectBackPressed()
	{
		ShowMainMenu();
	}

	private void OnLevelButtonPressed(string scenePath, string levelConfigId)
	{
		StartLevel(scenePath, levelConfigId);
	}

	private static void ConnectUiClickSound(Button button)
	{
		if (button != null)
		{
			button.ButtonDown += PlayUiClickSound;
			button.MouseEntered += PlayUiHoverSound;
		}
	}

	private static void PlayUiClickSound()
	{
		AudioManager.Instance?.PlayUiClick();
	}

	private static void PlayUiHoverSound()
	{
		AudioManager.Instance?.PlayUiHover();
	}

	private void StartLevel(string scenePath, string levelConfigId)
	{
		GetTree().Paused = false;
		GameSession.Instance?.SelectLevelConfig(levelConfigId);
		GetTree().ChangeSceneToFile(string.IsNullOrWhiteSpace(scenePath) ? FallbackLevelScenePath : scenePath);
	}

	private void ShowMainMenu()
	{
		ShowPanel(_mainMenuPanel);
	}

	private void ShowPanel(Control activePanel)
	{
		_mainMenuPanel.Visible = activePanel == _mainMenuPanel;
		_levelSelectPanel.Visible = activePanel == _levelSelectPanel;
	}

	private void ApplyLocalizedText()
	{
		if (_mainMenuTitleLabel != null)
		{
			_mainMenuTitleLabel.Text = GameText.Tr("ui.main.title");
		}

		if (_levelSelectTitleLabel != null)
		{
			_levelSelectTitleLabel.Text = GameText.Tr("ui.level_select.title");
		}

		if (_startGameButton != null)
		{
			_startGameButton.Text = GameText.Tr("ui.main.start");
		}

		if (_settingsButton != null)
		{
			_settingsButton.Text = GameText.Tr("ui.main.settings");
		}

		if (_quitButton != null)
		{
			_quitButton.Text = GameText.Tr("ui.main.quit");
		}

		if (_levelSelectBackButton != null)
		{
			_levelSelectBackButton.Text = GameText.Tr("ui.common.back");
		}
	}

	private void OnLanguageChanged(GameLanguage language)
	{
		ApplyLocalizedText();
		PopulateLevelButtons();
	}

	private void CreateAmbientBackground()
	{
		Texture2D grassTexture = ResourceLoader.Load<Texture2D>(GrassTexturePath);
		if (grassTexture is null)
		{
			GD.PushWarning($"Unable to load menu grass texture: {GrassTexturePath}");
			return;
		}

		ColorRect shade = GetNodeOrNull<ColorRect>("ColorRect");
		if (shade != null)
		{
			shade.Color = new Color(0.025f, 0.035f, 0.018f, 0.18f);
			shade.MouseFilter = MouseFilterEnum.Ignore;
		}

		_ambientLayer = new Node2D
		{
			Name = "AmbientBackground",
			YSortEnabled = true,
			ZIndex = -100,
		};
		AddChild(_ambientLayer);
		MoveChild(_ambientLayer, 0);

		_grassFloor = new Polygon2D
		{
			Name = "GrassFloor",
			Texture = grassTexture,
			TextureRepeat = TextureRepeatEnum.Enabled,
			ZIndex = -100,
		};
		_ambientLayer.AddChild(_grassFloor);

		_slimeTexture = ResourceLoader.Load<Texture2D>(SlimeTexturePath);
		if (_slimeTexture is null)
		{
			GD.PushWarning($"Unable to load menu slime texture: {SlimeTexturePath}");
			return;
		}

		_dropShadowScene = ResourceLoader.Load<PackedScene>(DropShadowScenePath);
		if (_dropShadowScene is null)
		{
			GD.PushWarning($"Unable to load menu slime shadow scene: {DropShadowScenePath}");
		}

		_obstaclePillarScene = ResourceLoader.Load<PackedScene>(ObstaclePillarScenePath);
		if (_obstaclePillarScene is null)
		{
			GD.PushWarning($"Unable to load menu obstacle pillar scene: {ObstaclePillarScenePath}");
		}
		else
		{
			CreateAmbientObstacles();
		}

		_ambientSlimeBaseCollisionRadius = GetConfiguredAmbientSlimeCollisionRadius();
		for (int i = 0; i < AmbientSlimeCount; i++)
		{
			AmbientSlime slime = CreateAmbientSlime(i);
			_ambientSlimes.Add(slime);
			_ambientLayer.AddChild(slime.Root);
		}

		UpdateAmbientViewportLayout(true);
	}

	private void CreateAmbientObstacles()
	{
		for (int i = 0; i < AmbientObstacleNormalizedPositions.Length; i++)
		{
			Node instance = _obstaclePillarScene.Instantiate();
			if (instance is not Node2D obstacle)
			{
				GD.PushWarning($"Menu obstacle pillar scene must instantiate a Node2D: {ObstaclePillarScenePath}");
				instance.QueueFree();
				continue;
			}

			obstacle.Name = $"AmbientObstaclePillar{i + 1}";
			obstacle.ZIndex = -20;
			DisableAmbientObstaclePhysics(obstacle);
			_ambientObstacles.Add(new AmbientObstacle
			{
				Root = obstacle,
				NormalizedPosition = AmbientObstacleNormalizedPositions[i],
				CollisionRadius = AmbientObstacleCollisionRadius,
			});
			_ambientLayer.AddChild(obstacle);
		}
	}

	private static void DisableAmbientObstaclePhysics(Node node)
	{
		if (node is CollisionObject2D collisionObject)
		{
			collisionObject.CollisionLayer = 0;
			collisionObject.CollisionMask = 0;
		}

		if (node is CollisionShape2D collisionShape)
		{
			collisionShape.Disabled = true;
		}

		foreach (Node child in node.GetChildren())
		{
			DisableAmbientObstaclePhysics(child);
		}
	}

	private AmbientSlime CreateAmbientSlime(int index)
	{
		Node2D root = new()
		{
			Name = $"AmbientSlime{index + 1}",
			ZIndex = -20,
		};
		float spriteScale = _rng.RandfRange(0.42f, 0.62f);
		float visualScaleMultiplier = spriteScale / Mathf.Max(0.001f, SlimeSceneSpriteBaseScale);
		root.Scale = Vector2.One * visualScaleMultiplier;

		Sprite2D sprite = new()
		{
			Name = "Sprite2D",
			Texture = _slimeTexture,
			Hframes = SlimeMoveAnimationFrameCount,
			Position = new Vector2(0.0f, -3.0f),
			Scale = Vector2.One * SlimeSceneSpriteBaseScale,
		};
		root.AddChild(sprite);
		float collisionRadius = GetAmbientSlimeCollisionRadius(visualScaleMultiplier);

		if (_dropShadowScene?.Instantiate() is DropShadow2D shadow)
		{
			shadow.SourceSpritePath = new NodePath("../Sprite2D");
			shadow.GroundOffset = new Vector2(7.0f, 19.0f);
			shadow.ScaleMultiplier = Vector2.One;
			shadow.FollowSourceScale = false;
			shadow.ShadowCanvasSize = new Vector2I(50, 20);
			shadow.SkewAmount = 0.0f;
			shadow.ShadowColor = new Color(0.0705882f, 0.0862745f, 0.0431373f, 0.34f);
			shadow.ContactCenter = new Vector2(0.36f, 0.58f);
			shadow.ContactRadius = new Vector2(0.34f, 0.21f);
			shadow.ContactStrength = 0.9f;
			shadow.CastCenter = new Vector2(0.58f, 0.58f);
			shadow.CastRadius = new Vector2(0.5f, 0.28f);
			shadow.CastAngleDegrees = 14.0f;
			shadow.CastStrength = 0.58f;
			shadow.ProceduralSoftness = 0.62f;
			root.AddChild(shadow);
		}

		return new AmbientSlime
		{
			Root = root,
			Sprite = sprite,
			CollisionRadius = collisionRadius,
			AnimationPhase = _rng.RandfRange(0.0f, SlimeMoveAnimationFrameCount),
		};
	}

	private float GetConfiguredAmbientSlimeCollisionRadius()
	{
		EnemyConfig slimeConfig = GameConfigManager.Instance?.GetEnemyConfig(AmbientSlimeEnemyConfigId);
		return Mathf.Max(1.0f, slimeConfig?.CollisionRadius ?? 12.0f);
	}

	private float GetAmbientSlimeCollisionRadius(float visualScaleMultiplier)
	{
		return Mathf.Max(4.0f, _ambientSlimeBaseCollisionRadius * visualScaleMultiplier);
	}

	private void UpdateAmbientViewportLayout(bool resetSlimes)
	{
		if (_grassFloor is null)
		{
			return;
		}

		Vector2 viewportSize = GetViewportRect().Size;
		if (viewportSize.X <= 1.0f || viewportSize.Y <= 1.0f)
		{
			return;
		}

		if (!resetSlimes && viewportSize.IsEqualApprox(_lastAmbientViewportSize))
		{
			return;
		}

		_lastAmbientViewportSize = viewportSize;
		Vector2[] corners =
		{
			Vector2.Zero,
			new Vector2(viewportSize.X, 0.0f),
			viewportSize,
			new Vector2(0.0f, viewportSize.Y),
		};
		_grassFloor.Polygon = corners;
		_grassFloor.UV = corners;
		UpdateAmbientObstacleLayout(viewportSize);

		foreach (AmbientSlime slime in _ambientSlimes)
		{
			if (resetSlimes)
			{
				slime.Root.Position = GetRandomAmbientSlimePosition(slime, viewportSize);
				slime.HasPosition = true;
				StartAmbientSlimeMove(slime, viewportSize, true);
				continue;
			}

			slime.Root.Position = ClampToAmbientBounds(slime.Root.Position, viewportSize);
			slime.MoveStart = ClampToAmbientBounds(slime.MoveStart, viewportSize);
			slime.MoveTarget = ClampToAmbientBounds(slime.MoveTarget, viewportSize);
		}
	}

	private void UpdateAmbientObstacleLayout(Vector2 viewportSize)
	{
		foreach (AmbientObstacle obstacle in _ambientObstacles)
		{
			obstacle.Root.Position = new Vector2(
				viewportSize.X * obstacle.NormalizedPosition.X,
				viewportSize.Y * obstacle.NormalizedPosition.Y);
		}
	}

	private void UpdateAmbientSlimes(double delta)
	{
		if (_ambientSlimes.Count == 0)
		{
			return;
		}

		Vector2 viewportSize = _lastAmbientViewportSize;
		if (viewportSize.X <= 1.0f || viewportSize.Y <= 1.0f)
		{
			return;
		}

		float deltaSeconds = (float)delta;
		foreach (AmbientSlime slime in _ambientSlimes)
		{
			bool isMoving = slime.IdleRemaining <= 0.0f;
			if (slime.IdleRemaining > 0.0f)
			{
				slime.IdleRemaining -= deltaSeconds;
				if (slime.IdleRemaining <= 0.0f)
				{
					StartAmbientSlimeMove(slime, viewportSize, false);
					isMoving = true;
				}
			}

			if (isMoving)
			{
				slime.MoveElapsed += deltaSeconds;
				float progress = Mathf.Clamp(slime.MoveElapsed / Mathf.Max(0.01f, slime.MoveDuration), 0.0f, 1.0f);
				float easedProgress = progress * progress * (3.0f - 2.0f * progress);
				slime.Root.Position = slime.MoveStart.Lerp(slime.MoveTarget, easedProgress);

				if (progress >= 1.0f)
				{
					slime.Root.Position = slime.MoveTarget;
					slime.IdleRemaining = _rng.RandfRange(SlimeMinIdleDuration, SlimeMaxIdleDuration);
					isMoving = false;
				}
			}

			UpdateAmbientSlimeAnimation(slime, deltaSeconds, isMoving);
		}

		ResolveAmbientSlimeObstacleOverlaps(viewportSize);
		ResolveAmbientSlimeOverlaps(viewportSize);
	}

	private void StartAmbientSlimeMove(AmbientSlime slime, Vector2 viewportSize, bool randomizeProgress)
	{
		Vector2 start = ClampToAmbientBounds(slime.Root.Position, viewportSize);
		Vector2 target = PickAmbientMoveTarget(slime, start, viewportSize);
		float distance = start.DistanceTo(target);
		float durationByDistance = distance / _rng.RandfRange(16.0f, 24.0f);

		slime.MoveStart = start;
		slime.MoveTarget = target;
		slime.MoveDuration = Mathf.Clamp(durationByDistance, SlimeMinMoveDuration, SlimeMaxMoveDuration);
		slime.MoveElapsed = randomizeProgress ? _rng.RandfRange(0.0f, slime.MoveDuration * 0.45f) : 0.0f;
		slime.IdleRemaining = 0.0f;

		if (!Mathf.IsZeroApprox(target.X - start.X))
		{
			slime.Sprite.FlipH = target.X < start.X;
		}
	}

	private Vector2 PickAmbientMoveTarget(AmbientSlime slime, Vector2 start, Vector2 viewportSize)
	{
		float minDistance = GetEffectiveMinMoveDistance(viewportSize);
		float maxDistance = Mathf.Max(minDistance + 1.0f, Mathf.Min(SlimeMaxMoveDistance, viewportSize.Length() * 0.72f));

		for (int attempt = 0; attempt < 16; attempt++)
		{
			float angle = _rng.RandfRange(0.0f, Mathf.Tau);
			float distance = _rng.RandfRange(minDistance, maxDistance);
			Vector2 candidate = start + Vector2.FromAngle(angle) * distance;
			candidate = ClampToAmbientBounds(candidate, viewportSize);

			if (candidate.DistanceSquaredTo(start) >= minDistance * minDistance * 0.72f
				&& IsAmbientSlimePositionClear(slime, candidate))
			{
				return candidate;
			}
		}

		return GetRandomAmbientSlimePosition(slime, viewportSize);
	}

	private Vector2 GetRandomAmbientSlimePosition(AmbientSlime slime, Vector2 viewportSize)
	{
		Vector2 fallback = GetRandomViewportPosition(viewportSize);
		for (int attempt = 0; attempt < 24; attempt++)
		{
			Vector2 candidate = GetRandomViewportPosition(viewportSize);
			if (IsAmbientSlimePositionClear(slime, candidate))
			{
				return candidate;
			}

			fallback = candidate;
		}

		return fallback;
	}

	private Vector2 GetRandomViewportPosition(Vector2 viewportSize)
	{
		float margin = GetEffectiveViewportMargin(viewportSize);
		float minX = margin;
		float minY = margin;
		float maxX = Mathf.Max(minX, viewportSize.X - margin);
		float maxY = Mathf.Max(minY, viewportSize.Y - margin);
		return new Vector2(_rng.RandfRange(minX, maxX), _rng.RandfRange(minY, maxY));
	}

	private Vector2 ClampToAmbientBounds(Vector2 position, Vector2 viewportSize)
	{
		float margin = GetEffectiveViewportMargin(viewportSize);
		float minX = margin;
		float minY = margin;
		float maxX = Mathf.Max(minX, viewportSize.X - margin);
		float maxY = Mathf.Max(minY, viewportSize.Y - margin);
		return new Vector2(
			Mathf.Clamp(position.X, minX, maxX),
			Mathf.Clamp(position.Y, minY, maxY));
	}

	private bool IsAmbientSlimePositionClear(AmbientSlime slime, Vector2 position)
	{
		foreach (AmbientSlime other in _ambientSlimes)
		{
			if (other == slime || !other.HasPosition)
			{
				continue;
			}

			float minDistance = slime.CollisionRadius + other.CollisionRadius + SlimeSeparationPadding;
			if (position.DistanceSquaredTo(other.Root.Position) < minDistance * minDistance)
			{
				return false;
			}
		}

		foreach (AmbientObstacle obstacle in _ambientObstacles)
		{
			float minDistance = slime.CollisionRadius + obstacle.CollisionRadius + SlimeSeparationPadding;
			if (position.DistanceSquaredTo(obstacle.Root.Position) < minDistance * minDistance)
			{
				return false;
			}
		}

		return true;
	}

	private void ResolveAmbientSlimeObstacleOverlaps(Vector2 viewportSize)
	{
		foreach (AmbientSlime slime in _ambientSlimes)
		{
			foreach (AmbientObstacle obstacle in _ambientObstacles)
			{
				ResolveAmbientSlimeObstaclePair(slime, obstacle, viewportSize);
			}
		}
	}

	private void ResolveAmbientSlimeObstaclePair(AmbientSlime slime, AmbientObstacle obstacle, Vector2 viewportSize)
	{
		Vector2 offset = slime.Root.Position - obstacle.Root.Position;
		float minDistance = slime.CollisionRadius + obstacle.CollisionRadius + SlimeSeparationPadding;
		float distanceSquared = offset.LengthSquared();
		if (distanceSquared >= minDistance * minDistance)
		{
			return;
		}

		float distance = Mathf.Sqrt(distanceSquared);
		Vector2 direction = distance > 0.001f ? offset / distance : Vector2.FromAngle(_rng.RandfRange(0.0f, Mathf.Tau));
		ApplyAmbientSlimeDisplacement(slime, direction * (minDistance - distance), viewportSize);
	}

	private void ResolveAmbientSlimeOverlaps(Vector2 viewportSize)
	{
		for (int iteration = 0; iteration < SlimeSeparationIterations; iteration++)
		{
			for (int i = 0; i < _ambientSlimes.Count; i++)
			{
				for (int j = i + 1; j < _ambientSlimes.Count; j++)
				{
					ResolveAmbientSlimePair(_ambientSlimes[i], _ambientSlimes[j], viewportSize);
				}
			}
		}
	}

	private void ResolveAmbientSlimePair(AmbientSlime first, AmbientSlime second, Vector2 viewportSize)
	{
		Vector2 offset = second.Root.Position - first.Root.Position;
		float minDistance = first.CollisionRadius + second.CollisionRadius + SlimeSeparationPadding;
		float distanceSquared = offset.LengthSquared();
		if (distanceSquared >= minDistance * minDistance)
		{
			return;
		}

		float distance = Mathf.Sqrt(distanceSquared);
		Vector2 direction = distance > 0.001f ? offset / distance : Vector2.FromAngle(_rng.RandfRange(0.0f, Mathf.Tau));
		float pushDistance = (minDistance - distance) * 0.5f;
		ApplyAmbientSlimeDisplacement(first, -direction * pushDistance, viewportSize);
		ApplyAmbientSlimeDisplacement(second, direction * pushDistance, viewportSize);
	}

	private void ApplyAmbientSlimeDisplacement(AmbientSlime slime, Vector2 displacement, Vector2 viewportSize)
	{
		Vector2 previousPosition = slime.Root.Position;
		Vector2 nextPosition = ClampToAmbientBounds(previousPosition + displacement, viewportSize);
		Vector2 appliedDisplacement = nextPosition - previousPosition;
		if (appliedDisplacement.LengthSquared() <= 0.0001f)
		{
			return;
		}

		slime.Root.Position = nextPosition;
		slime.MoveStart = ClampToAmbientBounds(slime.MoveStart + appliedDisplacement, viewportSize);
		slime.MoveTarget = ClampToAmbientBounds(slime.MoveTarget + appliedDisplacement, viewportSize);
	}

	private static float GetEffectiveViewportMargin(Vector2 viewportSize)
	{
		float shortSide = Mathf.Min(viewportSize.X, viewportSize.Y);
		return Mathf.Clamp(shortSide * 0.1f, 32.0f, SlimeViewportMargin);
	}

	private static float GetEffectiveMinMoveDistance(Vector2 viewportSize)
	{
		float shortSide = Mathf.Min(viewportSize.X, viewportSize.Y);
		return Mathf.Clamp(shortSide * 0.34f, 110.0f, SlimeMinMoveDistance);
	}

	private static void UpdateAmbientSlimeAnimation(AmbientSlime slime, float deltaSeconds, bool isMoving)
	{
		if (slime.Sprite is null)
		{
			return;
		}

		if (!isMoving)
		{
			slime.Sprite.Frame = 0;
			return;
		}

		slime.AnimationPhase += deltaSeconds * SlimeMoveAnimationFps;
		slime.Sprite.Frame = (int)slime.AnimationPhase % SlimeMoveAnimationFrameCount;
	}

	private void LoadMenuButtonStyles()
	{
		_buttonNormalStyle = CreateMenuButtonStyle(ButtonNormalTexturePath);
		_buttonHoverStyle = CreateMenuButtonStyle(ButtonHoverTexturePath);
		_buttonPressedStyle = CreateMenuButtonStyle(ButtonPressedTexturePath);
		_buttonDisabledStyle = CreateMenuButtonStyle(ButtonDisabledTexturePath);
	}

	private void LoadLevelSelectButtonStyles()
	{
		_levelButtonNormalStyle = CreateLevelSelectButtonStyle(LevelSelectButtonNormalTexturePath);
		_levelButtonHoverStyle = CreateLevelSelectButtonStyle(LevelSelectButtonHoverTexturePath);
		_levelButtonPressedStyle = CreateLevelSelectButtonStyle(LevelSelectButtonPressedTexturePath);
		_levelButtonDisabledStyle = CreateLevelSelectButtonStyle(LevelSelectButtonDisabledTexturePath);
	}

	private void ApplyMainMenuPanelStyle()
	{
		StyleBoxTexture panelStyle = CreateTextureStyle(MainMenuPanelTexturePath, MainMenuPanelTextureMarginX, MainMenuPanelTextureMarginY);
		if (panelStyle is null)
		{
			return;
		}

		_mainMenuPanelContainer.AddThemeStyleboxOverride("panel", panelStyle);
	}

	private void ApplyLevelSelectPanelStyle()
	{
		StyleBoxTexture panelStyle = CreateTextureStyle(LevelSelectPanelTexturePath, LevelSelectPanelTextureMarginX, LevelSelectPanelTextureMarginY);
		if (panelStyle is null)
		{
			return;
		}

		_levelSelectPanelContainer.AddThemeStyleboxOverride("panel", panelStyle);
	}

	private static StyleBoxTexture CreateMenuButtonStyle(string texturePath)
	{
		return CreateTextureStyle(texturePath, MenuButtonTextureMarginX, MenuButtonTextureMarginY);
	}

	private static StyleBoxTexture CreateLevelSelectButtonStyle(string texturePath)
	{
		return CreateTextureStyle(texturePath, LevelSelectButtonTextureMarginX, LevelSelectButtonTextureMarginY);
	}

	private static StyleBoxTexture CreateTextureStyle(string texturePath, float textureMarginX, float textureMarginY)
	{
		Texture2D texture = ResourceLoader.Load<Texture2D>(texturePath);
		if (texture is null)
		{
			GD.PushWarning($"Unable to load UI texture: {texturePath}");
			return null;
		}

		StyleBoxTexture style = new()
		{
			Texture = texture,
		};
		style.SetTextureMargin(Side.Left, textureMarginX);
		style.SetTextureMargin(Side.Right, textureMarginX);
		style.SetTextureMargin(Side.Top, textureMarginY);
		style.SetTextureMargin(Side.Bottom, textureMarginY);
		return style;
	}

	private void ApplyMenuButtonStyle(Button button)
	{
		if (button is null || _buttonNormalStyle is null)
		{
			return;
		}

		button.AddThemeStyleboxOverride("normal", _buttonNormalStyle);
		button.AddThemeStyleboxOverride("hover", _buttonHoverStyle ?? _buttonNormalStyle);
		button.AddThemeStyleboxOverride("pressed", _buttonPressedStyle ?? _buttonNormalStyle);
		button.AddThemeStyleboxOverride("disabled", _buttonDisabledStyle ?? _buttonNormalStyle);
		button.AddThemeStyleboxOverride("focus", _buttonHoverStyle ?? _buttonNormalStyle);
		button.AddThemeColorOverride("font_color", Colors.White);
		button.AddThemeColorOverride("font_hover_color", Colors.White);
		button.AddThemeColorOverride("font_pressed_color", Colors.White);
		button.AddThemeColorOverride("font_disabled_color", Colors.White);
		button.AddThemeColorOverride("font_outline_color", new Color(0.18f, 0.075f, 0.025f, 0.95f));
		button.AddThemeConstantOverride("outline_size", 2);
	}

	private void ApplyLevelSelectButtonStyle(Button button)
	{
		if (button is null || _levelButtonNormalStyle is null)
		{
			return;
		}

		button.AddThemeStyleboxOverride("normal", _levelButtonNormalStyle);
		button.AddThemeStyleboxOverride("hover", _levelButtonHoverStyle ?? _levelButtonNormalStyle);
		button.AddThemeStyleboxOverride("pressed", _levelButtonPressedStyle ?? _levelButtonNormalStyle);
		button.AddThemeStyleboxOverride("disabled", _levelButtonDisabledStyle ?? _levelButtonNormalStyle);
		button.AddThemeStyleboxOverride("focus", _levelButtonHoverStyle ?? _levelButtonNormalStyle);
		button.AddThemeColorOverride("font_color", Colors.White);
		button.AddThemeColorOverride("font_hover_color", Colors.White);
		button.AddThemeColorOverride("font_pressed_color", Colors.White);
		button.AddThemeColorOverride("font_disabled_color", Colors.White);
		button.AddThemeColorOverride("font_outline_color", new Color(0.18f, 0.075f, 0.025f, 0.95f));
		button.AddThemeConstantOverride("outline_size", 2);
	}

	private sealed class AmbientSlime
	{
		public Node2D Root { get; init; }

		public Sprite2D Sprite { get; init; }

		public float CollisionRadius { get; init; }

		public Vector2 MoveStart { get; set; }

		public Vector2 MoveTarget { get; set; }

		public float MoveDuration { get; set; }

		public float MoveElapsed { get; set; }

		public float IdleRemaining { get; set; }

		public float AnimationPhase { get; set; }

		public bool HasPosition { get; set; }
	}

	private sealed class AmbientObstacle
	{
		public Node2D Root { get; init; }

		public Vector2 NormalizedPosition { get; init; }

		public float CollisionRadius { get; init; }
	}
}
