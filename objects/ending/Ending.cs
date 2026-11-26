using Godot;

public partial class Ending : Control
{
	public override void _Ready()
	{
		Music.Play("final");
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetNode<Button>("Buttons/Menu").GrabFocus();
	}

	private void OnMenuPressed()
	{
		GetNode<SceneTransition>("/root/SceneTransition").ChangeScene("res://objects/main_menu/main_menu.tscn");
	}

	private void OnQuitPressed() => GetTree().Quit();
}
