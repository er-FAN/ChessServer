using ChessServer.Logic;

namespace ChessServer.Helpers
{
    public class EnPassantHelper
    {
        public int? enPassantSquare = null;

        public void CheckEnPassant(Piece?[] board, int square, Piece piece, List<int> moves)
        {
            if (enPassantSquare.HasValue)
            {
                int row = square / 8;
                int col = square % 8;
                int ep = enPassantSquare.Value;
                int epRow = ep / 8;
                int epCol = ep % 8;

                // en-passant زمانی می‌تواند باشد که پیاده دشمن دقیقا در هم‌ردیف ما و در ستون مجاور باشد
                if (row == epRow && Math.Abs(col - epCol) == 1)
                {
                    int captureSquare = (piece.Color == PieceColor.White) ? ep - 8 : ep + 8;
                    // خانه‌ای که مهاجم باید برود، باید خالی باشد (طبق قانون)، و خانهٔ گرفته شده باید پیادهٔ دشمن باشد
                    if (board[captureSquare] == null && board[ep] != null && board[ep]?.Type == PieceType.Pawn && board[ep]?.Color != piece.Color)
                    {
                        moves.Add(ep);
                    }
                }
            }
        }

        public void ExecuteEnPassant(Piece?[] board, int to, Piece piece)
        {
            int capturedPawnSquare = (piece.Color == PieceColor.White) ? to - 8 : to + 8;
            board[capturedPawnSquare] = null;
        }

        public bool MoveIsEnPassant(int to, Piece piece)
        {
            return piece.Type == PieceType.Pawn && enPassantSquare.HasValue && to == enPassantSquare.Value;
        }

        public void SetEnPassantSquareForNextMoveIfExist(int from, int to, Piece piece)
        {
            if (piece.Type == PieceType.Pawn && Math.Abs(to - from) == 16)
                enPassantSquare = (from + to) / 2;
            else
                enPassantSquare = null;
        }
    }
}
