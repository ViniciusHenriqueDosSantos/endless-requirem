using Godot;

// Escada em espiral (fim da Fase 2): do 2º ao 4º andar, subindo e atirando.
// Os fantasmas surgem sem parar e atravessam as paredes internas da espiral.
// A cada volta, um andar a mais: mais fantasmas, e a partir do 3º entram os Fast.
// Peças em levels/escada.tscn.
public partial class Escada : LevelBase
{
	[Export] public Room Subida;
	[Export] public AreaTrigger Andar3;
	[Export] public AreaTrigger Topo;

	// Próxima cena: a Fase 3 ainda não existe; por enquanto volta ao começo do jogo.
	private const string NextScene = "res://levels/fase_1.tscn";

	private int _floor = 2;

	protected override void SetupDefaultLoadout()
	{
		// Aberta direto (F6): todas as armas, como se tivesse vencido a Fase 2.
		for (int i = 1; i < Player.Weapons.Count; i++)
			Player.AddWeapon(Player.Weapons[i]);
	}

	public override void _Ready()
	{
		base._Ready();

		TrackRooms(Subida);
		Andar3.PlayerEntered += () => SetFloor(3);
		Topo.PlayerEntered += OnReachedTop;

		SetFloor(2);
		Hud.SetObjective("Suba a escada até o 4º andar");
		Hud.ShowMessage("O elevador quebrou. Só resta a escada... e eles sabem disso.", 4f);
	}

	private void SetFloor(int floor)
	{
		_floor = floor;
		Hud.SetCounter($"{floor}º ANDAR");

		if (floor == 2)
		{
			Subida.SurviveSpawnInterval = 1.5f;
			Subida.SurviveMaxAlive = 5;
			Subida.FastChance = 0;
		}
		else
		{
			Subida.SurviveSpawnInterval = 0.9f;
			Subida.SurviveMaxAlive = 8;
			Subida.FastChance = 45;
			Hud.ShowMessage($"{floor}º andar! Mais fantasmas subindo atrás de você...", 3f);
		}
	}

	private void OnReachedTop()
	{
		Hud.SetCounter("4º ANDAR");
		Subida.FinishFight();
		Hud.HidePointer();
		FinishLevel(
			"FIM DA FASE 2",
			"Silêncio, enfim. Lá em cima, os servidores estão acordando.",
			NextScene,
			"Fase 3 em construção. Aperte qualquer tecla para voltar ao começo.");
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		// Seta para o próximo trecho da subida.
		AreaTrigger next = _floor < 3 ? Andar3 : Topo;
		Hud.PointTo(next.GlobalPosition + next.Size / 2, _floor < 3 ? "3º ANDAR" : "4º ANDAR");
	}

	protected override void OnBeforeRespawn()
	{
		// Reprovado: recomeça a subida do 2º andar.
		Andar3.ResetTrigger();
		Topo.ResetTrigger();
		SetFloor(2);
	}
}
