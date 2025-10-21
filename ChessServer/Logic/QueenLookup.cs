namespace ChessServer.Logic
{
    public static class QueenLookup
    {
        public static readonly ulong[] Moves = new ulong[64];

        static QueenLookup()
        {
            for (int i = 0; i < 64; i++)
                Moves[i] = BishopLookup.Moves[i] | RookLookup.Moves[i];
        }
    }


}
