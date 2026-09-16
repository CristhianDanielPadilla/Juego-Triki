using System;
using Triki.Core;
using UnityEngine;

namespace Triki.Gameplay
{
    /// <summary>
    /// Presentación del tablero: dibuja casillas y aristas a partir de un <see cref="BoardGraph"/>
    /// y muestra las fichas. No conoce las reglas; solo pinta lo que le dicen.
    /// Todos los renderers se crean una vez en <see cref="Build"/> y luego solo se reutilizan.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoardView : MonoBehaviour
    {
        private const int CircleResolution = 128;
        private const int EdgeSortingOrder = 0;
        private const int NodeSortingOrder = 1;
        private const int WinLineSortingOrder = 2;
        private const int PieceSortingOrder = 3;

        [Header("Layout (unidades de mundo)")]
        [SerializeField, Min(0.5f)] private float _spacing = 3f;
        [SerializeField, Min(0.05f)] private float _nodeDiameter = 0.5f;
        [SerializeField, Min(0.05f)] private float _pieceDiameter = 1.1f;
        [SerializeField, Min(0.01f)] private float _edgeThickness = 0.08f;
        [Tooltip("Radio de selección alrededor de cada casilla, como fracción de la separación.")]
        [SerializeField, Range(0.1f, 0.5f)] private float _pickRadius = 0.45f;

        [Header("Victoria")]
        [SerializeField, Min(0.01f)] private float _winLineThickness = 0.3f;
        [SerializeField, Min(1f)] private float _winPieceScale = 1.2f;

        [Header("Sprites (opcionales: si faltan se generan en runtime)")]
        [SerializeField] private Sprite _circleSprite;
        [SerializeField] private Sprite _lineSprite;

        [Header("Colores")]
        [SerializeField] private Color _edgeColor = new Color(0.45f, 0.49f, 0.58f);
        [SerializeField] private Color _nodeColor = new Color(0.82f, 0.85f, 0.91f);
        [SerializeField] private Color _playerOneColor = new Color(0.95f, 0.36f, 0.33f);
        [SerializeField] private Color _playerTwoColor = new Color(0.29f, 0.64f, 0.96f);

        private readonly SpriteRenderer[] _pieceAtCell = new SpriteRenderer[BoardGraph.CellCount];
        private readonly SpriteRenderer[] _piecePool = new SpriteRenderer[TrikiGame.PiecesPerPlayer * 2];
        private int _piecesInUse;
        private SpriteRenderer _winLine;

        private Sprite _ownedCircle;
        private Sprite _ownedLine;
        private bool _built;

        public void Build(BoardGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            if (_built)
                throw new InvalidOperationException($"{nameof(BoardView)} ya está construido.");
            _built = true;

            if (_circleSprite == null)
                _ownedCircle = SpriteFactory.CreateCircle(CircleResolution);
            if (_lineSprite == null)
                _ownedLine = SpriteFactory.CreateSquare();

            var circle = _circleSprite != null ? _circleSprite : _ownedCircle;
            var line = _lineSprite != null ? _lineSprite : _ownedLine;

            var edgesRoot = CreateGroup("Edges");
            for (var i = 0; i < graph.EdgeCount; i++)
            {
                graph.GetEdge(i, out var a, out var b);
                var from = GetCellLocalPosition(a);
                var to = GetCellLocalPosition(b);
                var delta = to - from;

                var edge = CreateRenderer("Edge " + a + "-" + b, edgesRoot, line, _edgeColor, EdgeSortingOrder);
                var t = edge.transform;
                t.localPosition = (from + to) * 0.5f;
                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                t.localScale = new Vector3(delta.magnitude, _edgeThickness, 1f);
            }

            var nodesRoot = CreateGroup("Nodes");
            for (var cell = 0; cell < BoardGraph.CellCount; cell++)
            {
                var node = CreateRenderer("Node " + cell, nodesRoot, circle, _nodeColor, NodeSortingOrder);
                node.transform.localPosition = GetCellLocalPosition(cell);
                node.transform.localScale = new Vector3(_nodeDiameter, _nodeDiameter, 1f);
            }

            _winLine = CreateRenderer("Win Line", transform, line, Color.white, WinLineSortingOrder);
            _winLine.enabled = false;

            var piecesRoot = CreateGroup("Pieces");
            for (var i = 0; i < _piecePool.Length; i++)
            {
                var piece = CreateRenderer("Piece " + i, piecesRoot, circle, Color.white, PieceSortingOrder);
                piece.transform.localScale = new Vector3(_pieceDiameter, _pieceDiameter, 1f);
                piece.enabled = false;
                _piecePool[i] = piece;
            }
        }

        public void ShowPiece(int cell, Player player)
        {
            if (_pieceAtCell[cell] != null)
                throw new InvalidOperationException($"La casilla {cell} ya muestra una ficha.");
            if (_piecesInUse >= _piecePool.Length)
                throw new InvalidOperationException("No quedan fichas en el pool.");

            var piece = _piecePool[_piecesInUse++];
            piece.transform.localPosition = GetCellLocalPosition(cell);
            piece.color = GetPlayerColor(player);
            piece.enabled = true;
            _pieceAtCell[cell] = piece;
        }

        /// <summary>Resalta la línea ganadora de extremo a extremo y agranda sus fichas.</summary>
        public void ShowWin(BoardLine line, Player winner)
        {
            var from = GetCellLocalPosition(line.A);
            var to = GetCellLocalPosition(line.C);
            var delta = to - from;

            var t = _winLine.transform;
            t.localPosition = (from + to) * 0.5f;
            t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(delta.magnitude, _winLineThickness, 1f);
            _winLine.color = GetPlayerColor(winner);
            _winLine.enabled = true;

            var winScale = _pieceDiameter * _winPieceScale;
            SetPieceScale(line.A, winScale);
            SetPieceScale(line.B, winScale);
            SetPieceScale(line.C, winScale);
        }

        /// <summary>Oculta fichas y resaltado; deja el tablero listo para otra partida.</summary>
        public void ClearPieces()
        {
            var baseScale = new Vector3(_pieceDiameter, _pieceDiameter, 1f);
            for (var i = 0; i < _piecePool.Length; i++)
            {
                var piece = _piecePool[i];
                if (piece == null)
                    continue;
                piece.enabled = false;
                piece.transform.localScale = baseScale;
            }

            if (_winLine != null)
                _winLine.enabled = false;

            Array.Clear(_pieceAtCell, 0, _pieceAtCell.Length);
            _piecesInUse = 0;
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

        public Vector3 GetCellLocalPosition(int cell)
        {
            var half = (BoardGraph.Size - 1) * 0.5f;
            return new Vector3(
                (BoardGraph.GetColumn(cell) - half) * _spacing,
                (half - BoardGraph.GetRow(cell)) * _spacing,
                0f);
        }

        private void OnDestroy()
        {
            SpriteFactory.Release(_ownedCircle);
            SpriteFactory.Release(_ownedLine);
        }

        private Color GetPlayerColor(Player player) => player == Player.One ? _playerOneColor : _playerTwoColor;

        private void SetPieceScale(int cell, float diameter)
        {
            var piece = _pieceAtCell[cell];
            if (piece != null)
                piece.transform.localScale = new Vector3(diameter, diameter, 1f);
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
    }
}
