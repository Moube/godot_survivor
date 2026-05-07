using Godot;

public abstract partial class PausableLevelBase : Node2D
{
	private PauseMenu _pauseMenu;

	public override void _EnterTree()
	{
		EnsurePauseMenu();
	}

	protected virtual bool CanOpenPauseMenu()
	{
		return true;
	}

	protected void ClosePauseMenuWithoutUnpausing()
	{
		_pauseMenu?.CloseWithoutUnpausing();
	}

	private void EnsurePauseMenu()
	{
		if (_pauseMenu != null && IsInstanceValid(_pauseMenu))
		{
			return;
		}

		_pauseMenu = new PauseMenu
		{
			Name = "PauseMenu",
			CanOpenMenu = CanOpenPauseMenu,
		};
		AddChild(_pauseMenu);
	}
}
