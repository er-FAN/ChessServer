using ChessServer.Logic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChessServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChessController : ControllerBase
    {
        private static readonly ChessGame _game = new ChessGame();

        // وقتی بازیکن مهره‌ای را انتخاب می‌کند
        [HttpPost("pickup")]
        public IActionResult Pickup(PickupRequest request)
        {
            int square = ParseSquare(request.Square);
            var piece = _game.GetPieceAt(square);

            if (piece == null)
                return BadRequest("در این خانه مهره‌ای وجود ندارد.");

            if (piece.Color != _game.Turn)
                return BadRequest("نوبت بازیکن دیگر است.");

            var moves = _game.GetAvailableMoves(square);

            // خروجی به صورت خانه‌های قابل حرکت (مثل e4, e5, ...)
            var moveNames = moves.Select(IndexToSquareName).ToList();

            return Ok(new
            {
                Piece = $"{piece.Color} {piece.Type}",
                From = request.Square,
                Moves = moveNames
            });
        }

        // وقتی بازیکن مهره را روی خانه جدید می‌گذارد
        [HttpPost("place")]
        public IActionResult Place(PlaceRequest request)
        {
            int from = ParseSquare(request.FromSquare);
            int to = ParseSquare(request.ToSquare);

            var piece = _game.GetPieceAt(from);
            if (piece == null)
                return BadRequest("هیچ مهره‌ای در خانه‌ی مبدأ وجود ندارد.");

            if (piece.Color != _game.Turn)
                return BadRequest("نوبت این بازیکن نیست.");

            var legalMoves = _game.GetAvailableMoves(from);
            if (!legalMoves.Contains(to))
                return BadRequest("حرکت غیرمجاز است.");

            _game.MovePiece(from, to);
            string message = $"حرکت {piece.Color} {piece.Type} از {request.FromSquare} به {request.ToSquare} انجام شد.";
            string promotionMessage = "enter 1 for queen,2 for rook,3 for bishop,4 for knight";
            if (_game.pendingPromotionSquare != null)
            {
                message += "\n" + promotionMessage;
            }

            return Ok(new
            {
                Message = message,
                NextTurn = _game.Turn.ToString()
            });
        }

        [HttpPost("promotion")]
        public IActionResult PromotePawn(PromotionRequest request)
        {
            if (_game.pendingPromotionSquare == null)
                return BadRequest("No pawn is awaiting promotion.");

            // تبدیل عدد کاربر به enum PieceType
            PieceType newType = request.PieceType switch
            {
                1 => PieceType.Queen,
                2 => PieceType.Rook,
                3 => PieceType.Bishop,
                4 => PieceType.Knight,
                _ => throw new ArgumentException("Invalid piece type")
            };

            // انجام پروموشن
            _game.PromotePawn(_game.pendingPromotionSquare.Value, newType);

            return Ok(new
            {
                success = true,
                promotedTo = newType.ToString()
            });
        }

        // ------------------------
        // توابع کمکی
        // ------------------------

        private static int ParseSquare(string square)
        {
            square = square.ToLower();
            int file = square[0] - 'a'; // a→0 ... h→7
            int rank = int.Parse(square[1].ToString()) - 1; // 1→0 ... 8→7
            return BitBoardHelper.ToIndex(rank, file);
        }

        private static string IndexToSquareName(int index)
        {
            int rank = index / 8;
            int file = index % 8;
            return $"{(char)('a' + file)}{rank + 1}";
        }
    }

    // مدل‌های درخواست

    public class PickupRequest
    {
        public string Square { get; set; } = string.Empty; // مثل "e2"
    }

    public class PlaceRequest
    {
        public string FromSquare { get; set; } = string.Empty; // مثل "e2"
        public string ToSquare { get; set; } = string.Empty;   // مثل "e4"
    }

    public class PromotionRequest
    {
        public int PieceType { get; set; }  // 1=Queen, 2=Rook, 3=Bishop, 4=Knight
    }

}
