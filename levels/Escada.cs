using Godot;
using System.Collections.Generic;

// Escada (fim da Fase 2), vista de cima: uma volta só, em torno do vão da escada.
// O jogador dá voltas no mesmo anel; cada volta completa sobe um andar (2º -> 3º -> 4º).
// O contador na tela mostra o andar e quanto falta da volta. No 4º andar, a porta de saída abre.
// Os fantasmas surgem sem parar e cortam caminho pelo vão do meio.
// Peças em levels/escada.tscn.
public partial class Escada : LevelBase
{
	[Export] public Room Subida;

	// Nó com os 4 marcos da volta (AreaTrigger), na ordem da subida: esquerda, cima, direita, baixo.
	[Export] public Node2D Marcos;
	[Export] public Door PortaSaida;
	[Export] public AreaTrigger Saida;

	// Próxima cena: a Fase 3 ainda não existe; por enquanto volta ao começo do jogo.
	private const string NextScene = "res://levels/fase_1.tscn";
	private const int FirstFloor = 2;
	private const int LastFloor = 4;

	private readonly List<AreaTrigger> _marks = new();
	private int _floor = FirstFloor;
	private int _nextMark;
	private int _marksPassed;

	protected override void SetupDefaultLoadout()
	{
		// Aberta direto (F6 ou atalho): todas as armas, como se tivesse vencido a Fase 2.
		for (int i = 1; i < Player.Weapons.Count; i++)
			Player.AddWeapon(Player.Weapons[i]);
	}

	public override void _Ready()
	{
		base._Ready();

		foreach (Node child in Marcos.GetChildren())
		{
			if (child is not AreaTrigger mark) continue;
			int index = _marks.Count;
			mark.PlayerEntered += () => OnMarkPassed(index);
			_marks.Add(mark);
		}

		TrackRooms(Subida);
		Saida.PlayerEntered += OnReachedTop;

		ResetClimb();
		Hud.SetObjective("Suba a escada até o 4º andar (siga as setas)");
		Hud.ShowMessage("O elevador quebrou. Só resta a escada... e eles sabem disso.", 4f);
	}

	private void ResetClimb()
	{
		_floor = FirstFloor;
		_nextMark = 0;
		_marksPassed = 0;
		foreach (AreaTrigger mark in _marks)
			mark.ResetTrigger();
		PortaSaida.Lock();
		ApplyDifficulty();
		UpdateCounter();
	}

	private void OnMarkPassed(int index)
	{
		// Cada marco só dispara de novo depois de passar por outro.
		for (int i = 0; i < _marks.Count; i++)
			if (i != index)
				_marks[i].ResetTrigger();

		if (index != _nextMark)
		{
			if (index == (_nextMark + 2) % _marks.Count)
				Hud.ShowMessage("Você está descendo! A escada sobe pelo outro lado.", 2.5f);
			return;
		}

		_marksPassed++;
		_nextMark = (_nextMark + 1) % _marks.Count;

		// A primeira passagem pelo marco 0 só começa a contagem; cada volta completa depois sobe um andar.
		if (_marksPassed > 1 && index == 0 && _floor < LastFloor)
		{
			_floor++;
			ApplyDifficulty();
			if (_floor == LastFloor)
			{
				PortaSaida.Unlock();
				Hud.ShowMessage("4º ANDAR! A porta lá em cima abriu.", 3.5f);
			}
			else
			{
				Hud.ShowMessage($"{_floor}º andar! Mais fantasmas subindo atrás de você...", 3f);
			}
		}

		UpdateCounter();
	}

	// Mais fantasmas a cada andar; Fast a partir do 3º.
	private void ApplyDifficulty()
	{
		bool firstFloor = _floor == FirstFloor;
		Subida.SurviveSpawnInterval = firstFloor ? 1.5f : 0.9f;
		Subida.SurviveMaxAlive = firstFloor ? 5 : 8;
		Subida.FastChance = firstFloor ? 0 : 45;
	}

	// "3º ANDAR  (50%)": andar atual e quanto da volta já foi feito.
	private void UpdateCounter()
	{
		if (_floor >= LastFloor)
		{
			Hud.SetCounter($"{LastFloor}º ANDAR");
			return;
		}

		int quarters = _marksPassed == 0 ? 0 : (_marksPassed - 1) % _marks.Count;
		Hud.SetCounter($"{_floor}º ANDAR  ({quarters * 100 / _marks.Count}%)");
	}

	private void OnReachedTop()
	{
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

		// Seta: próximo marco da volta, ou a porta de saída no 4º andar.
		if (_floor >= LastFloor)
			Hud.PointTo(PortaSaida.GlobalPosition + new Vector2(0, -40), "SAÍDA");
		else if (_marks.Count > 0)
			Hud.PointTo(_marks[_nextMark].GlobalPosition + _marks[_nextMark].Size / 2, "SUBA");
	}

	protected override void OnBeforeRespawn()
	{
		// Reprovado: recomeça a subida do 2º andar.
		ResetClimb();
	}
}
