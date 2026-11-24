using Godot;

// Roteiro da Fase 3 (4º andar: laboratórios). As peças ficam em levels/fase_3.tscn (gerada por tools/levelgen/gen_fase3.py).
//
// Até entrar na área final (MVPs 5a, 5b e 5c):
//   1. O jogador chega pela escada (embaixo). Uma seta aponta para a senha da porta, no alto do corredor.
//      As portas dos laboratórios estão trancadas.
//   2. A porta de vidro (no pé do corredor de vidro que leva à área final) está trancada. A fechadura brilha;
//      com E abre o teclado de senha. Nessa hora as câmeras do corredor apitam e piscam, toca o ALARME
//      e os fantasmas começam a surgir no corredor (sala "Alarme", sem fim). Os laboratórios destrancam.
//   3. Ao sair do teclado: "Descubra a senha nos laboratórios". Os laboratórios são áreas seguras.
//   4. Laboratório 2 (direita): o computador pisca. Chegando perto, avisa que precisa da fiação do Laboratório 1.
//   5. Laboratório 1 (esquerda): o quadro de energia (fios) e, do outro lado da sala, o distribuidor (calibrar).
//      Os dois, em qualquer ordem (dá para fazer antes de ver o computador).
//   6. De volta ao computador: login (usuário aluno, senha unifesp) e o site da universidade (paródia). Em Assuntos Acadêmicos -> Diplomas está o aviso:
//      o diploma está no auditório, e a senha da porta (sorteada a cada partida).
//   7. Senha certa: a porta abre, o alarme para e todos os fantasmas somem.
// Reprovado com o alarme tocando: volta dentro do último laboratório visitado, com o que já fez guardado.
public partial class Fase3 : LevelBase
{
	[Export] public Room Lab1;
	[Export] public Room Lab2;
	[Export] public Room Convivencia;

	// Corredor assombrado enquanto o alarme toca (sobrevivência sem fim, só começa pelo roteiro).
	[Export] public Room Alarme;
	[Export] public Door PortaFinal;
	[Export] public Interactable Fechadura;
	[Export] public GlowingDevice LuzFechadura;
	[Export] public Interactable Computador;
	[Export] public GlowingDevice Monitor;
	[Export] public Interactable QuadroEnergia;

	// Lab 1, do lado oposto ao quadro (parede de baixo): o distribuidor de energia, com o desafio de calibrar.
	[Export] public Interactable CalibrarDistribuidor;
	[Export] public GlowingDevice Distribuidor;
	[Export] public GlowingDevice Quadro;

	// Onde o jogador volta se for reprovado (dentro de cada laboratório).
	[Export] public Marker2D VoltaLab1;
	[Export] public Marker2D VoltaLab2;

	// Portas dos laboratórios: trancadas até o alarme.
	[Export] public Door PortaLab1;
	[Export] public Door PortaLab2;

	// Auditório: área restrita. Por fora não se vê nada (Cobertura); a porta abre com E e a cobertura some ao entrar.
	[Export] public Door PortaAuditorio;
	[Export] public Interactable AbrirAuditorio;
	[Export] public AreaTrigger EntradaAuditorio;
	[Export] public CanvasItem CoberturaAuditorio;

	// Atraso entre os bipes das câmeras e a sirene.
	private const float SirenDelay = 0.75f;
	private const float AuditoriumLookTime = 1.3f;
	private const string Auditorium3DScene = "res://levels/auditorio_3d.tscn";
	private const int FirstGhosts = 3;
	private const int KeypadMaxGhosts = 3;
	private const float KeypadSpawnDistance = 380f;
	private int _corridorMaxGhosts;
	private float _corridorSpawnDistance;

	private const float AlarmVolume = -9f;
	private const float AlarmTintMax = 0.13f;

	private string _code;
	private bool _alarmOn;
	private bool _doorOpen;
	private bool _wiresFixed;
	private bool _calibrated;
	private bool PowerRestored => _wiresFixed && _calibrated;
	private bool _codeFound;

	private KeypadScreen _keypad;
	private WirePuzzle _wires;
	private CalibrationScreen _calibration;
	private IntranetScreen _site;
	private AudioStreamPlayer _alarmSound;
	private ColorRect _alarmTint;
	private ScreenAlert _alert;
	private float _alarmTime;

	protected override void SetupDefaultLoadout()
	{
		// Aberta direto (F4): todas as armas, como se tivesse vencido a Fase 2.
		for (int i = 1; i < Player.Weapons.Count; i++)
			Player.AddWeapon(Player.Weapons[i]);
	}

	public override void _Ready()
	{
		base._Ready();
		// o alarme não entra no "recomeço do trecho": reprovado, o jogador volta no último laboratório
		// e os fantasmas continuam onde estão (morrer não limpa o corredor; só a vida volta cheia)

		// senha nova a cada partida (sem números repetidos em sequência, para ficar fácil de ler)
		RandomNumberGenerator rng = new();
		rng.Randomize();
		do _code = rng.RandiRange(1000, 9999).ToString();
		while (_code[0] == _code[1] || _code[1] == _code[2] || _code[2] == _code[3]);

		_corridorMaxGhosts = Alarme.SurviveMaxAlive;
		_corridorSpawnDistance = Alarme.MinSpawnDistance;
		CreateScreens();
		// reprovado com o teclado aberto: fecha o teclado (a tela de REPROVADO fica por cima)
		Player.Died += () => _keypad.Close();
		CreateAlarm();

		Fechadura.Interacted += OpenKeypad;
		Computador.PlayerEntered += OnComputerApproached;
		Computador.Interacted += () => { if (PowerRestored) _site.Open(); };
		Computador.Enabled = false;
		QuadroEnergia.Interacted += () => _wires.Open();
		CalibrarDistribuidor.Interacted += () => _calibration.Open();
		Lab1.Entered += () => SetRespawn(VoltaLab1);
		Lab2.Entered += () => SetRespawn(VoltaLab2);

		// a cobertura ignora as luzes (senão a luz do auditório e a lanterna revelariam o que tem dentro)
		CoberturaAuditorio.Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
		AbrirAuditorio.Interacted += OpenAuditorium;

		// sem objetivo fixo na tela: só uma fala rápida ligada ao objetivo do personagem (o diploma)
		Hud.SetObjective("");
		Hud.ShowMessage("4º andar... Meu diploma tem que estar em algum lugar por aqui. Aquela porta de vidro no fim do corredor parece importante.", 6f);
	}

	private void CreateScreens()
	{
		_keypad = new KeypadScreen { Code = _code };
		_wires = new WirePuzzle();
		_calibration = new CalibrationScreen();
		AddChild(_calibration);
		_calibration.Solved += OnCalibrated;
		_site = new IntranetScreen { Code = _code };
		AddChild(_keypad);
		AddChild(_wires);
		AddChild(_site);
		_alert = new ScreenAlert();
		AddChild(_alert);

		_keypad.PausesGame = false;
		_keypad.Solved += OnCodeAccepted;
		_keypad.Closed += OnKeypadClosed;
		_wires.Solved += OnWiresFixed;
		_site.NoticeFound += OnNoticeFound;
		_site.Closed += () =>
		{
			if (_codeFound && !_doorOpen)
				Hud.ShowMessage("Não esqueça da senha...", 3.5f);
		};
	}

	private void CreateAlarm()
	{
		_alarmSound = new AudioStreamPlayer
		{
			Stream = GD.Load<AudioStream>("res://sounds/fase3/alarme.wav"),
			VolumeDb = AlarmVolume,
			ProcessMode = ProcessModeEnum.Always,   // continua tocando com as telas abertas
		};
		_alarmSound.Finished += () => { if (_alarmOn) _alarmSound.Play(); };
		AddChild(_alarmSound);

		// luz vermelha pulsando na tela inteira enquanto o alarme toca
		CanvasLayer layer = new() { Layer = 1 };
		_alarmTint = new ColorRect { Color = new Color(1f, 0.05f, 0.05f, 0f), MouseFilter = Control.MouseFilterEnum.Ignore };
		_alarmTint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		layer.AddChild(_alarmTint);
		AddChild(layer);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		CheckAuditoriumEntered();

		// antes de tentar a porta: seta para a área final
		if (!_alarmOn && !_doorOpen)
			Hud.PointTo(Fechadura.GlobalPosition, "PORTA");

		if (_alarmOn)
		{
			_alarmTime += (float)delta;
			float pulse = 0.5f + 0.5f * Mathf.Sin(_alarmTime * Mathf.Tau);   // 1 pulso por segundo
			_alarmTint.Color = new Color(_alarmTint.Color, AlarmTintMax * pulse);
		}
	}

	// ---------- porta e alarme ----------

	private void OpenKeypad()
	{
		if (_doorOpen) return;
		if (!_alarmOn)
			StartAlarm();

		// o jogo não pausa no teclado: o jogador fica parado digitando e os fantasmas continuam vindo,
		// só que em menor número e surgindo mais longe
		Player.InputLocked = true;
		Fechadura.Enabled = false;
		Alarme.SurviveMaxAlive = KeypadMaxGhosts;
		Alarme.MinSpawnDistance = KeypadSpawnDistance;
		_keypad.Open();
	}

	// Ao sair do teclado (fechou, acertou ou foi reprovado): o jogador volta a se mexer e o alarme volta ao normal.
	private void RestoreAfterKeypad()
	{
		Player.InputLocked = false;
		Fechadura.Enabled = !_doorOpen;
		Alarme.SurviveMaxAlive = _corridorMaxGhosts;
		Alarme.MinSpawnDistance = _corridorSpawnDistance;
	}

	private void StartAlarm()
	{
		SetRespawn(VoltaLab2);   // reprovado antes de entrar num laboratório: volta no Lab 2, o mais perto
		_alarmOn = true;
		Hud.HidePointer();
		Alarme.StartFight();
		// os primeiros fantasmas já surgem junto com o alarme, longe (chegam enquanto o jogador ainda digita)
		Alarme.MinSpawnDistance = KeypadSpawnDistance;
		for (int i = 0; i < FirstGhosts; i++)
			Alarme.SpawnExtraEnemy();
		PortaLab1.Unlock();
		PortaLab2.Unlock();
		SetCameras(true);
		// para quem joga sem som: borda vermelha piscando e legenda por cima do teclado
		_alert.Flash("AS CÂMERAS DETECTARAM VOCÊ!\nOs fantasmas foram alertados.", 4.5f);

		// as câmeras apitam primeiro; a sirene vem logo depois (o relógio corre mesmo com o teclado aberto)
		AudioStreamPlayer beep = new()
		{
			Stream = GD.Load<AudioStream>("res://sounds/fase3/camera.wav"),
			VolumeDb = -4f,
			ProcessMode = ProcessModeEnum.Always,
		};
		beep.Finished += beep.QueueFree;
		AddChild(beep);
		beep.Play();
		GetTree().CreateTimer(SirenDelay, true).Timeout += () => { if (_alarmOn) _alarmSound.Play(); };
	}

	// Câmeras do corredor: gravando (luz vermelha piscando) ou apagadas.
	private void SetCameras(bool recording)
	{
		Texture2D off = GD.Load<Texture2D>("res://sprites/props/fase3/camera0.png");
		Texture2D on = GD.Load<Texture2D>("res://sprites/props/fase3/camera1.png");
		foreach (Node node in GetTree().GetNodesInGroup("cameras"))
		{
			if (node is not GlowingDevice camera) continue;
			camera.SetTextures(recording ? on : off, recording ? off : null, recording ? 0.25f : 0f);
			camera.Glow = recording;
		}
	}

	private void StopAlarm()
	{
		_alarmOn = false;
		_alarmSound.Stop();
		_alarmTint.Color = new Color(_alarmTint.Color, 0f);
	}

	private void OnKeypadClosed()
	{
		RestoreAfterKeypad();
		if (_doorOpen) return;

		if (_codeFound)
		{
			Hud.ShowMessage("Não esqueça da senha...", 3f);
			return;
		}
		// sem objetivo fixo: só um aviso rápido (o jogador descobre o resto sozinho)
		Hud.SetObjective("");
		Hud.ShowMessage("A senha deve estar em algum dos laboratórios...", 3.5f);
	}

	private void OnCodeAccepted()
	{
		_doorOpen = true;
		StopAlarm();
		SetCameras(false);
		Alarme.FinishFight();               // todos os fantasmas somem
		PortaFinal.Unlock();
		Fechadura.Enabled = false;
		LuzFechadura.GlowColor = new Color(0.3f, 1f, 0.5f);
		Hud.ShowMessage("Acesso liberado. O alarme parou... e os fantasmas sumiram.", 4f);
	}

	// ---------- auditório ----------

	private void OpenAuditorium()
	{
		PortaAuditorio.Unlock();
		AbrirAuditorio.Enabled = false;
		Hud.ShowMessage("A porta do auditório destrancou...", 3f);
	}

	// Entrou no auditório (o jogador dentro do retângulo escuro, por qualquer ponto da porta): a escuridão some,
	// ele vê o auditório por um instante, a tela escurece e o jogo vai para a cena em 3D (primeira pessoa).
	private bool _auditoriumRevealed;

	private void CheckAuditoriumEntered()
	{
		if (_auditoriumRevealed || CoberturaAuditorio is not Control cover) return;
		if (!cover.GetGlobalRect().HasPoint(Player.GlobalPosition)) return;

		_auditoriumRevealed = true;
		RevealAuditorium();
		GetTree().CreateTimer(AuditoriumLookTime, false).Timeout += () =>
		{
			GameState.Save(Player, ElapsedTime);
			GetNode<SceneTransition>("/root/SceneTransition").ChangeScene(Auditorium3DScene);
		};
	}

	private void RevealAuditorium()
	{
		Tween tween = CreateTween();
		tween.TweenProperty(CoberturaAuditorio, "modulate:a", 0f, 0.8f);
		tween.TweenCallback(Callable.From(() => { CoberturaAuditorio.Visible = false; }));
	}

	// ---------- laboratórios ----------

	private void OnComputerApproached()
	{
		if (PowerRestored) return;
		Hud.ShowMessage("O computador ainda funciona, mas está sem energia: conserte o quadro e o distribuidor do outro laboratório.", 5f);
	}

	private void OnWiresFixed()
	{
		_wiresFixed = true;
		QuadroEnergia.Enabled = false;
		Quadro.SetTextures(GD.Load<Texture2D>("res://sprites/props/fase3/quadro_energia_ok.png"), null, 0f);
		Quadro.GlowColor = new Color(0.3f, 1f, 0.5f);
		CheckPower("Fiação consertada! Ainda falta calibrar o distribuidor, na parede de baixo.");
	}

	private void OnCalibrated()
	{
		_calibrated = true;
		CalibrarDistribuidor.Enabled = false;
		Distribuidor.SetTextures(GD.Load<Texture2D>("res://sprites/props/fase3/distribuidor_ok.png"), null, 0f);
		Distribuidor.GlowColor = new Color(0.3f, 1f, 0.5f);
		CheckPower("Distribuidor calibrado! Ainda falta consertar a fiação do quadro, na parede de cima.");
	}

	// Com as duas partes prontas, o computador do Lab 2 liga de vez. Senão, avisa o que falta.
	private void CheckPower(string missingMessage)
	{
		if (!PowerRestored)
		{
			Hud.ShowMessage(missingMessage, 4f);
			return;
		}
		Computador.Enabled = true;
		Monitor.SetTextures(GD.Load<Texture2D>("res://sprites/props/fase3/monitor1.png"), null, 0f);
		Hud.ShowMessage("Energia restaurada! O computador do outro laboratório voltou a funcionar.", 4f);
	}

	private void OnNoticeFound()
	{
		_codeFound = true;
	}

	// ---------- reprovado ----------

	// Reprovado: volta dentro do laboratório onde esteve por último (LevelBase usa StartPosition).
	private void SetRespawn(Marker2D point)
	{
		if (point != null)
			StartPosition = point.GlobalPosition;
	}
}
