

namespace ChessServer.Logic
{
    public static class BitBoardHelper
    {
        public const int BoardSize = 8;

        public static bool IsValidSquare(int rank, int file)
            => rank >= 0 && rank < BoardSize && file >= 0 && file < BoardSize;

        public static int ToIndex(int rank, int file) => rank * 8 + file;

        public static ulong SetBit(int index) => 1UL << index;
    }



}
