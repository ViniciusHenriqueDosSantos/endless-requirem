using Godot;

// Roteiro da Fase 1 (Entrada, RU e Cantina).
// Liga os acontecimentos da fase: o que destranca o quê e qual é o objetivo na tela.
// As peças (salas, portas, coletáveis) ficam na cena levels/fase_1.tscn.
//
// Ordem: pilhas de papel (crachá) -> catraca -> RU (marmita + micro-ondas, sobreviver)
//        -> corredor assombrado -> Cantina (Tia da Cantina) -> escopeta -> elevador.
public partial class Fase1 : Node2D
{
	[Export] public Player Player;
	[Export] public Hud Hud;
	[Export] public MessageScreen Screen;

	[Export] public Pickup Cracha;
	[Export] public Node2D Pilhas;
	[Export] public Door Catraca;
	[Export] public Room Ru;
	[Export] public Door PortaEntradaRu;
	[Export] public Pickup Marmita;
	[Export] public ItemSlot Microondas;
	[Export] public Room Corredor;
	[Export] public Room Cantina;
	[Export] public Door CantinaPorta1;
	[Export] public Door CantinaPorta2;
	[Export] public Boss Tia;
	[Export] public Pickup Escopeta;
	[Export] public Door PortaElevador;
	[Export] public AreaTrigger Elevador;

	private static readonly Vector2 EscopetaDropOffset = new(0, 190);
	private static readonly Vector2 TiaPointerOffset = new(0, -60);

	private Vector2 _startPosition;
	private Room _lastRoom;
	private float _time;
	private bool _finished;
	private bool _badgeRevealed;

	public override void _Ready()
	{
		GameState.Clear();
		_startPosition = Player.GlobalPosition;
		Player.Died += OnPlayerDied;
		Screen.Closed += OnScreenClosed;

		HideBadgeInRandomPile();
		Cracha.PickedUp += _ => Hud.SetObjective("Passe pela catraca da portaria");
		Catraca.Unlocked += () => Hud.SetObjective("Vá ao RU (porta à esquerda do corredor)");
		Ru.Entered += OnRuEntered;
		Marmita.PickedUp += _ => Hud.SetObjective("Esquente a marmita no micro-ondas");
		Microondas.Used += () =>
		{
			Ru.StartFight();
			_microondasSom = new AudioStreamPlayer2D
			{
				Stream = GD.Load<AudioStream>("res://sounds/objects/microondas.ogg"),
				MaxDistance = 1200f, Attenuation = 1.5f,
			};
			Microondas.AddChild(_microondasSom);
			_microondasSom.Play();
		};
		Ru.Cleared += OnRuCleared;
		Ru.Cleared += () =>
		{
			if (IsInstanceValid(_microondasSom))
				_microondasSom.QueueFree();
			var apito = new AudioStreamPlayer { Stream = GD.Load<AudioStream>("res://sounds/objects/microondas_apito.wav") };
			AddChild(apito);
			apito.Play();
			apito.Finished += apito.QueueFree;
		};
		// Voltou ao corredor depois do RU: a entrada do RU fecha de vez.
		Corredor.Entered += () => PortaEntradaRu.Lock();
		Corredor.Cleared += OnCorredorCleared;
		Cantina.Started += () => Tia.StartFight();
		Tia.Defeated += OnTiaDefeated;
		Escopeta.PickedUp += _ => Hud.SetObjective("Vá ao elevador (corredor à esquerda do balcão)");
		Elevador.PlayerEntered += OnElevadorEntered;

		// Guarda a última sala onde começou uma luta: é nela que o jogador recomeça.
		foreach (Room room in new[] { Ru, Corredor, Cantina })
		{
			Room current = room;
			current.Started += () => _lastRoom = current;
		}

		Hud.SetObjective("Procure o crachá nas pilhas de papel (atire nelas)");
	}

	// Sorteia uma pilha de papel para esconder o crachá. Muda a cada partida.
	private void HideBadgeInRandomPile()
	{
		var piles = new System.Collections.Generic.List<PaperPile>();
		foreach (Node child in Pilhas.GetChildren())
			if (child is PaperPile pile)
				piles.Add(pile);

		piles[GD.RandRange(0, piles.Count - 1)].HidesItem = true;
		foreach (PaperPile pile in piles)
			pile.Destroyed += OnPileDestroyed;
	}

	private void OnPileDestroyed(PaperPile pile)
	{
		if (pile.HidesItem)
		{
			_badgeRevealed = true;
			Cracha.Reveal(pile.GlobalPosition);
			Hud.ShowMessage("Tem alguma coisa debaixo desses papéis!");
		}
		else if (!_badgeRevealed)
		{
			Hud.ShowMessage("Só papel velho...", 1.5f);
		}
	}

	public override void _Process(double delta)
	{
		_time += (float)delta;

		if (Ru.IsFighting)
			Hud.SetObjective($"Sobreviva até o micro-ondas apitar: {Mathf.CeilToInt(Ru.TimeLeft)} s");

		if (Tia.IsFighting)
		{
			Hud.SetBoss(Tia.DisplayName, Tia.HealthRatio, Tia.IsShielded);
			Hud.PointTo(Tia.GlobalPosition + TiaPointerOffset, "TIA");
			Hud.SetObjective(Tia.IsShielded
				? $"Destrua os alvos nos cantos para baixar o escudo ({Tia.TargetsLeft} restantes)"
				: $"Escudo baixo! Atire na Tia: {Mathf.CeilToInt(Tia.VulnerableTimeLeft)} s");
		}
	}

	private void OnRuEntered()
	{
		if (Ru.IsCleared || Ru.IsFighting || Microondas.IsUsed) return;

		Hud.SetObjective(Player.HasItem("marmita")
			? "Esquente a marmita no micro-ondas"
			: "Pegue uma marmita no balcão");
	}

	private void OnRuCleared()
	{
		Player.HealFull();
		Hud.ShowMessage("PLIM! A marmita esquentou. Vida cheia!", 4f);
		Corredor.Activate();
		Hud.SetObjective("Saia do RU e limpe o corredor");
	}

	private void OnCorredorCleared()
	{
		CantinaPorta1.Unlock();
		CantinaPorta2.Unlock();
		Hud.SetObjective("Entre na Cantina e enfrente a Tia");
	}

	private void OnTiaDefeated()
	{
		Cantina.FinishFight();
		Hud.HideBoss();
		// A escopeta cai na frente do balcão, onde o jogador alcança.
		Escopeta.Reveal(Tia.GlobalPosition + EscopetaDropOffset);
		PortaElevador.Unlock();
		Hud.ShowMessage("A Tia da Cantina caiu! Ela deixou algo para trás...", 4f);
		Hud.SetObjective("Pegue a escopeta");
	}

	private void OnElevadorEntered()
	{
		_finished = true;
		int minutes = (int)(_time / 60);
		int seconds = (int)(_time % 60);

		Screen.Open(
			"FIM DA FASE 1",
			"A Tia da Cantina caiu. O elevador ainda funciona.\nPróxima parada: Biblioteca.\n\n" +
			$"Pontuação: {Player.Score}     Tempo: {minutes}:{seconds:00}",
			"Aperte qualquer tecla para subir até a Biblioteca");
	}

	private void OnPlayerDied()
	{
		Screen.OpenFail("Os fantasmas venceram desta vez.\nVocê volta para o começo deste trecho.");
	}

	private void OnScreenClosed()
	{
		if (_finished)
		{
			// Leva as armas, a pontuação e o tempo para a Fase 2.
			GameState.Save(Player, _time);
			GetTree().ChangeSceneToFile("res://levels/fase_2.tscn");
		}
		else
			Respawn();
	}

	// Recomeça o trecho atual: a sala da última luta volta ao início.
	// O que já foi conquistado (crachá, catraca, salas limpas) continua.
	private void Respawn()
	{
		Vector2 position = _startPosition;

		if (_lastRoom != null)
		{
			if (!_lastRoom.IsCleared)
			{
				_lastRoom.ResetRoom();
				if (_lastRoom == Ru)
					ResetRu();
				if (_lastRoom == Cantina)
				{
					Tia.ResetBoss();
					Hud.HideBoss();
					Hud.SetObjective("Entre na Cantina e enfrente a Tia");
				}
			}

			if (_lastRoom.RespawnPoint != null)
				position = _lastRoom.RespawnPoint.GlobalPosition;
		}

		Player.Revive(position);
	}

	private AudioStreamPlayer2D _microondasSom;

	private void ResetRu()
	{
		if (IsInstanceValid(_microondasSom))
			_microondasSom.QueueFree();
		Player.RemoveItem("marmita");
		Marmita.Reset();
		Microondas.Reset();
		Hud.SetObjective("Pegue uma marmita no balcão");
	}
}
