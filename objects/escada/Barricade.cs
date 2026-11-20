using Godot;

// Barricada da escada: uma pilha de cadeiras, mesas, caixas e entulho fechando a faixa toda.
// Só o Canhão derruba de verdade; a Escopeta e o Laser fazem pouco dano (Weapon.BarricadeDamage).
// O jogador e os tiros batem nela; os fantasmas passam por cima (eles atravessam o que não é parede de fora).
// Criada pelo roteiro da escada (Escada.cs), já do tamanho da faixa.
public partial class Barricade : StaticBody2D
{
	[Signal] public delegate void DestroyedEventHandler(Barricade barricade);

	// Tamanho do bloqueio (pixels), centrado na posição.
	public Vector2 Size = new(192, 48);
	public float Health = 6f;
	public int Seed;

	private const float PixelScale = 4f;
	private const float HitFlashTime = 0.08f;

	private static readonly string[] PieceSprites =
	{
		"res://sprites/props/fase1/cadeira_caida.png", "res://sprites/props/fase2/cadeira_espera_caida.png",
		"res://sprites/props/fase1/entulho.png", "res://sprites/props/fase2/caixa0.png",
		"res://sprites/props/fase2/caixa1.png", "res://sprites/props/fase2/mesa_sala.png",
	};

	private float _maxHealth;
	private float _flash;
	private bool _destroyed;
	private readonly System.Collections.Generic.List<(Texture2D tex, Vector2 at, float angle)> _pieces = new();

	public bool IsDestroyed => _destroyed;

	public override void _Ready()
	{
		_maxHealth = Health;
		AddToGroup("obstacles");
		CollisionLayer = 1;
		CollisionMask = 0;
		AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = Size } });

		// Peças espalhadas ao longo do bloqueio, sempre iguais para o mesmo Seed.
		RandomNumberGenerator rng = new() { Seed = (ulong)(Seed * 7919 + 3) };
		bool vertical = Size.Y > Size.X;
		float length = vertical ? Size.Y : Size.X;
		int count = Mathf.Max(3, Mathf.RoundToInt(length / 34f));
		for (int i = 0; i < count; i++)
		{
			Texture2D tex = GD.Load<Texture2D>(PieceSprites[rng.RandiRange(0, PieceSprites.Length - 1)]);
			float t = (i + 0.5f) / count - 0.5f;
			Vector2 along = vertical ? new Vector2(rng.RandfRange(-8, 8), t * length) : new Vector2(t * length, rng.RandfRange(-8, 8));
			_pieces.Add((tex, along, rng.RandfRange(-0.5f, 0.5f)));
		}
		QueueRedraw();
	}

	// Tiro de uma arma: o dano vem multiplicado pelo quanto aquela arma serve contra barricada.
	public void TakeHit(Weapon weapon, Vector2 direction)
	{
		if (_destroyed) return;

		Health -= weapon.Damage * weapon.BarricadeDamage;
		_flash = HitFlashTime;
		QueueRedraw();
		if (Health <= 0)
			Break();
	}

	private void Break()
	{
		_destroyed = true;
		(GetTree().GetFirstNodeInGroup("player") as Player)?.TriggerShake(8f);
		EmitSignal(SignalName.Destroyed, this);
		QueueFree();
	}

	public override void _Process(double delta)
	{
		if (_flash <= 0) return;
		_flash -= (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		Rect2 rect = new(-Size / 2, Size);
		// sombra no chão
		DrawRect(rect.Grow(6), new Color(0, 0, 0, 0.35f));
		float damaged = _maxHealth > 0 ? 1f - Mathf.Clamp(Health / _maxHealth, 0f, 1f) : 0f;
		foreach ((Texture2D tex, Vector2 at, float angle) in _pieces)
		{
			Vector2 size = tex.GetSize() * PixelScale;
			// quanto mais danificada, mais tortas as peças ficam
			DrawSetTransform(at, angle * (1f + damaged * 1.5f), Vector2.One);
			DrawTextureRect(tex, new Rect2(-size / 2, size), false, _flash > 0 ? new Color(2f, 2f, 2f) : Colors.White);
		}
		DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
	}
}
