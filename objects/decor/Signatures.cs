using Godot;

// Mãos pintadas e nomes dos formandos na parede (decoração do corredor da Fase 2, como nas fotos do ICT:
// as paredes do corredor ficam cobertas, do começo ao fim, por mãos de tinta colorida com o nome embaixo).
// Desenha dentro do retângulo Size (a partir da origem, para baixo e à direita): cada marca é uma mão em pixel art
// e o nome do dono na mesma cor, uma depois da outra, em duas fileiras desencontradas.
// Facing diz de que lado está o corredor, para o texto ficar de frente para quem passa:
//   Down = parede de cima (corredor embaixo), Right = parede da esquerda, Left = parede da direita.
// O Seed faz cada trecho de parede ficar sempre igual (não muda a cada partida).
[Tool]
public partial class Signatures : Node2D
{
	public enum Side { Down, Right, Left }

	private Vector2 _size = new(64, 256);
	private int _seed = 1;
	private Side _facing = Side.Right;

	[Export] public Vector2 Size { get => _size; set { _size = value; QueueRedraw(); } }
	[Export] public int Seed { get => _seed; set { _seed = value; QueueRedraw(); } }
	[Export] public Side Facing { get => _facing; set { _facing = value; QueueRedraw(); } }

	// Espaço livre entre um nome e o próximo na mesma fileira (pixels).
	[Export] public float Gap = 4f;

	private const int Rows = 2;
	private const int MinFont = 8;
	private const int MaxFont = 9;
	private const float HandPixel = 3f;

	// Mão aberta (6x5), como as das fotos: quatro dedos, o polegar de lado e a palma.
	private static readonly string[] Hand =
	{
		".1.1.1",
		".1.1.1",
		"111111",
		".11111",
		"..111.",
	};

	private static readonly string[] Names =
	{
		"Ana", "João P.", "Vini", "Rafa", "Bia", "Lucas", "Mari", "Gabi", "Pedro", "Júlia", "Caio", "Lari",
		"Thi", "Duda", "Léo", "Nina", "Igor", "Malu", "Davi", "Sofia", "Enzo", "Lívia", "Gui", "Manu",
		"CC 2039", "BCT 41", "Formei!", "Fui!", "T. 2040", "Biomed", "Eng. 38", "Valeu SJC", "Kobe", "Netes",
		"Silva", "Hans", "Joy", "Edu", "Alan", "Pedrão", "Isa", "Manu Z.", "Vitor F.", "Lais", "Jenifer", "Tetsu",
	};

	private static readonly Color[] Inks =
	{
		new(1f, 0.35f, 0.35f), new(1f, 0.65f, 0.2f), new(1f, 0.92f, 0.3f), new(0.45f, 0.95f, 0.45f),
		new(0.35f, 0.85f, 1f), new(0.6f, 0.6f, 1f), new(0.95f, 0.45f, 0.95f), new(1f, 1f, 1f),
	};

	public override void _Ready() => QueueRedraw();

	public override void _Draw()
	{
		RandomNumberGenerator rng = new() { Seed = (ulong)Seed };
		Font font = ThemeDB.FallbackFont;

		// Desenha num sistema "deitado": eixo X = ao longo da parede, eixo Y = atravessando a parede,
		// e depois gira para o lado certo. Assim o pé das letras fica virado para o corredor.
		float length = Facing == Side.Down ? Size.X : Size.Y;
		float thickness = Facing == Side.Down ? Size.Y : Size.X;
		Transform2D turn = Facing switch
		{
			Side.Right => new Transform2D(-Mathf.Pi / 2, new Vector2(0, Size.Y)),   // lê de baixo para cima
			Side.Left => new Transform2D(Mathf.Pi / 2, new Vector2(Size.X, 0)),     // lê de cima para baixo
			_ => Transform2D.Identity,
		};

		float rowHeight = thickness / Rows;
		for (int row = 0; row < Rows; row++)
		{
			float cursor = rng.RandfRange(2f, Gap) + row * 14f;   // fileiras desencontradas
			while (true)
			{
				string name = Names[rng.RandiRange(0, Names.Length - 1)];
				int fontSize = rng.RandiRange(MinFont, MaxFont);
				float nameWidth = font.GetStringSize(name, HorizontalAlignment.Left, -1, fontSize).X;
				float width = Mathf.Max(nameWidth, HandPixel * 6);
				if (cursor + width > length - 2f) break;

				Color ink = Inks[rng.RandiRange(0, Inks.Length - 1)];
				float top = rowHeight * row + 1f;
				float tilt = rng.RandfRange(-0.12f, 0.12f);
				Vector2 center = new(cursor + width / 2, top + rowHeight / 2);

				DrawSetTransformMatrix(turn * new Transform2D(tilt, center));
				// a mão (dedos para longe do corredor), com falhas de tinta
				Vector2 origin = new(-HandPixel * 3, -rowHeight / 2 + 1);
				for (int y = 0; y < Hand.Length; y++)
					for (int x = 0; x < Hand[y].Length; x++)
						if (Hand[y][x] == '1' && rng.Randf() > 0.08f)
							DrawRect(new Rect2(origin + new Vector2(x, y) * HandPixel, Vector2.One * HandPixel), ink);
				// o nome embaixo da mão
				float baseline = -rowHeight / 2 + Hand.Length * HandPixel + fontSize * 0.75f;
				DrawString(font, new Vector2(-nameWidth / 2, baseline), name, HorizontalAlignment.Left, -1, fontSize, ink);

				cursor += width + Gap + rng.RandfRange(0f, Gap);
			}
		}
		DrawSetTransformMatrix(Transform2D.Identity);
	}
}
