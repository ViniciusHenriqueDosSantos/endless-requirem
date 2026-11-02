using Godot;

// Tudo o que muda de um chefe para outro. Cada chefe é um arquivo .tres
// (ex.: tia_cantina.tres), igual às armas.
[GlobalClass]
public partial class BossConfig : Resource
{
	[Export] public string Name = "Chefe";
	[Export] public Color BodyColor = new(0.9f, 0.5f, 0.3f);

	// Vida total, dividida em partes iguais (uma por ciclo).
	[Export] public int Health = 60;
	[Export] public int Cycles = 3;

	// Quanto tempo o escudo fica desligado depois que os 4 alvos caem.
	[Export] public float ShieldDownTime = 8f;
	[Export] public int TargetHealth = 6;

	// Leque de projéteis.
	[Export] public string ProjectileName = "projétil";
	[Export] public Color ProjectileColor = new(1f, 0.75f, 0.3f);
	[Export] public int Pellets = 5;
	[Export] public float SpreadDegrees = 14f;
	[Export] public float ProjectileSpeed = 260f;
	[Export] public float FireInterval = 1.3f;

	// Durante a luta toda: chama um fantasma a cada SummonInterval segundos,
	// até SummonMaxAlive ao mesmo tempo. 0 = não chama.
	[Export] public float SummonInterval = 0f;
	[Export] public int SummonMaxAlive = 4;

	// No começo do último ciclo, chama SummonCount fantasmas de uma vez.
	[Export] public bool SummonOnLastCycle;
	[Export] public int SummonCount = 4;

	// Velocidade andando de um lado para o outro (0 = parado). Quanto anda é definido no chefe (MoveRange).
	[Export] public float MoveSpeed = 0f;

	[Export] public int ScoreValue = 500;
}
