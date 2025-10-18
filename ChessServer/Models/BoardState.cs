namespace ChessServer.Models
{
    public class BoardState
    {
        public Dictionary<string, Piece> Squares { get; set; } = new();
        public string? SelectedSquare { get; set; }

        public BoardState()
        {
            SeedInitialPosition();
        }

        private void SeedInitialPosition()
        {
            // سفیدها
            Squares["A1"] = new Piece { Type = "Rook", Color = "White" };
            Squares["B1"] = new Piece { Type = "Knight", Color = "White" };
            Squares["C1"] = new Piece { Type = "Bishop", Color = "White" };
            Squares["D1"] = new Piece { Type = "Queen", Color = "White" };
            Squares["E1"] = new Piece { Type = "King", Color = "White" };
            Squares["F1"] = new Piece { Type = "Bishop", Color = "White" };
            Squares["G1"] = new Piece { Type = "Knight", Color = "White" };
            Squares["H1"] = new Piece { Type = "Rook", Color = "White" };

            for (char c = 'A'; c <= 'H'; c++)
            {
                string square = $"{c}2";
                Squares[square] = new Piece { Type = "Pawn", Color = "White" };
            }

            // سیاه‌ها
            Squares["A8"] = new Piece { Type = "Rook", Color = "Black" };
            Squares["B8"] = new Piece { Type = "Knight", Color = "Black" };
            Squares["C8"] = new Piece { Type = "Bishop", Color = "Black" };
            Squares["D8"] = new Piece { Type = "Queen", Color = "Black" };
            Squares["E8"] = new Piece { Type = "King", Color = "Black" };
            Squares["F8"] = new Piece { Type = "Bishop", Color = "Black" };
            Squares["G8"] = new Piece { Type = "Knight", Color = "Black" };
            Squares["H8"] = new Piece { Type = "Rook", Color = "Black" };

            for (char c = 'A'; c <= 'H'; c++)
            {
                string square = $"{c}7";
                Squares[square] = new Piece { Type = "Pawn", Color = "Black" };
            }
        }
    }
}
