using Godot;

// Tela 2D do teclado de senha da porta da área final (Fase 3).
// O jogador clica nas teclas (ou digita 0-9, Backspace apaga, Enter confirma).
// Senha certa: "ACESSO LIBERADO", dispara Solved e fecha sozinha. Errada: zumbido, a placa treme e o visor limpa.
public partial class KeypadScreen : OverlayScreen
{
	// Senha certa (o roteiro da fase sorteia a cada partida).
	public string Code = "0000";

	private const int Digits = 4;
	private const float ShakeTime = 0.35f;
	private const float CloseDelay = 0.9f;

	private static readonly Color LcdOn = new(0.45f, 1f, 0.6f);
	private static readonly Color LcdError = new(1f, 0.35f, 0.3f);

	private Control _center;
	private PanelContainer _plate;
	private Label _display;
	private Label _status;
	private string _typed = "";
	private float _shake;
	private float _closeTimer = -1f;
	private bool _solved;

	protected override void BuildUi()
	{
		CenterContainer center = new();
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		Root.AddChild(center);
		_center = center;

		_plate = new PanelContainer();
		_plate.AddThemeStyleboxOverride("panel", MakeBox(new Color(0.16f, 0.17f, 0.19f), new Color(0.45f, 0.47f, 0.5f), 4, 10, 22));
		center.AddChild(_plate);

		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 14);
		_plate.AddChild(column);

		column.AddChild(MakeLabel("ÁREA FINAL — ACESSO RESTRITO", 18, new Color(1f, 0.55f, 0.45f)));

		PanelContainer lcd = new();
		lcd.AddThemeStyleboxOverride("panel", MakeBox(new Color(0.05f, 0.14f, 0.09f), new Color(0.02f, 0.05f, 0.03f), 3, 4, 10));
		_display = MakeLabel("", 40, LcdOn, false);
		lcd.AddChild(_display);
		column.AddChild(lcd);

		GridContainer keys = new() { Columns = 3 };
		keys.AddThemeConstantOverride("h_separation", 10);
		keys.AddThemeConstantOverride("v_separation", 10);
		foreach (string key in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "OK" })
		{
			string pressed = key;
			Button button = new()
			{
				Text = key,
				CustomMinimumSize = new Vector2(96, 64),
				FocusMode = Control.FocusModeEnum.None,
			};
			button.AddThemeFontSizeOverride("font_size", 30);
			Color face = key == "OK" ? new Color(0.2f, 0.5f, 0.3f) : key == "C" ? new Color(0.55f, 0.22f, 0.2f) : new Color(0.78f, 0.8f, 0.83f);
			Color text = key is "OK" or "C" ? Colors.White : new Color(0.12f, 0.12f, 0.14f);
			button.AddThemeStyleboxOverride("normal", MakeBox(face, face.Darkened(0.4f), 3, 6));
			button.AddThemeStyleboxOverride("hover", MakeBox(face.Lightened(0.15f), face.Darkened(0.4f), 3, 6));
			button.AddThemeStyleboxOverride("pressed", MakeBox(face.Darkened(0.2f), face.Darkened(0.5f), 3, 6));
			button.AddThemeColorOverride("font_color", text);
			button.AddThemeColorOverride("font_hover_color", text);
			button.AddThemeColorOverride("font_pressed_color", text);
			button.Pressed += () => Press(pressed);
			keys.AddChild(button);
		}
		column.AddChild(keys);

		_status = MakeLabel("Digite a senha de 4 números", 16, new Color(0.75f, 0.78f, 0.8f));
		column.AddChild(_status);
		column.AddChild(MakeLabel("Teclado ou mouse  •  Enter confirma  •  Esc sai", 13, new Color(0.5f, 0.52f, 0.55f)));
	}

	protected override void OnOpened()
	{
		_typed = "";
		_status.Text = "Digite a senha de 4 números";
		RefreshDisplay(LcdOn);
	}

	protected override bool HandleKey(InputEventKey key)
	{
		if (_solved) return true;

		Key k = key.Keycode;
		if (k >= Key.Key0 && k <= Key.Key9)
			Press(((int)(k - Key.Key0)).ToString());
		else if (k >= Key.Kp0 && k <= Key.Kp9)
			Press(((int)(k - Key.Kp0)).ToString());
		else if (k == Key.Backspace)
		{
			if (_typed.Length > 0) _typed = _typed[..^1];
			PlaySound("bip");
			RefreshDisplay(LcdOn);
		}
		else if (k is Key.Enter or Key.KpEnter)
			Press("OK");
		else
			return false;
		return true;
	}

	private void Press(string key)
	{
		if (_solved) return;

		switch (key)
		{
			case "C":
				_typed = "";
				PlaySound("bip");
				break;
			case "OK":
				Check();
				return;
			default:
				if (_typed.Length >= Digits) return;
				_typed += key;
				PlaySound("bip");
				break;
		}
		RefreshDisplay(LcdOn);
	}

	private void Check()
	{
		if (_typed == Code)
		{
			_solved = true;
			_status.Text = "ACESSO LIBERADO";
			_status.AddThemeColorOverride("font_color", LcdOn);
			RefreshDisplay(LcdOn);
			PlaySound("ok");
			_closeTimer = CloseDelay;
			return;
		}

		_status.Text = _typed.Length < Digits ? "A senha tem 4 números" : "SENHA INCORRETA";
		RefreshDisplay(LcdError);
		PlaySound("erro");
		_shake = ShakeTime;
		_typed = "";
	}

	private void RefreshDisplay(Color color)
	{
		string text = "";
		for (int i = 0; i < Digits; i++)
			text += (i < _typed.Length ? _typed[i].ToString() : "_") + (i < Digits - 1 ? " " : "");
		_display.Text = text;
		_display.AddThemeColorOverride("font_color", color);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		if (_shake > 0)
		{
			_shake -= dt;
			float amount = 10f * Mathf.Max(_shake, 0f) / ShakeTime;
			_center.Position = new Vector2((float)GD.RandRange(-amount, amount), 0);
			if (_shake <= 0)
				_center.Position = Vector2.Zero;
		}

		if (_closeTimer > 0)
		{
			_closeTimer -= dt;
			if (_closeTimer <= 0)
			{
				EmitSignal(SignalName.Solved);
				Close();
			}
		}
	}
}
