using System;

namespace Triki.Core
{
    /// <summary>
    /// Reglas y turnos.
    /// <list type="number">
    /// <item>Colocación: cada jugador pone sus <see cref="PiecesPerPlayer"/> fichas por turnos.</item>
    /// <item>Movimiento: en su turno, el jugador lleva una ficha propia a una casilla vacía
    /// conectada por una arista.</item>
    /// </list>
    /// Gana quien alinee sus 3 fichas en una recta unida por aristas (en cualquier fase), o
    /// quien deje al rival sin movimientos cuando le toca mover.
    /// En la fase de movimiento hay empate si una posición se repite o se agota el límite
    /// de movimientos (<see cref="TrikiRules"/>).
    /// La capa visual escucha los eventos; nunca consulta el estado cada frame.
    /// </summary>
    public sealed class TrikiGame
    {
        public const int PiecesPerPlayer = 3;

        /// <summary>Valor de <see cref="ForbiddenCell"/> cuando no hay ninguna casilla vetada.</summary>
        public const int NoCell = -1;

        // Indexado por (int)Player; la posición 0 (None) no se usa.
        private readonly int[] _piecesPlaced = new int[3];
        private readonly BoardLine[] _winLines;

        // Posiciones desde que empezó el movimiento (todas las jugadas son reversibles).
        // Tamaño fijo: la inicial + una por movimiento permitido.
        private readonly int[] _positionHistory;
        private int _positionCount;

        public TrikiGame() : this(BoardGraph.CreateSquare(), TrikiRules.Default)
        {
        }

        public TrikiGame(TrikiRules rules) : this(BoardGraph.CreateSquare(), rules)
        {
        }

        public TrikiGame(BoardGraph graph) : this(graph, TrikiRules.Default)
        {
        }

        public TrikiGame(BoardGraph graph, TrikiRules rules)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Board = new Board(graph);
            _winLines = BuildWinLines(graph);
            _positionHistory = new int[rules.MaxMovementMoves + 1];
            Reset();
        }

        /// <summary>Casilla y dueño de la ficha recién colocada.</summary>
        public event Action<int, Player> PiecePlaced;

        /// <summary>Origen, destino y dueño de la ficha recién movida.</summary>
        public event Action<int, int, Player> PieceMoved;

        /// <summary>Se emite al cambiar de turno. No se emite en la jugada que gana.</summary>
        public event Action<Player> TurnChanged;

        public event Action<GamePhase> PhaseChanged;

        /// <summary>Ganador y motivo. Se emite después de <see cref="PhaseChanged"/>.</summary>
        public event Action<Player, WinReason> GameWon;

        /// <summary>Empate y motivo. Se emite después de <see cref="PhaseChanged"/>.</summary>
        public event Action<DrawReason> GameDrawn;

        public event Action GameReset;

        public TrikiRules Rules { get; }

        public Board Board { get; }

        /// <summary>Movimientos hechos en la fase de movimiento (de ambos jugadores).</summary>
        public int MovementMovesPlayed { get; private set; }

        /// <summary>La partida no tiene ninguna ficha todavía: toca la primera colocación.</summary>
        public bool IsOpeningMove => Phase == GamePhase.Placement &&
                                     _piecesPlaced[(int)Player.One] + _piecesPlaced[(int)Player.Two] == 0;

        /// <summary>
        /// Casilla que ahora mismo no se puede usar, o <see cref="NoCell"/> si no hay ninguna.
        /// La vista la marca para que el veto se vea antes de intentar la jugada.
        /// </summary>
        public int ForbiddenCell => Rules.BanCenterOpening && IsOpeningMove ? BoardGraph.CenterCell : NoCell;

        /// <summary>La partida terminó sin ganador.</summary>
        public bool IsDraw => Phase == GamePhase.GameOver && Winner == Player.None;

        /// <summary>Solo tiene sentido si <see cref="IsDraw"/>.</summary>
        public DrawReason DrawReason { get; private set; }

        /// <summary>A quién le toca. Al terminar la partida conserva al ganador.</summary>
        public Player CurrentPlayer { get; private set; }

        public GamePhase Phase { get; private set; }

        /// <summary><see cref="Player.None"/> mientras nadie haya ganado.</summary>
        public Player Winner { get; private set; }

        /// <summary>Solo tiene sentido si hay <see cref="Winner"/>.</summary>
        public WinReason WinReason { get; private set; }

        /// <summary>Solo tiene sentido si <see cref="WinReason"/> es <see cref="Core.WinReason.Line"/>.</summary>
        public BoardLine WinningLine { get; private set; }

        /// <summary>Líneas que dan victoria con el grafo de esta partida.</summary>
        public int WinLineCount => _winLines.Length;

        public BoardLine GetWinLine(int index) => _winLines[index];

        public int GetPiecesPlaced(Player player) => _piecesPlaced[ToIndex(player)];

        public int GetPiecesInHand(Player player) => PiecesPerPlayer - _piecesPlaced[ToIndex(player)];

        /// <summary>Casillas vacías a las que puede ir la ficha de <paramref name="from"/>, como bits.</summary>
        public int GetMoveTargets(int from)
        {
            if (!BoardGraph.IsValidCell(from) || Board.IsEmpty(from))
                return 0;
            return Board.Graph.GetNeighborMask(from) & Board.GetMask(Player.None);
        }

        public bool HasAnyMove(Player player)
        {
            var pieces = Board.GetMask(player);
            var empty = Board.GetMask(Player.None);
            for (var cell = 0; pieces != 0; cell++, pieces >>= 1)
            {
                if ((pieces & 1) != 0 && (Board.Graph.GetNeighborMask(cell) & empty) != 0)
                    return true;
            }
            return false;
        }

        public PlaceResult TryPlace(int cell)
        {
            if (Phase != GamePhase.Placement)
                return PlaceResult.WrongPhase;
            if (!BoardGraph.IsValidCell(cell))
                return PlaceResult.InvalidCell;
            if (!Board.IsEmpty(cell))
                return PlaceResult.CellOccupied;
            if (cell == ForbiddenCell)
                return PlaceResult.ForbiddenOpening;

            var player = CurrentPlayer;
            Board.Set(cell, player);
            _piecesPlaced[(int)player]++;
            PiecePlaced?.Invoke(cell, player);

            if (TryWinByLine(player))
                return PlaceResult.Placed;

            var allPlaced = _piecesPlaced[(int)Player.One] == PiecesPerPlayer &&
                            _piecesPlaced[(int)Player.Two] == PiecesPerPlayer;
            if (allPlaced)
            {
                if (TryWinByBlock(player))
                    return PlaceResult.Placed;
                ChangePhase(GamePhase.Movement);
                RecordPosition(player.Opponent());
            }

            PassTurn(player);
            return PlaceResult.Placed;
        }

        public MoveResult TryMove(int from, int to)
        {
            if (Phase != GamePhase.Movement)
                return MoveResult.WrongPhase;
            if (!BoardGraph.IsValidCell(from) || !BoardGraph.IsValidCell(to))
                return MoveResult.InvalidCell;

            var player = CurrentPlayer;
            if (Board[from] != player)
                return MoveResult.NotYourPiece;
            if (!Board.IsEmpty(to))
                return MoveResult.CellOccupied;
            if (!Board.Graph.AreAdjacent(from, to))
                return MoveResult.NotAdjacent;

            Board.Set(from, Player.None);
            Board.Set(to, player);
            MovementMovesPlayed++;
            PieceMoved?.Invoke(from, to, player);

            // La victoria tiene prioridad sobre el empate, incluso en el último movimiento permitido.
            if (TryWinByLine(player) || TryWinByBlock(player))
                return MoveResult.Moved;

            if (RecordPosition(player.Opponent()) >= Rules.RepetitionLimit)
            {
                EndInDraw(DrawReason.Repetition);
                return MoveResult.Moved;
            }

            if (MovementMovesPlayed >= Rules.MaxMovementMoves)
            {
                EndInDraw(DrawReason.MoveLimit);
                return MoveResult.Moved;
            }

            PassTurn(player);
            return MoveResult.Moved;
        }

        public void Reset(Player startingPlayer = Player.One)
        {
            ToIndex(startingPlayer);

            Board.Clear();
            Array.Clear(_piecesPlaced, 0, _piecesPlaced.Length);
            CurrentPlayer = startingPlayer;
            Phase = GamePhase.Placement;
            Winner = Player.None;
            WinReason = default;
            WinningLine = default;
            DrawReason = default;
            MovementMovesPlayed = 0;
            _positionCount = 0;
            GameReset?.Invoke();
        }

        /// <summary>
        /// Guarda la posición actual (fichas de ambos jugadores + quién mueve) y devuelve
        /// cuántas veces ha aparecido, incluida esta. Búsqueda lineal: son como mucho
        /// <see cref="TrikiRules.MaxMovementMoves"/> + 1 entradas y no asigna memoria.
        /// </summary>
        private int RecordPosition(Player toMove)
        {
            var key = Board.GetMask(Player.One)
                      | (Board.GetMask(Player.Two) << BoardGraph.CellCount)
                      | (toMove == Player.Two ? 1 << (BoardGraph.CellCount * 2) : 0);

            var occurrences = 1;
            for (var i = 0; i < _positionCount; i++)
            {
                if (_positionHistory[i] == key)
                    occurrences++;
            }

            _positionHistory[_positionCount++] = key;
            return occurrences;
        }

        private void EndInDraw(DrawReason reason)
        {
            DrawReason = reason;
            ChangePhase(GamePhase.GameOver);
            GameDrawn?.Invoke(reason);
        }

        private bool TryWinByLine(Player player)
        {
            var mask = Board.GetMask(player);
            for (var i = 0; i < _winLines.Length; i++)
            {
                var line = _winLines[i];
                if ((mask & line.Mask) == line.Mask)
                {
                    WinningLine = line;
                    EndGame(player, WinReason.Line);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Gana <paramref name="player"/> si a su rival le toca mover y no puede.</summary>
        private bool TryWinByBlock(Player player)
        {
            if (HasAnyMove(player.Opponent()))
                return false;

            EndGame(player, WinReason.OpponentBlocked);
            return true;
        }

        private void EndGame(Player winner, WinReason reason)
        {
            Winner = winner;
            WinReason = reason;
            ChangePhase(GamePhase.GameOver);
            GameWon?.Invoke(winner, reason);
        }

        private void PassTurn(Player player)
        {
            CurrentPlayer = player.Opponent();
            TurnChanged?.Invoke(CurrentPlayer);
        }

        private void ChangePhase(GamePhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        /// <summary>Una recta solo cuenta si sus casillas se pueden recorrer por aristas del grafo.</summary>
        private static BoardLine[] BuildWinLines(BoardGraph graph)
        {
            var count = 0;
            for (var i = 0; i < BoardLine.StraightLineCount; i++)
            {
                if (IsConnected(graph, BoardLine.GetStraightLine(i)))
                    count++;
            }

            var lines = new BoardLine[count];
            var next = 0;
            for (var i = 0; i < BoardLine.StraightLineCount; i++)
            {
                var line = BoardLine.GetStraightLine(i);
                if (IsConnected(graph, line))
                    lines[next++] = line;
            }
            return lines;
        }

        private static bool IsConnected(BoardGraph graph, BoardLine line)
        {
            return graph.AreAdjacent(line.A, line.B) && graph.AreAdjacent(line.B, line.C);
        }

        private static int ToIndex(Player player)
        {
            if (player != Player.One && player != Player.Two)
                throw new ArgumentOutOfRangeException(nameof(player), player, "Se esperaba Player.One o Player.Two.");
            return (int)player;
        }
    }
}
