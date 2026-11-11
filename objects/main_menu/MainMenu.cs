using Godot;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		GetNode<Button>("Buttons/Play").GrabFocus();
	}

	private void OnPlayPressed()
	{
		GetNode<SplashBg>("SplashBg").LastStrike();
		GetNode<SceneTransition>("/root/SceneTransition").ChangeScene("res://levels/fase_1.tscn");
	}

	private void OnQuitPressed() => GetTree().Quit();
}
