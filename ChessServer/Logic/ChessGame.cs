using ChessServer.Helpers;
using System.Collections;
using System.Text.RegularExpressions;

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
        private List<(int from, int to, Piece moved)?> UndoMoves = [];

        // وضعیت صفحه (هر خانه یا خالی است یا یک مهره دارد)
        public readonly Piece?[] board = new Piece?[64];

        private List<string> FenHistory = new();

        bool IsThreefoldRepetition = false;

        private int halfMoveClock = 0;  // شمارنده نیم‌حرکت‌ها
        public bool IsFiftyMoveRuleTriggered { get; private set; } = false;

        public GameState GameState { get; private set; }

        public PieceColor Turn { get; private set; } = PieceColor.White;

        public Piece? GetPieceAt(int square) => board[square];

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

        public void Undo()
        {
            Piece piece = Moves.Last().Value.moved;
            int from = Moves.Last().Value.from;
            int to = Moves.Last().Value.to;
            board[from] = piece;
            board[to] = null;
            UndoMoves.Add(Moves.Last());
            Moves.RemoveAt(Moves.Count - 1);
        }

        public void Redo()
        {
            if (UndoMoves.Count > 0)
            {
                Piece piece = UndoMoves.Last().Value.moved;
                int from = UndoMoves.Last().Value.from;
                int to = UndoMoves.Last().Value.to;
                board[from] = null;
                board[to] = piece;
                Moves.Add(UndoMoves.Last());
                UndoMoves.RemoveAt(UndoMoves.Count - 1);
            }
        }

        public bool CheckThreefoldRepetition()
        {
            if (FenHistory.Count < 8) return false; // حداقل 3 موقعیت لازم

            string lastFen = FenHistory[FenHistory.Count - 1];

            // فقط بخش اول تا چهارم FEN مهم است (قبل از نیم حرکت و شماره حرکت)
            string normalize(string fen)
                => string.Join(" ", fen.Split(' ').Take(4));

            string target = normalize(lastFen);

            int count = FenHistory
                .Select(f => normalize(f))
                .Count(f => f == target);

            return count >= 3;
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

        public void UpdateFiftyMoveRule(Piece? movedPiece, Piece? capturedPiece)
        {
            // اگر پیاده حرکت کرده یا مهره‌ای گرفته شده، شمارنده صفر می‌شود
            if (movedPiece?.Type == PieceType.Pawn || capturedPiece != null)
            {
                halfMoveClock = 0;
                IsFiftyMoveRuleTriggered = false; // هنوز قانون فعال نشده
            }
            else
            {
                halfMoveClock++;
                if (halfMoveClock >= 100) // 50 حرکت کامل = 100 نیم‌حرکت
                {
                    IsFiftyMoveRuleTriggered = true;
                }
            }
        }

        public string GenerateFEN()
        {
            // 1) ساخت بخش چیدمان صفحه
            string boardPart = "";
            for (int rank = 7; rank >= 0; rank--)
            {
                int emptyCount = 0;

                for (int file = 0; file < 8; file++)
                {
                    int index = rank * 8 + file;
                    var piece = board[index];

                    if (piece == null)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            boardPart += emptyCount.ToString();
                            emptyCount = 0;
                        }

                        boardPart += PieceToFen(piece);
                    }
                }

                if (emptyCount > 0)
                    boardPart += emptyCount.ToString();

                if (rank > 0)
                    boardPart += "/";
            }

            // 2) نوبت
            string turnPart = (Turn == PieceColor.White) ? "w" : "b";

            // 3) وضعیت امکان قلعه رفتن
            string castlingPart = "";
            if (Turn == PieceColor.White && castlingHelper.canKingsideCastle) castlingPart += "K";
            if (Turn == PieceColor.White && castlingHelper.canQueensideCastle) castlingPart += "Q";
            if (Turn == PieceColor.Black && castlingHelper.canKingsideCastle) castlingPart += "k";
            if (Turn == PieceColor.Black && castlingHelper.canQueensideCastle) castlingPart += "q";
            if (castlingPart == "") castlingPart = "-";

            // 4) خانه en passant
            string enPassantPart = "-";
            if (enPassantHelper.enPassantSquare.HasValue)
                enPassantPart = IndexToSquare(enPassantHelper.enPassantSquare.Value);

            // 5) نیم‌حرکت برای قانون 50 حرکت
            // فعلاً ندارید → صفر
            string halfMoveClock = "0";

            // 6) شماره حرکت کامل
            // اگر نوبت سفید باشد: moveNumber = history.Count / 2 + 1
            int fullMoveNumber = (Moves.Count / 2) + 1;

            return $"{boardPart} {turnPart} {castlingPart} {enPassantPart} {halfMoveClock} {fullMoveNumber}";
        }

        private string PieceToFen(Piece piece)
        {
            char c = piece.Type switch
            {
                PieceType.Pawn => 'p',
                PieceType.Knight => 'n',
                PieceType.Bishop => 'b',
                PieceType.Rook => 'r',
                PieceType.Queen => 'q',
                PieceType.King => 'k',
                _ => '?'
            };

            // سفید باید uppercase باشد
            return (piece.Color == PieceColor.White) ? char.ToUpper(c).ToString() : c.ToString();
        }

        private string IndexToSquare(int index)
        {
            int rank = index / 8;
            int file = index % 8;

            char fileChar = (char)('a' + file);
            char rankChar = (char)('1' + rank);

            return $"{fileChar}{rankChar}";
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
            if (CheckThreefoldRepetition())
            {
                IsThreefoldRepetition = true;
            }

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
            var captured = board[to];

            board[to] = piece;
            board[from] = null;
            

            piece.HasMoved = true;
            UndoMoves = [];

            UpdateFiftyMoveRule(piece, captured);
            FenHistory.Add(GenerateFEN());


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
