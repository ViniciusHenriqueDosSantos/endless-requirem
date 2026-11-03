using Godot;
using System.Collections.Generic;

// Roteiro da Fase 2 (Biblioteca). As peças ficam em levels/fase_2.tscn.
//
// Ordem:
//   chegada -> porta da biblioteca (fechada: falta um livro)
//   -> Achados e Perdidos (trancado: a chave está com o motorista da van)
//   -> Hall das Caronas (as cadeiras bloqueiam a saída, 3 ondas, o motorista solta a chave)
//   -> Achados e Perdidos (caixas: uma tem o livro, algumas têm fantasma)
//   -> devolução do livro -> biblioteca abre
//   -> Bibliotecário (furtividade com as luminárias, depois luta) -> Canhão + chave da escada
//   -> porta da escada -> escada até o 4º andar (levels/escada.tscn).
public partial class Fase2 : LevelBase
{
	[Export] public Door PortaBiblioteca;
	[Export] public ItemSlot Devolucao;
	[Export] public Door PortaAchados;
	[Export] public Room Hall;
	[Export] public Pickup Chave;
	[Export] public Room Achados;
	[Export] public Node2D Caixas;
	[Export] public Pickup Livro;
	[Export] public Room Biblioteca;
	[Export] public Librarian Bibliotecario;
	[Export] public Pickup Canhao;
	[Export] public Pickup ChaveEscada;
	[Export] public Door PortaEscada;
	[Export] public AreaTrigger Escada;
	[Export] public Marker2D CentroHall;

	// Cadeiras de espera perto da saída do Hall: somem quando se arrastam para bloquear a passagem.
	[Export] public Node2D CadeirasDaSaida;

	private const string EscadaScene = "res://levels/escada.tscn";
	private const int GhostBoxes = 3;
	private static readonly Vector2 PointerOffset = new(0, -40);
	private static readonly Vector2 BossPointerOffset = new(0, -60);

	private static readonly string[] BoxMessages =
	{
		"Guarda-chuva (esquecido em 2038).",
		"Calculadora científica. Sem pilha.",
		"Uma marmita. Não abra. ...Tarde demais.",
		"Carregador de um celular que nem existe mais.",
		"Moletom da Atlética, tamanho GG.",
		"Crachá de um aluno de 2044.",
		"Fone de ouvido com um lado só.",
		"Apostila de Física, toda rabiscada.",
	};

	private const string AchadosShortMessage = "ACHADOS E PERDIDOS - trancado.";
	private const string AchadosFullMessage =
		"ACHADOS E PERDIDOS - trancado. \"Chave com o motorista da van (Hall das Caronas).\"";

	private bool _libraryTold;
	private bool _achadosTold;
	private bool _keyRevealed;
	private bool _bookRevealed;
	private bool _libraryOpen;
	private bool _rewardsRevealed;
	private int _messageIndex;

	protected override void SetupDefaultLoadout()
	{
		// Aberta direto (F6): Laser + Escopeta, como se tivesse vencido a Fase 1.
		if (Player.Weapons.Count > 1)
			Player.AddWeapon(Player.Weapons[1]);
	}

	public override void _Ready()
	{
		base._Ready();

		TrackRooms(Hall, Biblioteca);

		// Sequência da história: biblioteca (falta o livro) -> Achados (trancado) -> só então o Hall tranca.
		PortaAchados.LockedMessage = AchadosShortMessage;
		PortaBiblioteca.LockedTouched += OnLibraryTold;
		PortaAchados.LockedTouched += () =>
		{
			if (_libraryTold)
				_achadosTold = true;
		};
		PortaAchados.Unlocked += () => Hud.ShowMessage("A porta rangeu e abriu. Que bagunça...", 3f);

		Hall.Entered += OnHallEntered;
		Hall.Started += OnHallStarted;
		Hall.Cleared += OnHallCleared;

		SetupBoxes();
		Devolucao.Used += OnBookReturned;

		Biblioteca.Started += () => Bibliotecario.StartFight();
		Bibliotecario.Defeated += OnLibrarianDefeated;

		Escada.PlayerEntered += () => GoToScene(EscadaScene);

		Hud.ShowMessage("2º andar. Até os fantasmas falam baixo aqui.", 4f);
	}

	// ---------- Achados e Perdidos ----------

	// Sorteia a caixa do livro e as caixas com fantasma. Muda a cada partida.
	private void SetupBoxes()
	{
		List<PaperPile> boxes = new();
		foreach (Node child in Caixas.GetChildren())
			if (child is PaperPile box)
				boxes.Add(box);

		int bookIndex = GD.RandRange(0, boxes.Count - 1);
		boxes[bookIndex].HidesItem = true;

		List<PaperPile> others = new();
		for (int i = 0; i < boxes.Count; i++)
			if (i != bookIndex)
				others.Add(boxes[i]);

		for (int i = 0; i < GhostBoxes && others.Count > 0; i++)
		{
			int pick = GD.RandRange(0, others.Count - 1);
			others[pick].HidesGhost = true;
			others.RemoveAt(pick);
		}

		foreach (PaperPile box in boxes)
			box.Destroyed += OnBoxOpened;
	}

	private void OnBoxOpened(PaperPile box)
	{
		if (box.HidesItem)
		{
			_bookRevealed = true;
			Livro.Reveal(box.GlobalPosition);
			Hud.ShowMessage("\"Cálculo I\"! Atrasado desde 2041.", 3f);
			return;
		}

		if (box.HidesGhost)
		{
			Achados.SpawnEnemyAt(box.GlobalPosition, Enemy.Kind.Normal);
			Hud.ShowMessage("Tinha um fantasma aí dentro!", 2f);
			return;
		}

		Hud.ShowMessage(BoxMessages[_messageIndex % BoxMessages.Length], 2.5f);
		_messageIndex++;
	}

	// ---------- Hall das Caronas ----------

	private void OnLibraryTold()
	{
		_libraryTold = true;
		PortaAchados.LockedMessage = AchadosFullMessage;
	}

	// O Hall só tranca depois de o jogador ter passado pela biblioteca e pelos Achados.
	private void OnHallEntered()
	{
		if (_achadosTold && !Hall.IsCleared && !Hall.IsFighting)
			Hall.StartFight();
	}

	private void OnHallStarted()
	{
		SetExitChairsVisible(false);
		Hud.ShowMessage("As cadeiras se arrastaram sozinhas e bloquearam a saída! Fantasmas apressados à vista!", 4f);
	}

	private void SetExitChairsVisible(bool visible)
	{
		foreach (Node child in CadeirasDaSaida.GetChildren())
			if (child is CanvasItem chair)
				chair.Visible = visible;
	}

	private void OnHallCleared()
	{
		SetExitChairsVisible(true);
		_keyRevealed = true;
		Vector2 drop = Hall.FinalEnemyPosition;
		if (drop == Vector2.Zero || !GhostNav.TryNearestFreeFloor(this, drop, out drop))
			drop = CentroHall.GlobalPosition;
		Chave.Reveal(drop);
		Hud.ShowMessage("O motorista deixou cair uma chave: \"ACHADOS E PERDIDOS\".", 4f);
	}

	// ---------- Biblioteca ----------

	private void OnBookReturned()
	{
		_libraryOpen = true;
		PortaBiblioteca.LockedMessage = "";
		PortaBiblioteca.Unlock();
		Hud.ShowMessage("\"20 anos de atraso. A multa... é a sua vida.\"", 4.5f);
	}

	private void OnLibrarianDefeated()
	{
		Biblioteca.FinishFight();
		Hud.HideBoss();
		RevealRewards(Bibliotecario.GlobalPosition);
		Hud.ShowMessage("O Bibliotecário caiu! Ele deixou o Canhão e uma chave.", 4f);
	}

	private void RevealRewards(Vector2 around)
	{
		_rewardsRevealed = true;
		Vector2 left = around + new Vector2(-70, 0);
		Vector2 right = around + new Vector2(70, 0);
		GhostNav.TryNearestFreeFloor(this, left, out left);
		GhostNav.TryNearestFreeFloor(this, right, out right);
		Canhao.Reveal(left);
		ChaveEscada.Reveal(right);
	}

	protected override void OnBeforeRespawn()
	{
		// Fantasmas que saíram das caixas somem quando o jogador volta.
		Achados.ResetRoom();
	}

	protected override void OnRoomReset(Room room)
	{
		if (room == Hall)
			SetExitChairsVisible(true);

		if (room == Biblioteca)
		{
			Bibliotecario.ResetBoss();
			Hud.HideBoss();
		}
	}

	// ---------- Objetivo e seta ----------

	public override void _Process(double delta)
	{
		base._Process(delta);
		UpdateObjective();
	}

	private void UpdateObjective()
	{
		if (Hall.IsFighting)
		{
			Hud.SetObjective("Vença os fantasmas do Hall das Caronas");
			Hud.HidePointer();
			return;
		}

		if (Bibliotecario.InStealth)
		{
			Hud.HideBoss();
			ShieldLamp lamp = Bibliotecario.NearestLamp(Player.GlobalPosition);
			Hud.SetObjective($"Apague as luminárias das salas de estudo sem ser visto ({Bibliotecario.LampsLeft} restantes)");
			if (lamp != null)
				Hud.PointTo(lamp.GlobalPosition + PointerOffset, "LUMINÁRIA");
			return;
		}

		if (Bibliotecario.InFight)
		{
			Hud.SetObjective("Derrote o Bibliotecário");
			Hud.SetBoss(Bibliotecario.DisplayName, Bibliotecario.HealthRatio, false);
			Hud.PointTo(Bibliotecario.GlobalPosition + BossPointerOffset, "BIBLIOTECÁRIO");
			return;
		}

		bool hasKey = Player.HasItem("chave_achados");
		bool hasBook = Player.HasItem("livro");

		if (Player.HasItem("chave_escada") || (Bibliotecario.IsDefeated && Canhao.IsCollected && ChaveEscada.IsCollected))
			Objective("Suba pela escada (porta no fim do corredor)", PortaEscada.GlobalPosition, "ESCADA");
		else if (_rewardsRevealed && !ChaveEscada.IsCollected)
			Objective("Pegue o Canhão e a chave da escada", ChaveEscada.GlobalPosition, "CHAVE");
		else if (_libraryOpen)
			Objective("Entre na biblioteca", PortaBiblioteca.GlobalPosition, "BIBLIOTECA");
		else if (hasBook)
			Objective("Devolva o livro na caixa de devolução da biblioteca", Devolucao.GlobalPosition, "DEVOLUÇÃO");
		else if (_bookRevealed)
			Objective("Pegue o livro", Livro.GlobalPosition, "LIVRO");
		else if (!PortaAchados.IsLocked)
			Objective("Atire nas caixas e encontre o livro", Achados.GlobalPosition + Achados.Size / 2, "ACHADOS");
		else if (hasKey)
			Objective("Abra os Achados e Perdidos", PortaAchados.GlobalPosition, "ACHADOS");
		else if (_keyRevealed)
			Objective("Pegue a chave", Chave.GlobalPosition, "CHAVE");
		else if (_achadosTold)
			Objective("Pegue a chave com o motorista da van (Hall das Caronas)", CentroHall.GlobalPosition, "HALL");
		else if (_libraryTold)
			Objective("Procure o livro nos Achados e Perdidos", PortaAchados.GlobalPosition, "ACHADOS");
		else
			Objective("Vá até a biblioteca", PortaBiblioteca.GlobalPosition, "BIBLIOTECA");
	}

	private void Objective(string text, Vector2 target, string label)
	{
		Hud.SetObjective(text);
		Hud.PointTo(target + PointerOffset, label);
	}
}
