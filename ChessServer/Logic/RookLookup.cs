namespace ChessServer.Logic
{
    public static class RookLookup
    {
        public static readonly ulong[] Moves = new ulong[64];

        static RookLookup()
        {
            int[] dr = { 1, -1, 0, 0 };
            int[] df = { 0, 0, 1, -1 };

            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    int index = BitBoardHelper.ToIndex(rank, file);
                    ulong mask = 0;

                    for (int dir = 0; dir < 4; dir++)
                    {
                        int r = rank + dr[dir];
                        int f = file + df[dir];
                        while (BitBoardHelper.IsValidSquare(r, f))
                        {
                            mask |= BitBoardHelper.SetBit(BitBoardHelper.ToIndex(r, f));
                            r += dr[dir];
                            f += df[dir];
                        }
                    }

                    Moves[index] = mask;
                }
            }
        }
    }


}
