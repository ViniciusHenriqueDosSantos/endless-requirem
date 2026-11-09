using Godot;

// Chuva da fase: riscos e respingos (rain.gdshader), o som da chuva e os relâmpagos com trovão.
// A máscara (1 pixel por tile, gerada pelo tools/levelgen) diz onde chove; dentro das salas não chove,
// o som fica abafado (filtro passa-baixa no bus "Chuva") e o relâmpago clareia menos.
public partial class Rain : Node2D
{
	[Export] public Texture2D Mask;
	[Export] public Vector2 MaskOrigin;            // tile do pixel (0,0) da máscara
	[Export] public float TileSize = 64f;
	[Export] public int ChunkTiles = 6;            // a chuva é desenhada em pedaços: cada um só recebe as luzes de perto
	[Export] public float FirstLightning = 7f;     // segundos até o primeiro relâmpago
	[Export] public Vector2 LightningInterval = new(14f, 32f);
	[Export] public AudioStream RainSound;
	[Export] public AudioStream[] ThunderSounds = System.Array.Empty<AudioStream>();
	[Export] public float RainVolumeOutside = -6f;
	[Export] public float RainVolumeInside = -15f;

	private const string Bus = "Chuva";
	private static readonly Color Flash = new(0.78f, 0.82f, 0.98f);

	private ShaderMaterial _material;
	private Image _mask;
	private CanvasModulate _night;
	private Color _nightBase;
	private Node2D _player;
	private AudioStreamPlayer _rain, _thunder;
	private AudioEffectLowPassFilter _lowPass;
	private int _bus;
	private RandomNumberGenerator _rng = new();
	private float _toLightning, _time, _inside, _flash;
	private Tween _flashTween;

	public override void _Ready()
	{
		_rng.Randomize();
		_toLightning = FirstLightning;
		_mask = Mask?.GetImage();
		BuildChunks();
		SetupAudio();
	}

	private void BuildChunks()
	{
		_material = new ShaderMaterial { Shader = GD.Load<Shader>("res://objects/weather/rain.gdshader") };
		_material.SetShaderParameter("mask", Mask);
		_material.SetShaderParameter("mask_origin", MaskOrigin);
		_material.SetShaderParameter("tile_size", TileSize);
		if (_mask == null) return;

		int w = _mask.GetWidth(), h = _mask.GetHeight();
		for (int cy = 0; cy < h; cy += ChunkTiles)
		for (int cx = 0; cx < w; cx += ChunkTiles)
		{
			if (!ChunkHasRain(cx, cy, w, h)) continue;
			int tw = Mathf.Min(ChunkTiles, w - cx), th = Mathf.Min(ChunkTiles, h - cy);
			AddChild(new ColorRect
			{
				Position = (MaskOrigin + new Vector2(cx, cy)) * TileSize,
				Size = new Vector2(tw, th) * TileSize,
				Material = _material,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			});
		}
	}

	private bool ChunkHasRain(int cx, int cy, int w, int h)
	{
		for (int y = cy; y < Mathf.Min(cy + ChunkTiles, h); y++)
		for (int x = cx; x < Mathf.Min(cx + ChunkTiles, w); x++)
		{
			var c = _mask.GetPixel(x, y);
			if (c.R > 0f || c.B > 0f) return true;
		}
		return false;
	}

	private void SetupAudio()
	{
		_bus = AudioServer.GetBusIndex(Bus);
		if (_bus < 0)
		{
			AudioServer.AddBus();
			_bus = AudioServer.BusCount - 1;
			AudioServer.SetBusName(_bus, Bus);
			AudioServer.SetBusSend(_bus, "Master");
			AudioServer.AddBusEffect(_bus, new AudioEffectLowPassFilter { CutoffHz = 20000f });
		}
		_lowPass = AudioServer.GetBusEffect(_bus, 0) as AudioEffectLowPassFilter;

		// O som continua quando o jogo pausa (inspeção de item): só a imagem congela.
		if (RainSound is AudioStreamOggVorbis ogg) ogg.Loop = true;
		_rain = new AudioStreamPlayer { Stream = RainSound, Bus = Bus, VolumeDb = RainVolumeOutside, ProcessMode = ProcessModeEnum.Always };
		_thunder = new AudioStreamPlayer { Bus = Bus, ProcessMode = ProcessModeEnum.Always };
		AddChild(_rain);
		AddChild(_thunder);
		if (RainSound != null) _rain.Play();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_time += dt;
		if (!IsInstanceValid(_night))
		{
			_night = GetTree().GetFirstNodeInGroup("night") as CanvasModulate;
			if (_night != null) _nightBase = _night.Color;
		}
		if (!IsInstanceValid(_player)) _player = GetTree().GetFirstNodeInGroup("player") as Node2D;

		// dentro das salas: chuva abafada
		float target = _player != null && IsDry(_player.GlobalPosition) ? 1f : 0f;
		_inside = Mathf.MoveToward(_inside, target, dt * 2.5f);
		if (_lowPass != null) _lowPass.CutoffHz = Mathf.Exp(Mathf.Lerp(Mathf.Log(18000f), Mathf.Log(650f), _inside));
		_rain.VolumeDb = Mathf.Lerp(RainVolumeOutside, RainVolumeInside, _inside);

		// rajadas: a chuva engrossa e afina devagar
		float gust = 0.82f + 0.12f * Mathf.Sin(_time * 0.21f) + 0.06f * Mathf.Sin(_time * 0.83f + 1.7f);
		_material.SetShaderParameter("intensity", gust);

		// o relâmpago clareia a escuridão (menos dentro das salas)
		if (_night != null)
		{
			float f = _flash * (1f - 0.6f * _inside);
			var c = _nightBase.Lerp(Flash, f);
			_night.Color = c;
			float lum = 0.3f * c.R + 0.5f * c.G + 0.2f * c.B;
			_material.SetShaderParameter("boost", Mathf.Clamp(0.55f / Mathf.Max(lum, 0.01f), 1f, 4.5f));
		}

		_toLightning -= dt;
		if (_toLightning <= 0f)
		{
			_toLightning = _rng.RandfRange(LightningInterval.X, LightningInterval.Y);
			Lightning();
		}
	}

	private bool IsDry(Vector2 pos)
	{
		if (_mask == null) return false;
		var t = (pos / TileSize).Floor() - MaskOrigin;
		if (t.X < 0 || t.Y < 0 || t.X >= _mask.GetWidth() || t.Y >= _mask.GetHeight()) return false;
		return _mask.GetPixel((int)t.X, (int)t.Y).R <= 0f;
	}

	/// <summary>Clarão (às vezes duplo) e, depois de um tempo que depende da distância, o trovão.</summary>
	public void Lightning()
	{
		bool near = _rng.Randf() < 0.4f;
		float peak = near ? 0.75f : _rng.RandfRange(0.3f, 0.5f);

		_flashTween?.Kill();
		_flashTween = CreateTween();
		float from = _flash;
		void Step(float to, float time, Tween.TransitionType trans = Tween.TransitionType.Linear)
		{
			_flashTween.TweenMethod(Callable.From((float v) => _flash = v), from, to, time)
				.SetTrans(trans).SetEase(Tween.EaseType.Out);
			from = to;
		}
		Step(peak, 0.04f);
		Step(peak * 0.15f, 0.07f);
		if (near || _rng.Randf() < 0.5f)
		{
			Step(peak * 0.9f, 0.04f);
			Step(peak * 0.8f, 0.06f);
		}
		Step(0f, near ? 0.9f : 0.5f, Tween.TransitionType.Quad);

		if (ThunderSounds.Length == 0) return;
		// trovão perto: o estalo vem quase junto; longe: o ronco chega depois
		var stream = near || ThunderSounds.Length == 1 ? ThunderSounds[0] : ThunderSounds[_rng.RandiRange(1, ThunderSounds.Length - 1)];
		float delay = near ? _rng.RandfRange(0.15f, 0.5f) : _rng.RandfRange(1.2f, 3f);
		float volume = near ? 0f : _rng.RandfRange(-7f, -2f);
		GetTree().CreateTimer(delay).Timeout += () =>
		{
			if (!IsInstanceValid(this)) return;
			_thunder.Stream = stream;
			_thunder.VolumeDb = volume;
			_thunder.Play();
		};
	}
}
