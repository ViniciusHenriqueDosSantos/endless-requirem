using Godot;
using System.Collections.Generic;

// Tela 2D do computador do Laboratório 2 (Fase 3): uma paródia do site "SUA" da universidade, num navegador.
// Abre na página da Intranet. O aviso com a senha da porta da área final fica em
//   (menu) Assuntos Acadêmicos -> Diplomas e Certificados   (3 páginas contando a primeira).
// O aviso é um quadro amarelo grande com a senha em destaque.
// Ao abrir a página do aviso, dispara NoticeFound (uma vez). Esc ou o × do navegador fecham.
// Fonte: Inter (fonts/Inter-*.otf, licença OFL). Todo o conteúdo é inventado; só o visual lembra o site de verdade.
public partial class IntranetScreen : OverlayScreen
{
	[Signal] public delegate void NoticeFoundEventHandler();

	// Senha da porta (o roteiro da fase sorteia a cada partida).
	public string Code = "0000";

	public const string NoticePage = "diplomas";
	private const string StartPage = "intranet";

	// ---------- cores do site ----------
	private static readonly Color Green = new("2e6b3c");
	private static readonly Color Sidebar = new("3b3e42");
	private static readonly Color SidebarHover = new("4b4f54");
	private static readonly Color PageBg = new("f4f5f6");
	private static readonly Color Ink = new("2b2d30");
	private static readonly Color InkSoft = new("6b6f75");
	private static readonly Color LinkBlue = new("1f6fc5");
	private static readonly Color InfoBg = new("e1effc");
	private static readonly Color InfoStrip = new("63aef0");
	private static readonly Color InfoInk = new("1b4f8f");
	private static readonly Color Alert = new("f08c00");
	private static readonly Color AlertBg = new("fff4cc");
	private static readonly Color AlertInk = new("5a3600");

	// ---------- páginas ----------
	private sealed class Link
	{
		public string Text;
		public string Target;
		public string Badge;        // etiqueta laranja ao lado do link (ex.: "NOVO AVISO")
		public Link(string text, string target, string badge = null) { Text = text; Target = target; Badge = badge; }
	}

	private sealed class Section
	{
		public string Heading;
		public string Body;
		public string Info;
		public bool PasswordNotice;   // o quadro grande com a senha
		public Link[] Links = System.Array.Empty<Link>();
	}

	private sealed class Page
	{
		public string Path;
		public string Title;
		public string Subtitle;
		public Section[] Sections;
	}

	private readonly Dictionary<string, Page> _pages = new();
	private readonly List<string> _history = new();
	private bool _noticeFound;
	private string _current = StartPage;

	// ---------- interface ----------
	private Font _font;
	private Font _bold;
	private Label _url;
	private Label _crumbs;
	private Label _title;
	private Label _subtitle;
	private VBoxContainer _content;
	private ScrollContainer _scroll;
	private Button _back;

	protected override void BuildUi()
	{
		CloseWithInteract = false;
		_font = GD.Load<Font>("res://fonts/Inter-Regular.otf");
		_bold = GD.Load<Font>("res://fonts/Inter-Bold.otf");

		Theme theme = new() { DefaultFont = _font, DefaultFontSize = 16 };
		Control site = new() { Theme = theme };
		site.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		site.OffsetLeft = 36; site.OffsetTop = 24; site.OffsetRight = -36; site.OffsetBottom = -24;
		Root.AddChild(site);

		PanelContainer window = new();
		window.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		window.AddThemeStyleboxOverride("panel", Box(new Color("dfe1e5"), 0, 0, 8));
		site.AddChild(window);

		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 0);
		window.AddChild(column);
		column.AddChild(BuildBrowserBar());
		column.AddChild(BuildHeader());

		HBoxContainer body = new() { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", 0);
		column.AddChild(body);
		body.AddChild(BuildSidebar());
		body.AddChild(BuildMain());

		BuildPages();
	}

	// ---------- montagem ----------

	private Control BuildBrowserBar()
	{
		PanelContainer bar = new();
		bar.AddThemeStyleboxOverride("panel", Box(new Color("dfe1e5"), 10, 6));
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 10);
		bar.AddChild(row);

		_back = FlatButton("←  Voltar", Ink, 15);
		_back.Pressed += GoBack;
		row.AddChild(_back);

		PanelContainer address = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		address.AddThemeStyleboxOverride("panel", Box(Colors.White, 14, 6, 14));
		_url = Plain("", 14, InkSoft);
		address.AddChild(_url);
		row.AddChild(address);

		Button close = FlatButton("Fechar  ×", Ink, 15);
		close.Pressed += Close;
		row.AddChild(close);
		return bar;
	}

	private Control BuildHeader()
	{
		PanelContainer header = new() { CustomMinimumSize = new Vector2(0, 58) };
		header.AddThemeStyleboxOverride("panel", Box(Green, 16, 0));
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 14);
		header.AddChild(row);

		PanelContainer logo = new() { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
		StyleBoxFlat logoBox = Box(new Color(0, 0, 0, 0), 6, 2, 3);
		logoBox.BorderColor = Colors.White;
		logoBox.SetBorderWidthAll(2);
		logo.AddThemeStyleboxOverride("panel", logoBox);
		logo.AddChild(Plain("SUA", 14, Colors.White, true));
		row.AddChild(logo);
		Label name = Plain("SUA U.N.I.F.E.S.P.", 22, Colors.White);
		name.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		row.AddChild(name);
		return header;
	}

	private Control BuildSidebar()
	{
		PanelContainer panel = new() { CustomMinimumSize = new Vector2(250, 0) };
		panel.AddThemeStyleboxOverride("panel", Box(Sidebar, 0, 16));
		VBoxContainer list = new();
		list.AddThemeConstantOverride("separation", 2);
		panel.AddChild(list);

		MarginContainer head = Margins(20, 4, 16, 10);
		head.AddChild(Plain("CATÁLOGOS", 12, new Color("c9ccd0"), true));
		list.AddChild(head);
		foreach ((string text, string page) in new[]
		{
			("Início", "inicio"), ("Assuntos Acadêmicos", "academicos"), ("Tecnologia da Informação", "intranet"),
			("Disponibilidade dos sistemas", "sistemas"),
		})
		{
			string target = page;
			Button item = FlatButton("›   " + text, new Color("f2f3f4"), 15);
			item.Alignment = HorizontalAlignment.Left;
			item.AddThemeStyleboxOverride("normal", Box(new Color(0, 0, 0, 0), 20, 10));
			item.AddThemeStyleboxOverride("hover", Box(SidebarHover, 20, 10));
			item.AddThemeStyleboxOverride("pressed", Box(SidebarHover, 20, 10));
			item.Pressed += () => Navigate(target);
			list.AddChild(item);
		}
		return panel;
	}

	private Control BuildMain()
	{
		VBoxContainer main = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		main.AddThemeConstantOverride("separation", 0);

		PanelContainer top = new();
		top.AddThemeStyleboxOverride("panel", Box(new Color("eeeff0"), 24, 14));
		VBoxContainer topColumn = new();
		topColumn.AddThemeConstantOverride("separation", 6);
		top.AddChild(topColumn);
		_crumbs = Plain("", 13, InkSoft);
		topColumn.AddChild(_crumbs);
		_title = Plain("", 28, Ink, true);
		topColumn.AddChild(_title);
		_subtitle = Plain("", 14, InkSoft);
		topColumn.AddChild(_subtitle);
		main.AddChild(top);
		main.AddChild(new ColorRect { Color = new Color("d9dbde"), CustomMinimumSize = new Vector2(0, 1) });

		PanelContainer page = new() { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		page.AddThemeStyleboxOverride("panel", Box(PageBg, 0, 0));
		main.AddChild(page);
		_scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		page.AddChild(_scroll);

		MarginContainer pad = Margins(28, 20, 28, 30);
		pad.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_scroll.AddChild(pad);
		_content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_content.AddThemeConstantOverride("separation", 14);
		pad.AddChild(_content);
		return main;
	}

	// ---------- navegação ----------

	protected override void OnOpened()
	{
		_history.Clear();
		ShowPage(StartPage, false);
	}

	private void Navigate(string page)
	{
		PlaySound("clique");
		ShowPage(page, true);
	}

	private void GoBack()
	{
		if (_history.Count == 0) return;
		string previous = _history[^1];
		_history.RemoveAt(_history.Count - 1);
		PlaySound("clique");
		ShowPage(previous, false);
	}

	private void ShowPage(string id, bool remember)
	{
		if (!_pages.ContainsKey(id)) id = "indisponivel";
		if (remember && id != _current)
			_history.Add(_current);
		_current = id;
		_back.Disabled = _history.Count == 0;

		Page page = _pages[id];
		_url.Text = "https://sua.u-n-i-f-e-s-p.br/" + page.Path;
		_crumbs.Text = "Início  /  " + page.Path.Replace("/", "  /  ");
		_title.Text = page.Title;
		_subtitle.Text = page.Subtitle;

		foreach (Node child in _content.GetChildren())
		{
			_content.RemoveChild(child);
			child.QueueFree();
		}

		foreach (Section section in page.Sections)
		{
			if (section.PasswordNotice)
				_content.AddChild(PasswordNotice());
			if (section.Heading != null)
			{
				_content.AddChild(Plain(section.Heading, 22, Ink, true));
				_content.AddChild(new ColorRect { Color = new Color("b9bcc0"), CustomMinimumSize = new Vector2(0, 1) });
			}
			if (section.Body != null)
				_content.AddChild(Paragraph(section.Body, 16, Ink));
			if (section.Info != null)
				_content.AddChild(InfoBox(section.Info));
			foreach (Link link in section.Links)
				_content.AddChild(LinkCard(link));
		}
		_content.AddChild(Plain("Editado pela última vez por S.I.A. — Sistema Inteligente Acadêmico", 12, InkSoft));

		_scroll.ScrollVertical = 0;

		if (id == NoticePage && !_noticeFound)
		{
			_noticeFound = true;
			EmitSignal(SignalName.NoticeFound);
		}
	}

	// ---------- peças da página ----------

	private Label Plain(string text, int size, Color color, bool bold = false)
	{
		Label label = new() { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeFontOverride("font", bold ? _bold : _font);   // sempre a fonte do site (nunca a do jogo)
		return label;
	}

	private Label Paragraph(string text, int size, Color color, bool bold = false)
	{
		Label label = Plain(text, size, color, bold);
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		label.CustomMinimumSize = new Vector2(200, 0);
		return label;
	}

	private Control InfoBox(string text)
	{
		PanelContainer box = new();
		StyleBoxFlat style = Box(InfoBg, 18, 14, 4);
		style.BorderColor = InfoStrip;
		style.BorderWidthLeft = 6;
		box.AddThemeStyleboxOverride("panel", style);
		box.AddChild(Paragraph(text, 15, InfoInk));
		return box;
	}

	// O aviso dos diplomas: quadro amarelo com borda laranja grossa e a senha bem grande.
	private Control PasswordNotice()
	{
		PanelContainer box = new();
		StyleBoxFlat style = Box(AlertBg, 26, 20, 8);
		style.BorderColor = Alert;
		style.SetBorderWidthAll(4);
		box.AddThemeStyleboxOverride("panel", style);

		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 12);
		box.AddChild(column);
		column.AddChild(Plain("AVISO IMPORTANTE — RETIRADA DE DIPLOMAS", 18, new Color("b25e00"), true));
		column.AddChild(Paragraph(
			"Os diplomas registrados estão disponíveis para retirada no AUDITÓRIO do 4º andar. " +
			"O acesso é pela área de convivência, atravessando a porta de vidro do corredor.", 16, AlertInk));

		HBoxContainer codeRow = new();
		codeRow.AddThemeConstantOverride("separation", 16);
		Label caption = Plain("Senha da porta de vidro:", 18, AlertInk, true);
		caption.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		codeRow.AddChild(caption);
		PanelContainer codeBox = new();
		codeBox.AddThemeStyleboxOverride("panel", Box(new Color("2b2d30"), 22, 8, 6));
		codeBox.AddChild(Plain(string.Join("  ", Code.ToCharArray()), 40, new Color("ffd24a"), true));
		codeRow.AddChild(codeBox);
		column.AddChild(codeRow);

		column.AddChild(Paragraph("Fora do horário de expediente, o acesso é monitorado pelo S.I.A.", 13, AlertInk));
		return box;
	}

	private Control LinkCard(Link link)
	{
		string target = link.Target;
		bool highlighted = link.Badge != null;

		Button card = FlatButton(link.Text, LinkBlue, 16, highlighted);
		card.Alignment = HorizontalAlignment.Left;
		card.CustomMinimumSize = new Vector2(0, 50);
		StyleBoxFlat normal = Box(highlighted ? new Color("fffaf0") : Colors.White, 20, 14, 2);
		normal.BorderColor = highlighted ? Alert : new Color("dadce0");
		normal.BorderWidthLeft = highlighted ? 6 : 3;
		normal.ShadowColor = new Color(0, 0, 0, 0.08f);
		normal.ShadowSize = 2;
		StyleBoxFlat hover = (StyleBoxFlat)normal.Duplicate();
		hover.BgColor = new Color("f3f7fc");
		hover.BorderColor = highlighted ? Alert : LinkBlue;
		card.AddThemeStyleboxOverride("normal", normal);
		card.AddThemeStyleboxOverride("hover", hover);
		card.AddThemeStyleboxOverride("pressed", hover);
		card.AddThemeColorOverride("font_hover_color", LinkBlue.Darkened(0.2f));
		card.Pressed += () => Navigate(target);

		if (!highlighted) return card;

		// etiqueta "NOVO AVISO" no canto direito do link
		PanelContainer badge = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
		badge.AddThemeStyleboxOverride("panel", Box(Alert, 10, 3, 10));
		badge.AddChild(Plain(link.Badge, 12, Colors.White, true));
		badge.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterRight);
		badge.OffsetLeft = -130;
		badge.OffsetRight = -16;
		badge.OffsetTop = -13;
		badge.OffsetBottom = 13;
		card.AddChild(badge);
		return card;
	}

	private Button FlatButton(string text, Color color, int size, bool bold = false)
	{
		Button button = new()
		{
			Text = text, Flat = true, FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
		};
		button.AddThemeFontSizeOverride("font_size", size);
		button.AddThemeColorOverride("font_color", color);
		button.AddThemeColorOverride("font_hover_color", color.Lightened(0.2f));
		button.AddThemeColorOverride("font_pressed_color", color);
		button.AddThemeColorOverride("font_disabled_color", new Color(color, 0.35f));
		button.AddThemeFontOverride("font", bold ? _bold : _font);
		return button;
	}

	private static StyleBoxFlat Box(Color background, float marginX, float marginY, int radius = 0)
	{
		StyleBoxFlat box = new() { BgColor = background };
		box.SetCornerRadiusAll(radius);
		box.ContentMarginLeft = marginX;
		box.ContentMarginRight = marginX;
		box.ContentMarginTop = marginY;
		box.ContentMarginBottom = marginY;
		return box;
	}

	private static MarginContainer Margins(int left, int top, int right, int bottom)
	{
		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", left);
		margin.AddThemeConstantOverride("margin_top", top);
		margin.AddThemeConstantOverride("margin_right", right);
		margin.AddThemeConstantOverride("margin_bottom", bottom);
		return margin;
	}

	// ---------- o conteúdo do site (tudo inventado) ----------

	private void BuildPages()
	{
		_pages["inicio"] = new Page
		{
			Path = "catalogo", Title = "Catálogo de Serviços",
			Subtitle = "Serviços, sistemas e informações da U.N.I.F.E.S.P. em um só lugar",
			Sections = new[]
			{
				new Section
				{
					Heading = "Bem-vindo",
					Body = "Escolha um catálogo no menu ao lado.",
					Info = "O 4º andar está em manutenção desde 2019. Previsão de término: em breve.",
				},
			},
		};

		_pages["intranet"] = new Page
		{
			Path = "catalogo_ti/intranet", Title = "Intranet",
			Subtitle = "A Intranet agrupa informações, acesso e sistemas da U.N.I.F.E.S.P.",
			Sections = new[]
			{
				new Section
				{
					Heading = "Descrição",
					Body = "Com sua conta da U.N.I.F.E.S.P., você pode acessar vários sistemas da instituição, como e-mail, " +
						"sistema acadêmico e Moodle.",
					Info = "Procurando avisos sobre matrícula, histórico ou diplomas? Veja o catálogo Assuntos Acadêmicos, no menu ao lado.",
				},
				new Section
				{
					Heading = "Serviços",
					Links = new[]
					{
						new Link("Veja como recuperar a sua senha", "indisponivel"),
						new Link("Acesse os sistemas da U.N.I.F.E.S.P.", "sistemas"),
					},
				},
			},
		};

		_pages["academicos"] = new Page
		{
			Path = "catalogo_academico", Title = "Assuntos Acadêmicos",
			Subtitle = "Matrícula, histórico, diplomas e tudo o que o aluno precisa (ou quase)",
			Sections = new[]
			{
				new Section
				{
					Heading = "Serviços",
					Links = new[]
					{
						new Link("Diplomas e Certificados", NoticePage),
						new Link("Histórico escolar", "historico"),
						new Link("Rematrícula", "indisponivel"),
					},
				},
			},
		};

		_pages[NoticePage] = new Page
		{
			Path = "catalogo_academico/diplomas", Title = "Diplomas e Certificados",
			Subtitle = "Emissão, registro e retirada de diplomas da graduação",
			Sections = new[]
			{
				new Section { PasswordNotice = true },
				new Section
				{
					Heading = "Descrição",
					Body = "Depois da colação de grau, o diploma é registrado e fica disponível para retirada pelo " +
						"próprio formando, com documento de identificação.",
				},
			},
		};

		_pages["historico"] = new Page
		{
			Path = "catalogo_academico/historico", Title = "Histórico escolar",
			Subtitle = "Suas disciplinas cursadas",
			Sections = new[]
			{
				new Section
				{
					Heading = "Seu histórico",
					Body = "Cálculo I — REPROVADO\nCálculo I — REPROVADO\nCálculo I — CURSANDO\nAlgoritmos — APROVADO",
				},
			},
		};

		_pages["sistemas"] = new Page
		{
			Path = "catalogo_ti/disponibilidade", Title = "Disponibilidade dos sistemas",
			Subtitle = "Situação dos sistemas agora",
			Sections = new[]
			{
				new Section
				{
					Heading = "Situação",
					Body = "E-mail institucional — instável\nSistema acadêmico — fora do ar\nElevadores — sem energia\n" +
						"Câmeras do 4º andar — ONLINE (S.I.A.)",
				},
			},
		};

		_pages["indisponivel"] = new Page
		{
			Path = "erro/503", Title = "Página indisponível", Subtitle = "Erro 503",
			Sections = new[]
			{
				new Section { Body = "O servidor desta página foi reprovado. Tente novamente no próximo semestre." },
			},
		};
	}
}
