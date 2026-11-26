using System.Collections.Generic;

// O que o jogador leva de uma fase para a outra: armas conquistadas, pontuação e tempo.
// Fica guardado na memória enquanto o jogo está aberto (não é salvamento em arquivo).
public static class GameState
{
	// ===== Escolhas do menu (valem para o jogo todo; não são apagadas por Clear) =====
	public enum Difficulty { Facil, Medio, Dificil }

	// Médio = os números originais do jogo. Fácil e Difícil multiplicam esses números.
	public static Difficulty Level = Difficulty.Medio;

	// Personagem escolhido no menu: o estudante ou a estudante.
	public static bool PlayGirl;

	public static string PlayerSpritePath => PlayGirl
		? "res://sprites/characters/estudante_garota.png"
		: "res://sprites/characters/estudante.png";

	public static string DifficultyName => Level switch
	{
		Difficulty.Facil => "Fácil",
		Difficulty.Dificil => "Difícil",
		_ => "Médio",
	};

	private static float Pick(float facil, float medio, float dificil) => Level switch
	{
		Difficulty.Facil => facil,
		Difficulty.Dificil => dificil,
		_ => medio,
	};

	// vida dos fantasmas comuns
	public static float EnemyHealthScale => Pick(0.7f, 1f, 1.35f);
	// velocidade dos fantasmas comuns
	public static float EnemySpeedScale => Pick(0.85f, 1f, 1.12f);
	// quantos fantasmas por onda / vivos ao mesmo tempo nas salas de sobrevivência
	public static float EnemyCountScale => Pick(0.7f, 1f, 1.3f);
	// tempo das salas de sobrevivência (RU)
	public static float SurviveTimeScale => Pick(0.8f, 1f, 1.15f);
	// vida dos chefes (Tia da Cantina...)
	public static float BossHealthScale => Pick(0.7f, 1f, 1.3f);
	// corações a mais (ou a menos) do jogador
	public static int PlayerHealthBonus => (int)Pick(3, 0, -1);
	// velocidade da escada desabando
	public static float EscadaSpeedScale => Pick(0.88f, 1f, 1.1f);
	// velocidade dos fantasmas do Pac-Man
	public static float PacManGhostScale => Pick(0.88f, 1f, 1.08f);

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
