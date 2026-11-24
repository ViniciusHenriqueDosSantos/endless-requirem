using Godot;
using System.Threading.Tasks;

// Fase 3 — o auditório em 3D (primeira pessoa). Vem da fase 2D quando o jogador entra no auditório.
//
// O jogador só anda para FRENTE (W ou seta para cima) e olha com o mouse. Lá no palco está o diploma, num suporte,
// com uma luz em cima. Para subir no palco há duas escadinhas nas laterais. Chegando perto: "[E] Pegar o diploma".
// Ao pegar começa a cena (sem controle): o telão atrás do palco liga sozinho, aparece a S.I.A., fala com o jogador,
// a câmera vira para o telão e ele é puxado para dentro. Tela preta, "(continua...)".
// Por enquanto (teste), qualquer tecla no final recomeça esta cena. Atalho de teste: F5 abre esta cena direto.
//
// Tudo é montado por código (blocos 3D simples com texturas em pixel art de sprites/3d/), e a imagem é desenhada
// em resolução baixa (StretchShrink) para combinar com o resto do jogo.
public partial class Auditorio3D : Node
{
	// ---------- medidas do auditório (metros; o palco fica no -Z) ----------
	private const float HalfWidth = 6.5f;
	private const float BackZ = 3.5f;          // parede do fundo (de onde o jogador vem)
	private const float StageFrontZ = -13f;
	private const float StageBackZ = -16.5f;
	private const float StageHeight = 1f;
	private const float RoomHeight = 6f;
	// Plateia em degraus: cada fileira fica num degrau mais alto que a da frente (o auditório "sobe" para o fundo),
	// então o jogador entra lá em cima e vai descendo até o palco.
	private const int Rows = 5;
	private const float FirstRowZ = -10.5f;
	private const float RowStep = 2.2f;
	private const float TierRise = 0.35f;
	private static float TierTop(int row) => TierRise * (row + 1);
	private static float RowZ(int row) => FirstRowZ + row * RowStep;
	private static readonly float LandingZ = RowZ(Rows - 1) + RowStep / 2;   // começo do patamar do fundo
	private static float LandingY => TierTop(Rows);
	// altura do chão (aproximada, uma rampa) num ponto do corredor
	private static float RampY(float z) => Mathf.Clamp(TierRise + (z - FirstRowZ) * TierRise / RowStep, 0f, LandingY);
	private const float CenterAisleLeft = -1.28f, CenterAisleRight = 1.3f;
	private static readonly Vector3 DiplomaSpot = new(0, StageHeight, -14.4f);
	private static readonly Vector3 ScreenCenter = new(0, 3.7f, StageBackZ + 0.08f);

	// ---------- jogador ----------
	private const float WalkSpeed = 2.3f;
	private const float EyeHeight = 1.6f;
	private const float MouseSensitivity = 0.0025f;
	private const float PickDistance = 1.5f;
	private const float StepLength = 0.7f;
	private const int PixelShrink = 3;         // 1 pixel da imagem = 3 pixels da tela

	private enum State { Walking, Cutscene, End }

	private State _state = State.Walking;
	private CharacterBody3D _body;
	private Camera3D _camera;
	private float _pitch;
	private float _stepDistance;
	private float _bob;
	private float _shake;

	private MeshInstance3D _screen;
	private StandardMaterial3D _screenMaterial;
	private Texture2D _screenOff;
	private Texture2D _screenOn;
	private Node3D _diploma;
	private SpotLight3D _diplomaLight;

	private Label _prompt;
	private Label _subtitle;
	private Label _endText;
	private ColorRect _fade;
	private AudioStreamPlayer _sound;
	private AudioStreamPlayer _steps;

	public override void _Ready()
	{
		// continua rodando com o jogo pausado só para soltar o mouse no menu de pausa
		ProcessMode = ProcessModeEnum.Always;

		SubViewportContainer container = new()
		{
			Stretch = true,
			StretchShrink = PixelShrink,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		container.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(container);
		SubViewport viewport = new() { Msaa3D = Viewport.Msaa.Disabled };
		container.AddChild(viewport);
		Node3D world = new();
		viewport.AddChild(world);

		BuildEnvironment(world);
		BuildRoom(world);
		BuildStage(world);
		BuildSeats(world);
		BuildPlayer(world);
		BuildInterface();

		Input.MouseMode = Input.MouseModeEnum.Captured;
		Say("Ali, em cima do palco... o meu diploma.", 3.5f);
	}

	// ---------- montagem ----------

	private static StandardMaterial3D Textured(string texture, float tiles, Color tint)
	{
		return new StandardMaterial3D
		{
			AlbedoTexture = GD.Load<Texture2D>($"res://sprites/3d/{texture}.png"),
			AlbedoColor = tint,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
			Uv1Triplanar = true,
			Uv1Scale = new Vector3(tiles, tiles, tiles),
			Roughness = 1f,
		};
	}

	private static StandardMaterial3D Plain(Color color, bool glowing = false)
	{
		StandardMaterial3D material = new() { AlbedoColor = color, Roughness = 1f };
		if (glowing)
		{
			material.EmissionEnabled = true;
			material.Emission = color;
			material.EmissionEnergyMultiplier = 1.5f;
		}
		return material;
	}

	// Bloco 3D (com colisão se solid).
	private static MeshInstance3D Block(Node3D parent, Vector3 center, Vector3 size, Material material, bool solid)
	{
		MeshInstance3D mesh = new() { Mesh = new BoxMesh { Size = size }, MaterialOverride = material, Position = center };
		parent.AddChild(mesh);
		if (solid)
			Collider(parent, center, size);
		return mesh;
	}

	// Colisão invisível.
	private static void Collider(Node3D parent, Vector3 center, Vector3 size, Vector3 rotation = default)
	{
		StaticBody3D body = new() { Position = center, Rotation = rotation };
		body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
		parent.AddChild(body);
	}

	private void BuildEnvironment(Node3D world)
	{
		Environment environment = new()
		{
			BackgroundMode = Environment.BGMode.Color,
			BackgroundColor = new Color(0.02f, 0.02f, 0.03f),
			AmbientLightSource = Environment.AmbientSource.Color,
			AmbientLightColor = new Color(0.35f, 0.32f, 0.42f),
			AmbientLightEnergy = 0.35f,
			FogEnabled = true,
			FogLightColor = new Color(0.05f, 0.05f, 0.08f),
			FogDensity = 0.045f,
		};
		world.AddChild(new WorldEnvironment { Environment = environment });
	}

	private void BuildRoom(Node3D world)
	{
		float length = BackZ - StageBackZ;
		float midZ = (BackZ + StageBackZ) / 2;
		Material wood = Textured("madeira", 0.6f, Colors.White);
		Material wall = Textured("parede", 0.5f, Colors.White);

		Block(world, new Vector3(0, -0.1f, midZ), new Vector3(HalfWidth * 2, 0.2f, length), wood, true);                 // chão
		Block(world, new Vector3(0, RoomHeight + 0.1f, midZ), new Vector3(HalfWidth * 2, 0.2f, length), Plain(new Color(0.08f, 0.07f, 0.07f)), false);
		Block(world, new Vector3(-HalfWidth - 0.1f, RoomHeight / 2, midZ), new Vector3(0.2f, RoomHeight, length), wall, true);
		Block(world, new Vector3(HalfWidth + 0.1f, RoomHeight / 2, midZ), new Vector3(0.2f, RoomHeight, length), wall, true);
		Block(world, new Vector3(0, RoomHeight / 2, BackZ + 0.1f), new Vector3(HalfWidth * 2, RoomHeight, 0.2f), wall, true);
		Block(world, new Vector3(0, RoomHeight / 2, StageBackZ - 0.1f), new Vector3(HalfWidth * 2, RoomHeight, 0.2f), wall, true);

		BuildTiers(world, wood);

		// placa de saída verde no fundo e arandelas fracas nas paredes
		Block(world, new Vector3(0, LandingY + 2.2f, BackZ - 0.02f), new Vector3(0.9f, 0.3f, 0.04f), Plain(new Color(0.2f, 1f, 0.4f), true), false);
		foreach (float z in new[] { -9f, -3f, 1.5f })
			foreach (float side in new[] { -1f, 1f })
			{
				Vector3 at = new(side * (HalfWidth - 0.15f), 2.6f + RampY(z), z);
				Block(world, at, new Vector3(0.12f, 0.25f, 0.3f), Plain(new Color(1f, 0.75f, 0.45f), true), false);
				world.AddChild(new OmniLight3D { Position = at + new Vector3(-side * 0.3f, 0, 0), LightColor = new Color(1f, 0.7f, 0.45f), LightEnergy = 0.5f, OmniRange = 4f });
			}
	}

	// Os degraus da plateia (um por fileira), o patamar do fundo, o tapete vermelho descendo pelo meio e rampas
	// invisíveis nos corredores (para andar liso, sem travar nos degraus).
	private void BuildTiers(Node3D world, Material wood)
	{
		Material carpet = Textured("carpete", 1f, Colors.White);
		float[][] blocks = { new[] { -HalfWidth + 0.9f, CenterAisleLeft }, new[] { CenterAisleRight, HalfWidth - 0.88f } };
		for (int row = 0; row <= Rows; row++)
		{
			bool landing = row == Rows;
			float zFront = RowZ(row) - RowStep / 2;
			float zBack = landing ? BackZ : zFront + RowStep;
			float top = TierTop(row);
			float zMid = (zFront + zBack) / 2, depth = zBack - zFront;
			Block(world, new Vector3(0, top / 2, zMid), new Vector3(HalfWidth * 2, top, depth), wood, landing);
			Block(world, new Vector3(0, top + 0.01f, zMid), new Vector3(1.4f, 0.02f, depth), carpet, false);
			if (landing) continue;
			foreach (float[] b in blocks)   // colisão só debaixo das poltronas (os corredores usam a rampa)
				Collider(world, new Vector3((b[0] + b[1]) / 2, top / 2, zMid), new Vector3(b[1] - b[0], top, depth));
		}
		Block(world, new Vector3(0, 0.01f, (StageFrontZ + RowZ(0) - RowStep / 2) / 2), new Vector3(1.4f, 0.02f, RowZ(0) - RowStep / 2 - StageFrontZ), carpet, false);

		// rampas invisíveis: do chão da frente até o patamar do fundo
		// (o corredor do meio começa um pouco antes; os dos lados só depois das escadinhas do palco)
		float zEnd = LandingZ, rise = LandingY;
		(float left, float right, float zStart)[] aisles =
		{
			(CenterAisleLeft, CenterAisleRight, FirstRowZ - RowStep),
			(-HalfWidth, -HalfWidth + 0.9f, StageFrontZ + 2f),
			(HalfWidth - 0.88f, HalfWidth, StageFrontZ + 2f),
		};
		foreach ((float left, float right, float zStart) in aisles)
		{
			float run = zEnd - zStart;
			float angle = Mathf.Atan2(rise, run);
			Collider(world, new Vector3((left + right) / 2, rise / 2 - 0.05f, (zStart + zEnd) / 2),
				new Vector3(right - left, 0.1f, Mathf.Sqrt(run * run + rise * rise)), new Vector3(-angle, 0, 0));
		}
	}

	private void BuildStage(Node3D world)
	{
		Material wood = Textured("madeira", 0.6f, new Color(1f, 0.9f, 0.85f));
		float depth = StageFrontZ - StageBackZ;
		Block(world, new Vector3(0, StageHeight / 2, (StageFrontZ + StageBackZ) / 2), new Vector3(HalfWidth * 2, StageHeight, depth), wood, true);
		Block(world, new Vector3(0, StageHeight - 0.03f, StageFrontZ - 0.05f), new Vector3(HalfWidth * 2, 0.06f, 0.1f), Plain(new Color(0.9f, 0.75f, 0.45f)), false);

		// escadinhas nas duas laterais do palco (4 degraus) com rampa invisível para subir
		const float stepRise = 0.25f, stepDepth = 0.5f, stairWidth = 1.2f;
		foreach (float side in new[] { -1f, 1f })
		{
			float x = side * (HalfWidth - 0.75f);
			for (int k = 0; k < 4; k++)
			{
				float height = stepRise * (k + 1);
				float z = StageFrontZ + 2 * stepDepth - stepDepth * (k + 0.5f);
				Block(world, new Vector3(x, height / 2, z), new Vector3(stairWidth, height, stepDepth), wood, false);
			}
			float run = 4 * stepDepth;
			float angle = Mathf.Atan2(StageHeight, run);
			Collider(world, new Vector3(x, StageHeight / 2 - 0.05f, StageFrontZ + run / 2),
				new Vector3(stairWidth, 0.1f, Mathf.Sqrt(run * run + StageHeight * StageHeight)), new Vector3(angle, 0, 0));
		}

		// telão atrás do palco (desligado; é onde a S.I.A. está escondida), com moldura e cortinas
		_screenOff = GD.Load<Texture2D>("res://sprites/3d/tela_off.png");
		_screenOn = GD.Load<Texture2D>("res://sprites/3d/sia.png");
		_screenMaterial = new StandardMaterial3D
		{
			AlbedoTexture = _screenOff,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
		};
		_screen = new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2(8f, 4.5f) }, MaterialOverride = _screenMaterial, Position = ScreenCenter };
		world.AddChild(_screen);
		Block(world, ScreenCenter + new Vector3(0, 0, -0.05f), new Vector3(8.4f, 4.9f, 0.05f), Plain(new Color(0.05f, 0.05f, 0.06f)), false);
		Material curtain = Textured("cortina", 0.5f, Colors.White);
		foreach (float side in new[] { -1f, 1f })
			Block(world, new Vector3(side * 5.4f, StageHeight + 2.6f, StageBackZ + 0.3f), new Vector3(2.1f, 5.2f, 0.2f), curtain, false);

		// o diploma no meio, num suporte, com uma luz de cima
		Block(world, DiplomaSpot + new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.9f, 0.5f), Textured("madeira", 1f, new Color(0.55f, 0.4f, 0.3f)), true);

		_diploma = new Node3D { Position = DiplomaSpot + new Vector3(0, 0.98f, 0) };
		_diploma.AddChild(new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = 0.06f, BottomRadius = 0.06f, Height = 0.5f },
			MaterialOverride = Plain(new Color(0.96f, 0.93f, 0.84f), true),
			RotationDegrees = new Vector3(0, 0, 90),
		});
		_diploma.AddChild(new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = 0.066f, BottomRadius = 0.066f, Height = 0.07f },
			MaterialOverride = Plain(new Color(0.8f, 0.15f, 0.15f)),
			RotationDegrees = new Vector3(0, 0, 90),
		});
		world.AddChild(_diploma);
		world.AddChild(new OmniLight3D { Position = DiplomaSpot + new Vector3(0, 1.4f, 0.4f), LightColor = new Color(1f, 0.85f, 0.55f), LightEnergy = 1.2f, OmniRange = 2.5f });

		_diplomaLight = new SpotLight3D
		{
			Position = DiplomaSpot + new Vector3(0, 4.6f, 0.6f),
			RotationDegrees = new Vector3(-80, 0, 0),
			LightColor = new Color(1f, 0.92f, 0.7f),
			LightEnergy = 6f,
			SpotRange = 7f,
			SpotAngle = 16f,
		};
		world.AddChild(_diplomaLight);
	}

	private void BuildSeats(Node3D world)
	{
		Material fabric = Textured("poltrona", 2f, Colors.White);
		Material arm = Plain(new Color(0.22f, 0.15f, 0.12f));
		const float seatWidth = 0.72f;
		for (int row = 0; row < 5; row++)
		{
			float z = RowZ(row);
			float y = TierTop(row);
			foreach (float side in new[] { -1f, 1f })
			{
				float start = side < 0 ? -5.6f : 1.3f;
				for (int k = 0; k < 6; k++)
				{
					float x = start + seatWidth * (k + 0.5f);
					Block(world, new Vector3(x, y + 0.25f, z), new Vector3(seatWidth - 0.08f, 0.5f, 0.55f), fabric, false);
					Block(world, new Vector3(x, y + 0.65f, z + 0.3f), new Vector3(seatWidth - 0.08f, 0.8f, 0.12f), fabric, false);
					Block(world, new Vector3(x - seatWidth / 2, y + 0.55f, z + 0.05f), new Vector3(0.06f, 0.2f, 0.55f), arm, false);
				}
				// uma colisão só para a fileira inteira
				Collider(world, new Vector3(start + seatWidth * 3, y + 0.5f, z + 0.1f), new Vector3(seatWidth * 6, 1f, 0.8f));
			}
		}
	}

	private void BuildPlayer(Node3D world)
	{
		_body = new CharacterBody3D { Position = new Vector3(0, LandingY + 0.05f, BackZ - 1.2f), FloorSnapLength = 0.45f };
		_body.AddChild(new CollisionShape3D
		{
			Shape = new CapsuleShape3D { Radius = 0.3f, Height = 1.7f },
			Position = new Vector3(0, 0.85f, 0),
		});
		_camera = new Camera3D { Position = new Vector3(0, EyeHeight, 0), Fov = 70f, Current = true };
		_body.AddChild(_camera);
		world.AddChild(_body);

		_sound = new AudioStreamPlayer();
		AddChild(_sound);
		_steps = new AudioStreamPlayer { Stream = GD.Load<AudioStream>("res://sounds/fase3/passo.wav"), VolumeDb = -8f };
		AddChild(_steps);
	}

	private void BuildInterface()
	{
		CanvasLayer layer = new() { Layer = 5 };
		AddChild(layer);

		_fade = new ColorRect { Color = new Color(0, 0, 0, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
		_fade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		layer.AddChild(_fade);

		_prompt = MakeLabel(24, new Color(1f, 0.95f, 0.6f));
		_prompt.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
		_prompt.OffsetTop = -170; _prompt.OffsetBottom = -130; _prompt.OffsetLeft = -300; _prompt.OffsetRight = 300;
		layer.AddChild(_prompt);

		_subtitle = MakeLabel(26, Colors.White);
		_subtitle.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
		_subtitle.OffsetTop = -110; _subtitle.OffsetBottom = -40;
		layer.AddChild(_subtitle);

		_endText = MakeLabel(30, new Color(0.6f, 0.9f, 1f));
		_endText.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_endText.VerticalAlignment = VerticalAlignment.Center;
		layer.AddChild(_endText);
	}

	private static Label MakeLabel(int size, Color color)
	{
		Label label = new()
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeConstantOverride("outline_size", 8);
		label.AddThemeColorOverride("font_outline_color", Colors.Black);
		return label;
	}

	// ---------- jogo ----------

	public override void _Input(InputEvent @event)
	{
		if (GetTree().Paused) return;

		if (_state == State.End)
		{
			if (@event is InputEventKey { Pressed: true, Echo: false } || @event is InputEventMouseButton { Pressed: true })
				GetTree().ReloadCurrentScene();
			return;
		}

		if (_state != State.Walking) return;

		if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			_body.RotateY(-motion.Relative.X * MouseSensitivity);
			_pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, -1.2f, 1.2f);
			_camera.Rotation = new Vector3(_pitch, 0, 0);
		}
		else if (@event is InputEventMouseButton { Pressed: true } && Input.MouseMode != Input.MouseModeEnum.Captured)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;   // voltou do menu de pausa: prende o mouse de novo
		}
		else if (@event.IsActionPressed("interact") && NearDiploma())
		{
			StartCutscene();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GetTree().Paused)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			return;
		}

		float dt = (float)delta;
		Vector3 velocity = _body.Velocity;
		if (!_body.IsOnFloor())
			velocity.Y -= 9.8f * dt;

		bool forward = _state == State.Walking && Input.IsActionPressed("move_up");
		Vector3 direction = -_body.GlobalTransform.Basis.Z;
		direction.Y = 0;
		direction = direction.Normalized();
		velocity.X = forward ? direction.X * WalkSpeed : 0;
		velocity.Z = forward ? direction.Z * WalkSpeed : 0;
		_body.Velocity = velocity;
		_body.MoveAndSlide();

		if (_state != State.Walking) return;

		// passos e o balanço da cabeça
		float moved = new Vector2(_body.Velocity.X, _body.Velocity.Z).Length() * dt;
		_stepDistance += moved;
		_bob += moved * 9f;
		_camera.Position = new Vector3(0, EyeHeight + Mathf.Sin(_bob) * 0.035f, 0);
		if (_stepDistance >= StepLength)
		{
			_stepDistance = 0;
			_steps.PitchScale = (float)GD.RandRange(0.9, 1.1);
			_steps.Play();
		}

		_prompt.Text = NearDiploma() ? "[E] Pegar o diploma" : "";
	}

	public override void _Process(double delta)
	{
		if (_shake <= 0 || GetTree().Paused) return;
		_camera.HOffset = (float)GD.RandRange(-_shake, _shake);
		_camera.VOffset = (float)GD.RandRange(-_shake, _shake);
	}

	private bool NearDiploma()
	{
		Vector3 offset = _body.GlobalPosition - DiplomaSpot;
		return _body.GlobalPosition.Y > StageHeight - 0.2f && new Vector2(offset.X, offset.Z).Length() < PickDistance;
	}

	private async void Say(string text, float seconds)
	{
		_subtitle.Text = text;
		await Wait(seconds);
		if (_subtitle.Text == text)
			_subtitle.Text = "";
	}

	private SignalAwaiter Wait(float seconds) => ToSignal(GetTree().CreateTimer(seconds, false), SceneTreeTimer.SignalName.Timeout);

	private void Play(string name)
	{
		_sound.Stream = GD.Load<AudioStream>($"res://sounds/fase3/{name}.wav");
		_sound.Play();
	}

	// ---------- a cena: a S.I.A. puxa o jogador para dentro do telão ----------

	private async void StartCutscene()
	{
		_state = State.Cutscene;
		_prompt.Text = "";
		_subtitle.Text = "";

		// estica a mão: o diploma sobe um pouco na direção do jogador
		Tween lift = CreateTween();
		lift.TweenProperty(_diploma, "position", _diploma.Position + new Vector3(0, 0.15f, 0.2f), 0.4f);
		await Wait(0.5f);

		// o telão liga sozinho, piscando, e as luzes falham
		Play("glitch");
		for (int i = 0; i < 12; i++)
		{
			bool on = i % 2 == 0;
			_screenMaterial.AlbedoTexture = on ? _screenOn : _screenOff;
			_diplomaLight.LightEnergy = on ? 1f : 6f;
			await Wait((float)GD.RandRange(0.04, 0.12));
		}
		_screenMaterial.AlbedoTexture = _screenOn;
		_diplomaLight.LightEnergy = 0.6f;
		_shake = 0.01f;

		// a câmera vira para o telão
		Transform3D look = new Transform3D(Basis.Identity, _camera.GlobalPosition).LookingAt(ScreenCenter, Vector3.Up);
		Tween turn = CreateTween();
		turn.TweenProperty(_camera, "global_transform", look, 0.9f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		Say("S.I.A.: Achou mesmo que seria tão fácil?", 2.8f);
		await Wait(3f);

		// o diploma cai no chão do palco
		Tween drop = CreateTween();
		drop.TweenProperty(_diploma, "position", new Vector3(0.4f, StageHeight + 0.07f, DiplomaSpot.Z + 0.6f), 0.5f)
			.SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
		Say("S.I.A.: O seu diploma está aqui dentro... venha buscar.", 2.8f);
		await Wait(3f);

		// o puxão: a câmera voa para dentro do telão, o campo de visão estica e a tela estoura em branco
		Play("puxao");
		_shake = 0.04f;
		Transform3D inside = new Transform3D(look.Basis, ScreenCenter + new Vector3(0, 0, 0.15f));
		Tween pull = CreateTween().SetParallel();
		pull.TweenProperty(_camera, "global_transform", inside, 1.5f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
		pull.TweenProperty(_camera, "fov", 125f, 1.5f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
		pull.TweenProperty(_fade, "color", new Color(0.85f, 0.97f, 1f, 1f), 0.35f).SetDelay(1.25f);
		await Wait(1.7f);

		_shake = 0;
		Tween dark = CreateTween();
		dark.TweenProperty(_fade, "color", new Color(0, 0, 0, 1), 0.8f);
		await Wait(1.6f);

		_state = State.End;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		_endText.Text = "Você foi puxado para dentro do sistema da S.I.A.\n\n(continua...)\n\n\n[teste] aperte qualquer tecla para ver de novo";
	}
}
