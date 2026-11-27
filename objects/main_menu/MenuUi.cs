using Godot;

// Peças de interface usadas pelo menu principal e pelo menu de pausa (cores em Palette).
public static class MenuUi
{
	private static readonly PackedScene ArrowButtonScene = GD.Load<PackedScene>("res://objects/arrow_button/arrow_button.tscn");

	// Botão grande do menu ("> Jogar"), centralizado.
	public static Button BigButton(string text, int fontSize = 32)
	{
		Button button = ArrowButtonScene.Instantiate<Button>();
		button.Text = text;
		button.Alignment = HorizontalAlignment.Center;
		button.AddThemeFontSizeOverride("font_size", fontSize);
		return button;
	}

	// Caixa escura com borda, onde ficam as telas secundárias.
	public static PanelContainer Panel(Color border)
	{
		PanelContainer panel = new() { MouseFilter = Control.MouseFilterEnum.Stop };
		panel.AddThemeStyleboxOverride("panel", Box(new Color(Palette.Ink, 0.88f), border, 3, 20));
		return panel;
	}

	public static Label Text(string text, int size, Color color, bool center = true)
	{
		Label label = new() { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
		if (center) label.HorizontalAlignment = HorizontalAlignment.Center;
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeConstantOverride("outline_size", 4);
		label.AddThemeColorOverride("font_outline_color", Palette.Ink);
		return label;
	}

	// Lista "tecla — para que serve" (a mesma do quadro de comandos do Hud).
	public static Control ControlsList(int size = 18)
	{
		GridContainer grid = new() { Columns = 2, MouseFilter = Control.MouseFilterEnum.Ignore };
		grid.AddThemeConstantOverride("h_separation", 24);
		grid.AddThemeConstantOverride("v_separation", 6);
		foreach ((string key, string use) in Hud.Controls)
		{
			grid.AddChild(Text(key, size, Palette.Green, false));
			grid.AddChild(Text(use, size, Palette.Cream, false));
		}
		return grid;
	}

	// Botão de escolha (personagem, dificuldade): borda amarela quando escolhido.
	public static void Choice(Button button, bool chosen)
	{
		Color border = chosen ? Palette.Yellow : Palette.Frame;
		Color face = chosen ? new Color(0.17f, 0.16f, 0.2f) : new Color(0.13f, 0.12f, 0.15f);
		button.AddThemeStyleboxOverride("normal", Box(face, border, 3, 8));
		button.AddThemeStyleboxOverride("hover", Box(face.Lightened(0.1f), chosen ? Palette.Yellow : Palette.Cream, 3, 8));
		button.AddThemeStyleboxOverride("pressed", Box(face.Darkened(0.2f), Palette.Yellow, 3, 8));
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		Color text = chosen ? Palette.Yellow : Palette.Cream;
		button.AddThemeColorOverride("font_color", text);
		button.AddThemeColorOverride("font_hover_color", text);
		button.AddThemeColorOverride("font_pressed_color", Palette.Yellow);
		button.Modulate = chosen ? Colors.White : new Color(1, 1, 1, 0.75f);
	}

	public static StyleBoxFlat Box(Color background, Color border, int borderWidth, int margin)
	{
		StyleBoxFlat box = new()
		{
			BgColor = background,
			BorderColor = border,
			ContentMarginLeft = margin,
			ContentMarginRight = margin,
			ContentMarginTop = margin * 0.6f,
			ContentMarginBottom = margin * 0.6f,
		};
		box.SetBorderWidthAll(borderWidth);
		box.SetCornerRadiusAll(6);
		return box;
	}

	// Logotipo em pixel art, como filho do cenário (SplashBg), na mesma escala dele (×2,4), centralizado no alto.
	public static TextureRect Logo(string path, float nativeTop = 46f)
	{
		const float artScale = 2.4f;
		Texture2D texture = GD.Load<Texture2D>(path);
		Vector2 size = texture.GetSize() * artScale;
		return new TextureRect
		{
			Texture = texture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Position = new Vector2((1152 - size.X) / 2, nativeTop * artScale),
			Scale = new Vector2(artScale, artScale),
		};
	}
}
