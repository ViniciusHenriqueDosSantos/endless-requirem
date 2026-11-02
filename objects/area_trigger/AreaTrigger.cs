using Godot;

// Área que avisa quando o jogador entra (ex.: o elevador no fim da fase).
// Coloque o nó no canto superior esquerdo da área e ajuste Size.
// Dispara uma vez só.
[Tool]
public partial class AreaTrigger : Area2D
{
	[Signal] public delegate void PlayerEnteredEventHandler();

	private Vector2 _size = new(256, 256);
	private Color _color = new(0.2f, 1f, 0.55f, 0.25f);
	private string _text = "";

	[Export] public Vector2 Size { get => _size; set { _size = value; QueueRedraw(); } }
	[Export] public Color Color { get => _color; set { _color = value; QueueRedraw(); } }
	[Export] public string Text { get => _text; set { _text = value; QueueRedraw(); } }

	private bool _fired;

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = Size },
			Position = Size / 2,
		});
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_fired || body is not Player) return;

		_fired = true;
		EmitSignal(SignalName.PlayerEntered);
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(Vector2.Zero, Size), Color);
		if (string.IsNullOrEmpty(Text)) return;

		Font font = ThemeDB.FallbackFont;
		DrawString(font, new Vector2(0, Size.Y / 2 + 8), Text, HorizontalAlignment.Center, Size.X, 22,
			new Color(0.75f, 1f, 0.85f));
	}
}
