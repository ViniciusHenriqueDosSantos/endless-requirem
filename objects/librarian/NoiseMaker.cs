using Godot;

// Objeto que faz barulho na biblioteca: o jeito de atrair a Bibliotecária. Tem um em cada estante.
// Fica num banquinho perto da ponta de uma estante: com [E] ele faz barulho, ela vem ver o que foi e para
// na frente da estante (no ponto de isca, LureOffset), e aí é só empurrar a estante ([E] por trás) nela.
//   Campainha:   "ding!" na hora.
//   Rádio:       chia por uns 3 segundos (o barulho se repete enquanto toca).
//   Despertador: dá corda e toca DelayTime segundos depois (tempo de correr para trás da estante).
//   Telefone:    toca na hora, por uns 3 segundos.
//   Livros:      a pilha de livros desaba (barulhão na hora). Só uma vez: depois fica caída.
// Depois de usado, descansa um pouco (Cooldown) antes de poder usar de novo.
public partial class NoiseMaker : Interactable
{
	public enum KindEnum { Campainha, Radio, Despertador, Telefone, Livros }

	[Export] public KindEnum Kind = KindEnum.Campainha;
	// Folha com 2 quadros lado a lado: parado e fazendo barulho.
	[Export] public Texture2D Texture;
	[Export] public float NoiseRadius = 1300f;
	[Export] public float Cooldown = 6f;
	[Export] public float DelayTime = 3f;
	// Onde ela vai parar para olhar, a partir do objeto (na frente da estante, longe de onde você empurra).
	[Export] public Vector2 LureOffset = new(0, 20);

	private const float PulseInterval = 1.2f;

	private AudioStreamPlayer2D _sound;
	private Sprite2D _sprite;
	private float _delay;          // despertador com corda, esperando para tocar
	private float _ringing;        // tempo que ainda fica fazendo barulho
	private float _pulse;
	private float _cooldown;
	private float _shake;
	private bool _spent;           // pilha de livros que já desabou

	// Onde ela vai olhar (o ponto de isca, na frente da estante).
	public Vector2 NoisePoint => GlobalPosition + LureOffset;

	public override void _Ready()
	{
		Prompt = Kind switch
		{
			KindEnum.Radio => "Ligar o rádio",
			KindEnum.Despertador => "Dar corda no despertador",
			KindEnum.Telefone => "Discar no telefone",
			KindEnum.Livros => "Derrubar a pilha de livros",
			_ => "Tocar a campainha",
		};
		PromptHeight = 100f;
		base._Ready();
		AddToGroup("noise_makers");
		ProcessMode = ProcessModeEnum.Inherit;   // o barulho para junto com o jogo pausado

		_sprite = new Sprite2D
		{
			Texture = Texture,
			Hframes = 2,
			Scale = Vector2.One * 4f,
			Centered = true,
			TextureFilter = TextureFilterEnum.Nearest,
		};
		if (Texture != null)
			_sprite.Position = new Vector2(0, -Texture.GetHeight() * 2f);
		AddChild(_sprite);

		string file = Kind switch
		{
			KindEnum.Radio => "radio", KindEnum.Despertador => "despertador", KindEnum.Telefone => "telefone",
			KindEnum.Livros => "livros", _ => "campainha",
		};
		_sound = new AudioStreamPlayer2D
		{
			Stream = GD.Load<AudioStream>($"res://sounds/objects/{file}.wav"),
			MaxDistance = 1600f,
			VolumeDb = -4f,
		};
		AddChild(_sound);

		Interacted += Use;
	}

	private void Use()
	{
		if (_cooldown > 0 || _ringing > 0 || _delay > 0 || _spent) return;
		// atrás de uma estante, o [E] é para empurrar a estante (não para usar o objeto)
		if (GetTree().GetFirstNodeInGroup("player") is Node2D player && ToppleShelf.CanPush(player)) return;

		Hud hud = GetTree().GetFirstNodeInGroup("hud") as Hud;
		if (Kind == KindEnum.Despertador)
		{
			_delay = DelayTime;
			Enabled = false;
			hud?.ShowMessage($"Tic-tac... vai tocar em {DelayTime:0} segundos. Esconda-se atrás da estante!", 3f);
			return;
		}
		Ring();
	}

	private void Ring()
	{
		Enabled = false;
		_ringing = Kind switch
		{
			KindEnum.Campainha => 0.6f, KindEnum.Livros => 0.9f, KindEnum.Radio => 3f, KindEnum.Telefone => 2.8f, _ => 2.6f,
		};
		_pulse = 0;
		_sound.Play();
	}

	// Volta ao começo (jogador reprovado): para de tocar e pode usar de novo.
	public void ResetMaker()
	{
		_delay = _ringing = _cooldown = 0;
		_spent = false;
		_sound.Stop();
		Enabled = true;
		_sprite.Frame = 0;
		_sprite.Position = new Vector2(0, _sprite.Position.Y);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		float dt = (float)delta;

		if (_delay > 0)
		{
			_delay -= dt;
			_shake += dt;
			_sprite.Position = new Vector2(Mathf.Sin(_shake * 30f) * 1.5f, _sprite.Position.Y);   // tiquetaqueando
			if (_delay <= 0)
				Ring();
			return;
		}

		if (_ringing > 0)
		{
			_ringing -= dt;
			_pulse -= dt;
			if (_pulse <= 0)
			{
				_pulse = PulseInterval;
				Noise.Make(this, NoisePoint, NoiseRadius);
			}
			_shake += dt;
			_sprite.Frame = (int)(_shake * 10f) % 2 == 0 ? 1 : 0;
			_sprite.Position = new Vector2(Mathf.Sin(_shake * 45f) * 3f, _sprite.Position.Y);
			if (_ringing <= 0)
			{
				_cooldown = Cooldown;
				if (Kind == KindEnum.Livros)
				{
					// desabou de vez: fica caída e não dá para usar de novo
					_spent = true;
					_cooldown = 0;
					_sprite.Frame = 1;
					_sprite.Position = new Vector2(0, _sprite.Position.Y);
					return;
				}
				_sprite.Frame = 0;
				_sprite.Position = new Vector2(0, _sprite.Position.Y);
			}
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown -= dt;
			if (_cooldown <= 0)
				Enabled = true;
		}
	}
}
