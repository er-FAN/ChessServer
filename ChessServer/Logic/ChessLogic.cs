using ChessServer.Models;

namespace ChessServer.Logic
{
    public static class ChessLogic
    {
        public static List<string> GetValidMoves(BoardState board, string square)
        {
            // در نسخه نهایی باید قوانین واقعی را بررسی کند
            // فعلاً برای نمونه فقط حرکت سرباز سفید از E2 به E3/E4
            if (square == "E2")
                return new List<string> { "E3", "E4" };
            return new List<string>();
        }
    }
}
