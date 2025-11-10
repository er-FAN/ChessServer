using ChessServer.Logic;

namespace ChessServer.Helpers
{
    public class PromotionHelper
    {
        public int? pendingPromotionSquare;

        public void CheckPromotion(int to, Piece piece)
        {
            int toRank = to / 8;
            if (piece.Type == PieceType.Pawn &&
                ((piece.Color == PieceColor.White && toRank == 7) ||
                 (piece.Color == PieceColor.Black && toRank == 0)))
            {
                //board[to] = new Piece(PieceType.Queen, piece.Color);
                pendingPromotionSquare = to;
            }
        }

        public void PromotePawn(Piece?[] board, int promotionSquare, PieceType newType)
        {
            if (board[promotionSquare] != null)
            {
                board[promotionSquare] = new Piece(newType, board[promotionSquare].Color);
            }

        }
    }
}
