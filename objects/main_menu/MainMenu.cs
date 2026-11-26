using Godot;

// Menu principal: Jogar / Sair (embaixo à esquerda) e, embaixo à direita, a escolha do personagem (Téo ou Lia)
// e da dificuldade (Fácil / Médio / Difícil). As escolhas ficam em GameState.
public partial class MainMenu : Control
{
	// cores da paleta do jogo (tools/art/palette.py)
	private static readonly Color Ink = new(0.11f, 0.10f, 0.13f, 0.82f);        // ink 1c1a22
	private static readonly Color Frame = new(0.23f, 0.25f, 0.28f);             // uni_text 3a3f48
	private static readonly Color Selected = new(0.95f, 0.76f, 0.19f);          // tassel f2c230
	private static readonly Color Cream = new(0.89f, 0.87f, 0.82f);             // plaza_l e3ddd1
	private static readonly Color Green = new(0.17f, 0.71f, 0.45f);             // lanyard 2bb673

	private const float WalkFrameTime = 0.16f;

	private readonly Button[] _characterCards = new Button[2];
	private readonly AtlasTexture[] _characterFrames = new AtlasTexture[2];
	private readonly Button[] _difficultyButtons = new Button[3];
	private Label _difficultyHint;
	private float _walkTimer;
	private int _walkFrame;

	public override void _Ready()
	{
		BuildOptions();
		Refresh();
		GetNode<Button>("Buttons/Play").GrabFocus();
	}

	private void OnPlayPressed()
	{
		GetNode<SplashBg>("SplashBg").LastStrike();
		GetNode<SceneTransition>("/root/SceneTransition").ChangeScene("res://levels/fase_1.tscn");
	}

	private void OnQuitPressed() => GetTree().Quit();

	// ===================================================================== painel de opções
	private void BuildOptions()
	{
		PanelContainer panel = new() { MouseFilter = MouseFilterEnum.Stop };
		panel.AddThemeStyleboxOverride("panel", Box(Ink, Frame, 3, 18));
		panel.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
		panel.GrowHorizontal = GrowDirection.Begin;
		panel.GrowVertical = GrowDirection.Begin;
		panel.OffsetRight = -24;
		panel.OffsetBottom = -16;
		AddChild(panel);

		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 10);
		panel.AddChild(column);

		column.AddChild(Title("PERSONAGEM"));
		HBoxContainer characters = new() { Alignment = BoxContainer.AlignmentMode.Center };
		characters.AddThemeConstantOverride("separation", 16);
		column.AddChild(characters);
		string[] sheets = { "res://sprites/characters/estudante.png", "res://sprites/characters/estudante_garota.png" };
		string[] names = { "TÉO", "LIA" };   // os dois estudantes jogáveis
		for (int i = 0; i < 2; i++)
		{
			int index = i;
			_characterFrames[i] = new AtlasTexture
			{
				Atlas = GD.Load<Texture2D>(sheets[i]),
				Region = new Rect2(0, 0, 16, 24),
			};
			Button card = new()
			{
				Icon = _characterFrames[i],
				ExpandIcon = true,
				Text = names[i],
				IconAlignment = HorizontalAlignment.Center,
				VerticalIconAlignment = VerticalAlignment.Top,
				CustomMinimumSize = new Vector2(104, 150),
				FocusMode = FocusModeEnum.None,
				TextureFilter = TextureFilterEnum.Nearest,
			};
			card.AddThemeFontSizeOverride("font_size", 18);
			card.Pressed += () =>
			{
				GameState.PlayGirl = index == 1;
				Refresh();
			};
			characters.AddChild(card);
			_characterCards[i] = card;
		}

		column.AddChild(Title("DIFICULDADE"));
		HBoxContainer levels = new() { Alignment = BoxContainer.AlignmentMode.Center };
		levels.AddThemeConstantOverride("separation", 8);
		column.AddChild(levels);
		string[] labels = { "Fácil", "Médio", "Difícil" };
		for (int i = 0; i < 3; i++)
		{
			GameState.Difficulty level = (GameState.Difficulty)i;
			Button button = new()
			{
				Text = labels[i],
				CustomMinimumSize = new Vector2(80, 40),
				FocusMode = FocusModeEnum.None,
			};
			button.AddThemeFontSizeOverride("font_size", 18);
			button.Pressed += () =>
			{
				GameState.Level = level;
				Refresh();
			};
			levels.AddChild(button);
			_difficultyButtons[i] = button;
		}

		_difficultyHint = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(256, 0),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		_difficultyHint.AddThemeFontSizeOverride("font_size", 13);
		_difficultyHint.AddThemeColorOverride("font_color", Cream);
		column.AddChild(_difficultyHint);
	}

	private void Refresh()
	{
		for (int i = 0; i < 2; i++)
			Style(_characterCards[i], (i == 1) == GameState.PlayGirl);
		for (int i = 0; i < 3; i++)
			Style(_difficultyButtons[i], (int)GameState.Level == i);

		_difficultyHint.Text = GameState.Level switch
		{
			GameState.Difficulty.Facil => "Mais vida, menos fantasmas e mais lentos.",
			GameState.Difficulty.Dificil => "Menos vida, mais fantasmas, mais fortes e mais rápidos.",
			_ => "O jogo como foi pensado.",
		};
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

	private static Label Title(string text)
	{
		Label label = new() { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
		label.AddThemeFontSizeOverride("font_size", 16);
		label.AddThemeColorOverride("font_color", Green);
		return label;
	}

	private static void Style(Button button, bool chosen)
	{
		Color border = chosen ? Selected : Frame;
		Color face = chosen ? new Color(0.17f, 0.16f, 0.2f) : new Color(0.13f, 0.12f, 0.15f);
		button.AddThemeStyleboxOverride("normal", Box(face, border, 3, 8));
		button.AddThemeStyleboxOverride("hover", Box(face.Lightened(0.1f), chosen ? Selected : Cream, 3, 8));
		button.AddThemeStyleboxOverride("pressed", Box(face.Darkened(0.2f), Selected, 3, 8));
		Color text = chosen ? Selected : Cream;
		button.AddThemeColorOverride("font_color", text);
		button.AddThemeColorOverride("font_hover_color", text);
		button.AddThemeColorOverride("font_pressed_color", Selected);
		button.Modulate = chosen ? Colors.White : new Color(1, 1, 1, 0.75f);
	}

	private static StyleBoxFlat Box(Color background, Color border, int borderWidth, int margin)
	{
		StyleBoxFlat box = new()
		{
			BgColor = background,
			BorderColor = border,
			ContentMarginLeft = margin,
			ContentMarginRight = margin,
			ContentMarginTop = margin * 0.6f,
			ContentMarginBottom = margin * 0.6f,
		};
		box.SetBorderWidthAll(borderWidth);
		box.SetCornerRadiusAll(6);
		return box;
	}
}
