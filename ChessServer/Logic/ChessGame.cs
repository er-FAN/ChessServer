using System.Collections;

namespace ChessServer.Logic
{
    public class ChessGame
    {
        private (int from, int to, Piece moved)? lastMove;
        private List<(int from, int to, Piece moved)?> Moves;

        public int? pendingPromotionSquare;

        // وضعیت صفحه (هر خانه یا خالی است یا یک مهره دارد)
        private readonly Piece?[] board = new Piece?[64];

        private int? enPassantSquare = null;


        private PieceColor? castleMoveColor;

        private bool isCastlingInProgress = false;

        private bool canKingsideCastle = false;
        private bool canQueensideCastle = false;

        // برای اینکه بدانیم کدام قلعه در حال انجام است (کوچک یا بزرگ)
        private bool isKingsideCastle = false;
        private bool isQueensideCastle = false;
        public int SelectedDestinationForRookWhenIsCastlingInProgress { get; private set; }

        public GameState GameState { get; private set; }

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
            InitialWhitePiecesPosition();

            // مهره‌های سیاه
            InitialBlackPiecesPosition();
        }

        private void InitialBlackPiecesPosition()
        {
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

        private void InitialWhitePiecesPosition()
        {
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
        }

        public Piece? GetPieceAt(int square) => board[square];

        /// <summary>
        /// جابجایی مهره از خانه‌ای به خانه دیگر
        /// </summary>
        public void MovePiece(int from, int to)
        {
            Piece? piece = GetPieceAt(from);
            if (piece != null)
            {

                ExecuteMove(from, to, piece);

                AfterMoveChecks(from, to, piece);

                UpdateHistory(from, to, piece);

                if (!isCastlingInProgress)
                {
                    ChangeTurn();
                }

            }
        }

        private void CheckMoveIsCastleMove(int to, Piece piece)
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

        private void ResetInprogressCastlingFlags()
        {
            isKingsideCastle = false;
            isQueensideCastle = false;
            isCastlingInProgress = false;
        }

        private bool CanCastleKingside(PieceColor color)
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
            if (IsSquareAttacked(kingSquare, color) ||
                IsSquareAttacked(BitBoardHelper.ToIndex(rank, 5), color) ||
                IsSquareAttacked(BitBoardHelper.ToIndex(rank, 6), color))
                return false;

            return true;
        }

        private bool CanCastleQueenside(PieceColor color)
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
            if (IsSquareAttacked(kingSquare, color) ||
                IsSquareAttacked(BitBoardHelper.ToIndex(rank, 3), color) ||
                IsSquareAttacked(BitBoardHelper.ToIndex(rank, 2), color))
                return false;

            return true;
        }

        private bool IsSquareAttacked(int targetSquare, PieceColor defenderColor)
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
                            if (CanSlideAttack(rank, file, dr, df, targetRank, targetFile))
                                return true;
                        break;

                    // ♝ فیل — حرکت‌های مورب
                    case PieceType.Bishop:
                        foreach (var (dr, df) in directions.Skip(4))
                            if (CanSlideAttack(rank, file, dr, df, targetRank, targetFile))
                                return true;
                        break;

                    // ♛ وزیر — ترکیب فیل و رخ
                    case PieceType.Queen:
                        foreach (var (dr, df) in directions)
                            if (CanSlideAttack(rank, file, dr, df, targetRank, targetFile))
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

        private bool CanSlideAttack(int r, int f, int dr, int df, int targetRank, int targetFile)
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



        private void UpdateHistory(int from, int to, Piece piece)
        {
            SaveLastMove(from, to, piece);

            AddLastMoveToHistory();
        }

        private void AfterMoveChecks(int from, int to, Piece piece)
        {
            SetEnPassantSquareForNextMoveIfExist(from, to, piece);

            CheckPromotion(to, piece);

            CheckMoveIsCastleMove(to, piece);

            UpdateGameState();
        }

        private void UpdateGameState()
        {
            if (IsCheckmate(Turn))
            {
                GameState = GameState.Checkmate;
            }
            else if (IsStalemate(Turn))
            {
                GameState = GameState.Stalemate;
            }
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

        private void ExecuteEnPassant(int to, Piece piece)
        {
            int capturedPawnSquare = (piece.Color == PieceColor.White) ? to - 8 : to + 8;
            board[capturedPawnSquare] = null;
        }

        private bool MoveIsEnPassant(int to, Piece piece)
        {
            return piece.Type == PieceType.Pawn && enPassantSquare.HasValue && to == enPassantSquare.Value;
        }

        private void SetEnPassantSquareForNextMoveIfExist(int from, int to, Piece piece)
        {
            if (piece.Type == PieceType.Pawn && Math.Abs(to - from) == 16)
                enPassantSquare = (from + to) / 2;
            else
                enPassantSquare = null;
        }

        private void CheckPromotion(int to, Piece piece)
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

        private void SaveLastMove(int from, int to, Piece piece)
        {
            lastMove = (from, to, piece);
        }


        private void ChangeTurn()
        {
            Turn = (Turn == PieceColor.White) ? PieceColor.Black : PieceColor.White;

            
        }

        private void ExecuteMove(int from, int to, Piece piece)
        {
            board[to] = piece;
            board[from] = null;

            piece.HasMoved = true;

            if (isCastlingInProgress)
            {
                CheckCastlingDone(to, piece);
            }

            if (MoveIsEnPassant(to, piece))
            {
                ExecuteEnPassant(to, piece);
            }
        }

        private void CheckCastlingDone(int to, Piece piece)
        {
            if (piece.Type == PieceType.Rook && piece.Color == castleMoveColor && to == SelectedDestinationForRookWhenIsCastlingInProgress)
            {
                ResetCastlingFlagsWhenCompleted();
            }
        }

        private void AddLastMoveToHistory()
        {
            if (lastMove != null)
            {
                Moves.Add(lastMove);
            }
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
            var piece = GetPieceAt(square);
            if (piece == null) return new();

            var moves = new List<int>();
            if (IsCastlingInProgressAndCorrectRookPieceSelected(square, piece))
            {
                SelectedDestinationForRookWhenIsCastlingInProgress = GetCastleRookDestinationWhenIsCastlingInProgress(piece.Color, isKingsideCastle, isQueensideCastle);
                moves.Add(SelectedDestinationForRookWhenIsCastlingInProgress);
            }
            else
            {
                switch (piece.Type)
                {
                    case PieceType.Pawn:
                        moves.AddRange(GetPawnMoves(square, piece.Color));
                        CheckEnPassant(square, piece, moves);
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
                        moves.AddRange(GetCastleMoveIfCan(piece));
                        break;
                }
            }


            // ---------- فیلتر کیش ----------
            moves = CeckKish(square, piece, moves);

            return moves;
        }

        private bool IsCheckmate(PieceColor color)
        {
            // ۱. اگر شاه در کیش نیست، قطعاً مات نیست
            if (!IsKingInCheck(color))
                return false;

            // ۲. حالا باید بررسی کنیم که آیا هیچ حرکت قانونی برای خروج از کیش وجود دارد یا نه
            for (int square = 0; square < 64; square++)
            {
                var piece = board[square];
                if (piece == null || piece.Color != color)
                    continue;

                // همه‌ی حرکت‌های مجاز این مهره را بگیر
                var moves = GetAvailableMoves(square);

                // اگر حتی یک حرکت مجاز وجود داشته باشد که شاه را از کیش خارج کند
                if (moves.Count > 0)
                    return false;
            }

            // ۳. اگر هیچ حرکتی شاه را نجات نمی‌دهد → مات است
            return true;
        }


        private bool IsStalemate(PieceColor color)
        {
            // اگر شاه در کیش است، پات نیست
            if (IsKingInCheck(color))
                return false;

            // اگر هر مهره‌ای حرکت قانونی دارد، پات نیست
            for (int square = 0; square < 64; square++)
            {
                var piece = GetPieceAt(square);
                if (piece == null || piece.Color != color)
                    continue;

                var moves = GetAvailableMoves(square);
                if (moves.Count > 0)
                    return false;
            }

            // هیچ حرکت قانونی و هیچ کیشی وجود ندارد → پات است
            return true;
        }



        private bool IsCastlingInProgressAndCorrectRookPieceSelected(int square, Piece piece)
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

        private static int GetCastleRookDestinationWhenIsCastlingInProgress(PieceColor color, bool isKingsideCastle, bool isQueensideCastle)
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

        private List<int> GetCastleMoveIfCan(Piece king)
        {
            var moves = new List<int>();
            ResetBeforeCaslteMoveControlFlag();
            if (CanCastleKingside(king.Color))
            {

                // مقصد شاه در قلعه کوچک
                int to = (king.Color == PieceColor.White) ? 6 : 62;
                moves.Add(to);
                castleMoveColor = king.Color;
                canKingsideCastle = true;
            }

            if (CanCastleQueenside(king.Color))
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

        private List<int> CeckKish(int square, Piece piece, List<int> moves)
        {
            List<int> legalMoves = new List<int>();
            foreach (var move in moves)
            {
                // شبیه‌سازی حرکت
                var captured = board[move];
                board[move] = piece;
                board[square] = null;

                // نکته: اگر این حرکت، یک en-passant گرفتن باشد، باید پیادهٔ گرفته‌شده را هم موقتا پاک کنیم
                bool removedEnPassantPawn = false;
                Piece? removedPawn = null;
                if (piece.Type == PieceType.Pawn && enPassantSquare.HasValue && move == enPassantSquare.Value)
                {
                    int capturedPawnSquare = (piece.Color == PieceColor.White) ? move - 8 : move + 8;
                    removedPawn = board[capturedPawnSquare];
                    if (removedPawn != null && removedPawn.Type == PieceType.Pawn && removedPawn.Color != piece.Color)
                    {
                        board[capturedPawnSquare] = null;
                        removedEnPassantPawn = true;
                    }
                }

                if (!IsKingInCheck(piece.Color))
                {
                    legalMoves.Add(move);
                }

                // بازگرداندن صفحه (undo شبیه‌سازی)
                if (removedEnPassantPawn)
                {
                    int capturedPawnSquare = (piece.Color == PieceColor.White) ? move - 8 : move + 8;
                    board[capturedPawnSquare] = removedPawn;
                }

                board[square] = piece;
                board[move] = captured;
            }

            return legalMoves;
        }

        private void CheckEnPassant(int square, Piece piece, List<int> moves)
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

        internal void PromotePawn(int promotionSquare, PieceType newType)
        {
            if (board[promotionSquare] != null)
            {
                board[promotionSquare] = new Piece(newType, board[promotionSquare].Color);
            }

        }
    }


}
