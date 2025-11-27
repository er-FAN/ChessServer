using ChessServer.Logic;

namespace ChessServer.DTOs
{
    public class GameStatus
    {
        // ---------------------------------
        // اطلاعات صفحه
        // ---------------------------------
        public string[,] Board { get; set; } = new string[8, 8]; // یا هر ساختار دیگری که مهره‌ها را نشان دهد
        public PieceColor Turn { get; set; } // نوبت بازی

        // ---------------------------------
        // تاریخچه حرکت‌ها
        // ---------------------------------
        public List<(int from, int to, Piece moved)> MoveHistory { get; set; } = new();
        public List<string> FenHistory { get; set; } = new();

        // ---------------------------------
        // فلگ‌ها و وضعیت ویژه
        // ---------------------------------
        public bool IsCheck { get; set; } = false;
        public bool IsCheckmate { get; set; } = false;
        public bool IsStalemate { get; set; } = false;
        public bool IsFiftyMoveRuleTriggered { get; set; } = false;
        public bool IsThreefoldRepetitionTriggered { get; set; } = false;

        // en-passant
        public int? EnPassantSquare { get; set; } = null;

        // قلعه
        public bool CanWhiteCastleKingside { get; set; } = false;
        public bool CanWhiteCastleQueenside { get; set; } = false;
        public bool CanBlackCastleKingside { get; set; } = false;
        public bool CanBlackCastleQueenside { get; set; } = false;

        // پروموشن
        public int? PawnPromotionSquare { get; set; } = null;
        public PieceColor? PawnPromotionColor { get; set; } = null;

        // هر فلگ یا وضعیت دیگر که لازم دارید
        public bool IsCastlingInProgress { get; set; } = false;
        public bool IsKingsideCastle { get; set; } = false;
        public bool IsQueensideCastle { get; set; } = false;
    }

}