using Godot;

public partial class Hud : CanvasLayer
{
	[Export] public Player Player;

	private TextureProgressBar lifeTube;
	private Label lifeAmount;
	private TextureRect gunIcon;
	private Label gunName;
	private Label gunAmmo;
	private Caption _objective;
	private Caption _message;
	private float _messageTimer;

	private const float BossBarWidth = 520f;
	private Control _bossBar;
	private Label _bossName;
	private ColorRect _bossFill;

	// Distância da borda da tela onde a seta aparece.
	private const float PointerMargin = 48f;
	private ScreenPointer _pointer;
	private Label _counter;

	// Quadro de comandos (tecla + para que serve), pequeno, à direita. A Fase 1 mostra no começo; H mostra/esconde.
	public static readonly (string Key, string Use)[] Controls =
	{
		("W A S D", "andar"),
		("MOUSE", "mirar"),
		("CLIQUE", "atirar"),
		("ESPAÇO", "rolar: desvia de tiros e fantasmas"),
		("R", "recarregar a arma"),
		("E", "pegar itens, usar portas e aparelhos"),
		("1 2 3 / SCROLL", "trocar de arma"),
		("ESC", "pausar"),
	};
	private Control _controls;

	public override void _Ready()
	{
		AddToGroup("hud");
		lifeTube = GetNode<TextureProgressBar>("Bottom/Life/Tube");
		lifeAmount = GetNode<Label>("Bottom/Life/Amount");
		gunIcon = GetNode<TextureRect>("Bottom/Gun/Icon");
		gunName = GetNode<Label>("Bottom/Gun/Name");
		gunAmmo = GetNode<Label>("Bottom/Gun/Ammo");

		// Legendas embaixo, onde o jogador está olhando (cores da paleta, ver Caption):
		// objetivo atual (borda verde) na última linha e, logo acima, as mensagens rápidas (borda azul).
		_objective = Caption.AtBottom(Caption.Kind.Objective, 20, 66, 22);
		_objective.MaxWidth = 620f;   // não encosta na vida e na arma, no canto de baixo à esquerda
		AddChild(_objective);

		_message = Caption.AtBottom(Caption.Kind.Message, 22, 126, 76);
		AddChild(_message);

		CreateBossBar();

		// Contador grande no canto (ex.: andar da escada). Escondido até alguém usar.
		_counter = MakeLabel(40, new Color(0.55f, 1f, 0.7f));
		_counter.HorizontalAlignment = HorizontalAlignment.Right;
		_counter.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
		_counter.OffsetLeft = -360;
		_counter.OffsetRight = -24;
		_counter.OffsetTop = 16;
		_counter.OffsetBottom = 70;
		AddChild(_counter);

		_pointer = new ScreenPointer { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
		_pointer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(_pointer);

		BuildControls();

		Player.StatsChanged += Refresh;
		Refresh();
	}

	private void BuildControls()
	{
		PanelContainer panel = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
		StyleBoxFlat box = new()
		{
			BgColor = new Color(Caption.Ink, 0.85f),
			BorderColor = Caption.Yellow,
			ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6,
		};
		box.SetBorderWidthAll(2);
		box.SetCornerRadiusAll(6);
		panel.AddThemeStyleboxOverride("panel", box);
		panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterRight);
		panel.GrowHorizontal = Control.GrowDirection.Begin;
		panel.GrowVertical = Control.GrowDirection.Both;
		panel.OffsetRight = -16;
		AddChild(panel);

		VBoxContainer column = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 3);
		panel.AddChild(column);
		column.AddChild(ControlsLabel("COMANDOS", 13, Caption.Yellow));

		GridContainer grid = new() { Columns = 2, MouseFilter = Control.MouseFilterEnum.Ignore };
		grid.AddThemeConstantOverride("h_separation", 10);
		grid.AddThemeConstantOverride("v_separation", 1);
		foreach ((string key, string use) in Controls)
		{
			grid.AddChild(ControlsLabel(key, 11, Caption.Green));
			grid.AddChild(ControlsLabel(use, 11, Caption.Cream));
		}
		column.AddChild(grid);
		column.AddChild(ControlsLabel("H: mostrar / esconder", 10, new Color(Caption.Cream, 0.6f)));
		_controls = panel;
	}

	private static Label ControlsLabel(string text, int size, Color color)
	{
		Label label = new() { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeConstantOverride("outline_size", 4);
		label.AddThemeColorOverride("font_outline_color", Caption.Ink);
		return label;
	}

	public void ShowControls(bool visible) => _controls.Visible = visible;



	// Barra de vida do chefe, no topo da tela (escondida fora da luta).
	private void CreateBossBar()
	{
		_bossBar = new Control { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
		_bossBar.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
		_bossBar.OffsetLeft = -BossBarWidth / 2;
		_bossBar.OffsetRight = BossBarWidth / 2;
		_bossBar.OffsetTop = 124;
		_bossBar.OffsetBottom = 170;
		AddChild(_bossBar);

		_bossName = MakeLabel(16, Colors.White);
		_bossName.Size = new Vector2(BossBarWidth, 22);
		_bossBar.AddChild(_bossName);

		ColorRect background = new()
		{
			Color = new Color(0.1f, 0.1f, 0.12f, 0.85f),
			Position = new Vector2(0, 26),
			Size = new Vector2(BossBarWidth, 16),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_bossBar.AddChild(background);

		_bossFill = new ColorRect
		{
			Position = new Vector2(0, 26),
			Size = new Vector2(BossBarWidth, 16),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_bossBar.AddChild(_bossFill);
	}

	public void SetBoss(string name, float healthRatio, bool shielded)
	{
		_bossBar.Visible = true;
		_bossName.Text = shielded ? $"{name}  [ESCUDO]" : name;
		_bossFill.Size = new Vector2(BossBarWidth * Mathf.Clamp(healthRatio, 0f, 1f), 16);
		_bossFill.Color = shielded ? new Color(0.35f, 0.75f, 0.55f) : new Color(0.9f, 0.25f, 0.2f);
	}

	public void HideBoss()
	{
		_bossBar.Visible = false;
		HidePointer();
	}

	// Mostra uma seta na borda da tela apontando para um ponto do mundo,
	// só quando ele está fora da tela.
	public void PointTo(Vector2 globalPosition, string label)
	{
		Rect2 view = GetViewport().GetVisibleRect();
		Vector2 screen = GetViewport().GetCanvasTransform() * globalPosition;
		Rect2 inner = view.Grow(-PointerMargin);

		if (inner.HasPoint(screen))
		{
			HidePointer();
			return;
		}

		Vector2 clamped = new(
			Mathf.Clamp(screen.X, inner.Position.X, inner.End.X),
			Mathf.Clamp(screen.Y, inner.Position.Y, inner.End.Y));
		_pointer.Label = label;
		_pointer.PointAt(clamped, (screen - view.GetCenter()).Angle());
	}

	// Atalhos de teste (só ao rodar pelo editor, nunca no jogo exportado):
	// F1 = Fase 1, F2 = Fase 2, F3 = escada, F4 = Fase 3, F5 = auditório 3D (fim da Fase 3),
	// F6 = Pac-Man dentro do telão.
	// Começam com o equipamento padrão daquela fase.
	public override void _UnhandledInput(InputEvent @event)
	{
		// H mostra/esconde o quadro de comandos em qualquer fase (unhandled: digitar "h" numa caixa de texto não conta)
		if (@event is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.H })
		{
			_controls.Visible = !_controls.Visible;
			return;
		}

		if (!OS.IsDebugBuild() || @event is not InputEventKey { Pressed: true, Echo: false } key) return;

		string scene = key.Keycode switch
		{
			Key.F1 => "res://levels/fase_1.tscn",
			Key.F2 => "res://levels/fase_2.tscn",
			Key.F3 => "res://levels/escada.tscn",
			Key.F4 => "res://levels/fase_3.tscn",
			Key.F5 => "res://levels/auditorio_3d.tscn",
			Key.F6 => "res://levels/pacman.tscn",
			_ => "",
		};
		if (scene == "") return;

		GameState.Clear();
		GetTree().Paused = false;
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, scene);
	}

	public void SetCounter(string text)
	{
		_counter.Text = text;
	}

	public void HidePointer()
	{
		_pointer.Visible = false;
	}

	private static Label MakeLabel(int fontSize, Color color)
	{
		Label label = new()
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = color,
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeConstantOverride("outline_size", 6);
		label.AddThemeColorOverride("font_outline_color", Colors.Black);
		return label;
	}

	public override void _ExitTree()
	{
		Player.StatsChanged -= Refresh;
	}

	public override void _Process(double delta)
	{
		if (_messageTimer <= 0) return;

		_messageTimer -= (float)delta;
		if (_messageTimer <= 0)
			_message.Text = "";
	}

	public void SetObjective(string text)
	{
		_objective.Text = text;
	}

	public void ShowMessage(string text, float seconds = 3f)
	{
		_message.Text = text;
		_messageTimer = seconds;
	}

	private void Refresh()
	{
		gunIcon.Texture = Player.Weapon.Sprite;
		gunName.Text = Player.Weapon.Name;
		gunAmmo.Text = Player.IsReloading ? "RECARREGANDO" : $"{Player.Ammo}/{Player.Weapon.MagazineSize}";

		lifeTube.MaxValue = Player.MaxHealth;
		lifeTube.Value = Player.Health;
		lifeAmount.Text = $"{Player.Health}/{Player.MaxHealth}";
	}
}
