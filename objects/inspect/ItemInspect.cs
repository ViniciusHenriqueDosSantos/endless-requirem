using Godot;

// Tela de inspeção de item, no estilo Resident Evil: o jogo pausa, o fundo escurece e o item aparece grande no meio,
// girando devagar, com o nome e a descrição embaixo. Qualquer tecla ou clique fecha.
// Fica na fase (grupo "inspect"); quem chama é o Pickup, quando o item tem InspectTexture.
public partial class ItemInspect : CanvasLayer
{
	[Signal] public delegate void ClosedEventHandler();

	private const ulong MinShowTimeMs = 600;
	private const float FadeTime = 0.25f;

	private ColorRect _background;
	private TextureRect _item;
	private Label _header;
	private Label _title;
	private Label _text;
	private Label _hint;
	private ShaderMaterial _shine;
	private ulong _shownAt;
	private float _time;
	private bool _open;

	public bool IsOpen => _open;

	public override void _Ready()
	{
		AddToGroup("inspect");
		Layer = 9;
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;

		_background = new ColorRect { Color = new Color(0.01f, 0.012f, 0.02f, 0.9f), MouseFilter = Control.MouseFilterEnum.Ignore };
		_background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(_background);

		_shine = new ShaderMaterial { Shader = GD.Load<Shader>("res://objects/inspect/inspect_shine.gdshader") };
		_item = new TextureRect
		{
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Material = _shine,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		AddChild(_item);

		_header = MakeLabel(18, new Color(0.55f, 0.6f, 0.58f));
		_title = MakeLabel(36, new Color(0.85f, 1f, 0.88f));
		_text = MakeLabel(20, new Color(0.82f, 0.82f, 0.8f));
		_hint = MakeLabel(14, new Color(0.5f, 0.52f, 0.52f));
		_text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
	}

	private Label MakeLabel(int size, Color color)
	{
		Label label = new() { HorizontalAlignment = HorizontalAlignment.Center, Modulate = color, MouseFilter = Control.MouseFilterEnum.Ignore };
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeConstantOverride("outline_size", 6);
		label.AddThemeColorOverride("font_outline_color", Colors.Black);
		AddChild(label);
		return label;
	}

	public void Show(Texture2D texture, string title, string text)
	{
		_item.Texture = texture;
		_header.Text = "ITEM OBTIDO";
		_title.Text = title;
		_text.Text = text;
		_hint.Text = "Aperte qualquer tecla para guardar";
		GetNode<AudioStreamPlayer>("Som").Play();
		_shownAt = Time.GetTicksMsec();
		_time = 0;
		_open = true;
		Visible = true;
		Layout();
		GetTree().Paused = true;
	}

	// O item fica grande à esquerda (até 80% da altura, sempre em escala inteira para a pixel art ficar nítida)
	// e o nome e a descrição à direita, como na tela de itens do Resident Evil.
	private Vector2 _itemCenter;

	private void Layout()
	{
		Vector2 screen = GetViewport().GetVisibleRect().Size;
		Vector2 tex = _item.Texture.GetSize();
		float scale = Mathf.Max(1, Mathf.Floor(Mathf.Min(screen.Y * 0.8f / tex.Y, screen.X * 0.45f / tex.X)));
		Vector2 size = tex * scale;
		_item.Size = size;
		_item.PivotOffset = size / 2;
		_itemCenter = new Vector2(screen.X * 0.32f, screen.Y * 0.5f);
		_item.Position = _itemCenter - size / 2;

		float left = screen.X * 0.56f, width = screen.X * 0.38f;
		PlaceAt(_header, left, screen.Y * 0.30f, width, 26);
		PlaceAt(_title, left, screen.Y * 0.30f + 28, width, 48);
		PlaceAt(_text, left, screen.Y * 0.30f + 84, width, 200);
		PlaceAt(_hint, 0, screen.Y - 34, screen.X, 24);
		foreach (Label l in new[] { _header, _title, _text }) l.HorizontalAlignment = HorizontalAlignment.Left;
	}

	private static void PlaceAt(Label label, float x, float y, float w, float h)
	{
		label.Position = new Vector2(x, y);
		label.Size = new Vector2(w, h);
	}

	private void Place(Label label, float y, float height, float margin = 40)
	{
		Vector2 screen = GetViewport().GetVisibleRect().Size;
		label.Position = new Vector2(margin, y);
		label.Size = new Vector2(screen.X - margin * 2, height);
	}

	public override void _Process(double delta)
	{
		if (!_open) return;
		float dt = (float)delta;
		_time += dt;

		// Giro falso em 3D: o item balança em torno do eixo vertical e flutua um pouco.
		float swing = Mathf.Sin(_time * 0.9f) * 0.55f;
		_item.Scale = new Vector2(Mathf.Max(0.35f, Mathf.Cos(swing)), 1f) * Mathf.Min(1f, 0.85f + _time * 0.6f);
		_item.Position = _itemCenter - _item.Size / 2 + new Vector2(0, Mathf.Sin(_time * 1.7f) * 4f);
		_shine.SetShaderParameter("shine_pos", Mathf.PosMod(_time * 0.45f, 2.2f) - 0.4f);

		float a = Mathf.Min(1f, _time / FadeTime);
		_background.Color = new Color(_background.Color, 0.9f * a);
		_item.Modulate = new Color(1, 1, 1, a);
	}

	public override void _Input(InputEvent @event)
	{
		if (!_open || Time.GetTicksMsec() - _shownAt < MinShowTimeMs) return;
		bool pressed = (@event is InputEventKey key && key.Pressed && !key.Echo) || (@event is InputEventMouseButton mouse && mouse.Pressed);
		if (!pressed) return;

		GetViewport().SetInputAsHandled();
		_open = false;
		Visible = false;
		GetTree().Paused = false;
		EmitSignal(SignalName.Closed);
	}
}
