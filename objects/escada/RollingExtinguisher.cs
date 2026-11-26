using Godot;

// Extintor que vem rolando escada abaixo quando o jogador entra no lance.
// Encostou no jogador: dano e empurrão para trás. Um tiro de qualquer arma estoura (nuvem de pó).
// Fica no alto do lance esperando; rola para baixo (direção +Y da tela). Reset() volta ao lugar.
public partial class RollingExtinguisher : Area2D, IHittable
{
	[Export] public Texture2D Frame0;
	[Export] public Texture2D Frame1;
	[Export] public Texture2D Puff;

	// Começa a rolar quando o jogador está até esta distância abaixo dele (pixels).
	[Export] public float TriggerDistance = 520f;
	[Export] public float Speed = 300f;
	[Export] public float RollLength = 760f;

	private const float PixelScale = 4f;
	private const float PuffTime = 0.5f;

	private enum State { Waiting, Rolling, Popped, Gone }

	// Extintores rolando agora (a lâmpada espera eles passarem antes de cair).
	private static readonly System.Collections.Generic.List<RollingExtinguisher> _rolling = new();

	// Tem extintor rolando na mesma coluna que ainda vai passar por este ponto?
	public static bool AnyAbove(Vector2 point)
	{
		foreach (RollingExtinguisher e in _rolling)
			if (IsInstanceValid(e) && Mathf.Abs(e.GlobalPosition.X - point.X) < 200f && e.GlobalPosition.Y < point.Y + 60f)
				return true;
		return false;
	}

	private void SetState(State state)
	{
		if (state == State.Rolling) { if (!_rolling.Contains(this)) _rolling.Add(this); }
		else _rolling.Remove(this);
		_state = state;
	}

	public override void _ExitTree() => _rolling.Remove(this);

	private State _state = State.Waiting;
	private Vector2 _home;
	private float _rolled;
	private float _timer;
	private Player _player;

	public override void _Ready()
	{
		_home = Position;
		Monitoring = true;
		Monitorable = true;
		CollisionMask = 1u | Prop.FurnitureLayer;   // jogador, barricadas e buracos (Prop)
		AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(56, 32) }, Position = new Vector2(0, -16) });
		BodyEntered += OnBodyEntered;
		Hide();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (!IsInstanceValid(_player))
			_player = GetTree().GetFirstNodeInGroup("player") as Player;

		switch (_state)
		{
			case State.Waiting:
				if (_player != null && _player.GlobalPosition.Y > GlobalPosition.Y
					&& _player.GlobalPosition.Y - GlobalPosition.Y < TriggerDistance
					&& Mathf.Abs(_player.GlobalPosition.X - GlobalPosition.X) < 160f
					&& !FallingLamp.AnyBusy)   // não passa enquanto uma lâmpada avisa ou cai
				{
					SetState(State.Rolling);
					Show();
				}
				break;
			case State.Rolling:
				Position += new Vector2(0, Speed * dt);
				_rolled += Speed * dt;
				_timer += dt;
				if (_rolled > RollLength) { SetState(State.Gone); Hide(); }
				break;
			case State.Popped:
				_timer -= dt;
				if (_timer <= 0) { SetState(State.Gone); Hide(); }
				break;
		}
		QueueRedraw();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_state != State.Rolling) return;

		if (body is Player player)
		{
			player.TakeHit(GlobalPosition + new Vector2(0, -60), 650f, true);   // o dash não atravessa o extintor
			Pop();
		}
		else if (body is Barricade barricade && !barricade.IsDestroyed)
		{
			Pop();                              // bateu na barricada e estourou
		}
		else if (body is Prop { Solid: true, ZIndex: < 0 })
		{
			SetState(State.Gone);                // caiu no buraco
			Hide();
		}
	}

	// Tiro: estoura numa nuvem de pó.
	public void TakeHit(int damage, Vector2 direction)
	{
		if (_state == State.Rolling) Pop();
	}

	private void Pop()
	{
		SetState(State.Popped);
		_timer = PuffTime;
		_player?.TriggerShake(4f);
	}

	// O lance ficou para trás: não rola mais (e some, se estava rolando).
	public void Deactivate()
	{
		if (_state is State.Waiting or State.Rolling)
		{
			SetState(State.Gone);
			Hide();
		}
	}

	public void Reset()
	{
		SetState(State.Waiting);
		Position = _home;
		_rolled = 0;
		_timer = 0;
		Hide();
	}

	public override void _Draw()
	{
		if (_state == State.Rolling)
		{
			Texture2D tex = Mathf.PosMod(_timer, 0.16f) < 0.08f ? Frame0 : Frame1;
			if (tex == null) return;
			Vector2 size = tex.GetSize() * PixelScale;
			DrawTextureRect(tex, new Rect2(new Vector2(-size.X / 2, -size.Y), size), false);
		}
		else if (_state == State.Popped && Puff != null)
		{
			Vector2 size = Puff.GetSize() * PixelScale * (1.5f - _timer / PuffTime * 0.5f);
			DrawTextureRect(Puff, new Rect2(new Vector2(-size.X / 2, -size.Y), size), false,
				new Color(1, 1, 1, _timer / PuffTime));
		}
	}
}
