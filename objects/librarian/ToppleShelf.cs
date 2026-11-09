using Godot;
using System.Collections.Generic;

// Estante bamba da biblioteca: a arma contra a Bibliotecária.
//   - Tiro na estante: os livros caem lá de cima e fazem BARULHO (ela vem ver o que foi). A estante não cai com tiro.
//   - Empurrar: atrás da estante (do lado de cima), [Espaço] empurra e ela tomba para a frente.
//     Quem estiver na frente (FallZone) é esmagado. Dá para empurrar sem ela ver: a estante tapa a visão.
//   - Caída, vira entulho no chão por um tempo e depois a Bibliotecária a põe de pé de novo (ReriseTime).
[Tool]
public partial class ToppleShelf : Prop, IHittable
{
	[Signal] public delegate void FellEventHandler(ToppleShelf shelf);

	[Export] public Texture2D FallenTexture;
	[Export] public float FallDepth = 130f;      // até onde ela cai, para a frente (px)
	[Export] public float NoiseRadius = 1300f;
	[Export] public float ReriseTime = 9f;        // segundos caída até voltar a ficar de pé
	[Export] public float NoiseCooldown = 1.5f;   // um barulho por vez (tiro de escopeta não vira cinco)

	public enum State { Standing, Falling, Fallen, Rising }
	public State Current { get; private set; } = State.Standing;
	public bool IsStanding => Current == State.Standing;

	// Onde ela vai olhar quando ouvir o barulho: logo na frente da estante.
	public Vector2 NoisePoint => GlobalPosition + new Vector2(0, 64);
	// Um pouco maior que a estante dos lados: quem está "embaixo" dela também leva.
	public Rect2 FallZone => new(GlobalPosition.X - Size.X / 2 - 32, GlobalPosition.Y - 40, Size.X + 64, FallDepth + 40);
	// Atrás da estante (o corredor de cima), de onde o jogador empurra.
	public Rect2 PushZone => new(GlobalPosition.X - Size.X / 2 + 8, GlobalPosition.Y - Depth - 96, Size.X - 16, 104);

	private static readonly List<ToppleShelf> All = new();

	private float _wobble;
	private float _anim;
	private float _time;
	private float _noiseTimer;
	private float _fallenTimer;
	private bool _playerBehind;
	private Node2D _hint;
	private const float FallTime = 0.35f;
	private const float RiseTime = 0.6f;

	public override void _Ready()
	{
		base._Ready();
		SetProcess(true);
		if (Engine.IsEditorHint()) return;
		All.Add(this);
		// dica "[Espaço] Empurrar": num filho que aparece no escuro
		_hint = new Node2D { Material = SharedMaterials.Unshaded, ZIndex = 20, Visible = false };
		AddChild(_hint);
		_hint.Draw += DrawHint;
	}

	public override void _ExitTree() => All.Remove(this);

	// Chamado pelo jogador ao apertar [Espaço]: empurra a estante de pé que ele está atrás. Se empurrou, não rola.
	public static bool TryPush(Node2D player)
	{
		foreach (ToppleShelf shelf in All)
			if (shelf.IsStanding && shelf.Visible && shelf.PushZone.HasPoint(player.GlobalPosition))
			{
				shelf.Topple();
				return true;
			}
		return false;
	}

	public void TakeHit(int damage, Vector2 direction)
	{
		if (!IsStanding || _noiseTimer > 0) return;
		_wobble = 1f;
		_noiseTimer = NoiseCooldown;
		Noise.Make(this, NoisePoint, NoiseRadius);
		(GetTree().GetFirstNodeInGroup("player") as Player)?.TriggerShake(3f);
	}

	public void Topple()
	{
		if (!IsStanding) return;
		Current = State.Falling;
		_anim = 0f;
	}

	private void SetBlocking(bool blocking)
	{
		Tall = blocking;
		foreach (Node child in GetChildren())
			if (child is CollisionShape2D shape)
				shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, !blocking);
		if (blocking) { AddToGroup("obstacles"); AddToGroup("cover"); }
		else { RemoveFromGroup("obstacles"); RemoveFromGroup("cover"); }
	}

	private void Land()
	{
		Current = State.Fallen;
		_fallenTimer = ReriseTime;
		SetBlocking(false);
		Noise.Make(this, NoisePoint, NoiseRadius * 1.2f);
		(GetTree().GetFirstNodeInGroup("player") as Player)?.TriggerShake(14f);
		EmitSignal(SignalName.Fell, this);
	}

	// Volta a ficar de pé na hora (jogador reprovado).
	public void ResetShelf()
	{
		Current = State.Standing;
		_wobble = _anim = 0;
		SetBlocking(true);
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Engine.IsEditorHint()) return;
		float dt = (float)delta;
		_time += dt;
		if (_noiseTimer > 0) _noiseTimer -= dt;
		_wobble = Mathf.Max(0f, _wobble - dt * 1.5f);

		switch (Current)
		{
			case State.Falling:
				_anim += dt / FallTime;
				if (_anim >= 1f) Land();
				break;
			case State.Fallen:
				_fallenTimer -= dt;
				// não levanta em cima de ninguém
				if (_fallenTimer <= 0 && !SomeoneInFallZone())
				{
					Current = State.Rising;
					_anim = 0f;
				}
				break;
			case State.Rising:
				_anim += dt / RiseTime;
				if (_anim >= 1f)
				{
					Current = State.Standing;
					SetBlocking(true);
				}
				break;
		}

		var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
		bool behind = IsStanding && player != null && PushZone.HasPoint(player.GlobalPosition);
		if (behind != _playerBehind) { _playerBehind = behind; _hint.Visible = behind; _hint.QueueRedraw(); }
		QueueRedraw();
	}

	private bool SomeoneInFallZone()
	{
		var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
		if (player != null && FallZone.HasPoint(player.GlobalPosition)) return true;
		foreach (Node n in GetTree().GetNodesInGroup("librarian"))
			if (n is Node2D l && FallZone.HasPoint(l.GlobalPosition)) return true;
		return false;
	}

	public override void _Draw()
	{
		if (Texture == null) { base._Draw(); return; }
		Vector2 size = Texture.GetSize() * PixelScale;

		bool fallenLook = Current == State.Fallen || (Current == State.Falling && _anim > 0.6f) || (Current == State.Rising && _anim < 0.4f);
		if (FallenTexture != null && fallenLook)
		{
			Vector2 fs = FallenTexture.GetSize() * PixelScale;
			float k = Current == State.Falling ? (_anim - 0.6f) / 0.4f : Current == State.Rising ? 1f - _anim / 0.4f : 1f;
			DrawTextureRect(FallenTexture, new Rect2(-fs.X / 2, -24, fs.X, fs.Y * Mathf.Max(0.3f, k)), false);
			return;
		}

		if (Current is State.Falling or State.Rising)
		{
			// tomba para a frente (o topo desce até o chão) ou volta a subir
			float k = Current == State.Falling ? _anim : 1f - (_anim - 0.4f) / 0.6f;
			float h = size.Y * (1f - k * 0.85f);
			DrawTextureRect(Texture, new Rect2(-size.X / 2, -h + k * 40f, size.X, h), false,
				Current == State.Rising ? new Color(0.8f, 0.85f, 1f, 0.9f) : Colors.White);
			return;
		}

		float shake = Mathf.Sin(_time * 38f) * 6f * _wobble;
		DrawTextureRect(Texture, new Rect2(-size.X / 2 + shake, -size.Y, size.X, size.Y), false);

	}

	private void DrawHint()
	{
		Font font = ThemeDB.FallbackFont;
		string hint = "[Espaço] Empurrar a estante";
		float w = font.GetStringSize(hint, HorizontalAlignment.Left, -1, 18).X;
		float y = -Texture.GetSize().Y * PixelScale - 30;
		_hint.DrawRect(new Rect2(-w / 2 - 8, y - 4, w + 16, 28), new Color(0, 0, 0, 0.65f));
		_hint.DrawString(font, new Vector2(-w / 2, y + 18), hint, HorizontalAlignment.Left, -1, 18, new Color(1f, 0.92f, 0.6f));
	}
}
