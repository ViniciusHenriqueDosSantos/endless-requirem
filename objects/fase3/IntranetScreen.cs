using Godot;
using System.Collections.Generic;

// Tela 2D do computador do Laboratório 2 (Fase 3): uma paródia do site "SUA" da universidade, num navegador.
// Abre na página da Intranet. O aviso com a senha da porta da área final fica em
//   (menu) Assuntos Acadêmicos -> Diplomas e Certificados   (3 páginas contando a primeira).
// Ao abrir a página do aviso, dispara NoticeFound (uma vez). Esc ou o X do navegador fecham.
// Todo o conteúdo é inventado (nomes, páginas, avisos); só o visual lembra o site de verdade.
public partial class IntranetScreen : OverlayScreen
{
	[Signal] public delegate void NoticeFoundEventHandler();

	// Senha da porta (o roteiro da fase sorteia a cada partida).
	public string Code = "0000";

	public const string NoticePage = "diplomas";
	private const string StartPage = "intranet";

	// ---------- cores do site ----------
	private static readonly Color Green = new("2e6b3c");
	private static readonly Color GreenLight = new("5f8f68");
	private static readonly Color Sidebar = new("3b3e42");
	private static readonly Color SidebarLine = new("4b4f54");
	private static readonly Color PageBg = new("f4f5f6");
	private static readonly Color Text = new("2b2d30");
	private static readonly Color TextSoft = new("6b6f75");
	private static readonly Color LinkBlue = new("1f6fc5");
	private static readonly Color IndexBlue = new("2f73b8");
	private static readonly Color InfoBg = new("e1effc");
	private static readonly Color InfoStrip = new("63aef0");
	private static readonly Color InfoText = new("1b4f8f");
	private static readonly Color WarnBg = new("fff2dc");
	private static readonly Color WarnStrip = new("f0a030");
	private static readonly Color WarnText = new("6b3d00");

	// ---------- páginas ----------
	private sealed class Link
	{
		public string Text;
		public string Target;
		public bool External;
		public Link(string text, string target, bool external = false) { Text = text; Target = target; External = external; }
	}

	private sealed class Section
	{
		public string Heading;
		public string Body;
		public string Info;
		public string Warning;
		public Link[] Links = System.Array.Empty<Link>();
	}

	private sealed class Page
	{
		public string Path;          // "catalogo_ti / intranet"
		public string Title;
		public string Subtitle;
		public Section[] Sections;
		public string[] Keywords;
		public string EditedOn = "12/11/2026";
	}

	private readonly Dictionary<string, Page> _pages = new();
	private readonly List<string> _history = new();
	private bool _noticeFound;
	private string _current = StartPage;
	private string _searchText = "";

	// ---------- interface ----------
	private Font _font;
	private Font _bold;
	private Label _url;
	private Label _crumbs;
	private Label _title;
	private Label _subtitle;
	private VBoxContainer _content;
	private VBoxContainer _index;
	private HFlowContainer _keywords;
	private Label _editedOn;
	private ScrollContainer _scroll;
	private LineEdit _search;
	private Button _back;

	protected override void BuildUi()
	{
		CloseWithInteract = false;   // dá para digitar "e" na busca
		_font = new SystemFont { FontNames = new[] { "Roboto", "Segoe UI", "Arial", "Helvetica", "Liberation Sans", "sans-serif" }, Antialiasing = TextServer.FontAntialiasing.Gray };
		_bold = new SystemFont { FontNames = new[] { "Roboto", "Segoe UI", "Arial", "Helvetica", "Liberation Sans", "sans-serif" }, FontWeight = 700, Antialiasing = TextServer.FontAntialiasing.Gray };

		Theme theme = new() { DefaultFont = _font, DefaultFontSize = 15 };
		Control site = new() { Theme = theme };
		site.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		site.OffsetLeft = 28; site.OffsetTop = 20; site.OffsetRight = -28; site.OffsetBottom = -20;
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
		bar.AddThemeStyleboxOverride("panel", Box(new Color("dfe1e5"), 8, 6));
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 8);
		bar.AddChild(row);

		_back = FlatButton("←", Text, 18);
		_back.Pressed += GoBack;
		row.AddChild(_back);
		Button reload = FlatButton("⟳", Text, 16);
		reload.Pressed += () => ShowPage(_current, false);
		row.AddChild(reload);

		PanelContainer address = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		address.AddThemeStyleboxOverride("panel", Box(Colors.White, 14, 6, 14));
		_url = Plain("", 14, TextSoft);
		address.AddChild(_url);
		row.AddChild(address);

		Button close = FlatButton("×", Text, 22);
		close.Pressed += Close;
		row.AddChild(close);
		return bar;
	}

	private Control BuildHeader()
	{
		PanelContainer header = new() { CustomMinimumSize = new Vector2(0, 60) };
		header.AddThemeStyleboxOverride("panel", Box(Green, 10, 0));
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 14);
		header.AddChild(row);

		PanelContainer logo = new() { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
		StyleBoxFlat logoBox = Box(new Color(0, 0, 0, 0), 2, 6, 3);
		logoBox.BorderColor = Colors.White;
		logoBox.SetBorderWidthAll(2);
		logo.AddThemeStyleboxOverride("panel", logoBox);
		logo.AddChild(Plain("SUA", 13, Colors.White, true));
		row.AddChild(logo);
		Label name = Plain("SUA U.N.I.F.E.S.P.", 21, Colors.White);
		name.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		row.AddChild(name);

		row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
		_search = new LineEdit
		{
			PlaceholderText = "Pesquisar...",
			CustomMinimumSize = new Vector2(420, 40),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		_search.AddThemeStyleboxOverride("normal", Box(GreenLight, 18, 6, 20));
		_search.AddThemeStyleboxOverride("focus", Box(GreenLight.Lightened(0.1f), 18, 6, 20));
		_search.AddThemeColorOverride("font_color", Colors.White);
		_search.AddThemeColorOverride("font_placeholder_color", new Color(1, 1, 1, 0.75f));
		_search.TextSubmitted += OnSearch;
		row.AddChild(_search);
		row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
		return header;
	}

	private Control BuildSidebar()
	{
		PanelContainer panel = new() { CustomMinimumSize = new Vector2(250, 0) };
		panel.AddThemeStyleboxOverride("panel", Box(Sidebar, 0, 0));
		ScrollContainer scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		panel.AddChild(scroll);
		VBoxContainer list = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		list.AddThemeConstantOverride("separation", 0);
		scroll.AddChild(list);

		AddSidebarGroup(list, "CATÁLOGOS", ">",
			("Assuntos Acadêmicos", "academicos"), ("Gestão com Pessoas", "pessoas"), ("Ouvidoria", "ouvidoria"),
			("Serviços Administrativos", "administrativos"), ("Tecnologia da Informação", "intranet"));
		AddSidebarGroup(list, "CENTRAL DE ATENDIMENTO", "•",
			("Abertura de chamados", "chamados"), ("Manual do sistema de atendi...", "indisponivel"));
		AddSidebarGroup(list, "SERVIÇOS", "•",
			("Ferramenta de Suporte Remoto", "indisponivel"), ("Consulta Ramal/Voip", "ramal"),
			("Disponibilidade dos sistemas", "sistemas"));
		AddSidebarGroup(list, "CONHEÇA O PROJETO", "•", ("Conheça o SUA U.N.I.F.E.S.P.", "inicio"));
		return panel;
	}

	private void AddSidebarGroup(VBoxContainer list, string title, string bullet, params (string text, string page)[] items)
	{
		MarginContainer head = Margins(16, 14, 16, 8);
		head.AddChild(Plain(title, 12, new Color("e8e9ea"), true));
		list.AddChild(head);
		foreach ((string text, string page) in items)
		{
			string target = page;
			Button item = FlatButton($"{bullet}   {text}", new Color("f2f3f4"), 14, true);
			item.Alignment = HorizontalAlignment.Left;
			item.AddThemeStyleboxOverride("normal", Box(new Color(0, 0, 0, 0), 22, 9));
			item.AddThemeStyleboxOverride("hover", Box(SidebarLine, 22, 9));
			item.AddThemeStyleboxOverride("pressed", Box(SidebarLine.Darkened(0.2f), 22, 9));
			item.Pressed += () => Navigate(target);
			list.AddChild(item);
		}
		ColorRect line = new() { Color = SidebarLine, CustomMinimumSize = new Vector2(0, 1) };
		list.AddChild(line);
	}

	private Control BuildMain()
	{
		VBoxContainer main = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		main.AddThemeConstantOverride("separation", 0);

		PanelContainer top = new();
		top.AddThemeStyleboxOverride("panel", Box(new Color("eeeff0"), 18, 10));
		VBoxContainer topColumn = new();
		topColumn.AddThemeConstantOverride("separation", 10);
		top.AddChild(topColumn);
		HBoxContainer crumbsRow = new();
		Button home = FlatButton("⌂", Text, 16);
		home.Pressed += () => Navigate("inicio");
		crumbsRow.AddChild(home);
		_crumbs = Plain("", 13, Text);
		_crumbs.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		crumbsRow.AddChild(_crumbs);
		topColumn.AddChild(crumbsRow);
		_title = Plain("", 25, Text);
		topColumn.AddChild(_title);
		_subtitle = Plain("", 13, TextSoft);
		topColumn.AddChild(_subtitle);
		main.AddChild(top);
		main.AddChild(new ColorRect { Color = new Color("d9dbde"), CustomMinimumSize = new Vector2(0, 1) });

		PanelContainer page = new() { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		page.AddThemeStyleboxOverride("panel", Box(PageBg, 0, 0));
		main.AddChild(page);
		_scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		page.AddChild(_scroll);

		HBoxContainer columns = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		columns.AddThemeConstantOverride("separation", 22);
		MarginContainer pad = Margins(20, 18, 20, 30);
		pad.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		pad.AddChild(columns);
		_scroll.AddChild(pad);

		_content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_content.AddThemeConstantOverride("separation", 12);
		columns.AddChild(_content);

		VBoxContainer side = new() { CustomMinimumSize = new Vector2(250, 0) };
		side.AddThemeConstantOverride("separation", 16);
		columns.AddChild(side);

		_index = new VBoxContainer();
		_index.AddThemeConstantOverride("separation", 10);
		side.AddChild(Card("ÍNDICE", _index));

		_keywords = new HFlowContainer();
		_keywords.AddThemeConstantOverride("h_separation", 6);
		_keywords.AddThemeConstantOverride("v_separation", 6);
		side.AddChild(Card("PALAVRAS-CHAVE", _keywords));

		VBoxContainer edited = new();
		edited.AddChild(Plain("S.I.A. — SISTEMA INTELIGENTE ACADÊMICO", 13, Text));
		_editedOn = Plain("", 12, TextSoft);
		edited.AddChild(_editedOn);
		side.AddChild(Card("EDITADO PELA ÚLTIMA VEZ POR", edited));
		return main;
	}

	private Control Card(string title, Control content)
	{
		PanelContainer card = new();
		StyleBoxFlat box = Box(Colors.White, 16, 14, 3);
		box.ShadowColor = new Color(0, 0, 0, 0.12f);
		box.ShadowSize = 3;
		box.ShadowOffset = new Vector2(0, 1);
		card.AddThemeStyleboxOverride("panel", box);
		VBoxContainer column = new();
		column.AddThemeConstantOverride("separation", 12);
		card.AddChild(column);
		column.AddChild(Plain(title, 11, IndexBlue, true));
		column.AddChild(content);
		return card;
	}

	// ---------- navegação ----------

	protected override void OnOpened()
	{
		_history.Clear();
		_search.Text = "";
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

	private void OnSearch(string text)
	{
		_searchText = text.Trim();
		_search.ReleaseFocus();
		Navigate("busca");
	}

	private void ShowPage(string id, bool remember)
	{
		if (id != "busca" && !_pages.ContainsKey(id)) id = "indisponivel";
		if (remember && id != _current)
			_history.Add(_current);
		_current = id;
		_back.Disabled = _history.Count == 0;

		Page page = id == "busca" ? SearchPage() : _pages[id];
		string slug = page.Path.Replace(" ", "");
		_url.Text = $"https://sua.u-n-i-f-e-s-p.br/{slug}";
		_crumbs.Text = "  /  " + page.Path.Replace("/", "  /  ");
		_title.Text = page.Title;
		_subtitle.Text = page.Subtitle;
		_editedOn.Text = page.EditedOn;

		Clear(_content);
		Clear(_index);
		Clear(_keywords);

		foreach (Section section in page.Sections)
		{
			if (section.Heading != null)
			{
				_content.AddChild(Plain(section.Heading, 26, Text, true));
				_content.AddChild(new ColorRect { Color = new Color("8a8e93"), CustomMinimumSize = new Vector2(0, 1) });
				_index.AddChild(Plain("›   " + section.Heading, 13, Text));
			}
			if (section.Body != null)
				_content.AddChild(Paragraph(section.Body, 15, Text));
			if (section.Info != null)
				_content.AddChild(Notice(section.Info, InfoBg, InfoStrip, InfoText, "i"));
			if (section.Warning != null)
				_content.AddChild(Notice(section.Warning.Replace("{SENHA}", Code), WarnBg, WarnStrip, WarnText, "!"));
			foreach (Link link in section.Links)
				_content.AddChild(LinkCard(link));
		}

		foreach (string word in page.Keywords)
		{
			PanelContainer chip = new();
			chip.AddThemeStyleboxOverride("panel", Box(new Color("dcf1ea"), 10, 4, 3));
			chip.AddChild(Plain(word, 13, new Color("16806a")));
			_keywords.AddChild(chip);
		}

		_scroll.ScrollVertical = 0;

		if (id == NoticePage && !_noticeFound)
		{
			_noticeFound = true;
			EmitSignal(SignalName.NoticeFound);
		}
	}

	private Page SearchPage()
	{
		bool academic = _searchText.ToLower().Contains("diplom") || _searchText.ToLower().Contains("senha");
		Section results = new()
		{
			Heading = "Resultados",
			Body = academic
				? $"1 resultado encontrado para \"{_searchText}\"."
				: $"Nenhum resultado encontrado para \"{_searchText}\". A busca está em manutenção desde 2019.",
		};
		if (academic)
			results.Links = new[] { new Link("Assuntos Acadêmicos — catálogo de serviços", "academicos") };
		return new Page
		{
			Path = "busca", Title = "Pesquisa", Subtitle = "Resultados da pesquisa no catálogo",
			Sections = new[] { results }, Keywords = new[] { "busca" },
		};
	}

	// ---------- peças da página ----------

	private Label Plain(string text, int size, Color color, bool bold = false)
	{
		Label label = new() { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		if (bold) label.AddThemeFontOverride("font", _bold);
		return label;
	}

	private Label Paragraph(string text, int size, Color color)
	{
		Label label = Plain(text, size, color);
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		label.CustomMinimumSize = new Vector2(200, 0);
		return label;
	}

	private Control Notice(string text, Color background, Color strip, Color textColor, string icon)
	{
		HBoxContainer row = new();
		row.AddThemeConstantOverride("separation", 0);
		PanelContainer side = new() { CustomMinimumSize = new Vector2(54, 0) };
		side.AddThemeStyleboxOverride("panel", Box(strip, 0, 0, 4));
		Label mark = Plain(icon, 18, Colors.White, true);
		mark.HorizontalAlignment = HorizontalAlignment.Center;
		mark.VerticalAlignment = VerticalAlignment.Center;
		side.AddChild(mark);
		row.AddChild(side);
		PanelContainer box = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		box.AddThemeStyleboxOverride("panel", Box(background, 18, 16, 4));
		box.AddChild(Paragraph(text, 15, textColor));
		row.AddChild(box);
		return row;
	}

	private Control LinkCard(Link link)
	{
		string target = link.Target;
		Button card = FlatButton(link.Text + (link.External ? "  ↗" : ""), LinkBlue, 15);
		card.Alignment = HorizontalAlignment.Left;
		StyleBoxFlat normal = Box(Colors.White, 20, 16, 2);
		normal.BorderColor = new Color("dadce0");
		normal.BorderWidthLeft = 3;
		normal.ShadowColor = new Color(0, 0, 0, 0.08f);
		normal.ShadowSize = 2;
		StyleBoxFlat hover = (StyleBoxFlat)normal.Duplicate();
		hover.BgColor = new Color("f3f7fc");
		hover.BorderColor = LinkBlue;
		card.AddThemeStyleboxOverride("normal", normal);
		card.AddThemeStyleboxOverride("hover", hover);
		card.AddThemeStyleboxOverride("pressed", hover);
		card.AddThemeColorOverride("font_hover_color", LinkBlue.Darkened(0.2f));
		card.Pressed += () => Navigate(target);
		return card;
	}

	private Button FlatButton(string text, Color color, int size, bool bold = false)
	{
		Button button = new() { Text = text, Flat = true, FocusMode = Control.FocusModeEnum.None, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
		button.AddThemeFontSizeOverride("font_size", size);
		button.AddThemeColorOverride("font_color", color);
		button.AddThemeColorOverride("font_hover_color", color.Lightened(0.2f));
		button.AddThemeColorOverride("font_pressed_color", color);
		button.AddThemeColorOverride("font_disabled_color", new Color(color, 0.35f));
		if (bold) button.AddThemeFontOverride("font", _bold);
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

	private static void Clear(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			node.RemoveChild(child);
			child.QueueFree();
		}
	}

	// ---------- o conteúdo do site (tudo inventado) ----------

	private void BuildPages()
	{
		_pages["inicio"] = new Page
		{
			Path = "catalogo", Title = "Catálogo de Serviços",
			Subtitle = "O SUA reúne os serviços, sistemas e informações da U.N.I.F.E.S.P. em um só lugar",
			Keywords = new[] { "catálogo", "serviços", "ajuda" },
			Sections = new[]
			{
				new Section
				{
					Heading = "Bem-vindo",
					Body = "Escolha um catálogo no menu ao lado ou use a pesquisa no topo da página.",
					Info = "O 4º andar está em manutenção desde 2019. Previsão de término: em breve.",
				},
				new Section
				{
					Heading = "Mais acessados",
					Links = new[]
					{
						new Link("Intranet", "intranet"), new Link("Assuntos Acadêmicos", "academicos"),
						new Link("Disponibilidade dos sistemas", "sistemas"), new Link("Recuperação de senha", "senha"),
					},
				},
			},
		};

		_pages["intranet"] = new Page
		{
			Path = "catalogo_ti/intranet", Title = "Intranet",
			Subtitle = "A Intranet agrupa informações, acesso e sistemas da U.N.I.F.E.S.P.",
			Keywords = new[] { "tecnologia da informação", "intranet", "cadastro", "senha", "docente", "tae" },
			Sections = new[]
			{
				new Section
				{
					Heading = "Descrição",
					Body = "Com sua conta da U.N.I.F.E.S.P., você pode acessar vários sistemas da instituição, como e-mail, " +
						"SEI, sistema acadêmico, Moodle, entre outros.",
					Info = "Ao ingressar na U.N.I.F.E.S.P. todo servidor ou estudante deve criar sua conta institucional.\n" +
						"O procedimento é necessário para obter sua identificação e permite o uso de todos os sistemas " +
						"e serviços que exigem autenticação.",
				},
				new Section
				{
					Heading = "Serviços",
					Links = new[]
					{
						new Link("Como fazer o cadastro na Intranet", "indisponivel"),
						new Link("Veja como recuperar a sua senha", "senha"),
						new Link("Saiba como alterar o nome de usuário (login) da Intranet", "indisponivel"),
						new Link("Acesse os sistemas da U.N.I.F.E.S.P. em sistemas.u-n-i-f-e-s-p.br", "sistemas", true),
						new Link("Acesse o serviço de Busca de Pessoas", "indisponivel", true),
					},
				},
				new Section
				{
					Heading = "Veja como acessar os sistemas institucionais",
					Body = "Vídeo indisponível. O servidor de vídeos foi desligado para economizar energia.",
				},
			},
		};

		_pages["academicos"] = new Page
		{
			Path = "catalogo_academico", Title = "Assuntos Acadêmicos",
			Subtitle = "Matrícula, histórico, diplomas e tudo o que o aluno precisa (ou quase)",
			Keywords = new[] { "graduação", "matrícula", "diploma", "histórico" },
			Sections = new[]
			{
				new Section
				{
					Heading = "Descrição",
					Body = "Serviços da Pró-Reitoria de Graduação para estudantes. Os prazos do calendário acadêmico " +
						"continuam valendo mesmo durante a manutenção do prédio.",
				},
				new Section
				{
					Heading = "Serviços",
					Links = new[]
					{
						new Link("Rematrícula", "indisponivel"),
						new Link("Histórico escolar", "historico"),
						new Link("Diplomas e Certificados", NoticePage),
						new Link("Trancamento de matrícula", "indisponivel"),
						new Link("Calendário acadêmico", "calendario"),
					},
				},
			},
		};

		_pages[NoticePage] = new Page
		{
			Path = "catalogo_academico/diplomas", Title = "Diplomas e Certificados",
			Subtitle = "Emissão, registro e retirada de diplomas da graduação",
			Keywords = new[] { "diploma", "formatura", "retirada", "auditório" },
			Sections = new[]
			{
				new Section
				{
					Heading = "Descrição",
					Body = "Depois da colação de grau, o diploma é registrado e fica disponível para retirada pelo " +
						"próprio formando, com documento de identificação.",
				},
				new Section
				{
					Heading = "Avisos",
					Warning = "AVISO — Retirada de diplomas\n" +
						"Os diplomas registrados estão disponíveis para retirada no AUDITÓRIO do 4º andar, " +
						"com acesso pela área de convivência.\n\n" +
						"Senha da porta de vidro da área final:  {SENHA}\n\n" +
						"Fora do horário de expediente, o acesso é monitorado pelo S.I.A.",
				},
				new Section
				{
					Heading = "Serviços",
					Links = new[]
					{
						new Link("Solicitar 2ª via do diploma", "indisponivel"),
						new Link("Consultar registro do diploma", "indisponivel"),
					},
				},
			},
		};

		_pages["historico"] = new Page
		{
			Path = "catalogo_academico/historico", Title = "Histórico escolar",
			Subtitle = "Consulte suas disciplinas cursadas", Keywords = new[] { "histórico", "notas" },
			Sections = new[]
			{
				new Section
				{
					Heading = "Seu histórico",
					Body = "Cálculo I ........ REPROVADO\nCálculo I ........ REPROVADO\nCálculo I ........ CURSANDO\n" +
						"Algoritmos ..... APROVADO\nFísica I ......... trancado",
					Info = "Dúvidas sobre notas devem ser enviadas ao professor responsável até ontem.",
				},
			},
		};

		_pages["calendario"] = new Page
		{
			Path = "catalogo_academico/calendario", Title = "Calendário acadêmico",
			Subtitle = "Datas importantes do semestre", Keywords = new[] { "calendário", "prazos" },
			Sections = new[]
			{
				new Section
				{
					Heading = "Próximas datas",
					Body = "Início das aulas: segunda-feira.\nFim das aulas: logo depois da greve.\n" +
						"Colação de grau: assim que o aluno encontrar o diploma.",
				},
			},
		};

		_pages["senha"] = new Page
		{
			Path = "catalogo_ti/senha", Title = "Recuperação de senha",
			Subtitle = "Esqueceu a senha da Intranet?", Keywords = new[] { "senha", "intranet" },
			Sections = new[]
			{
				new Section
				{
					Heading = "Como recuperar",
					Body = "Para recuperar sua senha, compareça pessoalmente ao setor de TI, no 4º andar.\n" +
						"Para entrar no 4º andar, é preciso fazer login na Intranet.",
					Info = "Não é aqui que fica a senha da porta. Procure nos serviços acadêmicos.",
				},
			},
		};

		_pages["sistemas"] = new Page
		{
			Path = "catalogo_ti/disponibilidade", Title = "Disponibilidade dos sistemas",
			Subtitle = "Situação dos sistemas agora", Keywords = new[] { "status", "sistemas" },
			Sections = new[]
			{
				new Section
				{
					Heading = "Situação",
					Body = "E-mail institucional ........ instável\nSistema acadêmico ........ fora do ar\n" +
						"Elevadores ......................... sem energia\nCâmeras do 4º andar ...... ONLINE (S.I.A.)",
				},
			},
		};

		_pages["pessoas"] = Simple("catalogo_pessoas", "Gestão com Pessoas", "Servidores, férias e ponto",
			"O setor está em reunião desde 2019. Retornaremos o contato.");
		_pages["ouvidoria"] = Simple("ouvidoria", "Ouvidoria", "Elogios, sugestões e reclamações",
			"Sua manifestação foi registrada com o protocolo nº 000000001. Prazo de resposta: 4 anos.");
		_pages["administrativos"] = Simple("catalogo_adm", "Serviços Administrativos", "Compras, patrimônio e manutenção",
			"Pedido de troca das lâmpadas do 4º andar: em análise.\nPedido de conserto da escada: em análise.");
		_pages["chamados"] = Simple("atendimento/chamados", "Abertura de chamados", "Fale com o suporte",
			"Há 1.284 chamados na sua frente. Tempo estimado de espera: um semestre.");
		_pages["ramal"] = Simple("servicos/ramal", "Consulta Ramal/Voip", "Telefones dos setores",
			"Coordenação do curso: toca, toca e ninguém atende.\nSegurança: ramal desligado.");
		_pages["indisponivel"] = Simple("erro/503", "Página indisponível", "Erro 503",
			"O servidor desta página foi reprovado. Tente novamente no próximo semestre.");
	}

	private static Page Simple(string path, string title, string subtitle, string text) => new()
	{
		Path = path, Title = title, Subtitle = subtitle, Keywords = new[] { "atendimento" },
		Sections = new[] { new Section { Heading = "Informações", Body = text } },
	};
}
