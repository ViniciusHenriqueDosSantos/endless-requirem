using Godot;

// Roteiro da Fase 3 (4º andar: laboratórios). As peças ficam em levels/fase_3.tscn (gerada por tools/levelgen/gen_fase3.py).
//
// ESBOÇO (MVP 4a): por enquanto só dá para andar pela planta.
//   entrada pela escada (embaixo à direita) -> corredor -> Laboratório 1 (esquerda) -> Laboratório 2 (direita)
//   -> hall do outro elevador (não usado) -> área de convivência (tatame, ping-pong) no alto.
// As lutas, o chefe e o fim da fase entram nas próximas partes, depois de combinar o roteiro.
public partial class Fase3 : LevelBase
{
	[Export] public Room Lab1;
	[Export] public Room Lab2;
	[Export] public Room Convivencia;

	protected override void SetupDefaultLoadout()
	{
		// Aberta direto (F4): todas as armas, como se tivesse vencido a Fase 2.
		for (int i = 1; i < Player.Weapons.Count; i++)
			Player.AddWeapon(Player.Weapons[i]);
	}

	public override void _Ready()
	{
		base._Ready();

		TrackRooms(Lab1, Lab2, Convivencia);
		Hud.SetObjective("4º andar: explore os laboratórios");
		Hud.ShowMessage("4º andar. Os computadores dos laboratórios ainda estão ligados...", 4f);
	}
}
