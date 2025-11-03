namespace ChessServer.Logic
{
    public record Piece(PieceType Type, PieceColor Color)
    {
        public bool HasMoved { get; internal set; }
    }
}
