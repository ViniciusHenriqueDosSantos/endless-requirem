using Godot;
using System.Collections.Generic;

// Roteiro da Fase 2 (Biblioteca). As peças ficam em levels/fase_2.tscn.
//
// Ordem:
//   chegada -> porta da biblioteca (fechada: falta um livro)
//   -> Achados e Perdidos (trancado: a chave está com o zelador)
//   -> Hall das Caronas (as cadeiras bloqueiam a saída, 3 ondas, o zelador solta a chave)
//   -> Achados e Perdidos: o livro está no armário trancado com cadeado -> tarefa 2D (LockerPuzzle, estilo Among Us:
//      girar o disco até a combinação do post-it do zelador e puxar a alavanca)
//      (as caixas do chão da sala ainda podem ser quebradas: coisas perdidas e alguns fantasmas)
//   -> devolução do livro -> biblioteca abre
//   -> Bibliotecária (furtividade: barulho atrai, empurrar a estante esmaga) -> Canhão + chave da escada
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
	// Armário dos Achados: [E] abre o puzzle das caixas; resolvido, o livro aparece na frente dele.
	[Export] public Interactable Armario;
	[Export] public Room Biblioteca;
	[Export] public Librarian Bibliotecario;
	[Export] public Pickup Canhao;
	[Export] public Pickup ChaveEscada;

	// A escada fica encostada na parede do corredor: encostar com a chave leva à cena da escada.
	[Export] public ItemSlot Escada;
	[Export] public Marker2D CentroHall;

	// Cadeiras de espera perto da saída do Hall: somem quando se arrastam para bloquear a passagem.
	[Export] public Node2D CadeirasDaSaida;

	private const string EscadaScene = "res://levels/escada.tscn";
	private const int MinBoxGhosts = 2;
	private const int MaxBoxGhosts = 5;
	private const float GhostChance = 0.35f;
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
		"ACHADOS E PERDIDOS - trancado. \"Chave com o zelador (Hall das Caronas).\"";

	private bool _libraryTold;
	private bool _achadosTold;
	private bool _bookRevealed;
	private bool _libraryOpen;
	private bool _rewardsRevealed;
	private int _messageIndex;
	private int _boxGhosts;
	private readonly List<PaperPile> _boxes = new();

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

		Hall.Started += OnHallStarted;
		Hall.Cleared += OnHallCleared;

		SetupBoxes();
		SetupCabinet();
		Devolucao.Used += OnBookReturned;

		Biblioteca.Started += () => Bibliotecario.StartFight();
		Bibliotecario.Defeated += OnLibrarianDefeated;

		Escada.Used += () => GoToScene(EscadaScene);

		Hud.ShowMessage("2º andar. Até os fantasmas falam baixo aqui.", 4f);
	}

	// ---------- Achados e Perdidos ----------

	// Caixas do chão: o conteúdo é decidido na hora em que quebra. As primeiras sempre têm fantasma
	// (até MinBoxGhosts); depois, às vezes fantasma, às vezes só uma coisa perdida. O livro não está nelas:
	// está no armário (puzzle).
	private void SetupBoxes()
	{
		foreach (Node child in Caixas.GetChildren())
			if (child is PaperPile box)
			{
				_boxes.Add(box);
				box.Destroyed += OnBoxOpened;
			}
	}

	private void OnBoxOpened(PaperPile box)
	{
		if (_boxGhosts < MinBoxGhosts || (_boxGhosts < MaxBoxGhosts && GD.Randf() < GhostChance))
		{
			ReleaseGhost(box);
			return;
		}

		Hud.ShowMessage(BoxMessages[_messageIndex % BoxMessages.Length], 2.5f);
		_messageIndex++;
	}

	private void SetupCabinet()
	{
		_lockerPuzzle = new LockerPuzzle();
		AddChild(_lockerPuzzle);
		Armario.Interacted += () => { if (!_bookRevealed) _lockerPuzzle.Open(); };
		Armario.PlayerEntered += () =>
		{
			if (!_bookRevealed)
				Hud.ShowMessage("O livro deve estar nesse armário... trancado com cadeado. Tem um post-it colado na porta.", 3.5f);
		};
		_lockerPuzzle.Solved += () =>
		{
			_bookRevealed = true;
			Armario.Enabled = false;
			Vector2 drop = Armario.GlobalPosition + new Vector2(-80, 40);
			GhostNav.TryNearestFreeFloor(this, drop, out drop);
			Livro.Reveal(drop);
			Hud.ShowMessage("\"Cálculo I\"! Atrasado desde 2041.", 3f);
		};
	}

	private LockerPuzzle _lockerPuzzle;

	private void ReleaseGhost(PaperPile box)
	{
		_boxGhosts++;
		Achados.SpawnEnemyAt(box.GlobalPosition, Enemy.Kind.Normal);
		Hud.ShowMessage("Tinha um fantasma aí dentro!", 2f);
	}

	// ---------- Hall das Caronas ----------

	private void OnLibraryTold()
	{
		_libraryTold = true;
		PortaAchados.LockedMessage = AchadosFullMessage;
	}

	// O Hall só tranca depois de o jogador ter passado pela biblioteca e pelos Achados,
	// e só com ele já do lado de dentro da barreira de cadeiras. (Antes, a luta começava com o jogador
	// ainda em cima da barreira: as cadeiras trancavam em cima dele e ele ficava preso ou era jogado para fora.)
	private const float HallExitClearance = 160f;   // distância da borda direita do Hall (onde fica a barreira)

	private void CheckHallStart()
	{
		if (!_achadosTold || Hall.IsCleared || Hall.IsFighting) return;
		Rect2 inside = new(Hall.GlobalPosition + new Vector2(64, 64), Hall.Size - new Vector2(64 + HallExitClearance, 128));
		if (inside.HasPoint(Player.GlobalPosition))
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
		Vector2 drop = Hall.FinalEnemyPosition;
		if (drop == Vector2.Zero || !GhostNav.TryNearestFreeFloor(this, drop, out drop))
			drop = CentroHall.GlobalPosition;
		Chave.Reveal(drop);
		Hud.ShowMessage("O zelador deixou cair o molho de chaves: \"ACHADOS E PERDIDOS\".", 4f);
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
		Hud.ShowMessage("A Bibliotecária caiu! Ela deixou o Rifle de Precisão e uma chave.", 4f);
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
		CheckHallStart();
		UpdateObjective();
	}

	// O único objetivo mostrado é ir até a biblioteca. Como conseguir entrar (livro, Achados, chave)
	// o jogador descobre sozinho pelas mensagens das portas. Na biblioteca, a tela ajuda na luta.
	private void UpdateObjective()
	{
		if (Bibliotecario.IsHunting)
		{
			int seconds = Mathf.CeilToInt(Bibliotecario.HuntTimeLeft);
			Hud.SetObjective($"ELA TE VIU! Suma da vista dela atrás das estantes ({seconds} s)");
			Hud.SetBoss(Bibliotecario.DisplayName, Bibliotecario.HealthRatio, false);
			Hud.PointTo(Bibliotecario.GlobalPosition + BossPointerOffset, "BIBLIOTECÁRIA");
			return;
		}

		if (Bibliotecario.IsStunned)
		{
			Hud.SetObjective(Bibliotecario.IsTired
				? "Ela parou para respirar: ATIRE!"
				: "Ela está TONTA! Atire nela agora... e se esconda antes que ela acorde");
			Hud.SetBoss(Bibliotecario.DisplayName, Bibliotecario.HealthRatio, false);
			Hud.PointTo(Bibliotecario.GlobalPosition + BossPointerOffset, "BIBLIOTECÁRIA");
			return;
		}

		if (Bibliotecario.InStealth)
		{
			Hud.SetObjective(Bibliotecario.IsInvestigating
				? $"Ela ouviu! Dê a volta por trás da estante e, quando ela parar na frente, empurre com [Espaço] ({Bibliotecario.ShelvesLeft} de pé)"
				: $"Silêncio... Atire numa estante para os livros caírem e atraí-la ({Bibliotecario.ShelvesLeft} de pé)");
			Hud.SetBoss(Bibliotecario.DisplayName, Bibliotecario.HealthRatio, false);
			ToppleShelf shelf = Bibliotecario.NearestShelf(Player.GlobalPosition);
			if (shelf != null)
				Hud.PointTo(shelf.GlobalPosition + new Vector2(0, -150), "ESTANTE");
			return;
		}

		if (Bibliotecario.IsAttacking)
		{
			Hud.SetObjective("Nada mais de pé! Desvie dos livros e atire quando ela cansar");
			Hud.SetBoss(Bibliotecario.DisplayName, Bibliotecario.HealthRatio, false);
			Hud.PointTo(Bibliotecario.GlobalPosition + BossPointerOffset, "BIBLIOTECÁRIA");
			return;
		}

		// Hall das Caronas: barra de vida do zelador quando ele chega.
		if (Hall.IsFighting)
		{
			Hud.SetObjective("");
			Enemy zelador = Hall.FinalEnemy;
			if (IsInstanceValid(zelador))
			{
				_janitorShown = true;
				Hud.SetBoss("ZELADOR", zelador.HealthRatio, false);
				Hud.PointTo(zelador.GlobalPosition + BossPointerOffset, "ZELADOR");
			}
			else
				Hud.HidePointer();
			return;
		}
		if (_janitorShown)
		{
			_janitorShown = false;
			Hud.HideBoss();
		}

		UpdateGuide();
	}

	private bool _janitorShown;

	// Fora das lutas: a seta sempre aponta para o próximo passo da fase.
	// O texto fica curto de propósito: o porquê de cada passo vem das mensagens das portas.
	private void UpdateGuide()
	{
		if (!_libraryTold)
			Objective("Vá até a biblioteca", DoorPoint(PortaBiblioteca), "BIBLIOTECA");
		else if (!_achadosTold)
			Objective("", DoorPoint(PortaAchados), "ACHADOS E PERDIDOS");
		else if (!Hall.IsCleared)
			Objective("", CentroHall.GlobalPosition, "HALL DAS CARONAS");
		else if (!Chave.IsCollected && PortaAchados.IsLocked)
			Objective("", Chave.GlobalPosition, "CHAVE");
		else if (PortaAchados.IsLocked)
			Objective("", DoorPoint(PortaAchados), "ACHADOS E PERDIDOS");
		else if (!_bookRevealed)
			Objective("", Armario.GlobalPosition, "ARMÁRIO");
		else if (!Livro.IsCollected && !_libraryOpen)
			Objective("", Livro.GlobalPosition, "LIVRO");
		else if (!_libraryOpen)
			Objective("", Devolucao.GlobalPosition, "DEVOLUÇÃO");
		else if (!Bibliotecario.IsDefeated)
			Objective("", DoorPoint(PortaBiblioteca), "BIBLIOTECA");
		else if (!Canhao.IsCollected)
			Objective("", Canhao.GlobalPosition, "RIFLE");
		else if (!ChaveEscada.IsCollected)
			Objective("", ChaveEscada.GlobalPosition, "CHAVE DA ESCADA");
		else
			Objective("Suba pela escada", Escada.GlobalPosition, "ESCADA");
	}

	// Porta: a origem fica na base; o meio dela fica meia altura acima.
	private static Vector2 DoorPoint(Door door) => door.GlobalPosition + new Vector2(0, -door.Size.Y / 2);


	private void Objective(string text, Vector2 target, string label)
	{
		Hud.SetObjective(text);
		Hud.PointTo(target, label);
	}
}
