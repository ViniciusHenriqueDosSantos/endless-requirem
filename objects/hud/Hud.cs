using Godot;
using System.Linq;

public partial class Hud : CanvasLayer
{
	[Export] public Player Player;

	private static readonly Color ObjectiveColor = new(0.6f, 1f, 0.75f);

	private Label _label;
	private Label _objective;
	private Label _message;
	private float _messageTimer;

	private const float BossBarWidth = 520f;
	private Control _bossBar;
	private Label _bossName;
	private ColorRect _bossFill;

	// Distância da borda da tela onde a seta aparece.
	private const float PointerMargin = 48f;
	private ScreenPointer _pointer;
	private Label _counter;

	public override void _Ready()
	{
		AddToGroup("hud");
		_label = GetNode<Label>("Label");

		// Objetivo atual, no topo da tela.
		_objective = MakeLabel(22, ObjectiveColor);
		_objective.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
		_objective.OffsetTop = 84;
		_objective.OffsetBottom = 120;
		AddChild(_objective);

		// Mensagens rápidas ("Você pegou: Crachá"), embaixo.
		_message = MakeLabel(24, Colors.White);
		_message.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
		_message.OffsetTop = -150;
		_message.OffsetBottom = -90;
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

		Player.StatsChanged += Refresh;
		Refresh();
	}

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
	// F1 = Fase 1, F2 = Fase 2, F3 = escada. Começam com o equipamento padrão daquela fase.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!OS.IsDebugBuild() || @event is not InputEventKey { Pressed: true, Echo: false } key) return;

		string scene = key.Keycode switch
		{
			Key.F1 => "res://levels/fase_1.tscn",
			Key.F2 => "res://levels/fase_2.tscn",
			Key.F3 => "res://levels/escada.tscn",
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
		string weapons = string.Join("   ", Player.OwnedWeapons.Select((weapon, i) =>
			weapon == Player.Weapon ? $"[{i + 1}: {weapon.Name}]" : $"{i + 1}: {weapon.Name}"));

		string ammo = Player.IsReloading ? "recarregando..." : $"{Player.Ammo}/{Player.Weapon.MagazineSize}";
		string items = Player.ItemNames.Count > 0 ? "  |  Itens: " + string.Join(", ", Player.ItemNames) : "";

		_label.Text = $"Vida: {Player.Health}/{Player.MaxHealth}  |  Armas: {weapons}  ({ammo}){items}\n" +
					  "[WASD] Mover  |  [Mouse] Mirar e atirar  |  [Espaço] Dash";
	}
}
