using Godot;

// Bloco de cenário provisório (mesa, balcão, letreiro, lixeira...).
// A origem fica na BASE do objeto (visão 3/4): o desenho sobe a partir dela,
// e a colisão ocupa só a faixa de baixo (Depth), para dar a sensação de profundidade.
// Aparece no editor também, para facilitar a montagem das salas.
[Tool]
public partial class Prop : StaticBody2D
{
	private Vector2 _size = new(128, 96);
	private float _depth = 48f;
	private Color _color = new(0.55f, 0.56f, 0.6f);
	private string _text = "";
	private int _fontSize = 16;
	private Color _textColor = new(0.08f, 0.08f, 0.1f);

	// Tamanho do desenho, em pixels.
	[Export] public Vector2 Size { get => _size; set { _size = value; QueueRedraw(); } }

	// Altura da colisão, medida a partir da base.
	[Export] public float Depth { get => _depth; set { _depth = value; QueueRedraw(); } }

	// Cor do bloco. Com transparência total, só o texto aparece (bom para letreiros).
	[Export] public Color Color { get => _color; set { _color = value; QueueRedraw(); } }

	[Export(PropertyHint.MultilineText)] public string Text { get => _text; set { _text = value; QueueRedraw(); } }
	[Export] public int FontSize { get => _fontSize; set { _fontSize = value; QueueRedraw(); } }
	[Export] public Color TextColor { get => _textColor; set { _textColor = value; QueueRedraw(); } }

	// Desligado: o jogador e os tiros passam por cima/por trás.
	[Export] public bool Solid = true;

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint() || !Solid) return;

		AddToGroup("obstacles");
		AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(Size.X, Depth) },
			Position = new Vector2(0, -Depth / 2),
		});
	}

	public override void _Draw()
	{
		Rect2 body = new(-Size.X / 2, -Size.Y, Size.X, Size.Y);

		if (Color.A > 0)
		{
			DrawRect(body, Color);
			float topHeight = Mathf.Min(12f, Size.Y * 0.25f);
			DrawRect(new Rect2(body.Position, new Vector2(Size.X, topHeight)), Color.Lightened(0.25f));
			DrawRect(body, Color.Darkened(0.5f), false, 2f);
		}

		if (string.IsNullOrEmpty(Text)) return;

		Font font = ThemeDB.FallbackFont;
		int lines = Text.Split('\n').Length;
		float lineHeight = font.GetHeight(FontSize);
		float y = body.Position.Y + (Size.Y - lines * lineHeight) / 2 + font.GetAscent(FontSize);
		DrawMultilineString(font, new Vector2(body.Position.X, y), Text, HorizontalAlignment.Center, Size.X, FontSize, -1, TextColor);
	}
}
