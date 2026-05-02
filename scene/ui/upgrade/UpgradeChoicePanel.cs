using Godot;
using System;
using System.Collections.Generic;

public partial class UpgradeChoicePanel : Control
{
	[Signal]
	public delegate void OptionSelectedEventHandler(int optionIndex);

	private const int ChoiceCount = 3;

	private readonly Control[] _cards = new Control[ChoiceCount];
	private readonly Label[] _titleLabels = new Label[ChoiceCount];
	private readonly Label[] _typeLabels = new Label[ChoiceCount];
	private readonly Label[] _descriptionLabels = new Label[ChoiceCount];
	private readonly TextureRect[] _iconSlots = new TextureRect[ChoiceCount];
	private readonly Button[] _buttons = new Button[ChoiceCount];
	private readonly Dictionary<string, Texture2D> _iconCache = new(StringComparer.Ordinal);

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

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
			_buttons[i].Pressed += () => EmitSignal(SignalName.OptionSelected, optionIndex);
		}

		HideChoices();
	}

	public void ShowChoices(IReadOnlyList<UpgradeChoiceOption> options)
	{
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
	}

	public void HideChoices()
	{
		Visible = false;
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
