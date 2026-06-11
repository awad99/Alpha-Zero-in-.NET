using System;
using System.Collections.Generic;

namespace AlphaZero
{
    public class GameLogic
    {
        public bool IsWhiteTurn { get; private set; } = true;
        private string[,] boardState;

        private bool whiteKingMoved = false;
        private bool blackKingMoved = false;
        private bool whiteRookKingSideMoved = false;   
        private bool whiteRookQueenSideMoved = false;  
        private bool blackRookKingSideMoved = false;   
        private bool blackRookQueenSideMoved = false;  

        public GameLogic()
        {
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            IsWhiteTurn = true;
            boardState = new string[8, 8]
            {
                { "bR", "bN", "bB", "bQ", "bK", "bB", "bN", "bR" },
                { "bP", "bP", "bP", "bP", "bP", "bP", "bP", "bP" },
                { null, null, null, null, null, null, null, null },
                { null, null, null, null, null, null, null, null },
                { null, null, null, null, null, null, null, null },
                { null, null, null, null, null, null, null, null },
                { "wP", "wP", "wP", "wP", "wP", "wP", "wP", "wP" },
                { "wR", "wN", "wB", "wQ", "wK", "wB", "wN", "wR" }
            };

            whiteKingMoved = false;
            blackKingMoved = false;
            whiteRookKingSideMoved = false;
            whiteRookQueenSideMoved = false;
            blackRookKingSideMoved = false;
            blackRookQueenSideMoved = false;
        }

        public string GetPieceAt(int r, int c)
        {
            return boardState[r, c];
        }

        public void PromotePawn(int row, int col, string newPieceCode)
        {
            if (boardState[row, col] != null && boardState[row, col][1] == 'P')
            {
                boardState[row, col] = newPieceCode;
            }
        }

        public void MovePiece(int fromRow, int fromCol, int toRow, int toCol)
        {
            string piece = boardState[fromRow, fromCol];
            if (piece == null) return;

            if (piece == "wK")
            {
                whiteKingMoved = true;

                // Castling white
                if (Math.Abs(toCol - fromCol) == 2)
                {
                    // King side
                    if (toCol == 6)
                    {
                        boardState[7, 5] = boardState[7, 7];
                        boardState[7, 7] = null;
                    }

                    // Queen side
                    else if (toCol == 2)
                    {
                        boardState[7, 3] = boardState[7, 0];
                        boardState[7, 0] = null;
                    }
                }
            }

            else if (piece == "bK")
            {
                blackKingMoved = true;

                if (Math.Abs(toCol - fromCol) == 2)
                {
                    // King side
                    if (toCol == 6)
                    {
                        boardState[0, 5] = boardState[0, 7];
                        boardState[0, 7] = null;
                    }

                    // Queen side
                    else if (toCol == 2)
                    {
                        boardState[0, 3] = boardState[0, 0];
                        boardState[0, 0] = null;
                    }
                }
            }

            else if (piece == "wR")
            {
                if (fromRow == 7 && fromCol == 0) whiteRookQueenSideMoved = true;
                if (fromRow == 7 && fromCol == 7) whiteRookKingSideMoved = true;
            }
        
            else if (piece == "bR")
            {
                if (fromRow == 0 && fromCol == 0) blackRookQueenSideMoved = true;
                if (fromRow == 0 && fromCol == 7) blackRookKingSideMoved = true;
            }

            boardState[toRow, toCol] = piece;
            boardState[fromRow, fromCol] = null;

            IsWhiteTurn = !IsWhiteTurn;
        }

        public bool IsKingAlive(bool whiteKing)
        {
            string king = whiteKing ? "wK" : "bK";

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (boardState[r, c] == king)
                        return true;
                }
            }

            return false;
        }

        public bool IsKingInCheck(bool whiteKing)
        {
            int kingRow = -1;
            int kingCol = -1;

            string king = whiteKing ? "wK" : "bK";

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (boardState[r, c] == king)
                    {
                        kingRow = r;
                        kingCol = c;
                    }
                }
            }

            return IsSquareAttacked(
                kingRow,
                kingCol,
                whiteKing);
        }
       
        public List<Tuple<int, int>> GetValidMoves(int r, int c, string piece)
        {
            List<Tuple<int, int>> pseudoMoves = new List<Tuple<int, int>>();


            if (piece == "wP")
            {

                if (r - 1 >= 0 && boardState[r - 1, c] == null)
                {
                    pseudoMoves.Add(new Tuple<int, int>(r - 1, c));

                    if (r == 6 && boardState[r - 2, c] == null)
                        pseudoMoves.Add(new Tuple<int, int>(r - 2, c));
                }

                if (r - 1 >= 0 && c + 1 < 8 && boardState[r - 1, c + 1] != null && boardState[r - 1, c + 1].StartsWith("b"))
                    pseudoMoves.Add(new Tuple<int, int>(r - 1, c + 1));

                if (r - 1 >= 0 && c - 1 >= 0 && boardState[r - 1, c - 1] != null && boardState[r - 1, c - 1].StartsWith("b"))
                    pseudoMoves.Add(new Tuple<int, int>(r - 1, c - 1));
            }

            else if (piece == "bP")
            {
                if (r + 1 < 8 && boardState[r + 1, c] == null)
                {
                    pseudoMoves.Add(new Tuple<int, int>(r + 1, c));
                    if (r == 1 && boardState[r + 2, c] == null)
                        pseudoMoves.Add(new Tuple<int, int>(r + 2, c));
                }
                if (r + 1 < 8 && c + 1 < 8 && boardState[r + 1, c + 1] != null && boardState[r + 1, c + 1].StartsWith("w"))
                    pseudoMoves.Add(new Tuple<int, int>(r + 1, c + 1));
                if (r + 1 < 8 && c - 1 >= 0 && boardState[r + 1, c - 1] != null && boardState[r + 1, c - 1].StartsWith("w"))
                    pseudoMoves.Add(new Tuple<int, int>(r + 1, c - 1));
            }

            else if (piece.EndsWith("R") || piece.EndsWith("B") || piece.EndsWith("Q"))
            {
                List<int[]> dirs = new List<int[]>();
                if (piece.EndsWith("R") || piece.EndsWith("Q"))
                {
                    dirs.AddRange(new int[][] { new int[] { -1, 0 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { 0, 1 } });
                }
                if (piece.EndsWith("B") || piece.EndsWith("Q"))
                {
                    dirs.AddRange(new int[][] { new int[] { -1, -1 }, new int[] { -1, 1 }, new int[] { 1, -1 }, new int[] { 1, 1 } });
                }

                foreach (var d in dirs)
                {
                    int nr = r + d[0];
                    int nc = c + d[1];
                    while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                    {
                        if (boardState[nr, nc] == null)
                        {
                            pseudoMoves.Add(new Tuple<int, int>(nr, nc));
                        }
                        else
                        {
                            if (boardState[nr, nc][0] != piece[0])
                                pseudoMoves.Add(new Tuple<int, int>(nr, nc));
                            break;
                        }
                        nr += d[0];
                        nc += d[1];
                    }
                }
            }

            else if (piece.EndsWith("N"))
            {
                int[,] knightMoves = {
                    { -2, -1 }, { -2, 1 },
                    { -1, -2 }, { -1, 2 },
                    { 1, -2 }, { 1, 2 },
                    { 2, -1 }, { 2, 1 }
                };
                for (int i = 0; i < 8; i++)
                {
                    int nr = r + knightMoves[i, 0];
                    int nc = c + knightMoves[i, 1];
                    if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                    {
                        string target = boardState[nr, nc];
                        if (target == null || target[0] != piece[0])
                            pseudoMoves.Add(new Tuple<int, int>(nr, nc));
                    }
                }
            }

            else if (piece.EndsWith("K"))
            {
                int[,] kingMoves = {
                    { -1, -1 }, { -1, 0 }, { -1, 1 },
                    { 0, -1 },             { 0, 1 },
                    { 1, -1 }, { 1, 0 }, { 1, 1 }
                };
                for (int i = 0; i < 8; i++)
                {
                    int nr = r + kingMoves[i, 0];
                    int nc = c + kingMoves[i, 1];
                    if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                    {
                        string target = boardState[nr, nc];
                        if (target == null || target[0] != piece[0])
                            pseudoMoves.Add(new Tuple<int, int>(nr, nc));
                    }
                }

                if (piece == "wK" && !whiteKingMoved)
                {
                    // King side (O-O)
                    if (!whiteRookKingSideMoved &&
                        boardState[7, 5] == null &&
                        boardState[7, 6] == null)
                    {
                        if (!IsSquareAttacked(7, 4, false) &&
                            !IsSquareAttacked(7, 5, false) &&
                            !IsSquareAttacked(7, 6, false))
                        {
                            pseudoMoves.Add(new Tuple<int, int>(7, 6)); // O-O
                        }
                    }

                    // Queen side (O-O-O)
                    if (!whiteRookQueenSideMoved &&
                        boardState[7, 1] == null &&
                        boardState[7, 2] == null &&
                        boardState[7, 3] == null)
                    {
                        if (!IsSquareAttacked(7, 4, false) &&
                            !IsSquareAttacked(7, 3, false) &&
                            !IsSquareAttacked(7, 2, false))
                        {
                            pseudoMoves.Add(new Tuple<int, int>(7, 2)); // O-O-O
                        }
                    }
                }
                // ===== Castling BLACK =====
                else if (piece == "bK" && !blackKingMoved)
                {
                    // King side
                    if (!blackRookKingSideMoved &&
                        boardState[0, 5] == null &&
                        boardState[0, 6] == null)
                    {
                        if (!IsSquareAttacked(0, 4, true) &&
                            !IsSquareAttacked(0, 5, true) &&
                            !IsSquareAttacked(0, 6, true))
                        {
                            pseudoMoves.Add(new Tuple<int, int>(0, 6));
                        }
                    }

                    // Queen side
                    if (!blackRookQueenSideMoved &&
                            boardState[0, 1] == null &&
                            boardState[0, 2] == null &&
                            boardState[0, 3] == null)
                    {
                        if (!IsSquareAttacked(0, 4, true) &&
                                !IsSquareAttacked(0, 3, true) &&
                                !IsSquareAttacked(0, 2, true))
                        {
                            pseudoMoves.Add(new Tuple<int, int>(0, 2));
                        }
                    }
                }
            }

                List<Tuple<int, int>> validMoves = new List<Tuple<int, int>>();
            foreach (var move in pseudoMoves)
            {
                if (IsMoveLegal(r, c, move.Item1, move.Item2, piece))
                    validMoves.Add(move);
            }
            return validMoves;
    }
            

        private bool IsMoveLegal(int fromRow, int fromCol, int toRow, int toCol, string piece)
        {
            string[,] tempBoard = (string[,])boardState.Clone();
            string capturedPiece = tempBoard[toRow, toCol];

            tempBoard[toRow, toCol] = piece;
            tempBoard[fromRow, fromCol] = null;

            int kingRow, kingCol;
            if (piece[1] == 'K')
            {
                kingRow = toRow;
                kingCol = toCol;
            }
            else
            {
                GetKingPosition(piece[0] == 'w', tempBoard, out kingRow, out kingCol);
            }

 
            bool isAttacked = IsSquareAttacked(kingRow, kingCol, piece[0] == 'w', tempBoard);
            return !isAttacked;
        }

        private void GetKingPosition(bool isWhite, string[,] board, out int row, out int col)
        {
            string target = isWhite ? "wK" : "bK";
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    if (board[i, j] == target)
                    {
                        row = i; col = j;
                        return;
                    }
            row = col = -1;
        }

        private bool IsSquareAttacked(int row, int col, bool isWhiteTurn, string[,] board = null)
        {
            if (board == null) board = boardState;
            string opponentPrefix = isWhiteTurn ? "b" : "w";

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    string piece = board[i, j];
                    if (piece != null && piece[0] == opponentPrefix[0])
                    {
                        if (CanPieceMoveTo(i, j, row, col, piece, board))
                            return true;
                    }
                }
            }
            return false;
        }

        private bool CanPieceMoveTo(int fromRow, int fromCol, int toRow, int toCol, string piece, string[,] board)
        {
            int dr = Math.Sign(toRow - fromRow);
            int dc = Math.Sign(toCol - fromCol);
            int steps = Math.Max(Math.Abs(toRow - fromRow), Math.Abs(toCol - fromCol));

            if (piece[1] == 'P')
            {
                int forward = piece[0] == 'w' ? -1 : 1;
                if (toCol == fromCol && toRow == fromRow + forward && board[toRow, toCol] == null)
                    return true;
                if (Math.Abs(toCol - fromCol) == 1 && toRow == fromRow + forward && board[toRow, toCol] != null)
                    return true;
                return false;
            }

            if (piece[1] == 'N')
            {
                int dRow = Math.Abs(toRow - fromRow);
                int dCol = Math.Abs(toCol - fromCol);
                return (dRow == 2 && dCol == 1) || (dRow == 1 && dCol == 2);
            }


            if (piece[1] == 'K')
            {
                return Math.Abs(toRow - fromRow) <= 1 && Math.Abs(toCol - fromCol) <= 1;
            }

            bool isRook = piece[1] == 'R';
            bool isBishop = piece[1] == 'B';
            bool isQueen = piece[1] == 'Q';

            if (isQueen || isRook || isBishop)
            {
                if (isQueen || isRook)
                {
                    if (fromRow != toRow && fromCol != toCol) return false;
                }
                if (isQueen || isBishop)
                {
                    if (Math.Abs(toRow - fromRow) != Math.Abs(toCol - fromCol)) return false;
                }


                int r = fromRow + dr;
                int c = fromCol + dc;
                while (r != toRow || c != toCol)
                {
                    if (board[r, c] != null) return false;
                    r += dr;
                    c += dc;
                }
                return true;
            }

            return false;
        }

        public string GetFullPieceName(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length < 2) return "Unknown";
            string color = code.StartsWith("w") ? "White" : "Black";
            string type = "";
            switch (code[1])
            {
                case 'P': type = "Pawn"; break;
                case 'R': type = "Rook"; break;
                case 'N': type = "Knight"; break;
                case 'B': type = "Bishop"; break;
                case 'Q': type = "Queen"; break;
                case 'K': type = "King"; break;
                default: type = "Piece"; break;
            }
            return $"{color} {type}";
        }
    }
}