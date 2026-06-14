using System;
using System.Collections.Generic;
using System.Linq;

namespace AlphaZero
{
    public enum GameResult
    {
        Ongoing,
        WhiteWins,
        BlackWins,
        DrawStalemate,
        DrawInsufficientMaterial,
        DrawFiftyMoveRule,
        DrawThreefoldRepetition
    }


    public partial class GameLogic
    {
        public bool IsWhiteTurn { get; private set; } = true;
        private string[,] boardState;

        // ── Castling flags ───────────────────────────────────────────────────
        private bool whiteKingMoved          = false;
        private bool blackKingMoved          = false;
        private bool whiteRookKingSideMoved  = false;
        private bool whiteRookQueenSideMoved = false;
        private bool blackRookKingSideMoved  = false;
        private bool blackRookQueenSideMoved = false;

        // ── En-passant ───────────────────────────────────────────────────────
        private int enPassantCol = -1;
        private int enPassantRow = -1;

        // ── Draw-detection state ─────────────────────────────────────────────
        private int halfMoveClock = 0;
        private Dictionary<string, int> positionHistory = new Dictionary<string, int>();

        // ── Last move (for UI highlight) ─────────────────────────────────────
        public Tuple<int, int> LastMoveFrom { get; private set; }
        public Tuple<int, int> LastMoveTo   { get; private set; }

        // ── Promotion pending ────────────────────────────────────────────────
        public bool PromotionPending { get; private set; } = false;
        public int  PromotionRow     { get; private set; }
        public int  PromotionCol     { get; private set; }

        // ── Game result ──────────────────────────────────────────────────────
        public GameResult Result { get; private set; } = GameResult.Ongoing;

        // ════════════════════════════════════════════════════════════════════
        //  Constructor / Init
        // ════════════════════════════════════════════════════════════════════
        public GameLogic() { InitializeBoard(); }

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

            whiteKingMoved = blackKingMoved = false;
            whiteRookKingSideMoved = whiteRookQueenSideMoved = false;
            blackRookKingSideMoved = blackRookQueenSideMoved = false;
            enPassantCol = enPassantRow = -1;
            halfMoveClock = 0;
            positionHistory.Clear();
            Result = GameResult.Ongoing;
            PromotionPending = false;
            LastMoveFrom = LastMoveTo = null;

            RecordPosition();
        }

        // ════════════════════════════════════════════════════════════════════
        //  Public board access
        // ════════════════════════════════════════════════════════════════════
        public string GetPieceAt(int r, int c) => boardState[r, c];

        // ════════════════════════════════════════════════════════════════════
        //  Move execution
        // ════════════════════════════════════════════════════════════════════
        public void MovePiece(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (Result != GameResult.Ongoing) return;

            string piece = boardState[fromRow, fromCol];
            if (piece == null) return;

            bool isPawn    = piece[1] == 'P';
            bool isCapture = boardState[toRow, toCol] != null;

            // ── En-passant capture ───────────────────────────────────────────
            if (isPawn && toCol == enPassantCol && toRow == enPassantRow)
            {
                isCapture = true;
                int capturedRow = piece[0] == 'w' ? toRow + 1 : toRow - 1;
                boardState[capturedRow, toCol] = null;
            }

            // ── Castling ─────────────────────────────────────────────────────
            if (piece == "wK")
            {
                whiteKingMoved = true;
                if (Math.Abs(toCol - fromCol) == 2)
                {
                    if (toCol == 6) { boardState[7, 5] = boardState[7, 7]; boardState[7, 7] = null; }
                    else            { boardState[7, 3] = boardState[7, 0]; boardState[7, 0] = null; }
                }
            }
            else if (piece == "bK")
            {
                blackKingMoved = true;
                if (Math.Abs(toCol - fromCol) == 2)
                {
                    if (toCol == 6) { boardState[0, 5] = boardState[0, 7]; boardState[0, 7] = null; }
                    else            { boardState[0, 3] = boardState[0, 0]; boardState[0, 0] = null; }
                }
            }
            else if (piece == "wR")
            {
                if (fromRow == 7 && fromCol == 0) whiteRookQueenSideMoved = true;
                if (fromRow == 7 && fromCol == 7) whiteRookKingSideMoved  = true;
            }
            else if (piece == "bR")
            {
                if (fromRow == 0 && fromCol == 0) blackRookQueenSideMoved = true;
                if (fromRow == 0 && fromCol == 7) blackRookKingSideMoved  = true;
            }

            // ── En-passant target for NEXT move ──────────────────────────────
            if (isPawn && Math.Abs(toRow - fromRow) == 2)
            {
                enPassantCol = toCol;
                enPassantRow = (fromRow + toRow) / 2;
            }
            else
            {
                enPassantCol = enPassantRow = -1;
            }

            // ── Execute move ─────────────────────────────────────────────────
            boardState[toRow, toCol]     = piece;
            boardState[fromRow, fromCol] = null;

            LastMoveFrom = Sq(fromRow, fromCol);
            LastMoveTo   = Sq(toRow, toCol);

            // ── Half-move clock (50-move rule) ────────────────────────────────
            if (isPawn || isCapture) halfMoveClock = 0;
            else                     halfMoveClock++;

            // ── Pawn promotion ────────────────────────────────────────────────
            if (isPawn && (toRow == 0 || toRow == 7))
            {
                PromotionPending = true;
                PromotionRow = toRow;
                PromotionCol = toCol;
                return; // caller must call ConfirmPromotion()
            }

            FinalizeTurn();
        }

        public void ConfirmPromotion(string newPieceCode)
        {
            boardState[PromotionRow, PromotionCol] = newPieceCode;
            PromotionPending = false;
            FinalizeTurn();
        }

        private void FinalizeTurn()
        {
            IsWhiteTurn = !IsWhiteTurn;
            RecordPosition();
            Result = EvaluateGameResult();
        }

        // ════════════════════════════════════════════════════════════════════
        //  Game-result detection
        // ════════════════════════════════════════════════════════════════════
        public GameResult EvaluateGameResult()
        {
            if (halfMoveClock >= 100)
                return GameResult.DrawFiftyMoveRule;

            if (positionHistory.Values.Any(v => v >= 3))
                return GameResult.DrawThreefoldRepetition;

            if (IsInsufficientMaterial())
                return GameResult.DrawInsufficientMaterial;

            bool hasMoves = CurrentPlayerHasLegalMoves();
            if (!hasMoves)
            {
                bool inCheck = IsKingInCheck(IsWhiteTurn);
                return inCheck
                    ? (IsWhiteTurn ? GameResult.BlackWins : GameResult.WhiteWins)
                    : GameResult.DrawStalemate;
            }

            return GameResult.Ongoing;
        }

        private bool CurrentPlayerHasLegalMoves()
        {
            string prefix = IsWhiteTurn ? "w" : "b";
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    string p = boardState[r, c];
                    if (p != null && p.StartsWith(prefix) && GetValidMoves(r, c, p).Count > 0)
                        return true;
                }
            return false;
        }

        private bool IsInsufficientMaterial()
        {
            var pieces = new List<string>();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (boardState[r, c] != null)
                        pieces.Add(boardState[r, c]);

            if (pieces.Count == 2) return true;

            if (pieces.Count == 3 && pieces.Any(p => p[1] == 'N' || p[1] == 'B'))
                return true;

            if (pieces.Count == 4)
            {
                var bishops = pieces.Where(p => p[1] == 'B').ToList();
                if (bishops.Count == 2)
                {
                    var sq = new List<Tuple<int, int>>();
                    for (int r = 0; r < 8; r++)
                        for (int c = 0; c < 8; c++)
                            if (boardState[r, c] != null && boardState[r, c][1] == 'B')
                                sq.Add(Sq(r, c));
                    if (sq.Count == 2 && (sq[0].Item1 + sq[0].Item2) % 2 == (sq[1].Item1 + sq[1].Item2) % 2)
                        return true;
                }
            }

            return false;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Position recording (threefold repetition)
        // ════════════════════════════════════════════════════════════════════
        private void RecordPosition()
        {
            string key = GetPositionKey();
            if (positionHistory.ContainsKey(key)) positionHistory[key]++;
            else positionHistory[key] = 1;
        }

        private string GetPositionKey()
        {
            var sb = new System.Text.StringBuilder();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    sb.Append(boardState[r, c] ?? ".");
            sb.Append(IsWhiteTurn ? 'W' : 'B');
            sb.Append(whiteKingMoved          ? '0' : '1');
            sb.Append(blackKingMoved          ? '0' : '1');
            sb.Append(whiteRookKingSideMoved  ? '0' : '1');
            sb.Append(whiteRookQueenSideMoved ? '0' : '1');
            sb.Append(blackRookKingSideMoved  ? '0' : '1');
            sb.Append(blackRookQueenSideMoved ? '0' : '1');
            sb.Append(enPassantCol);
            return sb.ToString();
        }

        // ════════════════════════════════════════════════════════════════════
        //  Check detection
        // ════════════════════════════════════════════════════════════════════
        public bool IsKingInCheck(bool whiteKing)
        {
            string king = whiteKing ? "wK" : "bK";
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (boardState[r, c] == king)
                        return IsSquareAttacked(r, c, whiteKing);
            return false;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Valid move generation
        // ════════════════════════════════════════════════════════════════════
        public List<Tuple<int, int>> GetValidMoves(int r, int c, string piece)
        {
            var pseudo = new List<Tuple<int, int>>();

            if (piece == "wP")
            {
                if (r - 1 >= 0 && boardState[r - 1, c] == null)
                {
                    pseudo.Add(Sq(r - 1, c));
                    if (r == 6 && boardState[r - 2, c] == null)
                        pseudo.Add(Sq(r - 2, c));
                }
                if (r - 1 >= 0 && c + 1 < 8 && boardState[r-1,c+1] != null && boardState[r-1,c+1][0] == 'b')
                    pseudo.Add(Sq(r-1, c+1));
                if (r - 1 >= 0 && c - 1 >= 0 && boardState[r-1,c-1] != null && boardState[r-1,c-1][0] == 'b')
                    pseudo.Add(Sq(r-1, c-1));
                if (enPassantRow == r - 1 && Math.Abs(enPassantCol - c) == 1)
                    pseudo.Add(Sq(r - 1, enPassantCol));
            }
            else if (piece == "bP")
            {
                if (r + 1 < 8 && boardState[r+1,c] == null)
                {
                    pseudo.Add(Sq(r+1, c));
                    if (r == 1 && boardState[r+2,c] == null)
                        pseudo.Add(Sq(r+2, c));
                }
                if (r+1 < 8 && c+1 < 8 && boardState[r+1,c+1] != null && boardState[r+1,c+1][0] == 'w')
                    pseudo.Add(Sq(r+1, c+1));
                if (r+1 < 8 && c-1 >= 0 && boardState[r+1,c-1] != null && boardState[r+1,c-1][0] == 'w')
                    pseudo.Add(Sq(r+1, c-1));
                if (enPassantRow == r + 1 && Math.Abs(enPassantCol - c) == 1)
                    pseudo.Add(Sq(r + 1, enPassantCol));
            }
            else if (piece[1] == 'R' || piece[1] == 'B' || piece[1] == 'Q')
            {
                var dirs = new List<int[]>();
                if (piece[1] == 'R' || piece[1] == 'Q')
                    dirs.AddRange(new[] { new[]{-1,0}, new[]{1,0}, new[]{0,-1}, new[]{0,1} });
                if (piece[1] == 'B' || piece[1] == 'Q')
                    dirs.AddRange(new[] { new[]{-1,-1}, new[]{-1,1}, new[]{1,-1}, new[]{1,1} });
                foreach (var d in dirs)
                {
                    int nr = r+d[0], nc = c+d[1];
                    while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                    {
                        if (boardState[nr, nc] == null)      pseudo.Add(Sq(nr, nc));
                        else { if (boardState[nr,nc][0] != piece[0]) pseudo.Add(Sq(nr,nc)); break; }
                        nr += d[0]; nc += d[1];
                    }
                }
            }
            else if (piece[1] == 'N')
            {
                int[,] km = { {-2,-1},{-2,1},{-1,-2},{-1,2},{1,-2},{1,2},{2,-1},{2,1} };
                for (int i = 0; i < 8; i++)
                {
                    int nr = r + km[i,0], nc = c + km[i,1];
                    if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                    {
                        var t = boardState[nr, nc];
                        if (t == null || t[0] != piece[0]) pseudo.Add(Sq(nr, nc));
                    }
                }
            }
            else if (piece[1] == 'K')
            {
                int[,] km = { {-1,-1},{-1,0},{-1,1},{0,-1},{0,1},{1,-1},{1,0},{1,1} };
                for (int i = 0; i < 8; i++)
                {
                    int nr = r + km[i,0], nc = c + km[i,1];
                    if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                    {
                        var t = boardState[nr, nc];
                        if (t == null || t[0] != piece[0]) pseudo.Add(Sq(nr, nc));
                    }
                }


                if (piece == "wK" && !whiteKingMoved && !IsKingInCheck(true))
                {
                    if (!whiteRookKingSideMoved
                        && boardState[7,5] == null && boardState[7,6] == null
                        && !IsSquareAttacked(7,5,true) && !IsSquareAttacked(7,6,true))
                        pseudo.Add(Sq(7, 6));

                    if (!whiteRookQueenSideMoved
                        && boardState[7,1] == null && boardState[7,2] == null && boardState[7,3] == null
                        && !IsSquareAttacked(7,3,true) && !IsSquareAttacked(7,2,true))
                        pseudo.Add(Sq(7, 2));
                }

                else if (piece == "bK" && !blackKingMoved && !IsKingInCheck(false))
                {
                    if (!blackRookKingSideMoved
                        && boardState[0,5] == null && boardState[0,6] == null
                        && !IsSquareAttacked(0,5,false) && !IsSquareAttacked(0,6,false))
                        pseudo.Add(Sq(0, 6));

                    if (!blackRookQueenSideMoved
                        && boardState[0,1] == null && boardState[0,2] == null && boardState[0,3] == null
                        && !IsSquareAttacked(0,3,false) && !IsSquareAttacked(0,2,false))
                        pseudo.Add(Sq(0, 2));
                }
            }


            var legal = new List<Tuple<int, int>>();
            foreach (var mv in pseudo)
                if (IsMoveLegal(r, c, mv.Item1, mv.Item2, piece))
                    legal.Add(mv);
            return legal;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Legality filter – simulate move on temp board
        // ════════════════════════════════════════════════════════════════════
        private bool IsMoveLegal(int fr, int fc, int tr, int tc, string piece)
        {
            var tmp = (string[,])boardState.Clone();


            if (piece[1] == 'P' && tc == enPassantCol && tr == enPassantRow)
            {
                int cap = piece[0] == 'w' ? tr + 1 : tr - 1;
                tmp[cap, tc] = null;
            }

            tmp[tr, tc] = piece;
            tmp[fr, fc] = null;

            int kr, kc;
            if (piece[1] == 'K') { kr = tr; kc = tc; }
            else GetKingPosition(piece[0] == 'w', tmp, out kr, out kc);

            return !IsSquareAttacked(kr, kc, piece[0] == 'w', tmp);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Attack detection
        //  isWhiteKing = true  → we protect white king → opponent is black
        //  isWhiteKing = false → we protect black king → opponent is white
        // ════════════════════════════════════════════════════════════════════
        private bool IsSquareAttacked(int row, int col, bool isWhiteKing, string[,] board = null)
        {
            if (board == null) board = boardState;
            char opp = isWhiteKing ? 'b' : 'w';
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                {
                    var p = board[i, j];
                    if (p != null && p[0] == opp && CanPieceAttack(i, j, row, col, p, board))
                        return true;
                }
            return false;
        }


        private bool CanPieceAttack(int fr, int fc, int tr, int tc, string piece, string[,] board)
        {
            if (piece[1] == 'P')
            {
                int fwd = piece[0] == 'w' ? -1 : 1;
                // Pawns attack the two diagonal squares ahead, regardless of occupancy
                return Math.Abs(tc - fc) == 1 && tr == fr + fwd;
            }

            if (piece[1] == 'N')
            {
                int rankDiff = Math.Abs(tr - fr), fileDiff = Math.Abs(tc - fc);
                return (rankDiff == 2 && fileDiff == 1) || (rankDiff == 1 && fileDiff == 2);
            }

            if (piece[1] == 'K')
                return Math.Abs(tr - fr) <= 1 && Math.Abs(tc - fc) <= 1;

            // Sliders: Rook, Bishop, Queen
            bool isR = piece[1] == 'R';
            bool isB = piece[1] == 'B';
            bool isQ = piece[1] == 'Q';

            if (!isR && !isB && !isQ) return false;

            bool sameRow = fr == tr;
            bool sameCol = fc == tc;
            bool isDiag  = !sameRow && !sameCol && Math.Abs(tr - fr) == Math.Abs(tc - fc);

            if (isR && !sameRow && !sameCol) return false;
            if (isB && !isDiag)              return false;
            if (isQ && !sameRow && !sameCol && !isDiag) return false;

            // Path must be clear (no piece between fr,fc and tr,tc exclusive)
            int dr = Math.Sign(tr - fr), dc = Math.Sign(tc - fc);
            int r2 = fr + dr, c2 = fc + dc;
            while (r2 != tr || c2 != tc)
            {
                if (board[r2, c2] != null) return false;
                r2 += dr; c2 += dc;
            }
            return true;
        }

        private void GetKingPosition(bool isWhite, string[,] board, out int row, out int col)
        {
            string t = isWhite ? "wK" : "bK";
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    if (board[i, j] == t) { row = i; col = j; return; }
            row = col = -1;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Helpers
        // ════════════════════════════════════════════════════════════════════
        private static Tuple<int, int> Sq(int r, int c) => new Tuple<int, int>(r, c);

        public string GetFullPieceName(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length < 2) return "Unknown";
            string color = code[0] == 'w' ? "White" : "Black";
            string type;
            switch (code[1])
            {
                case 'P': type = "Pawn";   break;
                case 'R': type = "Rook";   break;
                case 'N': type = "Knight"; break;
                case 'B': type = "Bishop"; break;
                case 'Q': type = "Queen";  break;
                case 'K': type = "King";   break;
                default:  type = "Piece";  break;
            }
            return color + " " + type;
        }

        public void ResetGame() => InitializeBoard();
    }
}
