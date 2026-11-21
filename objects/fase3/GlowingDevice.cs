using Godot;

// Aparelho que brilha no escuro (a fechadura da área final, o monitor do laboratório, o quadro de energia).
// Desenha a sprite em pixel art (1 pixel = 4) com a base no nó, um halo pulsando atrás e uma luz no chão.
// Não sofre com a escuridão da noite (é "aceso"), para o jogador achar de longe.
//   BlinkTexture + BlinkInterval > 0: alterna entre as duas sprites (tela piscando).
//   Glow desligado: só a sprite, sem halo e sem luz.
[Tool]
public partial class GlowingDevice : Node2D
{
	private Texture2D _texture;
	private Color _glowColor = new(0.3f, 1f, 0.55f);
	private bool _glow = true;

	[Export] public Texture2D Texture { get => _texture; set { _texture = value; QueueRedraw(); } }
	[Export] public Texture2D BlinkTexture;
	[Export] public float BlinkInterval = 0f;
	[Export] public Color GlowColor { get => _glowColor; set { _glowColor = value; UpdateLight(); QueueRedraw(); } }
	[Export] public bool Glow { get => _glow; set { _glow = value; UpdateLight(); QueueRedraw(); } }

	// Tamanho do halo (pixels) e força da luz no chão.
	[Export] public float GlowRadius = 56f;
	[Export] public float LightEnergy = 0.9f;
	[Export] public float LightScale = 1.2f;

	private const float PixelScale = 4f;
	private PointLight2D _light;
	private float _time;
	private bool _blinkOn;

	public override void _Ready()
	{
		Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
		QueueRedraw();
		if (Engine.IsEditorHint()) return;

		_light = new PointLight2D
		{
			Texture = GD.Load<Texture2D>("res://sprites/lights/radial.png"),
			TextureScale = LightScale,
			Position = new Vector2(0, -SpriteHeight() / 2),
		};
		AddChild(_light);
		UpdateLight();
	}

	// Troca a sprite (e a de piscar; null = para de piscar).
	public void SetTextures(Texture2D texture, Texture2D blink, float interval)
	{
		Texture = texture;
		BlinkTexture = blink;
		BlinkInterval = interval;
		_blinkOn = false;
		QueueRedraw();
	}

	private float SpriteHeight() => _texture != null ? _texture.GetSize().Y * PixelScale : 32f;

	private void UpdateLight()
	{
		if (_light == null) return;
		_light.Visible = _glow;
		_light.Color = _glowColor;
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) return;

		_time += (float)delta;
		if (BlinkInterval > 0 && BlinkTexture != null)
		{
			bool on = Mathf.PosMod(_time, BlinkInterval * 2) < BlinkInterval;
			if (on != _blinkOn)
			{
				_blinkOn = on;
				QueueRedraw();
			}
		}
		if (_glow)
		{
			if (_light != null)
				_light.Energy = LightEnergy * (0.75f + 0.25f * Mathf.Sin(_time * 4f));
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		Texture2D texture = _blinkOn && BlinkTexture != null ? BlinkTexture : _texture;
		float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 4f);

		if (_glow)
		{
			Vector2 center = new(0, -SpriteHeight() / 2);
			for (int i = 3; i >= 1; i--)
			{
				float radius = GlowRadius * (0.45f + 0.2f * i) * (0.92f + 0.08f * pulse);
				DrawCircle(center, radius, new Color(_glowColor, (0.07f + 0.06f * pulse) * (4 - i) / 3f));
			}
		}

		if (texture == null) return;
		Vector2 size = texture.GetSize() * PixelScale;
		DrawTextureRect(texture, new Rect2(-size.X / 2, -size.Y, size.X, size.Y), false);
	}
}
