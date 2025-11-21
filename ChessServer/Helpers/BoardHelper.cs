using ChessServer.Logic;

namespace ChessServer.Helpers
{
    public class BoardHelper
    {
        public void SetupInitialPosition(Piece?[] board)
        {
            // مهره‌های سفید
            InitialWhitePiecesPosition(board);

            // مهره‌های سیاه
            InitialBlackPiecesPosition(board);
        }

        private void InitialBlackPiecesPosition(Piece?[] board)
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

        private void InitialWhitePiecesPosition(Piece?[] board)
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

        public Piece? GetPieceAt(Piece?[] board, int square) => board[square];

        
    }
}
