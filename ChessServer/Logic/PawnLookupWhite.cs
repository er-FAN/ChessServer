namespace ChessServer.Logic
{
    public static class PawnLookupWhite
    {
        public static readonly ulong[] Moves = new ulong[64];
        public static readonly ulong[] Attacks = new ulong[64];

        static PawnLookupWhite()
        {
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    int index = BitBoardHelper.ToIndex(rank, file);
                    ulong move = 0, attack = 0;

                    if (rank < 7)
                    {
                        move |= BitBoardHelper.SetBit(BitBoardHelper.ToIndex(rank + 1, file));
                        if (rank == 1)
                            move |= BitBoardHelper.SetBit(BitBoardHelper.ToIndex(rank + 2, file));

                        if (file > 0)
                            attack |= BitBoardHelper.SetBit(BitBoardHelper.ToIndex(rank + 1, file - 1));
                        if (file < 7)
                            attack |= BitBoardHelper.SetBit(BitBoardHelper.ToIndex(rank + 1, file + 1));
                    }

                    Moves[index] = move;
                    Attacks[index] = attack;
                }
            }
        }
    }


}
