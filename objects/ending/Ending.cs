using Godot;

public partial class Ending : Control
{
	public override void _Ready()
	{
		Music.Play("final");
		// título em pixel art ("PARABÉNS!" dourado, tools/art/logo.py) no lugar do pergaminho
		GetNode("SplashBg").AddChild(MenuUi.Logo("res://sprites/ui/menu/logo_parabens.png"));
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetNode<Button>("Buttons/Menu").GrabFocus();
	}

	private void OnMenuPressed()
	{
		GetNode<SceneTransition>("/root/SceneTransition").ChangeScene("res://objects/main_menu/main_menu.tscn");
	}

	private void OnQuitPressed() => GetTree().Quit();
}
