using ChessServer.Logic;

namespace ChessServer.Helpers
{
    public class MovementHelper
    {

        

        public List<int> GetPawnMoves(Piece?[] board,int from, PieceColor color)
        {
            List<int> result = new();
            int rank = from / 8;
            int file = from % 8;

            int dir = color == PieceColor.White ? 1 : -1;

            // حرکت مستقیم (فقط اگر خالی باشد)
            int oneStep = rank + dir;
            if (oneStep >= 0 && oneStep < 8)
            {
                int forward = BitBoardHelper.ToIndex(oneStep, file);
                if (board[forward] == null)
                {
                    result.Add(forward);

                    // حرکت دوخانه از خانه‌ی شروع
                    if (color == PieceColor.White && rank == 1 || color == PieceColor.Black && rank == 6)
                    {
                        int twoStep = BitBoardHelper.ToIndex(rank + dir * 2, file);
                        if (board[twoStep] == null)
                            result.Add(twoStep);
                    }
                }
            }

            // حمله‌ی مورب
            foreach (int df in new[] { -1, 1 })
            {
                int attackFile = file + df;
                int attackRank = rank + dir;

                if (attackFile >= 0 && attackFile < 8 && attackRank >= 0 && attackRank < 8)
                {
                    int attackIndex = BitBoardHelper.ToIndex(attackRank, attackFile);
                    var target = board[attackIndex];
                    if (target != null && target.Color != color)
                        result.Add(attackIndex);
                }
            }

            return result;
        }

        public List<int> GetSimpleMoves(Piece?[] board,int from, ulong moveMask, PieceColor color)
        {
            var result = new List<int>();
            for (int i = 0; i < 64; i++)
            {
                if ((moveMask & 1UL << i) == 0) continue;
                var target = board[i];

                // فقط اگر خانه خالی باشد یا دشمن باشد
                if (target == null || target.Color != color)
                    result.Add(i);
            }
            return result;
        }

        public List<int> GetSlidingMoves(Piece?[] board, int from, ulong moveMask, PieceColor color, (int dr, int df)[] directions)
        {
            List<int> result = new();
            int rank = from / 8;
            int file = from % 8;

            foreach (var (dr, df) in directions)
            {
                int r = rank + dr;
                int f = file + df;

                while (r >= 0 && r < 8 && f >= 0 && f < 8)
                {
                    int index = BitBoardHelper.ToIndex(r, f);
                    var target = board[index];

                    if (target == null)
                    {
                        result.Add(index);
                    }
                    else
                    {
                        // اگر دشمن است فقط همین خانه را می‌توان زد
                        if (target.Color != color)
                            result.Add(index);

                        // مسیر مسدود می‌شود
                        break;
                    }

                    r += dr;
                    f += df;
                }
            }

            return result;
        }

        
    }
}