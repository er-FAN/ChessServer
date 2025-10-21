namespace ChessServer.Logic
{
    public class ChessGame
    {
        

        // وضعیت صفحه (هر خانه یا خالی است یا یک مهره دارد)
        private readonly Piece?[] board = new Piece?[64];

        // نوبت فعلی
        public PieceColor Turn { get; private set; } = PieceColor.White;

        public ChessGame()
        {
            SetupInitialPosition();
        }

        /// <summary>
        /// تنظیم وضعیت اولیه صفحه
        /// </summary>
        private void SetupInitialPosition()
        {
            // مهره‌های سفید
            board[BitBoardHelper.ToIndex(0, 0)] = new Piece(PieceType.Rook, PieceColor.White);
            board[BitBoardHelper.ToIndex(0, 1)] = new Piece(PieceType.Knight, PieceColor.White);
            board[BitBoardHelper.ToIndex(0, 2)] = new Piece(PieceType.Bishop, PieceColor.White);
            board[BitBoardHelper.ToIndex(0, 3)] = new Piece(PieceType.Queen, PieceColor.White);
            board[BitBoardHelper.ToIndex(0, 4)] = new Piece(PieceType.King, PieceColor.White);
            board[BitBoardHelper.ToIndex(0, 5)] = new Piece(PieceType.Bishop, PieceColor.White);
            board[BitBoardHelper.ToIndex(0, 6)] = new Piece(PieceType.Knight, PieceColor.White);
            board[BitBoardHelper.ToIndex(0, 7)] = new Piece(PieceType.Rook, PieceColor.White);

            for (int file = 0; file < 8; file++)
                board[BitBoardHelper.ToIndex(1, file)] = new Piece(PieceType.Pawn, PieceColor.White);

            // مهره‌های سیاه
            board[BitBoardHelper.ToIndex(7, 0)] = new Piece(PieceType.Rook, PieceColor.Black);
            board[BitBoardHelper.ToIndex(7, 1)] = new Piece(PieceType.Knight, PieceColor.Black);
            board[BitBoardHelper.ToIndex(7, 2)] = new Piece(PieceType.Bishop, PieceColor.Black);
            board[BitBoardHelper.ToIndex(7, 3)] = new Piece(PieceType.Queen, PieceColor.Black);
            board[BitBoardHelper.ToIndex(7, 4)] = new Piece(PieceType.King, PieceColor.Black);
            board[BitBoardHelper.ToIndex(7, 5)] = new Piece(PieceType.Bishop, PieceColor.Black);
            board[BitBoardHelper.ToIndex(7, 6)] = new Piece(PieceType.Knight, PieceColor.Black);
            board[BitBoardHelper.ToIndex(7, 7)] = new Piece(PieceType.Rook, PieceColor.Black);

            for (int file = 0; file < 8; file++)
                board[BitBoardHelper.ToIndex(6, file)] = new Piece(PieceType.Pawn, PieceColor.Black);
        }

        public Piece? GetPieceAt(int square) => board[square];

        /// <summary>
        /// جابجایی مهره از خانه‌ای به خانه دیگر
        /// </summary>
        public void MovePiece(int from, int to)
        {
            var piece = board[from];
            if (piece == null) return;

            // انجام حرکت
            board[to] = piece;
            board[from] = null;

            // تغییر نوبت
            Turn = (Turn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        }

        private List<int> GetSimpleMoves(int from, ulong moveMask, PieceColor color)
        {
            var result = new List<int>();
            for (int i = 0; i < 64; i++)
            {
                if ((moveMask & (1UL << i)) == 0) continue;
                var target = board[i];

                // فقط اگر خانه خالی باشد یا دشمن باشد
                if (target == null || target.Color != color)
                    result.Add(i);
            }
            return result;
        }

        private List<int> GetSlidingMoves(int from, ulong moveMask, PieceColor color, (int dr, int df)[] directions)
        {
            List<int> result = new();
            int rank = from / 8;
            int file = from % 8;

            foreach (var (dr, df) in directions)
            {
                int r = rank + dr;
                int f = file + df;

                while (r >= 0 && r < 8 && f >= 0 && f < 8)
                {
                    int index = BitBoardHelper.ToIndex(r, f);
                    var target = board[index];

                    if (target == null)
                    {
                        result.Add(index);
                    }
                    else
                    {
                        // اگر دشمن است فقط همین خانه را می‌توان زد
                        if (target.Color != color)
                            result.Add(index);

                        // مسیر مسدود می‌شود
                        break;
                    }

                    r += dr;
                    f += df;
                }
            }

            return result;
        }

        private List<int> GetPawnMoves(int from, PieceColor color)
        {
            List<int> result = new();
            int rank = from / 8;
            int file = from % 8;

            int dir = (color == PieceColor.White) ? 1 : -1;

            // حرکت مستقیم (فقط اگر خالی باشد)
            int oneStep = rank + dir;
            if (oneStep >= 0 && oneStep < 8)
            {
                int forward = BitBoardHelper.ToIndex(oneStep, file);
                if (board[forward] == null)
                {
                    result.Add(forward);

                    // حرکت دوخانه از خانه‌ی شروع
                    if ((color == PieceColor.White && rank == 1) || (color == PieceColor.Black && rank == 6))
                    {
                        int twoStep = BitBoardHelper.ToIndex(rank + dir * 2, file);
                        if (board[twoStep] == null)
                            result.Add(twoStep);
                    }
                }
            }

            // حمله‌ی مورب
            foreach (int df in new[] { -1, 1 })
            {
                int attackFile = file + df;
                int attackRank = rank + dir;

                if (attackFile >= 0 && attackFile < 8 && attackRank >= 0 && attackRank < 8)
                {
                    int attackIndex = BitBoardHelper.ToIndex(attackRank, attackFile);
                    var target = board[attackIndex];
                    if (target != null && target.Color != color)
                        result.Add(attackIndex);
                }
            }

            return result;
        }


        /// <summary>
        /// تولید حرکت‌های ممکن برای یک مهره در یک خانه
        /// </summary>
        public List<int> GetAvailableMoves(int square)
        {
            var piece = board[square];
            if (piece == null) return new();

            var moves = new List<int>();

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    moves.AddRange(GetPawnMoves(square, piece.Color));
                    break;
                case PieceType.Knight:
                    moves.AddRange(GetSimpleMoves(square, KnightLookup.Moves[square], piece.Color));
                    break;
                case PieceType.Bishop:
                    moves.AddRange(GetSlidingMoves(square, BishopLookup.Moves[square], piece.Color, new (int, int)[] { (1, 1), (1, -1), (-1, 1), (-1, -1) }));
                    break;
                case PieceType.Rook:
                    moves.AddRange(GetSlidingMoves(square, RookLookup.Moves[square], piece.Color, new (int, int)[] { (1, 0), (-1, 0), (0, 1), (0, -1) }));
                    break;
                case PieceType.Queen:
                    moves.AddRange(GetSlidingMoves(square, QueenLookup.Moves[square], piece.Color, new (int, int)[] {
                (1, 0), (-1, 0), (0, 1), (0, -1), (1,1), (1,-1), (-1,1), (-1,-1)
            }));
                    break;
                case PieceType.King:
                    moves.AddRange(GetSimpleMoves(square, KingLookup.Moves[square], piece.Color));
                    break;
            }

            // ---------- فیلتر کیش ----------
            var legalMoves = new List<int>();

            foreach (var move in moves)
            {
                // شبیه‌سازی حرکت
                var captured = board[move];
                board[move] = piece;
                board[square] = null;

                if (!IsKingInCheck(piece.Color))
                {
                    legalMoves.Add(move);
                }

                // بازگرداندن صفحه
                board[square] = piece;
                board[move] = captured;
            }

            return legalMoves;
        }



        private bool IsKingInCheck(PieceColor color)
        {
            int kingSquare = -1;

            // پیدا کردن خانه شاه
            for (int i = 0; i < 64; i++)
            {
                var p = board[i];
                if (p != null && p.Type == PieceType.King && p.Color == color)
                {
                    kingSquare = i;
                    break;
                }
            }

            if (kingSquare == -1) return true; // شاه پیدا نشد → خطای جدی

            // بررسی تهدید از همه مهره‌های دشمن
            for (int i = 0; i < 64; i++)
            {
                var p = board[i];
                if (p != null && p.Color != color)
                {
                    var moves = new List<int>();
                    switch (p.Type)
                    {
                        case PieceType.Pawn:
                            moves.AddRange(GetPawnMoves(i, p.Color));
                            break;
                        case PieceType.Knight:
                            moves.AddRange(GetSimpleMoves(i, KnightLookup.Moves[i], p.Color));
                            break;
                        case PieceType.Bishop:
                            moves.AddRange(GetSlidingMoves(i, BishopLookup.Moves[i], p.Color, new (int, int)[] { (1, 1), (1, -1), (-1, 1), (-1, -1) }));
                            break;
                        case PieceType.Rook:
                            moves.AddRange(GetSlidingMoves(i, RookLookup.Moves[i], p.Color, new (int, int)[] { (1, 0), (-1, 0), (0, 1), (0, -1) }));
                            break;
                        case PieceType.Queen:
                            moves.AddRange(GetSlidingMoves(i, QueenLookup.Moves[i], p.Color, new (int, int)[]{
                        (1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)
                    }));
                            break;
                        case PieceType.King:
                            moves.AddRange(GetSimpleMoves(i, KingLookup.Moves[i], p.Color));
                            break;
                    }

                    if (moves.Contains(kingSquare))
                        return true; // شاه تحت تهدید است
                }
            }

            return false;
        }




        //private static List<int> ExtractSquares(ulong mask)
        //{
        //    List<int> result = new();
        //    for (int i = 0; i < 64; i++)
        //        if ((mask & (1UL << i)) != 0)
        //            result.Add(i);
        //    return result;
        //}
    }



}
