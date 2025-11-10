using ChessServer.Helpers;
using System.Collections;

namespace ChessServer.Logic
{
    public class ChessGame
    {
        public EnPassantHelper enPassantHelper;
        public PromotionHelper promotionHelper;
        public CastlingHelper castlingHelper;

        private (int from, int to, Piece moved)? lastMove;
        private List<(int from, int to, Piece moved)?> Moves;

        // وضعیت صفحه (هر خانه یا خالی است یا یک مهره دارد)
        public readonly Piece?[] board = new Piece?[64];

        public GameState GameState { get; private set; }

        public PieceColor Turn { get; private set; } = PieceColor.White;

        public ChessGame()
        {
            SetupInitialPosition();
        }

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

        public void MovePiece(int from, int to)
        {
            Piece? piece = GetPieceAt(from);
            if (piece != null)
            {

                ExecuteMove(from, to, piece);

                AfterMoveChecks(from, to, piece);

                UpdateHistory(from, to, piece);

                if (!castlingHelper.isCastlingInProgress)
                {
                    ChangeTurn();
                }

            }
        }

        private void UpdateHistory(int from, int to, Piece piece)
        {
            SaveLastMove(from, to, piece);

            AddLastMoveToHistory();
        }

        private void AfterMoveChecks(int from, int to, Piece piece)
        {
            enPassantHelper.SetEnPassantSquareForNextMoveIfExist(from, to, piece);

            promotionHelper.CheckPromotion(to, piece);

            castlingHelper.CheckMoveIsCastleMove(to, piece);

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

            if (castlingHelper.isCastlingInProgress)
            {
                castlingHelper.CheckCastlingDone(to, piece);
            }

            if (enPassantHelper.MoveIsEnPassant(to, piece))
            {
                enPassantHelper.ExecuteEnPassant(board, to, piece);
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

        public List<int> GetAvailableMoves(int square)
        {
            var piece = GetPieceAt(square);
            if (piece == null) return new();

            var moves = new List<int>();
            if (castlingHelper.IsCastlingInProgressAndCorrectRookPieceSelected(square, piece))
            {
                castlingHelper.SelectedDestinationForRookWhenIsCastlingInProgress = CastlingHelper.GetCastleRookDestinationWhenIsCastlingInProgress(piece.Color, castlingHelper.isKingsideCastle, castlingHelper.isQueensideCastle);
                moves.Add(castlingHelper.SelectedDestinationForRookWhenIsCastlingInProgress);
            }
            else
            {
                switch (piece.Type)
                {
                    case PieceType.Pawn:
                        moves.AddRange(GetPawnMoves(square, piece.Color));
                        enPassantHelper.CheckEnPassant(board, square, piece, moves);
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
                        moves.AddRange(castlingHelper.GetCastleMoveIfCan(board, piece));
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
                if (piece.Type == PieceType.Pawn && enPassantHelper.enPassantSquare.HasValue && move == enPassantHelper.enPassantSquare.Value)
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
    }
}
