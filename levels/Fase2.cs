using Godot;
using System.Collections.Generic;

// Roteiro da Fase 2 (Biblioteca). As peças ficam em levels/fase_2.tscn.
//
// Ordem:
//   chegada -> porta da biblioteca (fechada: falta um livro)
//   -> Achados e Perdidos (trancado: a chave está com o zelador)
//   -> Hall das Caronas (as cadeiras bloqueiam a saída, 3 ondas, o zelador solta a chave)
//   -> Achados e Perdidos (caixas: uma tem o livro, algumas têm fantasma)
//   -> devolução do livro -> biblioteca abre
//   -> Bibliotecária (furtividade com as luminárias, depois luta) -> Canhão + chave da escada
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

	// A escada fica encostada na parede do corredor: encostar com a chave leva à cena da escada.
	[Export] public ItemSlot Escada;
	[Export] public Marker2D CentroHall;

	// Cadeiras de espera perto da saída do Hall: somem quando se arrastam para bloquear a passagem.
	[Export] public Node2D CadeirasDaSaida;

	private const string EscadaScene = "res://levels/escada.tscn";
	private const int MinGhostsBeforeBook = 2;
	private const int MaxBoxGhosts = 5;
	private const float GhostChanceBeforeBook = 0.45f;
	private const float GhostChanceAfter = 0.25f;
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

		Hall.Entered += OnHallEntered;
		Hall.Started += OnHallStarted;
		Hall.Cleared += OnHallCleared;

		SetupBoxes();
		Devolucao.Used += OnBookReturned;

		Biblioteca.Started += () => Bibliotecario.StartFight();
		Bibliotecario.Defeated += OnLibrarianDefeated;

		Escada.Used += () => GoToScene(EscadaScene);

		Hud.ShowMessage("2º andar. Até os fantasmas falam baixo aqui.", 4f);
	}

	// ---------- Achados e Perdidos ----------

	// O conteúdo de cada caixa é decidido na hora em que ela quebra:
	// - antes do livro saem pelo menos 2 fantasmas (obrigatório);
	// - depois disso o livro pode sair em qualquer caixa, e a última caixa sempre tem o livro.
	private void SetupBoxes()
	{
		foreach (Node child in Caixas.GetChildren())
			if (child is PaperPile box)
			{
				_boxes.Add(box);
				box.Destroyed += OnBoxOpened;
			}
	}

	private int BoxesLeft()
	{
		int count = 0;
		foreach (PaperPile box in _boxes)
			if (!box.IsDestroyed)
				count++;
		return count;
	}

	private void OnBoxOpened(PaperPile box)
	{
		int left = BoxesLeft(); // caixas fechadas depois desta

		if (!_bookRevealed)
		{
			if (_boxGhosts < MinGhostsBeforeBook)
			{
				// Ainda faltam fantasmas: garante que sobra caixa para eles e para o livro.
				int needed = MinGhostsBeforeBook - _boxGhosts;
				if (left <= needed || GD.Randf() < GhostChanceBeforeBook)
				{
					ReleaseGhost(box);
					return;
				}
			}
			else if (left == 0 || GD.Randf() < 1f / (left + 1))
			{
				_bookRevealed = true;
				Livro.Reveal(box.GlobalPosition);
				Hud.ShowMessage("\"Cálculo I\"! Atrasado desde 2041.", 3f);
				return;
			}
			else if (_boxGhosts < MaxBoxGhosts && GD.Randf() < GhostChanceAfter)
			{
				ReleaseGhost(box);
				return;
			}
		}
		else if (_boxGhosts < MaxBoxGhosts && GD.Randf() < GhostChanceAfter)
		{
			ReleaseGhost(box);
			return;
		}

		Hud.ShowMessage(BoxMessages[_messageIndex % BoxMessages.Length], 2.5f);
		_messageIndex++;
	}

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
		Hud.ShowMessage("A Bibliotecária caiu! Ela deixou o Canhão e uma chave.", 4f);
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

	// O único objetivo mostrado é ir até a biblioteca. Como conseguir entrar (livro, Achados, chave)
	// o jogador descobre sozinho pelas mensagens das portas. Na biblioteca, a tela ajuda na luta.
	private void UpdateObjective()
	{
		if (Bibliotecario.IsHunting)
		{
			int seconds = Mathf.CeilToInt(Bibliotecario.HuntTimeLeft);
			Hud.SetObjective($"ELA TE VIU! Fique longe da luz (lanterna e luminárias) por {seconds} s");
			Hud.HideBoss();
			Hud.PointTo(Bibliotecario.GlobalPosition + BossPointerOffset, "BIBLIOTECÁRIA");
			return;
		}

		if (Bibliotecario.InStealth)
		{
			Hud.SetObjective($"Apague as luminárias das salas de estudo sem ser vista na luz ({Bibliotecario.LampsLeft} restantes)");
			Hud.HideBoss();
			ShieldLamp lamp = Bibliotecario.NearestLamp(Player.GlobalPosition);
			if (lamp != null)
				Hud.PointTo(lamp.GlobalPosition + PointerOffset, "LUMINÁRIA");
			return;
		}

		if (Bibliotecario.IsAttacking)
		{
			Hud.SetObjective("Derrote a Bibliotecária");
			Hud.SetBoss(Bibliotecario.DisplayName, Bibliotecario.HealthRatio, false);
			Hud.PointTo(Bibliotecario.GlobalPosition + BossPointerOffset, "BIBLIOTECÁRIA");
			return;
		}

		if (Bibliotecario.IsDefeated || _libraryOpen || Hall.IsFighting)
		{
			Hud.SetObjective("");
			Hud.HidePointer();
			return;
		}

		Objective("Vá até a biblioteca", PortaBiblioteca.GlobalPosition, "BIBLIOTECA");
	}

	private void Objective(string text, Vector2 target, string label)
	{
		Hud.SetObjective(text);
		Hud.PointTo(target + PointerOffset, label);
	}
}
