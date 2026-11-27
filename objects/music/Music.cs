using Godot;

// Música de fundo do jogo inteiro (autoload "Music", sempre presente). As faixas ficam em sounds/music/
// e são geradas por tools/audio/music.py:
//   tema     intro, menu, final e o auditório (cutscenes)
//   explorar andando pelas fases
//   luta       ondas de fantasmas, chefes e a escada desabando
//   biblioteca Bibliotecária patrulhando (furtividade): quase silêncio
//   cacada     Bibliotecária te caçando ou na luta aberta
//   chefe      Tia da Cantina (Room.FightMusic da Cantina)
//   fuga       escada desabando
//   laboratorio Fase 3 (4º andar)
//   alarme     Fase 3 com o alarme tocando (Room.FightMusic do corredor)
//   sia        auditório 3D, a S.I.A. no telão
//   arcade     Pac-Man dentro do telão
//   final      tela final (o diploma)
// Music.Play("faixa") troca com fade (pedir a que já está tocando não faz nada).
// As salas avisam quando uma luta começa e termina (FightStarted/FightEnded): com alguma luta acontecendo,
// toca "luta"; sem nenhuma, volta para a música da fase (a última pedida com Play).
// Music.Override("faixa") passa na frente de tudo (ex.: a luta da Bibliotecária, que muda com o que ela faz);
// Music.Override("") devolve para a música normal.
public partial class Music : Node
{
	private const float Volume = -11f;        // dB: baixo o bastante para os efeitos e a chuva aparecerem

	// Ajuste por faixa (dB), somado ao Volume: a da biblioteca é para ser quase silêncio.
	private static readonly System.Collections.Generic.Dictionary<string, float> TrackGain = new()
	{
		["biblioteca"] = -3f,
	};
	private const float Silent = -60f;
	private const float FadeTime = 1.2f;

	private static Music _instance;

	private AudioStreamPlayer _current;
	private AudioStreamPlayer _other;
	private string _playing = "";
	private string _levelTrack = "";
	private string _override = "";
	private string _fightTrack = "luta";
	private int _fights;

	public override void _Ready()
	{
		_instance = this;
		ProcessMode = ProcessModeEnum.Always;   // continua tocando com o jogo pausado (telas, menu de pausa)
		_current = MakePlayer();
		_other = MakePlayer();
	}

	private AudioStreamPlayer MakePlayer()
	{
		AudioStreamPlayer player = new() { VolumeDb = Silent, ProcessMode = ProcessModeEnum.Always };
		AddChild(player);
		return player;
	}

	// Música de fundo da cena (a fase, o menu...). Durante uma luta, fica guardada para quando ela acabar.
	public static void Play(string track)
	{
		if (_instance == null) return;
		_instance._levelTrack = track;
		_instance._fights = 0;   // cena nova: nenhuma luta em andamento
		_instance._override = "";
		_instance.SwitchTo(track);
	}

	// Faixa que passa na frente da música da fase e da de luta ("" = volta ao normal).
	// Pode chamar a cada quadro: só troca quando muda.
	public static void Override(string track)
	{
		if (_instance == null || track == _instance._override) return;
		_instance._override = track;
		_instance.SwitchTo(track != "" ? track : _instance.NormalTrack());
	}

	private string NormalTrack() => _fights > 0 ? _fightTrack : _levelTrack;

	// Uma sala começou a luta (ondas, chefe, alarme): toca "luta" até a última acabar.
	// track: a música desta luta (cada sala escolhe a sua, Room.FightMusic; o padrão é "luta").
	public static void FightStarted(string track = "luta")
	{
		if (_instance == null) return;
		_instance._fights++;
		_instance._fightTrack = track == "" ? "luta" : track;
		if (_instance._override == "")
			_instance.SwitchTo(_instance._fightTrack);
	}

	public static void FightEnded()
	{
		if (_instance == null || _instance._fights == 0) return;
		_instance._fights--;
		if (_instance._fights == 0 && _instance._levelTrack != "" && _instance._override == "")
			_instance.SwitchTo(_instance._levelTrack);
	}

	private void SwitchTo(string track)
	{
		if (track == _playing) return;
		_playing = track;

		AudioStream stream = Load(track);
		if (stream == null) return;

		// a que está tocando some; a outra entra com a faixa nova
		(_current, _other) = (_other, _current);
		_current.Stream = stream;
		_current.VolumeDb = Silent;
		_current.Play();

		Tween tween = CreateTween().SetParallel();
		float volume = Volume + (TrackGain.TryGetValue(track, out float gain) ? gain : 0f);
		tween.TweenProperty(_current, "volume_db", volume, FadeTime).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(_other, "volume_db", Silent, FadeTime).SetTrans(Tween.TransitionType.Sine);
		tween.Chain().TweenCallback(Callable.From(() => { if (_other.VolumeDb <= Silent + 1) _other.Stop(); }));
	}

	// As faixas são OGG (WAV se alguém gerar de novo sem o ffmpeg): liga a repetição aqui
	// (o fim de cada uma já emenda no começo).
	private static AudioStream Load(string track)
	{
		string path = $"res://sounds/music/{track}.ogg";
		if (!ResourceLoader.Exists(path))
			path = $"res://sounds/music/{track}.wav";
		AudioStream stream = GD.Load<AudioStream>(path);
		if (stream is AudioStreamOggVorbis ogg)
			ogg.Loop = true;
		else if (stream is AudioStreamWav wav && wav.LoopMode == AudioStreamWav.LoopModeEnum.Disabled)
		{
			wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
			wav.LoopBegin = 0;
			wav.LoopEnd = (int)(wav.GetLength() * wav.MixRate);
		}
		return stream;
	}
}
