using Godot;

// Bloco de cenário provisório (mesa, balcão, letreiro, lixeira...).
// A origem fica na BASE do objeto (visão 3/4): o desenho sobe a partir dela,
// e a colisão ocupa só a faixa de baixo (Depth), para dar a sensação de profundidade.
// Aparece no editor também, para facilitar a montagem das salas.
//
// Móveis ficam na camada de colisão 2: o jogador e os tiros batem neles,
// mas os fantasmas atravessam (são fantasmas!). Paredes e portas ficam na camada 1.
//
// Cobertura de mão única:
//   - móvel baixo (mesa, sinuca, balcão): o tiro do jogador passa por cima;
//     os projéteis do chefe param nele; fantasmas atravessam devagar.
//   - móvel alto (Tall ligado: prédio, guarita): bloqueia todos os tiros.
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

	// Miolo de outra cor (ex.: jardim verde com borda de concreto). Transparente = sem miolo.
	private Color _innerColor = new(0, 0, 0, 0);
	[Export] public Color InnerColor { get => _innerColor; set { _innerColor = value; QueueRedraw(); } }
	private const float InnerBorder = 14f;

	// Sprite em pixel art (1 pixel da arte = PixelScale pixels do jogo). Com sprite, o bloco colorido
	// e o texto não aparecem: o desenho fica alinhado pela base, centralizado. Pode ser mais alto que Size
	// (ex.: palmeira saindo do jardim); a colisão continua vindo de Size e Depth.
	private Texture2D _texture;
	[Export] public Texture2D Texture { get => _texture; set { _texture = value; QueueRedraw(); } }
	public const float PixelScale = 4f;

	// Desligado: o jogador e os tiros passam por cima/por trás.
	[Export] public bool Solid = true;

	// Alto: bloqueia também os tiros do jogador.
	[Export] public bool Tall;

	// Balcão de onde o chefe atira: os projéteis dele atravessam este móvel.
	[Export] public bool BossCounter;

	// Ligado: fantasmas também não atravessam (balcões). Usa a camada 4 (bit 8),
	// a mesma das paredes de fora do mapa.
	[Export] public bool BlocksGhosts;
	public const uint GhostBlockerLayer = 8;

	// Camada de colisão dos móveis (ver comentário no topo). A camada 2 vale o bit 2,
	// então o mesmo número serve para CollisionLayer e para SetCollisionMaskValue.
	public const uint FurnitureLayer = 2;

	// Retângulo do desenho no mundo. Os projéteis do chefe param ao entrar nele.
	public Rect2 GlobalVisualRect => new(GlobalPosition + new Vector2(-Size.X / 2, -Size.Y), Size);

	// Retângulo da base no mundo (onde fica a colisão). Fantasmas dentro dele andam devagar.
	public Rect2 GlobalFootRect => new(GlobalPosition + new Vector2(-Size.X / 2, -Depth), new Vector2(Size.X, Depth));

	public override void _Ready()
	{
		QueueRedraw();
		if (Engine.IsEditorHint() || !Solid) return;

		AddToGroup("obstacles");
		AddToGroup("cover");
		CollisionLayer = BlocksGhosts ? FurnitureLayer | GhostBlockerLayer : FurnitureLayer;
		CollisionMask = 0;
		AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(Size.X, Depth) },
			Position = new Vector2(0, -Depth / 2),
		});
	}

	public override void _Draw()
	{
		Rect2 body = new(-Size.X / 2, -Size.Y, Size.X, Size.Y);

		if (Texture != null)
		{
			Vector2 size = Texture.GetSize() * PixelScale;
			DrawTextureRect(Texture, new Rect2(-size.X / 2, -size.Y, size.X, size.Y), false);
			return;
		}

		if (Color.A > 0)
		{
			DrawRect(body, Color);
			float topHeight = Mathf.Min(12f, Size.Y * 0.25f);
			DrawRect(new Rect2(body.Position, new Vector2(Size.X, topHeight)), Color.Lightened(0.25f));
			DrawRect(body, Color.Darkened(0.5f), false, 2f);
		}

		if (InnerColor.A > 0)
			DrawRect(body.Grow(-InnerBorder), InnerColor);

		if (string.IsNullOrEmpty(Text)) return;

		Font font = ThemeDB.FallbackFont;
		int lines = Text.Split('\n').Length;
		float lineHeight = font.GetHeight(FontSize);
		float y = body.Position.Y + (Size.Y - lines * lineHeight) / 2 + font.GetAscent(FontSize);
		DrawMultilineString(font, new Vector2(body.Position.X, y), Text, HorizontalAlignment.Center, Size.X, FontSize, -1, TextColor);
	}
}
