using Godot;

// Legenda na parte de baixo da tela (onde o olho do jogador está), numa caixinha escura com borda
// nas cores da paleta do jogo (Palette). Some sozinha quando o texto fica vazio.
// Uso: Caption caption = Caption.AtBottom(Caption.Kind.Message, 24, 124, 76); parent.AddChild(caption); caption.Text = "...";
public partial class Caption : CenterContainer
{
	public enum Kind { Objective, Message, Banner, Alert, Prompt }

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
			Kind.Objective => (Palette.Green, Palette.Cream),
			Kind.Banner => (Palette.Yellow, Palette.Yellow),
			Kind.Alert => (Palette.Red, new Color(1f, 0.85f, 0.8f)),
			Kind.Prompt => (Palette.Yellow, Palette.Cream),
			_ => (Palette.Blue, Palette.White),
		};

		PanelContainer panel = new() { MouseFilter = MouseFilterEnum.Ignore };
		StyleBoxFlat box = new()
		{
			BgColor = new Color(Palette.Ink, 0.8f),
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
		_label.AddThemeColorOverride("font_outline_color", Palette.Ink);
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
