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
//   5. Laboratório 1 (esquerda): o quadro de energia, com o desafio dos fios (dá para fazer antes, em qualquer ordem).
//   6. De volta ao computador: o site da universidade (paródia). Em Assuntos Acadêmicos -> Diplomas está o aviso:
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
	[Export] public GlowingDevice Quadro;

	// Onde o jogador volta se for reprovado (dentro de cada laboratório).
	[Export] public Marker2D VoltaLab1;
	[Export] public Marker2D VoltaLab2;

	// Portas dos laboratórios: trancadas até o alarme.
	[Export] public Door PortaLab1;
	[Export] public Door PortaLab2;

	// Atraso entre os bipes das câmeras e a sirene.
	private const float SirenDelay = 0.75f;

	private const float AlarmVolume = -9f;
	private const float AlarmTintMax = 0.13f;

	private string _code;
	private bool _alarmOn;
	private bool _doorOpen;
	private bool _wiresFixed;
	private bool _codeFound;

	private KeypadScreen _keypad;
	private WirePuzzle _wires;
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
		TrackRooms(Alarme);

		// senha nova a cada partida (sem números repetidos em sequência, para ficar fácil de ler)
		RandomNumberGenerator rng = new();
		rng.Randomize();
		do _code = rng.RandiRange(1000, 9999).ToString();
		while (_code[0] == _code[1] || _code[1] == _code[2] || _code[2] == _code[3]);

		CreateScreens();
		CreateAlarm();

		Fechadura.Interacted += OpenKeypad;
		Computador.PlayerEntered += OnComputerApproached;
		Computador.Interacted += () => { if (_wiresFixed) _site.Open(); };
		Computador.Enabled = false;
		QuadroEnergia.Interacted += () => _wires.Open();
		Lab1.Entered += () => SetRespawn(VoltaLab1);
		Lab2.Entered += () => SetRespawn(VoltaLab2);

		Hud.SetObjective("Vá até a porta da área final, no alto do corredor");
		Hud.ShowMessage("4º andar. A porta da área final fica lá no alto do corredor.", 4f);
	}

	private void CreateScreens()
	{
		_keypad = new KeypadScreen { Code = _code };
		_wires = new WirePuzzle();
		_site = new IntranetScreen { Code = _code };
		AddChild(_keypad);
		AddChild(_wires);
		AddChild(_site);
		_alert = new ScreenAlert();
		AddChild(_alert);

		_keypad.Solved += OnCodeAccepted;
		_keypad.Closed += OnKeypadClosed;
		_wires.Solved += OnWiresFixed;
		_site.NoticeFound += OnNoticeFound;
		_site.Closed += () =>
		{
			if (_codeFound && !_doorOpen)
				Hud.ShowMessage($"Senha da porta: {_code}. Volte para a área final!", 5f);
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

		// antes de tentar a porta: seta para a área final
		if (!_alarmOn && !_doorOpen)
			Hud.PointTo(Fechadura.GlobalPosition, "SENHA");

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
		_keypad.Open();
	}

	private void StartAlarm()
	{
		_alarmOn = true;
		Hud.HidePointer();
		Alarme.StartFight();
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
		if (_doorOpen) return;

		if (_codeFound)
		{
			Hud.SetObjective($"Digite a senha {_code} na porta da área final");
			return;
		}
		Hud.SetObjective("Descubra a senha nos laboratórios");
		Hud.ShowMessage("As câmeras chamaram os fantasmas! Os laboratórios destrancaram: procure a senha lá dentro (eles não entram).", 5f);
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
		Hud.SetObjective("Entre na área final");
		Hud.ShowMessage("Acesso liberado. O alarme parou... e os fantasmas sumiram.", 4f);
	}

	// ---------- laboratórios ----------

	private void OnComputerApproached()
	{
		if (_wiresFixed) return;
		Hud.ShowMessage("O computador ainda funciona, mas está sem rede: a fiação do outro laboratório precisa ser consertada.", 5f);
		if (_alarmOn)
			Hud.SetObjective("Conserte a fiação no Laboratório 1");
	}

	private void OnWiresFixed()
	{
		_wiresFixed = true;
		QuadroEnergia.Enabled = false;
		Quadro.SetTextures(GD.Load<Texture2D>("res://sprites/props/fase3/quadro_energia_ok.png"), null, 0f);
		Quadro.GlowColor = new Color(0.3f, 1f, 0.5f);
		Computador.Enabled = true;
		Monitor.SetTextures(GD.Load<Texture2D>("res://sprites/props/fase3/monitor1.png"), null, 0f);
		Hud.ShowMessage("Energia restaurada! O computador do outro laboratório voltou a funcionar.", 4f);
		if (!_doorOpen && !_codeFound)
			Hud.SetObjective("Use o computador do Laboratório 2");
	}

	private void OnNoticeFound()
	{
		_codeFound = true;
		if (!_doorOpen)
			Hud.SetObjective($"Digite a senha {_code} na porta da área final");
	}

	// ---------- reprovado ----------

	// O alarme recomeça do laboratório onde o jogador esteve por último.
	private void SetRespawn(Marker2D point)
	{
		if (Alarme.RespawnPoint != null && point != null)
			Alarme.RespawnPoint.GlobalPosition = point.GlobalPosition;
	}

	protected override void OnRoomReset(Room room)
	{
		if (room == Alarme && _alarmOn)
			Alarme.StartFight();
	}
}
