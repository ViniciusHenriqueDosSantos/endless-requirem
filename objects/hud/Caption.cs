using Godot;

// Legenda na parte de baixo da tela (onde o olho do jogador está), numa caixinha escura com borda
// nas cores da paleta do jogo (tools/art/palette.py). Some sozinha quando o texto fica vazio.
// Uso: Caption caption = Caption.AtBottom(Caption.Kind.Message, 24, 124, 76); parent.AddChild(caption); caption.Text = "...";
public partial class Caption : CenterContainer
{
	public enum Kind { Objective, Message, Banner, Alert, Prompt }

	// cores da paleta
	public static readonly Color Ink = new(0.11f, 0.10f, 0.13f);          // ink 1c1a22
	public static readonly Color Cream = new(0.89f, 0.87f, 0.82f);        // plaza_l e3ddd1
	public static readonly Color White = new(0.95f, 0.96f, 0.96f);        // gran_l f3f4f5
	public static readonly Color Green = new(0.17f, 0.71f, 0.45f);        // lanyard 2bb673
	public static readonly Color Blue = new(0.31f, 0.53f, 0.81f);         // uni_blue_l 4f86cf
	public static readonly Color Yellow = new(0.95f, 0.76f, 0.19f);       // tassel f2c230
	public static readonly Color Red = new(0.78f, 0.22f, 0.18f);          // bin_red c7372f

	// largura máxima antes de quebrar a linha
	public float MaxWidth = 860f;

	private Label _label;
	private int _fontSize;

	public string Text
	{
		get => _label.Text;
		set
		{
			_label.Text = value ?? "";
			Visible = !string.IsNullOrEmpty(_label.Text);
			FitWidth();
		}
	}

	// Cria a legenda presa na parte de baixo da tela, entre fromBottom e toBottom pixels da borda de baixo.
	public static Caption AtBottom(Kind kind, int fontSize, float fromBottom, float toBottom)
	{
		Caption caption = new();
		caption.Build(kind, fontSize);
		caption.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
		caption.OffsetTop = -fromBottom;
		caption.OffsetBottom = -toBottom;
		caption.GrowVertical = GrowDirection.Begin;   // texto de duas linhas cresce para cima
		return caption;
	}

	private void Build(Kind kind, int fontSize)
	{
		_fontSize = fontSize;
		MouseFilter = MouseFilterEnum.Ignore;
		Visible = false;

		(Color border, Color text) = kind switch
		{
			Kind.Objective => (Green, Cream),
			Kind.Banner => (Yellow, Yellow),
			Kind.Alert => (Red, new Color(1f, 0.85f, 0.8f)),
			Kind.Prompt => (Yellow, Cream),
			_ => (Blue, White),
		};

		PanelContainer panel = new() { MouseFilter = MouseFilterEnum.Ignore };
		StyleBoxFlat box = new()
		{
			BgColor = new Color(Ink, 0.8f),
			BorderColor = border,
			ContentMarginLeft = 18,
			ContentMarginRight = 18,
			ContentMarginTop = 6,
			ContentMarginBottom = 6,
		};
		box.SetBorderWidthAll(kind == Kind.Banner ? 3 : 2);
		box.BorderWidthTop = 0;
		box.BorderWidthBottom = kind == Kind.Banner ? 4 : 3;
		box.SetCornerRadiusAll(6);
		panel.AddThemeStyleboxOverride("panel", box);
		AddChild(panel);

		_label = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_label.AddThemeFontSizeOverride("font_size", fontSize);
		_label.AddThemeColorOverride("font_color", text);
		_label.AddThemeConstantOverride("outline_size", 6);
		_label.AddThemeColorOverride("font_outline_color", Ink);
		panel.AddChild(_label);
	}

	// Linha curta: a caixa abraça o texto. Linha longa: quebra em MaxWidth.
	private void FitWidth()
	{
		Font font = _label.GetThemeFont("font");
		float width = font.GetMultilineStringSize(_label.Text, HorizontalAlignment.Left, -1, _fontSize).X;
		bool wrap = width > MaxWidth;
		_label.AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off;
		_label.CustomMinimumSize = new Vector2(wrap ? MaxWidth : 0, 0);
	}
}
