using Godot;

// Base dos roteiros de fase (Fase 2, Escada...): o que toda fase faz igual.
//   - traz as armas e a pontuação da fase anterior (GameState);
//   - REPROVADO: volta ao começo do trecho da última luta;
//   - fim de fase: tela com pontuação e tempo, e segue para a próxima cena.
public partial class LevelBase : Node2D
{
	[Export] public Player Player;
	[Export] public Hud Hud;
	[Export] public MessageScreen Screen;

	protected Vector2 StartPosition;
	protected Room LastRoom;
	protected float ElapsedTime;

	private bool _finished;
	private string _nextScene = "";

	public override void _Ready()
	{
		StartPosition = Player.GlobalPosition;
		Player.Died += OnPlayerDied;
		Screen.Closed += OnScreenClosed;

		if (GameState.HasSave)
		{
			GameState.Load(Player);
			ElapsedTime = GameState.Time;
		}
		else
		{
			// Fase aberta direto pelo editor (F6): começa com um equipamento padrão.
			SetupDefaultLoadout();
		}
	}

	// Equipamento quando a fase é aberta sem passar pela anterior.
	protected virtual void SetupDefaultLoadout() { }

	public override void _Process(double delta)
	{
		ElapsedTime += (float)delta;
	}

	// Guarda a última sala onde começou uma luta: é nela que o jogador recomeça.
	protected void TrackRooms(params Room[] rooms)
	{
		foreach (Room room in rooms)
		{
			Room current = room;
			current.Started += () => LastRoom = current;
		}
	}

	private void OnPlayerDied()
	{
		Screen.OpenFail("Os fantasmas venceram desta vez.\nVocê volta para o começo deste trecho.");
	}

	private void OnScreenClosed()
	{
		if (!_finished)
		{
			Respawn();
			return;
		}

		if (_nextScene != "")
			GetTree().ChangeSceneToFile(_nextScene);
		else
			GetTree().ReloadCurrentScene();
	}

	// Recomeça o trecho atual: a sala da última luta volta ao início.
	// O que já foi conquistado continua.
	private void Respawn()
	{
		Vector2 position = StartPosition;

		if (LastRoom != null)
		{
			if (!LastRoom.IsCleared)
			{
				LastRoom.ResetRoom();
				OnRoomReset(LastRoom);
			}

			if (LastRoom.RespawnPoint != null)
				position = LastRoom.RespawnPoint.GlobalPosition;
		}

		OnBeforeRespawn();
		Player.Revive(position);
	}

	// Reprovado por outro motivo que não a vida (ex.: o tempo acabou). Ao fechar, recomeça o trecho.
	protected void Fail(string reason)
	{
		if (_finished) return;
		Screen.OpenFail(reason);
	}

	// Para a fase desfazer o que é dela quando uma sala recomeça (ex.: chefe volta ao início).
	protected virtual void OnRoomReset(Room room) { }

	// Chamado sempre antes de o jogador voltar (com ou sem sala).
	protected virtual void OnBeforeRespawn() { }

	// Vai direto para outra cena levando armas, pontos e tempo (sem tela).
	protected void GoToScene(string scene)
	{
		GameState.Save(Player, ElapsedTime);
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, scene);
	}

	// Tela de fim de fase. Ao fechar, vai para nextScene (vazio = recomeça a cena).
	protected void FinishLevel(string title, string story, string nextScene, string hint)
	{
		if (_finished) return;

		_finished = true;
		_nextScene = nextScene;
		GameState.Save(Player, ElapsedTime);

		int minutes = (int)(ElapsedTime / 60);
		int seconds = (int)(ElapsedTime % 60);
		Screen.Open(title, $"{story}\n\nPontuação: {Player.Score}     Tempo: {minutes}:{seconds:00}", hint);
	}
}
