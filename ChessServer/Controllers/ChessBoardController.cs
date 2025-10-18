using ChessServer.Logic;
using ChessServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChessServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChessBoardController : ControllerBase
    {
        private static BoardState board = new();

        [HttpPost("pickup")]
        public IActionResult Pickup(MoveRequest request)
        {
            var square = request.Square.ToUpper();
            if (!board.Squares.ContainsKey(square))
                return BadRequest("No piece on that square.");

            board.SelectedSquare = square;

            var validMoves = ChessLogic.GetValidMoves(board, square);
            return Ok(new { validMoves });
        }

        [HttpPost("place")]
        public IActionResult Place(MoveRequest request)
        {
            var square = request.Square.ToUpper();
            if (board.SelectedSquare == null)
                return BadRequest("No piece selected.");

            var validMoves = ChessLogic.GetValidMoves(board, board.SelectedSquare);
            bool isValid = validMoves.Contains(square);

            if (isValid)
            {
                var piece = board.Squares[board.SelectedSquare];
                board.Squares.Remove(board.SelectedSquare);
                board.Squares[square] = piece;
                board.SelectedSquare = null;

                return Ok(new { isMoveValid = true, message = $"Move executed: {piece.Type} to {square}" });
            }

            board.SelectedSquare = null;
            return Ok(new { isMoveValid = false, message = "Invalid move" });
        }
    }
}
