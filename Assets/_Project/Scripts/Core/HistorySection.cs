namespace Triki.Core
{
    /// <summary>Registros del histórico que se muestran y se pueden borrar por separado.</summary>
    public enum HistorySection : byte
    {
        /// <summary>Todas las partidas, sin importar el modo.</summary>
        Overall,

        VsAi,

        TwoPlayer,
    }
}
