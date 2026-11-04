using Godot;
using System.Collections.Generic;

// Escada (fim da Fase 2), vista de cima: uma volta só, em torno do vão da escada.
// Corrida contra o tempo: suba do 2º ao 4º andar antes do relógio zerar.
//   - Cada volta completa no anel sobe um andar (o contador mostra andar e tempo).
//   - Fantasmas pesados ficam PARADOS nos degraus, bloqueando o caminho:
//     ou você gasta tempo derrubando, ou passa raspando e leva dano.
//     A cada andar novo, os que você derrubou voltam.
//   - Fantasmas rápidos perseguem e cortam caminho pelo vão do meio.
//   - A porta do 4º andar não existe (é parede) até você chegar no 4º andar.
// Tempo acabou = REPROVADO, e a subida recomeça do 2º andar.
// Peças em levels/escada.tscn.
public partial class Escada : LevelBase
{
	[Export] public Room Subida;

	// Nó com os 4 marcos da volta (AreaTrigger), na ordem da subida: esquerda, cima, direita, baixo.
	[Export] public Node2D Marcos;

	// Nó com os pontos (Marker2D) onde ficam os fantasmas pesados parados.
	[Export] public Node2D Bloqueios;
	[Export] public Door PortaSaida;
	[Export] public AreaTrigger Saida;

	// Cobre o patamar do 4º andar enquanto ele ainda "não existe".
	[Export] public CanvasItem CoberturaSaida;

	// Segundos para chegar ao 4º andar (o relógio começa quando você entra no anel).
	[Export] public float TimeLimit = 60f;

	// Próxima cena: a Fase 3 ainda não existe; por enquanto volta ao começo do jogo.
	private const string NextScene = "res://levels/fase_1.tscn";
	private const int FirstFloor = 2;
	private const int LastFloor = 4;
	private const float LowTime = 10f;

	private readonly List<AreaTrigger> _marks = new();
	private readonly List<Marker2D> _blockPoints = new();
	private readonly Dictionary<Marker2D, Enemy> _blockers = new();
	private int _floor = FirstFloor;
	private int _nextMark;
	private int _marksPassed;
	private float _timeLeft;
	private bool _running;

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

		foreach (Node child in Bloqueios.GetChildren())
			if (child is Marker2D point)
				_blockPoints.Add(point);

		TrackRooms(Subida);
		Subida.Started += OnClimbStarted;
		Saida.PlayerEntered += OnReachedTop;

		ResetClimb();
		Hud.SetObjective($"Suba até o 4º andar em {TimeLimit:0} segundos (siga as setas)");
		Hud.ShowMessage("O elevador quebrou. Só resta a escada... e o tempo está correndo.", 4f);
	}

	private void ResetClimb()
	{
		_floor = FirstFloor;
		_nextMark = 0;
		_marksPassed = 0;
		_timeLeft = TimeLimit;
		_running = false;
		foreach (AreaTrigger mark in _marks)
			mark.ResetTrigger();
		_blockers.Clear();
		SetExitVisible(false);
		ApplyDifficulty();
		UpdateCounter();
	}

	// Entrou no anel: o relógio começa e os fantasmas pesados aparecem nos degraus.
	private void OnClimbStarted()
	{
		_running = true;
		PlaceBlockers();
	}

	// Põe um fantasma pesado parado em cada ponto que estiver vazio.
	private void PlaceBlockers()
	{
		foreach (Marker2D point in _blockPoints)
		{
			if (_blockers.TryGetValue(point, out Enemy old) && IsInstanceValid(old) && !old.IsQueuedForDeletion())
				continue;

			Enemy blocker = Subida.SpawnEnemyAt(point.GlobalPosition, Enemy.Kind.Tank);
			blocker.Stationary = true;
			_blockers[point] = blocker;
		}
	}

	// A saída só "existe" no 4º andar: antes disso é parede e o patamar fica coberto.
	private void SetExitVisible(bool visible)
	{
		if (visible)
			PortaSaida.Unlock();
		else
			PortaSaida.Lock();
		CoberturaSaida.Visible = !visible;
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
				SetExitVisible(true);
				Hud.ShowMessage("4º ANDAR! Uma porta apareceu lá em cima. Corre!", 3.5f);
			}
			else
			{
				PlaceBlockers();
				Hud.ShowMessage($"{_floor}º andar! Os fantasmas pesados voltaram aos degraus...", 3f);
			}
		}

		UpdateCounter();
	}

	// Mais perseguidores e mais rápidos a cada andar. Os bloqueios contam no limite da sala.
	private void ApplyDifficulty()
	{
		bool firstFloor = _floor == FirstFloor;
		Subida.SurviveSpawnInterval = firstFloor ? 1.6f : 1.0f;
		Subida.SurviveMaxAlive = _blockPoints.Count + (firstFloor ? 4 : 6);
		Subida.FastChance = firstFloor ? 50 : 80;
	}

	// "3º ANDAR  0:42": andar atual e tempo que falta.
	private void UpdateCounter()
	{
		int seconds = Mathf.CeilToInt(Mathf.Max(_timeLeft, 0f));
		Hud.SetCounter($"{_floor}º ANDAR   {seconds / 60}:{seconds % 60:00}");
	}

	private void OnReachedTop()
	{
		_running = false;
		Subida.FinishFight();
		Hud.HidePointer();
		int seconds = Mathf.CeilToInt(_timeLeft);
		FinishLevel(
			"FIM DA FASE 2",
			$"Chegou ao 4º andar com {seconds} segundos de sobra.\nLá em cima, os servidores estão acordando.",
			NextScene,
			"Fase 3 em construção. Aperte qualquer tecla para voltar ao começo.");
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (_running)
		{
			float before = _timeLeft;
			_timeLeft -= (float)delta;
			if (before > LowTime && _timeLeft <= LowTime)
				Hud.ShowMessage("10 segundos!", 2f);
			UpdateCounter();

			if (_timeLeft <= 0)
			{
				_running = false;
				Fail("O tempo acabou! A aula no 4º andar começou sem você.\nVocê volta para o 2º andar.");
				return;
			}
		}

		// Seta: próximo marco da volta, ou a porta de saída no 4º andar.
		if (_floor >= LastFloor)
			Hud.PointTo(PortaSaida.GlobalPosition + new Vector2(0, -40), "SAÍDA");
		else if (_marks.Count > 0)
			Hud.PointTo(_marks[_nextMark].GlobalPosition + _marks[_nextMark].Size / 2, "SUBA");
	}

	protected override void OnBeforeRespawn()
	{
		// Reprovado (vida ou tempo): recomeça a subida do 2º andar.
		ResetClimb();
	}
}
