using Godot;
using System;

public partial class FailScreen : CanvasLayer
{
	public event Action Retry;

	public override void _Ready()
	{
		Hide();
	}

	public void Open(string reason)
	{
		Control content = GetNode<Control>("Content");
		GetNode<Label>("Content/SplashBg/Diploma/Text").Text = reason;

		content.Modulate = Colors.Transparent;
		Show();
		CreateTween().TweenProperty(content, "modulate:a", 1f, 1f);

		GetNode<SplashBg>("Content/SplashBg").Strike();
		GetNode<Button>("Content/Buttons/Retry").GrabFocus();
	}

	private void OnRetryPressed()
	{
		Hide();
		Retry?.Invoke();
	}

	private void OnMenuPressed()
	{
		GetNode<SceneTransition>("/root/SceneTransition").ChangeScene("res://objects/main_menu/main_menu.tscn");
	}
}
