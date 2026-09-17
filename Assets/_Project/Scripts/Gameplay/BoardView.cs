using System;
using Triki.Core;
using UnityEngine;

namespace Triki.Gameplay
{
    /// <summary>
    /// Presentación del tablero: dibuja la placa, casillas y aristas a partir de un
    /// <see cref="BoardGraph"/>, y muestra fichas, selección y victoria con animaciones cortas.
    /// No conoce las reglas; solo pinta lo que le dicen.
    /// Todos los renderers se crean una vez en <see cref="Build"/> y luego solo se reutilizan;
    /// las animaciones usan arrays fijos y <c>Update</c> no hace nada si no hay ninguna activa.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoardView : MonoBehaviour
    {
        private const int CircleResolution = 128;
        private const int PanelResolution = 64;
        private const int PanelCornerRadius = 16;
        private const int NoPiece = -1;

        private const int BoardSortingOrder = -10;
        private const int EdgeSortingOrder = 0;
        private const int NodeSortingOrder = 1;
        private const int HintSortingOrder = 2;
        private const int WinLineSortingOrder = 3;
        private const int SelectionSortingOrder = 4;
        private const int ShadowSortingOrder = 5;
        private const int PieceSortingOrder = 6;

        [Header("Layout (unidades de mundo)")]
        [SerializeField, Min(0.5f)] private float _spacing = 3f;
        [SerializeField, Min(0.05f)] private float _nodeDiameter = 0.5f;
        [SerializeField, Min(0.05f)] private float _pieceDiameter = 1.1f;
        [SerializeField, Min(0.01f)] private float _edgeThickness = 0.08f;
        [Tooltip("Radio de selección alrededor de cada casilla, como fracción de la separación.")]
        [SerializeField, Range(0.1f, 0.5f)] private float _pickRadius = 0.45f;
        [Tooltip("Desplazamiento de la sombra, como fracción del diámetro de la ficha.")]
        [SerializeField] private Vector2 _shadowOffset = new Vector2(0.05f, -0.08f);

        [Header("Selección")]
        [SerializeField, Min(0.05f)] private float _selectionDiameter = 1.45f;
        [SerializeField, Min(0.05f)] private float _hintDiameter = 0.8f;
        [SerializeField] private Color _selectionColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color _hintColor = new Color(1f, 1f, 1f, 0.35f);

        [Header("Victoria")]
        [SerializeField, Min(0.01f)] private float _winLineThickness = 0.3f;
        [SerializeField, Min(1f)] private float _winPieceScale = 1.2f;

        [Header("Animación (segundos; 0 = instantáneo)")]
        [SerializeField, Min(0f)] private float _placeDuration = 0.18f;
        [SerializeField, Min(0f)] private float _moveDuration = 0.22f;
        [SerializeField, Min(0f)] private float _winDuration = 0.3f;

        [Header("Sprites (opcionales: si faltan se generan en runtime)")]
        [SerializeField] private Sprite _circleSprite;
        [SerializeField] private Sprite _lineSprite;
        [Tooltip("Ficha en escala de grises; se tiñe con el color del jugador.")]
        [SerializeField] private Sprite _pieceSprite;
        [SerializeField] private Sprite _shadowSprite;
        [Tooltip("Fondo del tablero. Se dibuja en modo Sliced: define los bordes 9-slice del sprite.")]
        [SerializeField] private Sprite _boardSprite;

        [Header("Colores")]
        [SerializeField] private Color _boardColor = new Color(0.13f, 0.15f, 0.19f);
        [SerializeField] private Color _edgeColor = new Color(0.45f, 0.49f, 0.58f);
        [SerializeField] private Color _nodeColor = new Color(0.82f, 0.85f, 0.91f);
        [SerializeField] private Color _shadowColor = new Color(0f, 0f, 0f, 0.4f);
        [SerializeField] private Color _playerOneColor = new Color(0.95f, 0.36f, 0.33f);
        [SerializeField] private Color _playerTwoColor = new Color(0.29f, 0.64f, 0.96f);

        private readonly int[] _pieceAtCell = new int[BoardGraph.CellCount];
        private readonly SpriteRenderer[] _piecePool = new SpriteRenderer[TrikiGame.PiecesPerPlayer * 2];
        private readonly Tween[] _pieceTweens = new Tween[TrikiGame.PiecesPerPlayer * 2];
        private readonly SpriteRenderer[] _hints = new SpriteRenderer[BoardGraph.CellCount];
        private int _piecesInUse;
        private SpriteRenderer _winLine;
        private Tween _winLineTween;
        private SpriteRenderer _selection;
        private int _activeTweens;

        private Sprite _ownedCircle;
        private Sprite _ownedLine;
        private Sprite _ownedPiece;
        private Sprite _ownedShadow;
        private Sprite _ownedBoard;
        private bool _built;

        private enum TweenKind : byte
        {
            None,

            /// <summary>Escala con rebote (aparecer, agrandarse al ganar).</summary>
            Pop,

            /// <summary>Posición con aceleración suave (mover).</summary>
            Slide,

            /// <summary>Escala sin rebote (la línea ganadora crece).</summary>
            Grow,
        }

        public void Build(BoardGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            if (_built)
                throw new InvalidOperationException($"{nameof(BoardView)} ya está construido.");
            _built = true;

            // Los sprites asignados en el inspector tienen prioridad; solo se genera lo que falte.
            if (_circleSprite == null)
                _ownedCircle = SpriteFactory.CreateCircle(CircleResolution);
            if (_lineSprite == null)
                _ownedLine = SpriteFactory.CreateSquare();
            if (_pieceSprite == null)
                _ownedPiece = SpriteFactory.CreatePiece(CircleResolution);
            if (_shadowSprite == null)
                _ownedShadow = SpriteFactory.CreateSoftCircle(CircleResolution);
            if (_boardSprite == null)
                _ownedBoard = SpriteFactory.CreateRoundedPanel(PanelResolution, PanelCornerRadius);

            var circle = _circleSprite != null ? _circleSprite : _ownedCircle;
            var line = _lineSprite != null ? _lineSprite : _ownedLine;
            var pieceSprite = _pieceSprite != null ? _pieceSprite : _ownedPiece;
            var shadowSprite = _shadowSprite != null ? _shadowSprite : _ownedShadow;
            var boardSprite = _boardSprite != null ? _boardSprite : _ownedBoard;

            var board = CreateRenderer("Board Plate", transform, boardSprite, _boardColor, BoardSortingOrder);
            board.drawMode = SpriteDrawMode.Sliced;
            board.size = GetLocalHalfExtents() * 2f;

            var edgesRoot = CreateGroup("Edges");
            for (var i = 0; i < graph.EdgeCount; i++)
            {
                graph.GetEdge(i, out var a, out var b);
                var edge = CreateRenderer("Edge " + a + "-" + b, edgesRoot, line, _edgeColor, EdgeSortingOrder);
                PlaceBar(edge.transform, a, b, _edgeThickness);
            }

            var nodesRoot = CreateGroup("Nodes");
            var hintsRoot = CreateGroup("Hints");
            for (var cell = 0; cell < BoardGraph.CellCount; cell++)
            {
                var node = CreateRenderer("Node " + cell, nodesRoot, circle, _nodeColor, NodeSortingOrder);
                PlaceCircle(node, cell, _nodeDiameter);

                var hint = CreateRenderer("Hint " + cell, hintsRoot, circle, _hintColor, HintSortingOrder);
                PlaceCircle(hint, cell, _hintDiameter);
                hint.enabled = false;
                _hints[cell] = hint;
            }

            _winLine = CreateRenderer("Win Line", transform, line, Color.white, WinLineSortingOrder);
            _winLine.enabled = false;

            _selection = CreateRenderer("Selection", transform, circle, _selectionColor, SelectionSortingOrder);
            _selection.transform.localScale = new Vector3(_selectionDiameter, _selectionDiameter, 1f);
            _selection.enabled = false;

            var piecesRoot = CreateGroup("Pieces");
            for (var i = 0; i < _piecePool.Length; i++)
            {
                var piece = CreateRenderer("Piece " + i, piecesRoot, pieceSprite, Color.white, PieceSortingOrder);
                piece.transform.localScale = new Vector3(_pieceDiameter, _pieceDiameter, 1f);

                // La sombra es hija: se mueve y escala con la ficha. Su posición local está en
                // unidades de la ficha (diámetro 1), de ahí que el offset sea una fracción.
                var shadow = CreateRenderer("Shadow", piece.transform, shadowSprite, _shadowColor, ShadowSortingOrder);
                shadow.transform.localPosition = _shadowOffset;

                piece.gameObject.SetActive(false);
                _piecePool[i] = piece;
            }

            ClearPieces();
        }

        public void ShowPiece(int cell, Player player)
        {
            if (_pieceAtCell[cell] != NoPiece)
                throw new InvalidOperationException($"La casilla {cell} ya muestra una ficha.");
            if (_piecesInUse >= _piecePool.Length)
                throw new InvalidOperationException("No quedan fichas en el pool.");

            var index = _piecesInUse++;
            var piece = _piecePool[index];
            piece.color = GetPlayerColor(player);
            piece.transform.localPosition = GetCellLocalPosition(cell);
            piece.gameObject.SetActive(true);
            _pieceAtCell[cell] = index;

            StartPieceTween(index, TweenKind.Pop, Vector3.zero, Diameter(_pieceDiameter), _placeDuration);
        }

        public void MovePiece(int from, int to)
        {
            var index = _pieceAtCell[from];
            if (index == NoPiece)
                throw new InvalidOperationException($"La casilla {from} no muestra ninguna ficha.");
            if (_pieceAtCell[to] != NoPiece)
                throw new InvalidOperationException($"La casilla {to} ya muestra una ficha.");

            _pieceAtCell[to] = index;
            _pieceAtCell[from] = NoPiece;

            // Un tween por ficha: si seguía apareciendo, se termina antes para no dejarla a medio tamaño.
            CompleteTween(index);
            var start = _piecePool[index].transform.localPosition;
            StartPieceTween(index, TweenKind.Slide, start, GetCellLocalPosition(to), _moveDuration);
        }

        /// <summary>Marca la ficha elegida y las casillas a las que puede ir (bits de <paramref name="targetMask"/>).</summary>
        public void ShowSelection(int cell, int targetMask)
        {
            _selection.transform.localPosition = GetCellLocalPosition(cell);
            _selection.enabled = true;

            for (var i = 0; i < _hints.Length; i++)
                _hints[i].enabled = (targetMask & (1 << i)) != 0;
        }

        public void HideSelection()
        {
            if (_selection != null)
                _selection.enabled = false;

            for (var i = 0; i < _hints.Length; i++)
            {
                if (_hints[i] != null)
                    _hints[i].enabled = false;
            }
        }

        /// <summary>Resalta la línea ganadora (crece desde el centro) y agranda sus fichas.</summary>
        public void ShowWinningLine(BoardLine line, Player winner)
        {
            var from = GetCellLocalPosition(line.A);
            var to = GetCellLocalPosition(line.C);
            var delta = to - from;

            var t = _winLine.transform;
            t.localPosition = (from + to) * 0.5f;
            t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            _winLine.color = GetPlayerColor(winner);
            _winLine.enabled = true;

            var full = new Vector3(delta.magnitude, _winLineThickness, 1f);
            StartTween(ref _winLineTween, TweenKind.Grow, new Vector3(0f, _winLineThickness, 1f), full, _winDuration);
            ApplyTween(ref _winLineTween, t);

            ShowWinningPieces(line.Mask);
        }

        /// <summary>Agranda las fichas de las casillas marcadas en <paramref name="cellMask"/>.</summary>
        public void ShowWinningPieces(int cellMask)
        {
            HideSelection();
            var target = Diameter(_pieceDiameter * _winPieceScale);
            for (var cell = 0; cell < _pieceAtCell.Length; cell++)
            {
                var index = _pieceAtCell[cell];
                if (index == NoPiece || (cellMask & (1 << cell)) == 0)
                    continue;

                // Si todavía se estaba moviendo, se deja en su casilla antes de agrandarla.
                CompleteTween(index);
                StartPieceTween(index, TweenKind.Pop, _piecePool[index].transform.localScale, target, _winDuration);
            }
        }

        /// <summary>Oculta fichas, selección y resaltado; deja el tablero listo para otra partida.</summary>
        public void ClearPieces()
        {
            var baseScale = Diameter(_pieceDiameter);
            for (var i = 0; i < _piecePool.Length; i++)
            {
                _pieceTweens[i] = default;
                var piece = _piecePool[i];
                if (piece == null)
                    continue;
                piece.gameObject.SetActive(false);
                piece.transform.localScale = baseScale;
            }

            _winLineTween = default;
            _activeTweens = 0;
            if (_winLine != null)
                _winLine.enabled = false;
            HideSelection();

            for (var cell = 0; cell < _pieceAtCell.Length; cell++)
                _pieceAtCell[cell] = NoPiece;
            _piecesInUse = 0;
        }

        /// <summary>Lleva todas las animaciones a su estado final (útil para capturas y tests).</summary>
        public void CompleteAnimations()
        {
            for (var i = 0; i < _pieceTweens.Length; i++)
                CompleteTween(i);

            if (_winLineTween.Kind != TweenKind.None)
            {
                _winLineTween.Elapsed = _winLineTween.Duration;
                AdvanceTween(ref _winLineTween, _winLine.transform, 0f);
            }
        }

        /// <summary>Casilla bajo un punto del mundo, o <c>false</c> si el punto no cae cerca de ninguna.</summary>
        public bool TryGetCell(Vector3 worldPosition, out int cell)
        {
            var local = transform.InverseTransformPoint(worldPosition);
            var half = (BoardGraph.Size - 1) * 0.5f;
            var column = Mathf.RoundToInt(local.x / _spacing + half);
            var row = Mathf.RoundToInt(half - local.y / _spacing);

            cell = -1;
            if ((uint)column >= BoardGraph.Size || (uint)row >= BoardGraph.Size)
                return false;

            var candidate = BoardGraph.ToCell(row, column);
            var offset = (Vector2)local - (Vector2)GetCellLocalPosition(candidate);
            var maxDistance = _spacing * _pickRadius;
            if (offset.sqrMagnitude > maxDistance * maxDistance)
                return false;

            cell = candidate;
            return true;
        }

        /// <summary>
        /// Mitad del ancho y alto que ocupa el tablero en el mundo, incluido lo que sobresale de
        /// las casillas de los bordes (anillo de selección, fichas agrandadas al ganar).
        /// </summary>
        public Vector2 GetWorldHalfExtents()
        {
            var local = GetLocalHalfExtents();
            var scale = transform.lossyScale;
            return new Vector2(local.x * Mathf.Abs(scale.x), local.y * Mathf.Abs(scale.y));
        }

        public Vector3 GetCellLocalPosition(int cell)
        {
            var half = (BoardGraph.Size - 1) * 0.5f;
            return new Vector3(
                (BoardGraph.GetColumn(cell) - half) * _spacing,
                (half - BoardGraph.GetRow(cell)) * _spacing,
                0f);
        }

        private void Update()
        {
            if (_activeTweens == 0)
                return;

            var deltaTime = Time.deltaTime;
            for (var i = 0; i < _pieceTweens.Length; i++)
            {
                if (_pieceTweens[i].Kind != TweenKind.None)
                    AdvanceTween(ref _pieceTweens[i], _piecePool[i].transform, deltaTime);
            }

            if (_winLineTween.Kind != TweenKind.None)
                AdvanceTween(ref _winLineTween, _winLine.transform, deltaTime);
        }

        private void OnDestroy()
        {
            SpriteFactory.Release(_ownedCircle);
            SpriteFactory.Release(_ownedLine);
            SpriteFactory.Release(_ownedPiece);
            SpriteFactory.Release(_ownedShadow);
            SpriteFactory.Release(_ownedBoard);
        }

        private Vector2 GetLocalHalfExtents()
        {
            var cells = (BoardGraph.Size - 1) * 0.5f * _spacing;
            var overhang = Mathf.Max(_nodeDiameter, _pieceDiameter * _winPieceScale, _selectionDiameter) * 0.5f;
            return new Vector2(cells + overhang, cells + overhang);
        }

        private void StartPieceTween(int index, TweenKind kind, Vector3 from, Vector3 to, float duration)
        {
            StartTween(ref _pieceTweens[index], kind, from, to, duration);
            ApplyTween(ref _pieceTweens[index], _piecePool[index].transform);
        }

        private void StartTween(ref Tween tween, TweenKind kind, Vector3 from, Vector3 to, float duration)
        {
            if (tween.Kind == TweenKind.None)
                _activeTweens++;

            tween.Kind = kind;
            tween.From = from;
            tween.To = to;
            tween.Duration = duration;
            tween.Elapsed = 0f;
        }

        /// <summary>Aplica el primer fotograma (o el final, si la duración es 0) sin esperar a Update.</summary>
        private void ApplyTween(ref Tween tween, Transform target) => AdvanceTween(ref tween, target, 0f);

        private void CompleteTween(int index)
        {
            ref var tween = ref _pieceTweens[index];
            if (tween.Kind == TweenKind.None)
                return;
            tween.Elapsed = tween.Duration;
            AdvanceTween(ref tween, _piecePool[index].transform, 0f);
        }

        private void AdvanceTween(ref Tween tween, Transform target, float deltaTime)
        {
            tween.Elapsed += deltaTime;
            var t = tween.Duration > 0f ? Mathf.Clamp01(tween.Elapsed / tween.Duration) : 1f;

            switch (tween.Kind)
            {
                case TweenKind.Pop:
                    target.localScale = Vector3.LerpUnclamped(tween.From, tween.To, Easing.OutBack(t));
                    break;
                case TweenKind.Slide:
                    target.localPosition = Vector3.LerpUnclamped(tween.From, tween.To, Easing.InOutCubic(t));
                    break;
                case TweenKind.Grow:
                    target.localScale = Vector3.LerpUnclamped(tween.From, tween.To, Easing.OutCubic(t));
                    break;
            }

            if (t >= 1f)
            {
                tween.Kind = TweenKind.None;
                _activeTweens--;
            }
        }

        private static Vector3 Diameter(float diameter) => new Vector3(diameter, diameter, 1f);

        private Color GetPlayerColor(Player player) => player == Player.One ? _playerOneColor : _playerTwoColor;

        private void PlaceCircle(SpriteRenderer circle, int cell, float diameter)
        {
            var t = circle.transform;
            t.localPosition = GetCellLocalPosition(cell);
            t.localScale = Diameter(diameter);
        }

        /// <summary>Estira un sprite de 1 unidad para unir los centros de dos casillas.</summary>
        private void PlaceBar(Transform bar, int fromCell, int toCell, float thickness)
        {
            var from = GetCellLocalPosition(fromCell);
            var to = GetCellLocalPosition(toCell);
            var delta = to - from;

            bar.localPosition = (from + to) * 0.5f;
            bar.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            bar.localScale = new Vector3(delta.magnitude, thickness, 1f);
        }

        private Transform CreateGroup(string groupName)
        {
            var group = new GameObject(groupName).transform;
            group.SetParent(transform, false);
            return group;
        }

        private static SpriteRenderer CreateRenderer(string objectName, Transform parent, Sprite sprite, Color color, int sortingOrder)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            for (var cell = 0; cell < BoardGraph.CellCount; cell++)
                Gizmos.DrawWireSphere(GetCellLocalPosition(cell), _spacing * _pickRadius);
        }

        private struct Tween
        {
            public TweenKind Kind;
            public float Elapsed;
            public float Duration;
            public Vector3 From;
            public Vector3 To;
        }
    }
}
