using Godot;

// Menu principal do "Instituto de Ciência e Terror".
// Tela inicial: logotipo no alto e as opções no meio (Jogar, Controles, Opções, Sair).
// Telas secundárias (Esc ou Voltar retornam):
//   Jogar     -> NOVO JOGO: escolha do personagem (Téo ou Lia) e da dificuldade, e Começar
//   Controles -> tecla e para que serve
//   Opções    -> volume geral e brilho da tela
// As escolhas ficam em GameState (valem até fechar o jogo).
public partial class MainMenu : Control
{
	private const float WalkFrameTime = 0.16f;

	private Control _home;
	private Control _newGame;
	private Control _controls;
	private Control _options;
	private Button _homeFirst;

	private readonly Button[] _characterCards = new Button[2];
	private readonly AtlasTexture[] _characterFrames = new AtlasTexture[2];
	private readonly Button[] _difficultyButtons = new Button[3];
	private Label _difficultyHint;
	private float _walkTimer;
	private int _walkFrame;
	private bool _starting;

	public override void _Ready()
	{
		Music.Play("tema");
		GetNode("SplashBg").AddChild(MenuUi.Logo("res://sprites/ui/menu/logo.png"));

		_home = BuildHome();
		_newGame = Screen(BuildNewGame());
		_controls = Screen(BuildControls());
		_options = Screen(BuildOptions());
		RefreshChoices();
		ShowScreen(_home);
	}

	// ===================================================================== telas
	private void ShowScreen(Control screen)
	{
		foreach (Control s in new[] { _home, _newGame, _controls, _options })
			s.Visible = s == screen;
		if (screen == _home)
			_homeFirst.GrabFocus();
		else if (screen.FindChild("Primeiro", true, false) is Control first)
			first.GrabFocus();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel") && !_home.Visible)
		{
			ShowScreen(_home);
			GetViewport().SetInputAsHandled();
		}
	}

	// tela secundária: centralizada, um pouco abaixo do logotipo
	private Control Screen(Control content)
	{
		CenterContainer center = new() { MouseFilter = MouseFilterEnum.Ignore };
		center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		center.OffsetTop = 236;   // abaixo do logotipo
		center.AddChild(content);
		AddChild(center);
		return center;
	}

	private Button BackButton()
	{
		Button back = MenuUi.BigButton("Voltar", 24);
		back.Pressed += () => ShowScreen(_home);
		return back;
	}

	// ---------- inicial ----------
	private Control BuildHome()
	{
		CenterContainer center = new() { MouseFilter = MouseFilterEnum.Ignore };
		center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		center.OffsetTop = 170;
		AddChild(center);

		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 2);
		center.AddChild(column);

		(string text, System.Action action)[] items =
		{
			("Jogar", () => ShowScreen(_newGame)),
			("Controles", () => ShowScreen(_controls)),
			("Opções", () => ShowScreen(_options)),
			("Sair", () => GetTree().Quit()),
		};
		foreach ((string text, System.Action action) in items)
		{
			Button button = MenuUi.BigButton(text, 34);
			button.CustomMinimumSize = new Vector2(320, 0);
			button.Pressed += action;
			column.AddChild(button);
			_homeFirst ??= button;
		}
		return center;
	}

	// ---------- novo jogo: personagem e dificuldade ----------
	private Control BuildNewGame()
	{
		PanelContainer panel = MenuUi.Panel(MenuUi.Yellow);
		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 10);
		panel.AddChild(column);
		column.AddChild(MenuUi.Text("NOVO JOGO", 24, MenuUi.Yellow));

		// duas colunas: personagem à esquerda, dificuldade à direita
		HBoxContainer sides = new();
		sides.AddThemeConstantOverride("separation", 32);
		column.AddChild(sides);

		VBoxContainer left = new();
		left.AddThemeConstantOverride("separation", 8);
		sides.AddChild(left);
		left.AddChild(MenuUi.Text("PERSONAGEM", 16, MenuUi.Green));
		HBoxContainer characters = new() { Alignment = BoxContainer.AlignmentMode.Center };
		characters.AddThemeConstantOverride("separation", 14);
		left.AddChild(characters);
		string[] sheets = { "res://sprites/characters/estudante.png", "res://sprites/characters/estudante_garota.png" };
		string[] names = { "TÉO", "LIA" };   // os dois estudantes jogáveis
		for (int i = 0; i < 2; i++)
		{
			int index = i;
			_characterFrames[i] = new AtlasTexture { Atlas = GD.Load<Texture2D>(sheets[i]), Region = new Rect2(0, 0, 16, 24) };
			Button card = new()
			{
				Icon = _characterFrames[i],
				ExpandIcon = true,
				Text = names[i],
				IconAlignment = HorizontalAlignment.Center,
				VerticalIconAlignment = VerticalAlignment.Top,
				CustomMinimumSize = new Vector2(110, 150),
				TextureFilter = TextureFilterEnum.Nearest,
			};
			card.AddThemeFontSizeOverride("font_size", 20);
			card.Pressed += () =>
			{
				GameState.PlayGirl = index == 1;
				RefreshChoices();
			};
			characters.AddChild(card);
			_characterCards[i] = card;
		}

		VBoxContainer right = new();
		right.AddThemeConstantOverride("separation", 8);
		sides.AddChild(right);
		right.AddChild(MenuUi.Text("DIFICULDADE", 16, MenuUi.Green));
		VBoxContainer levels = new();
		levels.AddThemeConstantOverride("separation", 6);
		right.AddChild(levels);
		string[] labels = { "Fácil", "Médio", "Difícil" };
		for (int i = 0; i < 3; i++)
		{
			GameState.Difficulty level = (GameState.Difficulty)i;
			Button button = new() { Text = labels[i], CustomMinimumSize = new Vector2(200, 40) };
			button.AddThemeFontSizeOverride("font_size", 18);
			button.Pressed += () =>
			{
				GameState.Level = level;
				RefreshChoices();
			};
			levels.AddChild(button);
			_difficultyButtons[i] = button;
		}

		_difficultyHint = MenuUi.Text("", 13, MenuUi.Cream);
		_difficultyHint.CustomMinimumSize = new Vector2(200, 0);
		_difficultyHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		right.AddChild(_difficultyHint);

		HBoxContainer actions = new() { Alignment = BoxContainer.AlignmentMode.Center };
		actions.AddThemeConstantOverride("separation", 24);
		column.AddChild(actions);
		actions.AddChild(BackButton());
		Button start = MenuUi.BigButton("Começar", 28);
		start.Name = "Primeiro";
		start.Pressed += StartGame;
		actions.AddChild(start);
		return panel;
	}

	private void RefreshChoices()
	{
		for (int i = 0; i < 2; i++)
			MenuUi.Choice(_characterCards[i], (i == 1) == GameState.PlayGirl);
		for (int i = 0; i < 3; i++)
			MenuUi.Choice(_difficultyButtons[i], (int)GameState.Level == i);

		_difficultyHint.Text = GameState.Level switch
		{
			GameState.Difficulty.Facil => "Mais vida, menos fantasmas e mais lentos.",
			GameState.Difficulty.Dificil => "Menos vida, mais fantasmas, mais fortes e mais rápidos.",
			_ => "O jogo como foi pensado.",
		};
	}

	private void StartGame()
	{
		if (_starting) return;
		_starting = true;
		GetNode<SplashBg>("SplashBg").LastStrike();
		GetNode<SceneTransition>("/root/SceneTransition").ChangeScene("res://levels/fase_1.tscn");
	}

	// ---------- controles ----------
	private Control BuildControls()
	{
		PanelContainer panel = MenuUi.Panel(MenuUi.Green);
		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 8);
		panel.AddChild(column);
		column.AddChild(MenuUi.Text("CONTROLES", 22, MenuUi.Yellow));
		column.AddChild(MenuUi.ControlsList(15));
		column.AddChild(MenuUi.Text("No jogo, H mostra ou esconde este quadro.", 13, MenuUi.Cream));
		Button back = BackButton();
		back.Name = "Primeiro";
		column.AddChild(back);
		return panel;
	}

	// ---------- opções ----------
	private Control BuildOptions()
	{
		PanelContainer panel = MenuUi.Panel(MenuUi.Green);
		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 14);
		panel.AddChild(column);
		column.AddChild(MenuUi.Text("OPÇÕES", 26, MenuUi.Yellow));

		HBoxContainer volumeRow = new();
		volumeRow.AddThemeConstantOverride("separation", 16);
		Label volumeLabel = MenuUi.Text("Volume", 18, MenuUi.Cream, false);
		volumeLabel.CustomMinimumSize = new Vector2(90, 0);
		volumeRow.AddChild(volumeLabel);
		HSlider volume = new()
		{
			Name = "Primeiro",
			MinValue = 0,
			MaxValue = 1,
			Step = 0.05,
			Value = AudioServer.IsBusMute(0) ? 0 : Mathf.DbToLinear(AudioServer.GetBusVolumeDb(0)),
			CustomMinimumSize = new Vector2(240, 24),
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
		};
		volume.ValueChanged += v =>
		{
			AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb((float)Mathf.Max(v, 0.0001)));
			AudioServer.SetBusMute(0, v <= 0.001);
		};
		volumeRow.AddChild(volume);
		column.AddChild(volumeRow);

		HBoxContainer brightnessRow = new();
		brightnessRow.AddThemeConstantOverride("separation", 16);
		Label brightnessLabel = MenuUi.Text("Brilho", 18, MenuUi.Cream, false);
		brightnessLabel.CustomMinimumSize = new Vector2(90, 0);
		brightnessRow.AddChild(brightnessLabel);
		HSlider brightness = new()
		{
			MinValue = 0.5,
			MaxValue = 1.6,
			Step = 0.05,
			Value = GameState.Brightness,
			CustomMinimumSize = new Vector2(240, 24),
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
		};
		brightness.ValueChanged += v => GetNode<Brightness>("/root/Brightness").Set((float)v);
		brightnessRow.AddChild(brightness);
		column.AddChild(brightnessRow);

		column.AddChild(BackButton());
		return panel;
	}

	// o personagem escolhido anda no lugar; o outro fica parado
	public override void _Process(double delta)
	{
		_walkTimer -= (float)delta;
		if (_walkTimer > 0) return;
		_walkTimer = WalkFrameTime;
		_walkFrame = (_walkFrame + 1) % 4;
		for (int i = 0; i < 2; i++)
		{
			bool chosen = (i == 1) == GameState.PlayGirl;
			_characterFrames[i].Region = new Rect2((chosen ? _walkFrame : 0) * 16, 0, 16, 24);
		}
	}
}
