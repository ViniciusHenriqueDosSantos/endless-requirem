using Godot;

// Menu de pausa (Esc no jogo): título "PAUSADO" em pixel art e as opções no meio da tela:
// Continuar, Controles (mostra o quadro de teclas) e Voltar ao menu.
public partial class PauseMenu : CanvasLayer
{
	private Control _buttons;
	private Control _controls;
	private Button _continue;

	public override void _Ready()
	{
		GetNode("SplashBg").AddChild(MenuUi.Logo("res://sprites/ui/menu/logo_pausado.png"));
		BuildButtons();
		BuildControls();
		Hide();
	}

	private void BuildButtons()
	{
		CenterContainer center = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		center.OffsetTop = 170;
		AddChild(center);

		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 2);
		center.AddChild(column);

		_continue = Add(column, "Continuar", Resume);
		Add(column, "Controles", () => ShowControls(true));
		Add(column, "Voltar ao menu", BackToMenu);
		_buttons = center;
	}

	private static Button Add(VBoxContainer column, string text, System.Action action)
	{
		Button button = MenuUi.BigButton(text, 34);
		button.CustomMinimumSize = new Vector2(380, 0);
		button.Pressed += action;
		column.AddChild(button);
		return button;
	}

	private void BuildControls()
	{
		CenterContainer center = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		center.OffsetTop = 236;   // abaixo do título
		AddChild(center);

		PanelContainer panel = MenuUi.Panel(MenuUi.Green);
		center.AddChild(panel);
		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 8);
		panel.AddChild(column);
		column.AddChild(MenuUi.Text("CONTROLES", 22, MenuUi.Yellow));
		column.AddChild(MenuUi.ControlsList(15));
		Button back = MenuUi.BigButton("Voltar", 24);
		back.Name = "Voltar";
		back.Pressed += () => ShowControls(false);
		column.AddChild(back);
		_controls = center;
	}

	private void ShowControls(bool show)
	{
		_controls.Visible = show;
		_buttons.Visible = !show;
		if (show)
			_controls.FindChild("Voltar", true, false)?.Call(Control.MethodName.GrabFocus);
		else
			_continue.GrabFocus();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel")) return;

		if (Visible && _controls.Visible)
			ShowControls(false);
		else if (Visible)
			Resume();
		else if (CanPause())
			Pause();
		else
			return;
		GetViewport().SetInputAsHandled();
	}

	private bool CanPause()
	{
		bool inGame = GetTree().CurrentScene is not MainMenu and not Intro;
		bool alreadyPaused = GetTree().Paused;
		return inGame && !alreadyPaused;
	}

	private void Pause()
	{
		Show();
		ShowControls(false);
		GetTree().Paused = true;
	}

	private void Resume()
	{
		Hide();
		GetTree().Paused = false;
	}

	// Sai da partida e volta ao menu principal (o jogo continua pausado durante o escurecimento da tela).
	private void BackToMenu()
	{
		Hide();
		GetNode<SceneTransition>("/root/SceneTransition").ChangeScene("res://objects/main_menu/main_menu.tscn");
	}
}
