using System;

namespace Triki.Core
{
    /// <summary>
    /// Oponente automático. Busca con negamax + poda alfa-beta sobre una copia ligera del
    /// tablero (solo enteros), con buffers de jugadas creados una vez: elegir jugada no asigna memoria.
    /// Aplica las mismas reglas que <see cref="TrikiGame"/> (línea, bloqueo y límite de movimientos),
    /// salvo el empate por repetición, que no se modela en la búsqueda.
    /// </summary>
    public sealed class TrikiAi
    {
        public const int HardDepth = 8;
        public const int NormalDepth = 3;
        public const int EasyDepth = 1;
        public const double EasyRandomChance = 0.5;

        private const int CellCount = BoardGraph.CellCount;
        private const int AllCells = (1 << CellCount) - 1;
        private const int TotalPieces = TrikiGame.PiecesPerPlayer * 2;

        // Colocación: como mucho 9 casillas vacías. Movimiento: 3 vacías, cada una alcanzable
        // por como mucho 3 fichas propias.
        private const int MaxMovesPerPly = CellCount;

        private const int WinScore = 10000;
        private const int Infinity = int.MaxValue / 2;

        private readonly Random _random;
        private readonly int[][] _moveBuffers;
        private readonly int[] _rootScores = new int[MaxMovesPerPly];
        private readonly int[] _neighbors = new int[CellCount];
        private readonly int[] _lines = new int[BoardLine.StraightLineCount];
        private int _lineCount;
        private int _maxMovementMoves;
        private int _forbiddenOpeningCell;

        public TrikiAi(AiDifficulty difficulty, Random random = null)
        {
            Difficulty = difficulty;
            _random = random ?? new Random();
            _moveBuffers = new int[HardDepth + 1][];
            for (var i = 0; i < _moveBuffers.Length; i++)
                _moveBuffers[i] = new int[MaxMovesPerPly];
        }

        private enum Outcome : byte
        {
            None,
            MoverWins,
            Draw,
        }

        public AiDifficulty Difficulty { get; }

        /// <summary>Elige la jugada para <see cref="TrikiGame.CurrentPlayer"/>.</summary>
        public AiMove ChooseMove(TrikiGame game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));
            if (game.Phase == GamePhase.GameOver)
                throw new InvalidOperationException("La partida ya terminó.");

            LoadRules(game);
            var state = new SearchState
            {
                One = game.Board.GetMask(Player.One),
                Two = game.Board.GetMask(Player.Two),
                ToMove = game.CurrentPlayer,
                PlacedTotal = game.GetPiecesPlaced(Player.One) + game.GetPiecesPlaced(Player.Two),
                MovesPlayed = game.MovementMovesPlayed,
            };

            var moves = _moveBuffers[0];
            var count = GenerateMoves(state, moves);
            if (count == 0)
                throw new InvalidOperationException("No hay jugadas posibles.");

            int chosen;
            switch (Difficulty)
            {
                case AiDifficulty.Easy:
                    chosen = _random.NextDouble() < EasyRandomChance
                        ? moves[_random.Next(count)]
                        : SearchBest(state, count, EasyDepth);
                    break;
                case AiDifficulty.Normal:
                    chosen = SearchBest(state, count, NormalDepth);
                    break;
                default:
                    chosen = SearchBest(state, count, HardDepth);
                    break;
            }

            return Decode(chosen);
        }

        private void LoadRules(TrikiGame game)
        {
            var graph = game.Board.Graph;
            for (var cell = 0; cell < CellCount; cell++)
                _neighbors[cell] = graph.GetNeighborMask(cell);

            _lineCount = game.WinLineCount;
            for (var i = 0; i < _lineCount; i++)
                _lines[i] = game.GetWinLine(i).Mask;

            _maxMovementMoves = game.Rules.MaxMovementMoves;
            _forbiddenOpeningCell = game.Rules.BanCenterOpening ? BoardGraph.CenterCell : TrikiGame.NoCell;
        }

        /// <summary>Puntúa cada jugada de la raíz y elige al azar entre las mejores.</summary>
        private int SearchBest(SearchState state, int count, int depth)
        {
            var moves = _moveBuffers[0];
            var best = -Infinity;
            for (var i = 0; i < count; i++)
            {
                var child = state;
                var outcome = Apply(ref child, moves[i]);
                var score = ScoreChild(outcome, child, depth - 1, -Infinity, Infinity, 1);
                _rootScores[i] = score;
                if (score > best)
                    best = score;
            }

            var ties = 0;
            for (var i = 0; i < count; i++)
            {
                if (_rootScores[i] == best)
                    ties++;
            }

            var pick = _random.Next(ties);
            for (var i = 0; i < count; i++)
            {
                if (_rootScores[i] == best && pick-- == 0)
                    return moves[i];
            }

            return moves[0];
        }

        private int Negamax(SearchState state, int depth, int alpha, int beta, int ply)
        {
            if (depth <= 0)
                return Evaluate(state);

            var moves = _moveBuffers[ply];
            var count = GenerateMoves(state, moves);
            if (count == 0)
                return -(WinScore - ply);

            var best = -Infinity;
            for (var i = 0; i < count; i++)
            {
                var child = state;
                var outcome = Apply(ref child, moves[i]);
                var score = ScoreChild(outcome, child, depth - 1, alpha, beta, ply + 1);
                if (score > best)
                    best = score;
                if (best > alpha)
                    alpha = best;
                if (alpha >= beta)
                    break;
            }

            return best;
        }

        /// <summary>
        /// Puntuación de una jugada desde el punto de vista de quien la hizo.
        /// Ganar antes vale más que ganar después (<c>WinScore - ply</c>).
        /// </summary>
        private int ScoreChild(Outcome outcome, SearchState child, int depth, int alpha, int beta, int ply)
        {
            switch (outcome)
            {
                case Outcome.MoverWins: return WinScore - ply;
                case Outcome.Draw: return 0;
                default: return -Negamax(child, depth, -beta, -alpha, ply);
            }
        }

        private int GenerateMoves(SearchState state, int[] moves)
        {
            var empty = ~(state.One | state.Two) & AllCells;
            var count = 0;

            if (state.PlacedTotal < TotalPieces)
            {
                // La primera ficha de la partida no puede ir al centro: mismo veto que TrikiGame.
                var forbidden = state.PlacedTotal == 0 ? _forbiddenOpeningCell : TrikiGame.NoCell;
                for (var cell = 0; cell < CellCount; cell++)
                {
                    if ((empty & (1 << cell)) != 0 && cell != forbidden)
                        moves[count++] = cell;
                }
                return count;
            }

            var mine = state.GetMask(state.ToMove);
            for (var from = 0; from < CellCount; from++)
            {
                if ((mine & (1 << from)) == 0)
                    continue;

                var targets = _neighbors[from] & empty;
                for (var to = 0; targets != 0; to++, targets >>= 1)
                {
                    if ((targets & 1) != 0)
                        moves[count++] = EncodeMove(from, to);
                }
            }
            return count;
        }

        /// <summary>Mismo orden de comprobaciones que <see cref="TrikiGame"/>: línea, bloqueo, límite.</summary>
        private Outcome Apply(ref SearchState state, int move)
        {
            var mover = state.ToMove;
            var mine = state.GetMask(mover);
            var isPlacement = move < CellCount;

            if (isPlacement)
            {
                mine |= 1 << move;
                state.PlacedTotal++;
            }
            else
            {
                var from = (move - CellCount) / CellCount;
                var to = (move - CellCount) % CellCount;
                mine = (mine & ~(1 << from)) | (1 << to);
                state.MovesPlayed++;
            }

            state.SetMask(mover, mine);
            if (HasLine(mine))
                return Outcome.MoverWins;

            var opponent = mover.Opponent();
            state.ToMove = opponent;

            if (state.PlacedTotal >= TotalPieces && !HasAnyMove(state, opponent))
                return Outcome.MoverWins;

            if (!isPlacement && state.MovesPlayed >= _maxMovementMoves)
                return Outcome.Draw;

            return Outcome.None;
        }

        /// <summary>Heurística desde el punto de vista de quien mueve: amenazas de línea y movilidad.</summary>
        private int Evaluate(SearchState state)
        {
            var me = state.GetMask(state.ToMove);
            var them = state.GetMask(state.ToMove.Opponent());
            var score = 0;

            for (var i = 0; i < _lineCount; i++)
            {
                var mine = CountBits(_lines[i] & me);
                var theirs = CountBits(_lines[i] & them);
                if (theirs == 0)
                    score += mine == 2 ? 10 : mine;
                if (mine == 0)
                    score -= theirs == 2 ? 10 : theirs;
            }

            if (state.PlacedTotal >= TotalPieces)
                score += CountMoves(state, state.ToMove) - CountMoves(state, state.ToMove.Opponent());

            return score;
        }

        private bool HasLine(int mask)
        {
            for (var i = 0; i < _lineCount; i++)
            {
                if ((mask & _lines[i]) == _lines[i])
                    return true;
            }
            return false;
        }

        private bool HasAnyMove(SearchState state, Player player)
        {
            var pieces = state.GetMask(player);
            var empty = ~(state.One | state.Two) & AllCells;
            for (var cell = 0; pieces != 0; cell++, pieces >>= 1)
            {
                if ((pieces & 1) != 0 && (_neighbors[cell] & empty) != 0)
                    return true;
            }
            return false;
        }

        private int CountMoves(SearchState state, Player player)
        {
            var pieces = state.GetMask(player);
            var empty = ~(state.One | state.Two) & AllCells;
            var total = 0;
            for (var cell = 0; pieces != 0; cell++, pieces >>= 1)
            {
                if ((pieces & 1) != 0)
                    total += CountBits(_neighbors[cell] & empty);
            }
            return total;
        }

        private static int CountBits(int value)
        {
            var count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }
            return count;
        }

        private static int EncodeMove(int from, int to) => CellCount + from * CellCount + to;

        private static AiMove Decode(int move)
        {
            if (move < CellCount)
                return AiMove.Place(move);
            return AiMove.Move((move - CellCount) / CellCount, (move - CellCount) % CellCount);
        }

        private struct SearchState
        {
            public int One;
            public int Two;
            public Player ToMove;
            public int PlacedTotal;
            public int MovesPlayed;

            public int GetMask(Player player) => player == Player.One ? One : Two;

            public void SetMask(Player player, int mask)
            {
                if (player == Player.One)
                    One = mask;
                else
                    Two = mask;
            }
        }
    }
}
