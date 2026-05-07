using Godot;
using System;
using System.Collections.Generic;

public partial class UpgradeChoicePanel : Control
{
	[Signal]
	public delegate void OptionSelectedEventHandler(int optionIndex);

	private const int ChoiceCount = 3;
	private const double ShowTweenDurationSeconds = 0.32;
	private const double HideTweenDurationSeconds = 0.14;
	private static readonly Vector2 ShowStartScale = Vector2.One * 0.80f;
	private static readonly Vector2 HideEndScale = Vector2.One * 0.94f;

	private readonly Control[] _cards = new Control[ChoiceCount];
	private readonly Label[] _titleLabels = new Label[ChoiceCount];
	private readonly Label[] _typeLabels = new Label[ChoiceCount];
	private readonly Label[] _descriptionLabels = new Label[ChoiceCount];
	private readonly TextureRect[] _iconSlots = new TextureRect[ChoiceCount];
	private readonly Button[] _buttons = new Button[ChoiceCount];
	private readonly Dictionary<string, Texture2D> _iconCache = new(StringComparer.Ordinal);
	private Control _popupRoot;
	private Tween _popupTween;
	private bool _isHiding;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		_popupRoot = GetNode<Control>("CenterContainer");

		for (int i = 0; i < ChoiceCount; i++)
		{
			int optionIndex = i;
			string cardPath = $"CenterContainer/PanelContainer/MarginContainer/Content/CardRow/Card{i + 1}";
			_cards[i] = GetNode<Control>(cardPath);
			string contentPath = $"{cardPath}/OptionCardPanel/CardMargin/CardContent";
			_iconSlots[i] = GetNode<TextureRect>($"{contentPath}/IconSlot");
			_titleLabels[i] = GetNode<Label>($"{contentPath}/TitleLabel");
			_typeLabels[i] = GetNode<Label>($"{contentPath}/TypeLabel");
			_descriptionLabels[i] = GetNode<Label>($"{contentPath}/DescriptionLabel");
			_buttons[i] = GetNode<Button>($"{cardPath}/SelectButton");
			_buttons[i].ButtonDown += PlayUiClickSound;
			_buttons[i].MouseEntered += PlayUiHoverSound;
			_buttons[i].Pressed += () => OnSelectButtonPressed(optionIndex);
		}

		if (GameSettings.Instance != null)
		{
			GameSettings.Instance.LanguageChanged += OnLanguageChanged;
		}

		ApplyLocalizedText();
		HideChoices();
	}

	public override void _ExitTree()
	{
		KillPopupTween();

		if (GameSettings.Instance != null)
		{
			GameSettings.Instance.LanguageChanged -= OnLanguageChanged;
		}
	}

	public void ShowChoices(IReadOnlyList<UpgradeChoiceOption> options)
	{
		bool shouldPlayShowTween = !Visible || _isHiding;
		KillPopupTween();
		_isHiding = false;
		SetPanelAlpha(1.0f);
		SetOptionButtonsDisabled(false);
		Visible = true;

		for (int i = 0; i < ChoiceCount; i++)
		{
			bool hasOption = options != null && i < options.Count;
			_cards[i].Visible = hasOption;

			if (!hasOption)
			{
				continue;
			}

			UpgradeChoiceOption option = options[i];
			_titleLabels[i].Text = option.Title;
			_typeLabels[i].Text = option.TypeLabel;
			_descriptionLabels[i].Text = option.Description;
			_iconSlots[i].Texture = LoadIconTexture(option.IconTexturePath);
		}

		if (shouldPlayShowTween)
		{
			PlayShowTween();
		}
		else if (_popupRoot != null)
		{
			_popupRoot.Scale = Vector2.One;
		}
	}

	public void HideChoices(bool animate = false)
	{
		if (!animate || !Visible)
		{
			ForceHideChoices();
			return;
		}

		PlayHideTween();
	}

	private void OnSelectButtonPressed(int optionIndex)
	{
		EmitSignal(SignalName.OptionSelected, optionIndex);
	}

	private void ApplyLocalizedText()
	{
		foreach (Button button in _buttons)
		{
			if (button != null)
			{
				button.Text = GameText.Tr("ui.common.select");
			}
		}
	}

	private void OnLanguageChanged(GameLanguage language)
	{
		ApplyLocalizedText();
	}

	private void PlayShowTween()
	{
		KillPopupTween();
		_popupTween = UiTweenUtility.ScaleEaseOutBack(
			_popupRoot,
			ShowStartScale,
			Vector2.One,
			ShowTweenDurationSeconds);
	}

	private void PlayHideTween()
	{
		KillPopupTween();
		_isHiding = true;
		SetOptionButtonsDisabled(true);
		SetPanelAlpha(1.0f);
		if (_popupRoot == null)
		{
			ForceHideChoices();
			return;
		}

		UiTweenUtility.SetPivotToCenter(_popupRoot);
		_popupRoot.Scale = Vector2.One;
		_popupTween = CreateTween();
		_popupTween.BindNode(this);
		_popupTween.SetPauseMode(Tween.TweenPauseMode.Process);
		_popupTween.SetParallel(true);
		_popupTween.TweenProperty(_popupRoot, "scale", HideEndScale, HideTweenDurationSeconds)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.In);
		_popupTween.TweenProperty(this, "modulate:a", 0.0f, HideTweenDurationSeconds)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);
		_popupTween.Finished += OnHideTweenFinished;
	}

	private void OnHideTweenFinished()
	{
		_popupTween = null;
		ForceHideChoices();
	}

	private void KillPopupTween()
	{
		UiTweenUtility.KillTween(_popupTween);
		_popupTween = null;
	}

	private void ForceHideChoices()
	{
		KillPopupTween();
		_isHiding = false;
		SetOptionButtonsDisabled(false);
		SetPanelAlpha(1.0f);
		if (_popupRoot != null)
		{
			_popupRoot.Scale = Vector2.One;
		}

		Visible = false;
	}

	private void SetOptionButtonsDisabled(bool disabled)
	{
		foreach (Button button in _buttons)
		{
			if (button != null)
			{
				button.Disabled = disabled;
			}
		}
	}

	private void SetPanelAlpha(float alpha)
	{
		Color modulate = Modulate;
		modulate.A = alpha;
		Modulate = modulate;
	}

	private static void PlayUiClickSound()
	{
		AudioManager.Instance?.PlayUiClick();
	}

	private static void PlayUiHoverSound()
	{
		AudioManager.Instance?.PlayUiHover();
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
		texture ??= LoadImageTexture(path);
		if (texture == null)
		{
			GD.PushWarning($"Unable to load upgrade icon texture: {path}");
		}

		_iconCache[path] = texture;
		return texture;
	}

	private static Texture2D LoadImageTexture(string path)
	{
		if (!FileAccess.FileExists(path))
		{
			return null;
		}

		Image image = Image.LoadFromFile(path);
		return image == null || image.IsEmpty() ? null : ImageTexture.CreateFromImage(image);
	}
}
