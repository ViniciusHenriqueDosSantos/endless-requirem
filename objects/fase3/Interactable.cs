using Godot;

// Ponto onde o jogador aperta E (ação "interact") para usar alguma coisa: a fechadura da área final,
// o computador do Laboratório 2, o quadro de energia do Laboratório 1...
// Com o jogador dentro da área (Size, centrada no nó), aparece "[E] Prompt" em cima do objeto.
//   PlayerEntered: o jogador chegou perto (para mostrar uma mensagem automática).
//   Interacted:    o jogador apertou E aqui.
// Enabled desligado: não mostra o "[E]" e ignora a tecla (PlayerEntered continua avisando).
public partial class Interactable : Area2D
{
	[Signal] public delegate void InteractedEventHandler();
	[Signal] public delegate void PlayerEnteredEventHandler();

	[Export] public Vector2 Size = new(160, 128);
	[Export] public string Prompt = "Usar";
	[Export] public bool Enabled = true;

	// Altura do "[E]" acima do nó (pixels do mundo).
	[Export] public float PromptHeight = 90f;

	private bool _playerInside;
	private Label _label;
	private float _time;

	public bool PlayerInside => _playerInside;

	public override void _Ready()
	{
		// Continua rodando com o jogo pausado só para esconder o "[E]" atrás das telas.
		ProcessMode = ProcessModeEnum.Always;
		Monitorable = false;
		AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = Size } });
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		// O texto fica numa camada de tela, para a escuridão da noite não apagar.
		CanvasLayer layer = new() { Layer = 2 };
		_label = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Size = new Vector2(360, 32),
			Modulate = new Color(1f, 0.95f, 0.6f),
		};
		_label.AddThemeFontSizeOverride("font_size", 20);
		_label.AddThemeConstantOverride("outline_size", 6);
		_label.AddThemeColorOverride("font_outline_color", Colors.Black);
		layer.AddChild(_label);
		AddChild(layer);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player) return;
		_playerInside = true;
		EmitSignal(SignalName.PlayerEntered);
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is Player)
			_playerInside = false;
	}

	public override void _Process(double delta)
	{
		_time += (float)delta;
		bool show = _playerInside && Enabled && !GetTree().Paused;
		_label.Visible = show;
		if (!show) return;

		_label.Text = $"[E] {Prompt}";
		Vector2 screen = GetViewport().GetCanvasTransform() * (GlobalPosition + new Vector2(0, -PromptHeight));
		float bob = Mathf.Sin(_time * 4f) * 3f;
		_label.Position = screen - new Vector2(_label.Size.X / 2, _label.Size.Y / 2 - bob);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_playerInside || !Enabled || GetTree().Paused) return;
		if (!@event.IsActionPressed("interact")) return;

		GetViewport().SetInputAsHandled();
		EmitSignal(SignalName.Interacted);
	}
}
