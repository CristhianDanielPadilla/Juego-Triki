using System;

namespace Triki.Core
{
    /// <summary>Jugada elegida por la IA: colocar en una casilla o mover de una a otra.</summary>
    public readonly struct AiMove : IEquatable<AiMove>
    {
        private AiMove(bool isPlacement, int from, int to)
        {
            IsPlacement = isPlacement;
            From = from;
            To = to;
        }

        public bool IsPlacement { get; }

        /// <summary>Casilla de origen; -1 en una colocación.</summary>
        public int From { get; }

        public int To { get; }

        public static AiMove Place(int cell) => new AiMove(true, -1, cell);

        public static AiMove Move(int from, int to) => new AiMove(false, from, to);

        /// <summary>Ejecuta la jugada en la partida. Devuelve <c>false</c> si las reglas la rechazan.</summary>
        public bool ApplyTo(TrikiGame game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));
            return IsPlacement
                ? game.TryPlace(To) == PlaceResult.Placed
                : game.TryMove(From, To) == MoveResult.Moved;
        }

        public bool Equals(AiMove other) => IsPlacement == other.IsPlacement && From == other.From && To == other.To;

        public override bool Equals(object obj) => obj is AiMove other && Equals(other);

        public override int GetHashCode() => (IsPlacement ? 1000 : 0) + (From + 1) * 10 + To;

        public override string ToString() => IsPlacement ? $"colocar {To}" : $"mover {From}->{To}";

        public static bool operator ==(AiMove left, AiMove right) => left.Equals(right);

        public static bool operator !=(AiMove left, AiMove right) => !left.Equals(right);
    }
}
