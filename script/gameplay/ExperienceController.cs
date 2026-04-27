using Godot;

public partial class ExperienceController : Node
{
	[Signal]
	public delegate void ExperienceChangedEventHandler(int currentExperience, int requiredExperience, int level);

	[Signal]
	public delegate void LevelUpRequestedEventHandler(int level);

	public static ExperienceController Instance { get; private set; }

	public int Level { get; private set; } = 1;

	public int CurrentExperience { get; private set; }

	public int RequiredExperience { get; private set; } = 1;

	public bool IsLevelUpPending { get; private set; }

	private ExperienceCurveConfig _experienceCurve;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void StartNewRun(string experienceCurveId)
	{
		_experienceCurve = GameConfigManager.Instance?.GetExperienceCurveConfig(experienceCurveId);
		Level = 1;
		CurrentExperience = 0;
		IsLevelUpPending = false;
		RequiredExperience = GetRequiredExperienceForLevel(Level);
		EmitExperienceChanged();
	}

	public void AddExperience(int amount)
	{
		if (amount <= 0 || IsLevelUpPending)
		{
			return;
		}

		CurrentExperience = Mathf.Min(RequiredExperience, CurrentExperience + amount);
		EmitExperienceChanged();

		if (CurrentExperience >= RequiredExperience)
		{
			IsLevelUpPending = true;
			EmitSignal(SignalName.LevelUpRequested, Level);
		}
	}

	public void CompletePendingLevelUp()
	{
		if (!IsLevelUpPending)
		{
			return;
		}

		Level++;
		CurrentExperience = 0;
		IsLevelUpPending = false;
		RequiredExperience = GetRequiredExperienceForLevel(Level);
		EmitExperienceChanged();
	}

	private int GetRequiredExperienceForLevel(int level)
	{
		if (_experienceCurve is null || _experienceCurve.RequiredExperienceByLevel.Count == 0)
		{
			return 1;
		}

		int index = Mathf.Clamp(level - 1, 0, _experienceCurve.RequiredExperienceByLevel.Count - 1);
		return Mathf.Max(1, _experienceCurve.RequiredExperienceByLevel[index]);
	}

	private void EmitExperienceChanged()
	{
		EmitSignal(SignalName.ExperienceChanged, CurrentExperience, RequiredExperience, Level);
	}
}
