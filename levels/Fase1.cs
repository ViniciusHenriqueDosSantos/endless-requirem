using Godot;

// Roteiro da Fase 1 (Entrada, RU e Cantina).
// Liga os acontecimentos da fase: o que destranca o quê e qual é o objetivo na tela.
// As peças (salas, portas, coletáveis) ficam na cena levels/fase_1.tscn.
//
// Ordem: pilhas de papel (crachá) -> catraca -> RU (marmita + micro-ondas, sobreviver)
//        -> corredor assombrado -> Cantina -> elevador.
public partial class Fase1 : Node2D
{
	[Export] public Player Player;
	[Export] public Hud Hud;
	[Export] public MessageScreen Screen;

	[Export] public Pickup Cracha;
	[Export] public Node2D Pilhas;
	[Export] public Door Catraca;
	[Export] public Room Ru;
	[Export] public Pickup Marmita;
	[Export] public ItemSlot Microondas;
	[Export] public Room Corredor;
	[Export] public Room Cantina;
	[Export] public Door CantinaPorta1;
	[Export] public Door CantinaPorta2;
	[Export] public Door PortaElevador;
	[Export] public AreaTrigger Elevador;

	private Vector2 _startPosition;
	private Room _lastRoom;
	private float _time;
	private bool _finished;
	private bool _badgeRevealed;

	public override void _Ready()
	{
		_startPosition = Player.GlobalPosition;
		Player.Died += OnPlayerDied;
		Screen.Closed += OnScreenClosed;

		HideBadgeInRandomPile();
		Cracha.PickedUp += _ => Hud.SetObjective("Passe pela catraca da portaria");
		Catraca.Unlocked += () => Hud.SetObjective("Vá ao RU (porta à esquerda do corredor)");
		Ru.Entered += OnRuEntered;
		Marmita.PickedUp += _ => Hud.SetObjective("Esquente a marmita no micro-ondas");
		Microondas.Used += () => Ru.StartFight();
		Ru.Cleared += OnRuCleared;
		Corredor.Cleared += OnCorredorCleared;
		Cantina.Entered += OnCantinaEntered;
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
		Hud.SetObjective("Entre na Cantina");
	}

	private void OnCantinaEntered()
	{
		if (!PortaElevador.IsLocked) return;

		// Provisório: a Tia da Cantina chega na MVP 2b. Por enquanto, o elevador já abre.
		Hud.ShowMessage("A Tia da Cantina chega na MVP 2b. O elevador já está liberado.", 5f);
		PortaElevador.Unlock();
		Hud.SetObjective("Vá ao elevador (corredor à esquerda do balcão)");
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
			"Aperte qualquer tecla para jogar de novo");
	}

	private void OnPlayerDied()
	{
		Screen.Open("REPROVADO", "Os fantasmas venceram desta vez.\nVocê volta para o começo deste trecho.");
	}

	private void OnScreenClosed()
	{
		if (_finished)
			GetTree().ReloadCurrentScene();
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
			}

			if (_lastRoom.RespawnPoint != null)
				position = _lastRoom.RespawnPoint.GlobalPosition;
		}

		Player.Revive(position);
	}

	private void ResetRu()
	{
		Player.RemoveItem("marmita");
		Marmita.Reset();
		Microondas.Reset();
		Hud.SetObjective("Pegue uma marmita no balcão");
	}
}
