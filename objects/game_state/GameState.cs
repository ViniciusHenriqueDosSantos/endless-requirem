using System.Collections.Generic;

// O que o jogador leva de uma fase para a outra: armas conquistadas, pontuação e tempo.
// Fica guardado na memória enquanto o jogo está aberto (não é salvamento em arquivo).
public static class GameState
{
	public static bool HasSave { get; private set; }
	public static int Score { get; private set; }
	public static float Time { get; private set; }

	// Posição de cada arma conquistada na lista Player.Weapons.
	private static readonly List<int> _ownedWeapons = new();

	// Começo de um jogo novo (chamado pela Fase 1).
	public static void Clear()
	{
		HasSave = false;
		Score = 0;
		Time = 0;
		_ownedWeapons.Clear();
	}

	// Guarda o estado do jogador ao sair de uma fase.
	public static void Save(Player player, float time)
	{
		_ownedWeapons.Clear();
		foreach (Weapon weapon in player.OwnedWeapons)
			_ownedWeapons.Add(player.Weapons.IndexOf(weapon));

		Score = player.Score;
		Time = time;
		HasSave = true;
	}

	// Devolve ao jogador o que foi guardado.
	public static void Load(Player player)
	{
		if (HasSave)
			player.RestoreState(_ownedWeapons, Score);
	}
}
