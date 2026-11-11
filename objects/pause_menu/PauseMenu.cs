using Godot;

public partial class PauseMenu : CanvasLayer
{
	public override void _Ready()
	{
		Hide();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel")) return;

		if (Visible)
			Resume();
		else if (CanPause())
			Pause();
		else
			return;
	}

	private bool CanPause()
	{
		bool inGame = GetTree().CurrentScene is not MainMenu;
		bool alreadyPaused = GetTree().Paused;
		return inGame && !alreadyPaused;
	}

	private void Pause()
	{
		Show();
		GetTree().Paused = true;
		GetNode<Button>("Buttons/Continue").GrabFocus();
	}

	private void Resume()
	{
		Hide();
		GetTree().Paused = false;
	}
}
