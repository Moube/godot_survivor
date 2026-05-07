using Godot;

public static class UiTweenUtility
{
	public static Tween ScaleEaseInElastic(
		Control control,
		Vector2 fromScale,
		Vector2 toScale,
		double durationSeconds,
		bool centerPivot = true,
		bool processWhenPaused = true)
	{
		return TweenScale(
			control,
			fromScale,
			toScale,
			durationSeconds,
			Tween.TransitionType.Elastic,
			Tween.EaseType.In,
			centerPivot,
			processWhenPaused);
	}

	public static Tween ScaleEaseOutElastic(
		Control control,
		Vector2 fromScale,
		Vector2 toScale,
		double durationSeconds,
		bool centerPivot = true,
		bool processWhenPaused = true)
	{
		return TweenScale(
			control,
			fromScale,
			toScale,
			durationSeconds,
			Tween.TransitionType.Elastic,
			Tween.EaseType.Out,
			centerPivot,
			processWhenPaused);
	}

	public static Tween ScaleEaseOutBack(
		Control control,
		Vector2 fromScale,
		Vector2 toScale,
		double durationSeconds,
		bool centerPivot = true,
		bool processWhenPaused = true)
	{
		return TweenScale(
			control,
			fromScale,
			toScale,
			durationSeconds,
			Tween.TransitionType.Back,
			Tween.EaseType.Out,
			centerPivot,
			processWhenPaused);
	}

	public static Tween ScaleEaseInBack(
		Control control,
		Vector2 fromScale,
		Vector2 toScale,
		double durationSeconds,
		bool centerPivot = true,
		bool processWhenPaused = true)
	{
		return TweenScale(
			control,
			fromScale,
			toScale,
			durationSeconds,
			Tween.TransitionType.Back,
			Tween.EaseType.In,
			centerPivot,
			processWhenPaused);
	}

	public static Tween TweenScale(
		Control control,
		Vector2 fromScale,
		Vector2 toScale,
		double durationSeconds,
		Tween.TransitionType transition,
		Tween.EaseType ease,
		bool centerPivot = true,
		bool processWhenPaused = true)
	{
		if (control is null || !GodotObject.IsInstanceValid(control))
		{
			return null;
		}

		if (centerPivot)
		{
			SetPivotToCenter(control);
		}

		control.Scale = fromScale;
		if (durationSeconds <= 0.0)
		{
			control.Scale = toScale;
			return null;
		}

		Tween tween = control.CreateTween();
		tween.BindNode(control);
		if (processWhenPaused)
		{
			tween.SetPauseMode(Tween.TweenPauseMode.Process);
		}

		tween.TweenProperty(control, "scale", toScale, durationSeconds)
			.SetTrans(transition)
			.SetEase(ease);
		return tween;
	}

	public static void KillTween(Tween tween)
	{
		if (tween is null || !GodotObject.IsInstanceValid(tween) || !tween.IsValid())
		{
			return;
		}

		tween.Kill();
	}

	public static void SetPivotToCenter(Control control)
	{
		if (control is null || !GodotObject.IsInstanceValid(control))
		{
			return;
		}

		Vector2 size = control.Size;
		if (size.X <= 0.0f || size.Y <= 0.0f)
		{
			size = control.GetCombinedMinimumSize();
		}

		control.PivotOffset = size * 0.5f;
	}
}
