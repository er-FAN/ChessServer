namespace ChessServer.Logic
{
    public static class KingLookup
    {
        public static readonly ulong[] Moves = new ulong[64];

        static KingLookup()
        {
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    int index = BitBoardHelper.ToIndex(rank, file);
                    ulong mask = 0;

                    for (int dr = -1; dr <= 1; dr++)
                    {
                        for (int df = -1; df <= 1; df++)
                        {
                            if (dr == 0 && df == 0) continue;
                            int r = rank + dr;
                            int f = file + df;
                            if (BitBoardHelper.IsValidSquare(r, f))
                                mask |= BitBoardHelper.SetBit(BitBoardHelper.ToIndex(r, f));
                        }
                    }
                    Moves[index] = mask;
                }
            }
        }
    }


}
