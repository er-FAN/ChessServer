namespace ChessServer.Logic
{
    public static class KnightLookup
    {
        public static readonly ulong[] Moves = new ulong[64];

        static KnightLookup()
        {
            int[] dr = { 2, 2, 1, 1, -1, -1, -2, -2 };
            int[] df = { 1, -1, 2, -2, 2, -2, 1, -1 };

            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    int index = BitBoardHelper.ToIndex(rank, file);
                    ulong mask = 0;

                    for (int i = 0; i < 8; i++)
                    {
                        int r = rank + dr[i];
                        int f = file + df[i];
                        if (BitBoardHelper.IsValidSquare(r, f))
                            mask |= BitBoardHelper.SetBit(BitBoardHelper.ToIndex(r, f));
                    }
                    Moves[index] = mask;
                }
            }
        }
    }


}
