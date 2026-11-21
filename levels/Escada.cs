using Godot;
using System.Collections.Generic;

// Escada (fim da Fase 2): a escada de verdade do prédio, em zigue-zague, vista de cima.
// São 8 lances, 2 por andar, do térreo (0) ao 4º andar. Estilo "Temple Run": quando você começa a subir,
// os degraus desabam ATRÁS de você e o buraco vem seguindo. Se alcançar, você cai (REPROVADO) e recomeça.
// Sem fantasmas. Os obstáculos (um por lance) estão na cena:
//   - barricada (o Canhão derruba com 1 tiro; Escopeta e Laser demoram)  -> Barricade
//   - degrau quebrado e lance pela metade (buracos: só dá para passar pelo lado)  -> Prop sólido
//   - lâmpada caindo do teto (a sombra avisa)  -> FallingLamp
//   - extintor rolando escada abaixo (desvie ou atire)  -> RollingExtinguisher
// Quanto mais perto o buraco, mais a tela treme, mais alto o barulho de desabamento e mais vermelha a borda.
// A porta do 4º andar só aparece quando você chega no 4º andar. A subida toda leva uns 40 segundos.
// Peças em levels/escada.tscn (gerada por tools/levelgen/gen_escada.py).
public partial class Escada : LevelBase
{
	[Export] public Door PortaSaida;
	[Export] public AreaTrigger Saida;
	[Export] public AreaTrigger Inicio;

	// Cobre o patamar do 4º andar enquanto ele ainda "não existe".
	[Export] public CanvasItem CoberturaSaida;

	// Desenho do buraco que vem atrás do jogador e o nó com os obstáculos (para pôr tudo de volta ao reprovar).
	[Export] public CollapseFloor Desabamento;
	[Export] public Node2D Obstaculos;

	// O meio da faixa, do começo ao fim (pixels), a largura da faixa e onde começa cada andar (s do caminho).
	[Export] public Vector2[] Caminho = System.Array.Empty<Vector2>();
	[Export] public float Faixa = 192f;
	[Export] public float[] InicioDosAndares = System.Array.Empty<float>();

	// Som do desabamento (em loop; o volume sobe quando o buraco chega perto).
	[Export] public AudioStream SomDesabamento;

	// Velocidade do desabamento (pixels por segundo; o jogador anda a 240) e quanto ela sobe a cada andar.
	[Export] public float VelocidadeInicial = 190f;
	[Export] public float AumentoPorAndar = 8f;

	// Vantagem inicial (pixels) e a vantagem máxima antes de o desabamento acelerar.
	[Export] public float VantagemInicial = 256f;
	[Export] public float VantagemMaxima = 900f;

	// Próxima cena: o 4º andar (Fase 3).
	private const string NextScene = "res://levels/fase_3.tscn";
	private const int LastFloor = 4;
	private const float Tile = 64f;
	private const float StartDelay = 1.2f;
	private const float DangerDistance = 6 * Tile;   // a partir daqui: tremor, barulho e vermelho

	private PolyPath _path;
	private int _floor;
	private bool _running;
	private float _startTimer;
	private float _sPlayer;      // quanto o jogador já subiu (pixels ao longo do caminho)
	private float _sCollapse;    // até onde o chão já caiu
	private float _danger;       // 0 (longe) a 1 (encostando)

	private AudioStreamPlayer _rumble;
	private ColorRect _redEdge;
	private ShaderMaterial _redEdgeMaterial;

	protected override void SetupDefaultLoadout()
	{
		// Aberta direto (F3): todas as armas, como se tivesse vencido a Fase 2 (o Canhão é a arma desta parte).
		for (int i = 1; i < Player.Weapons.Count; i++)
			Player.AddWeapon(Player.Weapons[i]);
	}

	public override void _Ready()
	{
		base._Ready();

		_path = new PolyPath(Caminho, Faixa);
		Desabamento.Path = _path;
		Inicio.PlayerEntered += OnClimbStarted;
		Saida.PlayerEntered += OnReachedTop;

		// A câmera do jogador fica um pouco à frente (para cima), para ver o que vem.
		Camera2D camera = Player.GetNodeOrNull<Camera2D>("Camera2D");
		if (camera != null)
			camera.Position = new Vector2(0, -140);

		CreateDangerFeedback();
		ResetClimb();
		Hud.SetObjective("Suba até o 4º andar");
		Hud.ShowMessage("O elevador quebrou. Só resta a escada... e ela não parece nada firme.", 4f);
	}

	// Borda vermelha da tela e o barulho de desabamento (os dois começam desligados).
	private void CreateDangerFeedback()
	{
		CanvasLayer layer = new() { Layer = 5 };
		AddChild(layer);
		_redEdgeMaterial = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code = @"shader_type canvas_item;
uniform float strength = 0.0;
void fragment() {
	vec2 d = UV - vec2(0.5);
	float edge = smoothstep(0.25, 0.75, length(d) * 1.4);
	COLOR = vec4(0.85, 0.05, 0.03, edge * strength);
}"
			}
		};
		_redEdge = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Material = _redEdgeMaterial };
		_redEdge.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		layer.AddChild(_redEdge);

		_rumble = new AudioStreamPlayer { Stream = SomDesabamento, VolumeDb = -40f, Bus = "Master" };
		_rumble.Finished += () => { if (_running) _rumble.Play(); };
		AddChild(_rumble);
	}

	private void ResetClimb()
	{
		_floor = 0;
		_running = false;
		_danger = 0;
		Inicio.ResetTrigger();
		Desabamento.Clear();
		foreach (Node child in Obstaculos.GetChildren())
		{
			if (child is Barricade barricade) barricade.Reset();
			else if (child is FallingLamp lamp) lamp.Reset();
			else if (child is RollingExtinguisher extinguisher) extinguisher.Reset();
		}
		SetExitVisible(false);
		_rumble.Stop();
		_redEdgeMaterial.SetShaderParameter("strength", 0f);
		Hud.SetCounter("ANDAR 0");
	}

	// Pisou no primeiro degrau: o chão começa a cair atrás.
	private void OnClimbStarted()
	{
		_sPlayer = _path.Project(Player.GlobalPosition);
		_sCollapse = _sPlayer - VantagemInicial;
		_startTimer = StartDelay;
		_running = true;
		_rumble.Play();
		Player.TriggerShake(12f);
		Hud.ShowMessage("A escada está desabando! CORRA!  (o Canhão derruba as barricadas)", 3.5f);
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

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (!_running) return;

		float dt = (float)delta;
		// Só avança: andar para trás não "desconta" (e o buraco vem do mesmo jeito).
		_sPlayer = _path.Project(Player.GlobalPosition);
		UpdateCollapse(dt);
		if (!_running) return;

		UpdateFloor();
		UpdateDanger(dt);
		UpdateHud();
	}

	private void UpdateCollapse(float dt)
	{
		if (_startTimer > 0)
		{
			_startTimer -= dt;
		}
		else
		{
			float speed = VelocidadeInicial + AumentoPorAndar * _floor;
			float lead = _sPlayer - _sCollapse;
			if (lead > VantagemMaxima)
				speed += (lead - VantagemMaxima) * 0.8f;   // abriu muita vantagem: o desabamento acelera
			_sCollapse += speed * dt;
		}

		Desabamento.Front = _sCollapse;
		Desabamento.Active = true;

		if (_sPlayer < _sCollapse)
		{
			_running = false;
			_rumble.Stop();
			Player.TriggerShake(16f);
			Fail("A escada desabou embaixo de você!\nVocê volta para o térreo.");
		}
	}

	private void UpdateFloor()
	{
		int floor = 0;
		for (int i = 0; i < InicioDosAndares.Length; i++)
			if (_sPlayer >= InicioDosAndares[i])
				floor = i;
		if (floor <= _floor) return;

		_floor = floor;
		if (_floor >= LastFloor)
		{
			SetExitVisible(true);
			Hud.ShowMessage("4º ANDAR! A porta apareceu. Corre!", 3f);
		}
		else
		{
			Hud.ShowMessage($"{_floor}º ANDAR", 1.5f);
		}
	}

	// Perto do buraco: a tela treme, o barulho sobe e a borda fica vermelha.
	private void UpdateDanger(float dt)
	{
		float lead = _sPlayer - _sCollapse;
		float target = Mathf.Clamp(1f - lead / DangerDistance, 0f, 1f);
		_danger = Mathf.MoveToward(_danger, target, dt * 3f);

		if (_danger > 0.05f)
			Player.TriggerShake(1.5f + 9f * _danger);
		_rumble.VolumeDb = Mathf.Lerp(-22f, 2f, _danger);
		float pulse = 0.85f + 0.15f * Mathf.Sin(Time.GetTicksMsec() / 90f);
		_redEdgeMaterial.SetShaderParameter("strength", _danger * 0.85f * pulse);
	}

	private void UpdateHud()
	{
		Hud.SetCounter(_floor == 0 ? "ANDAR 0" : $"{_floor}º ANDAR");
		if (_floor >= LastFloor)
			Hud.PointTo(PortaSaida.GlobalPosition + new Vector2(0, -40), "SAÍDA");
		else
			Hud.HidePointer();
	}

	private void OnReachedTop()
	{
		_running = false;
		_rumble.Stop();
		_redEdgeMaterial.SetShaderParameter("strength", 0f);
		Hud.HidePointer();
		FinishLevel(
			"FIM DA FASE 2",
			"Chegou ao 4º andar. Atrás de você, a escada não existe mais.\nLá em cima, os servidores estão acordando.",
			NextScene,
			"Aperte qualquer tecla para entrar no 4º andar.");
	}

	protected override void OnBeforeRespawn()
	{
		// Reprovado (caiu ou se machucou demais): recomeça do térreo, com tudo no lugar.
		ResetClimb();
	}
}
