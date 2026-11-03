using Godot;

// Assinaturas dos alunos rabiscadas na parede (decoração do corredor da Fase 2).
// Desenha nomes curtos, coloridos e tortos dentro do retângulo Size (a partir da origem, para baixo e à direita).
// O Seed faz cada trecho de parede ficar sempre igual (não muda a cada partida).
[Tool]
public partial class Signatures : Node2D
{
	private Vector2 _size = new(64, 256);
	private int _seed = 1;

	[Export] public Vector2 Size { get => _size; set { _size = value; QueueRedraw(); } }
	[Export] public int Seed { get => _seed; set { _seed = value; QueueRedraw(); } }

	// Quantos pixels quadrados de parede para cada assinatura (menor = mais cheio).
	[Export] public float AreaPerSignature = 1400f;

	private static readonly string[] Names =
	{
		"Ana", "João P.", "Vini", "Rafa", "Bia", "Lucas", "Mari", "Gabi", "Pedro", "Júlia", "Caio", "Lari",
		"Thi", "Duda", "Léo", "Nina", "Igor", "Malu", "Davi", "Sofia", "Enzo", "Lívia", "Gui", "Manu",
		"CC 2039", "BCT 41", "Formei!", "<3", "Fui!", "T. 2040", "Biomed", "Eng. 38", "Valeu SJC", "★",
	};

	private static readonly Color[] Inks =
	{
		new(1f, 0.35f, 0.35f), new(1f, 0.65f, 0.2f), new(1f, 0.92f, 0.3f), new(0.45f, 0.95f, 0.45f),
		new(0.35f, 0.85f, 1f), new(0.55f, 0.55f, 1f), new(0.95f, 0.45f, 0.95f), new(1f, 1f, 1f),
	};

	public override void _Ready() => QueueRedraw();

	public override void _Draw()
	{
		RandomNumberGenerator rng = new() { Seed = (ulong)Seed };
		Font font = ThemeDB.FallbackFont;
		bool vertical = Size.Y > Size.X;
		int count = Mathf.Max(1, Mathf.RoundToInt(Size.X * Size.Y / AreaPerSignature));

		for (int i = 0; i < count; i++)
		{
			string name = Names[rng.RandiRange(0, Names.Length - 1)];
			Color ink = Inks[rng.RandiRange(0, Inks.Length - 1)];
			int fontSize = rng.RandiRange(9, 13);
			Vector2 at = new(rng.RandfRange(6, Size.X - 6), rng.RandfRange(6, Size.Y - 6));
			float angle = rng.RandfRange(-0.35f, 0.35f) + (vertical && rng.Randf() < 0.6f ? -Mathf.Pi / 2 : 0f);

			DrawSetTransform(at, angle, Vector2.One);
			float width = font.GetStringSize(name, HorizontalAlignment.Left, -1, fontSize).X;
			DrawString(font, new Vector2(-width / 2, fontSize / 3f), name, HorizontalAlignment.Left, -1, fontSize, ink);
			// risquinho embaixo, como em assinatura de verdade
			if (rng.Randf() < 0.5f)
				DrawLine(new Vector2(-width / 2, fontSize / 2f + 2), new Vector2(width / 2 + 4, fontSize / 2f), ink, 1.2f);
		}
		DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
	}
}
