using ChessServer.Logic;

namespace ChessServer.Helpers
{
    public class CastlingHelper
    {
        public PieceColor? castleMoveColor;

        public bool isCastlingInProgress = false;

        public bool canKingsideCastle = false;
        public bool canQueensideCastle = false;

        // برای اینکه بدانیم کدام قلعه در حال انجام است (کوچک یا بزرگ)
        public bool isKingsideCastle = false;
        public bool isQueensideCastle = false;
        public int SelectedDestinationForRookWhenIsCastlingInProgress { get; set; }

        public void CheckMoveIsCastleMove(int to, Piece piece)
        {
            if (piece.Color == castleMoveColor && (canQueensideCastle || canKingsideCastle) && piece.Type == PieceType.King)
            {
                ResetInprogressCastlingFlags();
                if (to == 6 || to == 62) // g1 یا g8
                {
                    isKingsideCastle = true;
                }
                else if (to == 2 || to == 58) // c1 یا c8
                {
                    isQueensideCastle = true;
                }
                isCastlingInProgress = isKingsideCastle || isQueensideCastle;
            }

        }

        public void ResetInprogressCastlingFlags()
        {
            isKingsideCastle = false;
            isQueensideCastle = false;
            isCastlingInProgress = false;
        }

        public bool CanCastleKingside(Piece?[] board, PieceColor color)
        {
            int rank = (color == PieceColor.White) ? 0 : 7;
            int kingSquare = BitBoardHelper.ToIndex(rank, 4);
            int rookSquare = BitBoardHelper.ToIndex(rank, 7);

            var king = board[kingSquare];
            var rook = board[rookSquare];
            if (king == null || rook == null) return false;
            if (king.HasMoved || rook.HasMoved) return false;

            // بین‌شان نباید مهره‌ای باشد
            if (board[BitBoardHelper.ToIndex(rank, 5)] != null) return false;
            if (board[BitBoardHelper.ToIndex(rank, 6)] != null) return false;

            // شاه نباید در مسیر یا خانه مقصد کیش باشد
            if (IsSquareAttacked(board, kingSquare, color) ||
                IsSquareAttacked(board, BitBoardHelper.ToIndex(rank, 5), color) ||
                IsSquareAttacked(board, BitBoardHelper.ToIndex(rank, 6), color))
                return false;

            return true;
        }

        private bool CanCastleQueenside(Piece?[] board, PieceColor color)
        {
            int rank = (color == PieceColor.White) ? 0 : 7;
            int kingSquare = BitBoardHelper.ToIndex(rank, 4);
            int rookSquare = BitBoardHelper.ToIndex(rank, 0);

            var king = board[kingSquare];
            var rook = board[rookSquare];
            if (king == null || rook == null) return false;
            if (king.HasMoved || rook.HasMoved) return false;

            // بین‌شان نباید مهره‌ای باشد
            if (board[BitBoardHelper.ToIndex(rank, 1)] != null) return false;
            if (board[BitBoardHelper.ToIndex(rank, 2)] != null) return false;
            if (board[BitBoardHelper.ToIndex(rank, 3)] != null) return false;

            // شاه نباید در مسیر یا خانه مقصد کیش باشد
            if (IsSquareAttacked(board, kingSquare, color) ||
                IsSquareAttacked(board, BitBoardHelper.ToIndex(rank, 3), color) ||
                IsSquareAttacked(board, BitBoardHelper.ToIndex(rank, 2), color))
                return false;

            return true;
        }

        private bool IsSquareAttacked(Piece?[] board, int targetSquare, PieceColor defenderColor)
        {
            var attackerColor = (defenderColor == PieceColor.White) ? PieceColor.Black : PieceColor.White;

            int targetRank = targetSquare / 8;
            int targetFile = targetSquare % 8;

            // 🔹 بررسی جهت‌های حمله — مثل رخ، فیل، وزیر
            (int dr, int df)[] directions =
            {
        (1,0), (-1,0), (0,1), (0,-1), // رخ
        (1,1), (1,-1), (-1,1), (-1,-1) // فیل
    };

            // بررسی همه‌ی خانه‌ها برای مهره‌های حریف
            for (int i = 0; i < 64; i++)
            {
                var piece = board[i];
                if (piece == null || piece.Color != attackerColor)
                    continue;

                int rank = i / 8;
                int file = i % 8;

                switch (piece.Type)
                {
                    // ♜ رخ — حرکت‌های عمودی و افقی
                    case PieceType.Rook:
                        foreach (var (dr, df) in directions.Take(4))
                            if (CanSlideAttack(board, rank, file, dr, df, targetRank, targetFile))
                                return true;
                        break;

                    // ♝ فیل — حرکت‌های مورب
                    case PieceType.Bishop:
                        foreach (var (dr, df) in directions.Skip(4))
                            if (CanSlideAttack(board, rank, file, dr, df, targetRank, targetFile))
                                return true;
                        break;

                    // ♛ وزیر — ترکیب فیل و رخ
                    case PieceType.Queen:
                        foreach (var (dr, df) in directions)
                            if (CanSlideAttack(board, rank, file, dr, df, targetRank, targetFile))
                                return true;
                        break;

                    // ♞ اسب — الگوی حرکت خاص
                    case PieceType.Knight:
                        int[,] knightMoves = { { 2, 1 }, { 2, -1 }, { -2, 1 }, { -2, -1 }, { 1, 2 }, { 1, -2 }, { -1, 2 }, { -1, -2 } };
                        for (int j = 0; j < knightMoves.GetLength(0); j++)
                        {
                            int nr = rank + knightMoves[j, 0];
                            int nf = file + knightMoves[j, 1];
                            if (nr == targetRank && nf == targetFile)
                                return true;
                        }
                        break;

                    // ♙ پیاده — فقط خانه‌های مورب جلو را تهدید می‌کند
                    case PieceType.Pawn:
                        int dir = (attackerColor == PieceColor.White) ? 1 : -1; // سفید به بالا (rank+1)، سیاه به پایین (rank-1)
                        if (rank + dir == targetRank && Math.Abs(file - targetFile) == 1)
                            return true;
                        break;

                    // ♚ شاه — فقط خانه‌های اطرافش را تهدید می‌کند
                    case PieceType.King:
                        if (Math.Abs(rank - targetRank) <= 1 && Math.Abs(file - targetFile) <= 1)
                            return true;
                        break;
                }
            }

            return false; // هیچ مهره‌ای تهدید نمی‌کند
        }

        private bool CanSlideAttack(Piece?[] board, int r, int f, int dr, int df, int targetRank, int targetFile)
        {
            r += dr;
            f += df;

            while (r >= 0 && r < 8 && f >= 0 && f < 8)
            {
                int idx = BitBoardHelper.ToIndex(r, f);

                // اگر رسیدیم به خانه‌ی هدف → حمله ممکن است
                if (r == targetRank && f == targetFile)
                    return true;

                // اگر مهره‌ای سر راه است → نمی‌شود ادامه داد
                if (board[idx] != null)
                    return false;

                r += dr;
                f += df;
            }

            return false;
        }

        private void ResetCastlingFlagsWhenCompleted()
        {
            if (isCastlingInProgress)
            {
                castleMoveColor = null;

                isCastlingInProgress = false;

                canKingsideCastle = false;
                canQueensideCastle = false;

                isKingsideCastle = false;
                isQueensideCastle = false;
                SelectedDestinationForRookWhenIsCastlingInProgress = 65;
            }
        }

        public void CheckCastlingDone(int to, Piece piece)
        {
            if (piece.Type == PieceType.Rook && piece.Color == castleMoveColor && to == SelectedDestinationForRookWhenIsCastlingInProgress)
            {
                ResetCastlingFlagsWhenCompleted();
            }
        }

        public bool IsCastlingInProgressAndCorrectRookPieceSelected(int square, Piece piece)
        {
            bool resualt = false;
            if (isCastlingInProgress && piece.Type == PieceType.Rook)
            {
                if (isQueensideCastle && square == (castleMoveColor == PieceColor.White ? 0 : 3))
                {
                    resualt = true;
                }
                else if (isKingsideCastle && square == (castleMoveColor == PieceColor.White ? 0 : 3))
                {
                    resualt = true;
                }
                else
                {
                    resualt = false;
                }
            }

            return resualt;
        }

        public static int GetCastleRookDestinationWhenIsCastlingInProgress(PieceColor color, bool isKingsideCastle, bool isQueensideCastle)
        {
            int result = 65;
            if (isKingsideCastle)
            {
                // قلعه کوچک
                result = (color == PieceColor.White) ? 5 : 61; // f1 یا f8
            }

            if (isQueensideCastle)
            {
                // قلعه بزرگ
                result = (color == PieceColor.White) ? 3 : 59; // d1 یا d8
            }

            return result;
        }

        public List<int> GetCastleMoveIfCan(Piece?[] board, Piece king)
        {
            var moves = new List<int>();
            ResetBeforeCaslteMoveControlFlag();
            if (CanCastleKingside(board,king.Color))
            {

                // مقصد شاه در قلعه کوچک
                int to = (king.Color == PieceColor.White) ? 6 : 62;
                moves.Add(to);
                castleMoveColor = king.Color;
                canKingsideCastle = true;
            }

            if (CanCastleQueenside(board, king.Color))
            {
                // مقصد شاه در قلعه بزرگ
                int to = (king.Color == PieceColor.White) ? 2 : 58;
                moves.Add(to);
                castleMoveColor = king.Color;
                canQueensideCastle = true;
            }

            return moves;
        }

        private void ResetBeforeCaslteMoveControlFlag()
        {
            castleMoveColor = null;
            canKingsideCastle = false;
            canQueensideCastle = false;
        }
    }
}
