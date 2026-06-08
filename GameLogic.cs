using System;
using System.Collections.Generic;

namespace AlphaZero
{
    public class GameLogic
    {
        public bool IsWhiteTurn { get; private set; } = true;
        private string[,] boardState;

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
        }

        public string GetPieceAt(int r, int c)
        {
            return boardState[r, c];
        }

        public void MovePiece(int fromRow, int fromCol, int toRow, int toCol)
        {
            string piece = boardState[fromRow, fromCol];
            boardState[fromRow, fromCol] = null;
            boardState[toRow, toCol] = piece;
            IsWhiteTurn = !IsWhiteTurn;
        }

        public List<Tuple<int, int>> GetValidMoves(int r, int c, string piece)
        {
            List<Tuple<int, int>> moves = new List<Tuple<int, int>>();
            if (piece == "wP")
            {
                if (r - 1 >= 0 && boardState[r - 1, c] == null)
                {
                    moves.Add(new Tuple<int, int>(r - 1, c));
                    if (r == 6 && boardState[r - 2, c] == null)
                    {
                        moves.Add(new Tuple<int, int>(r - 2, c));
                    }
                }
                if (r - 1 >= 0 && c - 1 >= 0 && boardState[r - 1, c - 1] != null && boardState[r - 1, c - 1].StartsWith("b"))
                {
                    moves.Add(new Tuple<int, int>(r - 1, c - 1));
                }
                if (r - 1 >= 0 && c + 1 < 8 && boardState[r - 1, c + 1] != null && boardState[r - 1, c + 1].StartsWith("b"))
                {
                    moves.Add(new Tuple<int, int>(r - 1, c + 1));
                }
            }
           
            else if (piece == "bP")
            {
                if (r + 1 < 8 && boardState[r + 1, c] == null)
                {
                    moves.Add(new Tuple<int, int>(r + 1, c));
                    if (r == 1 && boardState[r + 2, c] == null)
                    {
                        moves.Add(new Tuple<int, int>(r + 2, c));
                    }
                }
                if (r + 1 < 8 && c - 1 >= 0 && boardState[r + 1, c - 1] != null && boardState[r + 1, c - 1].StartsWith("w"))
                {
                    moves.Add(new Tuple<int, int>(r + 1, c - 1));
                }
                if (r + 1 < 8 && c + 1 < 8 && boardState[r + 1, c + 1] != null && boardState[r + 1, c + 1].StartsWith("w"))
                {
                    moves.Add(new Tuple<int, int>(r + 1, c + 1));
                }
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
                            moves.Add(new Tuple<int, int>(nr, nc));
                        }
                        else
                        {
                            if (boardState[nr, nc][0] != piece[0])
                            {
                                moves.Add(new Tuple<int, int>(nr, nc));
                            }
                            break;
                        }
                        nr += d[0];
                        nc += d[1];
                    }
                }
            }
            
            else if (piece.EndsWith("N"))
            {
                int[,] knightMoves =
                {
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
                        {
                            moves.Add(new Tuple<int, int>(nr, nc));
                        }
                    }
                }
            }

            else
            {
                // Allow other pieces to move anywhere except squares with same color piece
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        if (i == r && j == c) continue;
                        string targetPiece = boardState[i, j];
                        if (targetPiece == null || targetPiece[0] != piece[0])
                        {
                            moves.Add(new Tuple<int, int>(i, j));
                        }
                    }
                }
            }
           
            return moves;
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
