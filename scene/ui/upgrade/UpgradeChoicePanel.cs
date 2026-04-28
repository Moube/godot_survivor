using Godot;
using System.Collections.Generic;

public partial class UpgradeChoicePanel : Control
{
	[Signal]
	public delegate void OptionSelectedEventHandler(int optionIndex);

	private const int ChoiceCount = 3;

	private readonly PanelContainer[] _cards = new PanelContainer[ChoiceCount];
	private readonly Label[] _titleLabels = new Label[ChoiceCount];
	private readonly Label[] _typeLabels = new Label[ChoiceCount];
	private readonly Label[] _descriptionLabels = new Label[ChoiceCount];
	private readonly Button[] _buttons = new Button[ChoiceCount];

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		for (int i = 0; i < ChoiceCount; i++)
		{
			int optionIndex = i;
			string cardPath = $"CenterContainer/PanelContainer/MarginContainer/Content/CardRow/Card{i + 1}";
			_cards[i] = GetNode<PanelContainer>(cardPath);
			_titleLabels[i] = GetNode<Label>($"{cardPath}/CardMargin/CardContent/TitleLabel");
			_typeLabels[i] = GetNode<Label>($"{cardPath}/CardMargin/CardContent/TypeLabel");
			_descriptionLabels[i] = GetNode<Label>($"{cardPath}/CardMargin/CardContent/DescriptionLabel");
			_buttons[i] = GetNode<Button>($"{cardPath}/CardMargin/CardContent/SelectButton");
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
		}
	}

	public void HideChoices()
	{
		Visible = false;
	}
}
