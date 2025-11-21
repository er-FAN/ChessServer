using ChessServer.Helpers;
using System.Collections;

namespace ChessServer.Logic
{
    public class ChessGame : MovementHelper
    {
        public BoardHelper boardHelper;

        public EnPassantHelper enPassantHelper;
        public PromotionHelper promotionHelper;
        public CastlingHelper castlingHelper;
        public MovementHelper movementHelper;

        private (int from, int to, Piece moved)? lastMove;
        private List<(int from, int to, Piece moved)?> Moves;

        // وضعیت صفحه (هر خانه یا خالی است یا یک مهره دارد)
        public readonly Piece?[] board = new Piece?[64];

        public GameState GameState { get; private set; }

        public PieceColor Turn { get; private set; } = PieceColor.White;

        public ChessGame()
        {
            boardHelper = new BoardHelper();
            enPassantHelper = new EnPassantHelper();
            promotionHelper = new PromotionHelper();
            castlingHelper = new CastlingHelper();
            movementHelper = new MovementHelper();
            Moves = [];
            boardHelper.SetupInitialPosition(board);
        }

        public void MovePiece(Piece?[] board, int from, int to)
        {
            Piece? piece = boardHelper.GetPieceAt(board, from);
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

        public List<int> GetAvailableMoves(int square)
        {
            var piece = boardHelper.GetPieceAt(board, square);
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
                        moves.AddRange(movementHelper.GetPawnMoves(board, square, piece.Color));
                        enPassantHelper.CheckEnPassant(board, square, piece, moves);
                        break;

                    case PieceType.Knight:
                        moves.AddRange(movementHelper.GetSimpleMoves(board, square, KnightLookup.Moves[square], piece.Color));
                        break;
                    case PieceType.Bishop:
                        moves.AddRange(movementHelper.GetSlidingMoves(board, square, BishopLookup.Moves[square], piece.Color, new (int, int)[] { (1, 1), (1, -1), (-1, 1), (-1, -1) }));
                        break;
                    case PieceType.Rook:
                        moves.AddRange(movementHelper.GetSlidingMoves(board, square, RookLookup.Moves[square], piece.Color, new (int, int)[] { (1, 0), (-1, 0), (0, 1), (0, -1) }));
                        break;
                    case PieceType.Queen:
                        moves.AddRange(movementHelper.GetSlidingMoves(board, square, QueenLookup.Moves[square], piece.Color, new (int, int)[] {
                (1, 0), (-1, 0), (0, 1), (0, -1), (1,1), (1,-1), (-1,1), (-1,-1)
            }));
                        break;
                    case PieceType.King:
                        moves.AddRange(movementHelper.GetSimpleMoves(board, square, KingLookup.Moves[square], piece.Color));
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

        private bool IsStalemate(PieceColor color)
        {
            // اگر شاه در کیش است، پات نیست
            if (IsKingInCheck(color))
                return false;

            // اگر هر مهره‌ای حرکت قانونی دارد، پات نیست
            for (int square = 0; square < 64; square++)
            {
                var piece = boardHelper.GetPieceAt(board, square);
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
                            moves.AddRange(movementHelper.GetPawnMoves(board, i, p.Color));
                            break;
                        case PieceType.Knight:
                            moves.AddRange(movementHelper.GetSimpleMoves(board, i, KnightLookup.Moves[i], p.Color));
                            break;
                        case PieceType.Bishop:
                            moves.AddRange(movementHelper.GetSlidingMoves(board, i, BishopLookup.Moves[i], p.Color, new (int, int)[] { (1, 1), (1, -1), (-1, 1), (-1, -1) }));
                            break;
                        case PieceType.Rook:
                            moves.AddRange(movementHelper.GetSlidingMoves(board, i, RookLookup.Moves[i], p.Color, new (int, int)[] { (1, 0), (-1, 0), (0, 1), (0, -1) }));
                            break;
                        case PieceType.Queen:
                            moves.AddRange(movementHelper.GetSlidingMoves(board, i, QueenLookup.Moves[i], p.Color, new (int, int)[]{
                        (1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)
                    }));
                            break;
                        case PieceType.King:
                            moves.AddRange(movementHelper.GetSimpleMoves(board, i, KingLookup.Moves[i], p.Color));
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
