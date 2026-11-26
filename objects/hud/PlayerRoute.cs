using Godot;

// Caminho do jogador até o objetivo, pelo chão da fase (contornando paredes, passando pelas portas).
// É uma grade com um quadrado por tile, montada a partir da camada de chão (TileMapLayer no grupo "floor"):
// tudo que é chão está livre, menos (a cada consulta) os móveis sólidos e as portas trancadas, que o jogador
// não atravessa. Serve para a seta de direção (GuideArrow) apontar para onde andar, contornando paredes,
// balcões e estantes, e não em linha reta através deles.
public static class PlayerRoute
{
	private static AStarGrid2D _grid;
	private static TileMapLayer _floor;

	private static bool EnsureBuilt(Node anyNode)
	{
		if (_grid != null && GodotObject.IsInstanceValid(_floor))
			return true;

		_floor = anyNode.GetTree().GetFirstNodeInGroup("floor") as TileMapLayer;
		if (_floor == null) return false;

		_grid = new AStarGrid2D
		{
			Region = _floor.GetUsedRect(),
			CellSize = Vector2.One,
			DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles,
		};
		_grid.Update();

		Rect2I region = _grid.Region;
		for (int y = region.Position.Y; y < region.End.Y; y++)
			for (int x = region.Position.X; x < region.End.X; x++)
				if (_floor.GetCellSourceId(new Vector2I(x, y)) == -1)
					_grid.SetPointSolid(new Vector2I(x, y));
		return true;
	}

	private static Vector2I ToCell(Vector2 globalPosition) => _floor.LocalToMap(_floor.ToLocal(globalPosition));

	private static Vector2 ToWorld(Vector2I cell) => _floor.ToGlobal(_floor.MapToLocal(cell));

	private static bool IsFree(Vector2I cell) => _grid.IsInBoundsv(cell) && !_grid.IsPointSolid(cell);

	// O próprio quadrado ou o vizinho livre mais perto (o alvo pode estar encostado numa parede).
	private static bool TryNearestFree(Vector2I cell, out Vector2I free)
	{
		free = cell;
		if (IsFree(cell)) return true;
		for (int radius = 1; radius <= 2; radius++)
			for (int dy = -radius; dy <= radius; dy++)
				for (int dx = -radius; dx <= radius; dx++)
				{
					Vector2I neighbor = new(cell.X + dx, cell.Y + dy);
					if (!IsFree(neighbor)) continue;
					free = neighbor;
					return true;
				}
		return false;
	}

	// Até onde procura a próxima curva no caminho (em quadrados).
	private const int MaxLookahead = 24;
	private const float BodyHalfWidth = 18f;

	// Ponto para onde andar agora: a próxima curva do caminho, isto é, o ponto mais adiantado do caminho
	// que dá para alcançar em linha reta sem esbarrar em parede, móvel ou porta trancada. Assim a seta só
	// muda de direção quando é preciso virar (e não a cada quadrado andado).
	// Sem chão na fase (ou sem caminho), é o próprio destino (a seta aponta em linha reta).
	public static Vector2 NextStep(Node anyNode, Vector2 from, Vector2 to)
	{
		if (!EnsureBuilt(anyNode)) return to;
		if (!TryNearestFree(ToCell(from), out Vector2I start) || !TryNearestFree(ToCell(to), out Vector2I end))
			return to;
		if (start == end) return to;

		// Móveis e portas trancadas bloqueiam só durante esta consulta (eles mudam: estante cai, porta abre).
		System.Collections.Generic.List<Vector2I> blocked = BlockDynamic(anyNode, start, end);
		Godot.Collections.Array<Vector2I> path = _grid.GetIdPath(start, end, true);
		Vector2 step = to;
		if (path.Count >= 2)
		{
			bool reachesEnd = path[path.Count - 1] == end;
			step = reachesEnd ? to : ToWorld(path[path.Count - 1]);
			if (!(reachesEnd && HasClearLine(from, to)))
			{
				// do mais longe para o mais perto: o primeiro que dá para ver em linha reta
				int last = Mathf.Min(path.Count - 1, MaxLookahead);
				step = ToWorld(path[1]);
				for (int i = last; i >= 1; i--)
					if (HasClearLine(from, ToWorld(path[i])))
					{
						step = ToWorld(path[i]);
						break;
					}
			}
		}
		foreach (Vector2I cell in blocked)
			_grid.SetPointSolid(cell, false);
		return step;
	}

	// Linha reta livre (na largura do corpo do jogador) pela grade: só chão, sem móvel nem porta trancada.
	private static bool HasClearLine(Vector2 from, Vector2 to)
	{
		Vector2 delta = to - from;
		float length = delta.Length();
		if (length < 1f) return true;
		Vector2 side = delta / length * BodyHalfWidth;
		side = new Vector2(-side.Y, side.X);
		int samples = Mathf.CeilToInt(length / 16f);
		for (int k = 0; k <= samples; k++)
		{
			Vector2 p = from + delta * (k / (float)samples);
			if (!IsFree(ToCell(p)) || !IsFree(ToCell(p + side)) || !IsFree(ToCell(p - side)))
				return false;
		}
		return true;
	}

	// Marca como bloqueados os quadrados (com o centro) debaixo de móveis sólidos e portas trancadas.
	// Nunca o quadrado do jogador nem o do destino. Devolve os que marcou, para desmarcar depois.
	private static System.Collections.Generic.List<Vector2I> BlockDynamic(Node anyNode, Vector2I start, Vector2I end)
	{
		System.Collections.Generic.List<Vector2I> marked = new();
		void Block(Rect2 rect)
		{
			Vector2I from = ToCell(rect.Position);
			Vector2I to = ToCell(rect.End);
			for (int y = from.Y; y <= to.Y; y++)
				for (int x = from.X; x <= to.X; x++)
				{
					Vector2I cell = new(x, y);
					if (cell == start || cell == end || !IsFree(cell)) continue;
					if (!rect.HasPoint(ToWorld(cell))) continue;
					_grid.SetPointSolid(cell, true);
					marked.Add(cell);
				}
		}

		foreach (Node node in anyNode.GetTree().GetNodesInGroup("cover"))
			if (node is Prop prop && prop.IsVisibleInTree())
				Block(prop.GlobalFootRect);
		foreach (Node node in anyNode.GetTree().GetNodesInGroup("obstacles"))
			if (node is Door door && door.IsLocked)
				Block(new Rect2(door.GlobalPosition + new Vector2(-door.Size.X / 2, -door.Size.Y), door.Size));
		return marked;
	}
}
