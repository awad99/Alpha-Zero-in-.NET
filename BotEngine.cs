using System;
using System.Collections.Generic;

namespace AlphaZero
{
    public enum BotDifficulty { Easy, Medium, Hard }

    public partial class GameLogic
    {
        private static readonly Random Rng = new Random();

        // ── Piece values (positive = good for white) ──────────────────────────
        private static int PieceVal(char type)
        {
            switch (type)
            {
                case 'P': return 100;
                case 'N': return 320;
                case 'B': return 330;
                case 'R': return 500;
                case 'Q': return 900;
                case 'K': return 20000;
                default:  return 0;
            }
        }

        // ── Snapshot of all state needed for search ───────────────────────────
        private struct SearchState
        {
            public string[,] board;
            public bool isWhiteTurn;
            public bool wKingMoved, bKingMoved;
            public bool wRookK, wRookQ, bRookK, bRookQ;
            public int  epCol, epRow, halfClock;
        }

        private SearchState SaveState() => new SearchState
        {
            board       = (string[,])boardState.Clone(),
            isWhiteTurn = IsWhiteTurn,
            wKingMoved  = whiteKingMoved,
            bKingMoved  = blackKingMoved,
            wRookK      = whiteRookKingSideMoved,
            wRookQ      = whiteRookQueenSideMoved,
            bRookK      = blackRookKingSideMoved,
            bRookQ      = blackRookQueenSideMoved,
            epCol       = enPassantCol,
            epRow       = enPassantRow,
            halfClock   = halfMoveClock,
        };

        private void RestoreState(SearchState s)
        {
            boardState              = s.board;
            IsWhiteTurn             = s.isWhiteTurn;
            whiteKingMoved          = s.wKingMoved;
            blackKingMoved          = s.bKingMoved;
            whiteRookKingSideMoved  = s.wRookK;
            whiteRookQueenSideMoved = s.wRookQ;
            blackRookKingSideMoved  = s.bRookK;
            blackRookQueenSideMoved = s.bRookQ;
            enPassantCol            = s.epCol;
            enPassantRow            = s.epRow;
            halfMoveClock           = s.halfClock;
        }

        // ── Material evaluation (positive = good for white) ───────────────────
        private int Evaluate()
        {
            int score = 0;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    string p = boardState[r, c];
                    if (p == null) continue;
                    int val = PieceVal(p[1]);
                    score += p[0] == 'w' ? val : -val;
                }
            return score;
        }

        // ── Apply move without full bookkeeping (for search only) ─────────────
        private void ApplyMoveForSearch(int fr, int fc, int tr, int tc)
        {
            string piece     = boardState[fr, fc];
            bool   isPawn    = piece[1] == 'P';
            bool   isCapture = boardState[tr, tc] != null;

            // En-passant capture
            if (isPawn && tc == enPassantCol && tr == enPassantRow)
            {
                isCapture = true;
                int capturedRow = piece[0] == 'w' ? tr + 1 : tr - 1;
                boardState[capturedRow, tc] = null;
            }

            // Castling — move rook
            if (piece == "wK")
            {
                whiteKingMoved = true;
                if (Math.Abs(tc - fc) == 2)
                {
                    if (tc == 6) { boardState[7, 5] = boardState[7, 7]; boardState[7, 7] = null; }
                    else         { boardState[7, 3] = boardState[7, 0]; boardState[7, 0] = null; }
                }
            }
            else if (piece == "bK")
            {
                blackKingMoved = true;
                if (Math.Abs(tc - fc) == 2)
                {
                    if (tc == 6) { boardState[0, 5] = boardState[0, 7]; boardState[0, 7] = null; }
                    else         { boardState[0, 3] = boardState[0, 0]; boardState[0, 0] = null; }
                }
            }
            else if (piece == "wR")
            {
                if (fr == 7 && fc == 0) whiteRookQueenSideMoved = true;
                if (fr == 7 && fc == 7) whiteRookKingSideMoved  = true;
            }
            else if (piece == "bR")
            {
                if (fr == 0 && fc == 0) blackRookQueenSideMoved = true;
                if (fr == 0 && fc == 7) blackRookKingSideMoved  = true;
            }

            // En-passant target for next ply
            if (isPawn && Math.Abs(tr - fr) == 2)
            {
                enPassantCol = tc;
                enPassantRow = (fr + tr) / 2;
            }
            else
            {
                enPassantCol = enPassantRow = -1;
            }

            // Place piece
            boardState[tr, tc] = piece;
            boardState[fr, fc] = null;

            // Auto-promote to queen (no dialog during search)
            if (isPawn && (tr == 0 || tr == 7))
                boardState[tr, tc] = piece[0].ToString() + "Q";

            if (isPawn || isCapture) halfMoveClock = 0;
            else                     halfMoveClock++;

            IsWhiteTurn = !IsWhiteTurn;
        }

        // ── Minimax ────
        private int Minimax(int depth)
        {
            if (depth == 0) return Evaluate();

            bool isWhite = IsWhiteTurn;
            char color   = isWhite ? 'w' : 'b';
            var  moves   = GetAllMoves(color);

            if (moves.Count == 0)
            {
                if (IsKingInCheck(isWhite))
                    return isWhite ? -99999 : 99999;
                return 0; // stalemate
            }

            if (isWhite) // maximizing
            {
                int best = int.MinValue;
                foreach (var mv in moves)
                {
                    var snap = SaveState();
                    ApplyMoveForSearch(mv.Item1, mv.Item2, mv.Item3, mv.Item4);
                    best = Math.Max(best, Minimax(depth - 1));
                    RestoreState(snap);
                }
                return best;
            }
            else // minimizing
            {
                int best = int.MaxValue;
                foreach (var mv in moves)
                {
                    var snap = SaveState();
                    ApplyMoveForSearch(mv.Item1, mv.Item2, mv.Item3, mv.Item4);
                    best = Math.Min(best, Minimax(depth - 1));
                    RestoreState(snap);
                }
                return best;
            }
        }

        // ── Pick best move using minimax (bot plays as black → minimize) ──────
        private Tuple<int,int,int,int> GetBestMoveMinMax(List<Tuple<int,int,int,int>> moves, int depth)
        {
            Tuple<int,int,int,int> bestMove  = null;
            int                    bestScore = int.MaxValue;

            foreach (var mv in moves)
            {
                var snap = SaveState();
                ApplyMoveForSearch(mv.Item1, mv.Item2, mv.Item3, mv.Item4);
                int score = Minimax(depth - 1);
                RestoreState(snap);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestMove  = mv;
                }
            }

            return bestMove ?? moves[Rng.Next(moves.Count)];
        }

        // ── Public entry point ────────────────────────────────────────────────
        public Tuple<int,int,int,int> GetBestMove(BotDifficulty difficulty)
        {
            var moves = GetAllMoves(IsWhiteTurn ? 'w' : 'b');
            if (moves.Count == 0) return null;

            if (difficulty == BotDifficulty.Easy)
                return GetBestMoveMinMax(moves, depth: 3);

            return moves[Rng.Next(moves.Count)];
        }

        private List<Tuple<int,int,int,int>> GetAllMoves(char color)
        {
            var list = new List<Tuple<int,int,int,int>>();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    string p = boardState[r, c];
                    if (p == null || p[0] != color) continue;
                    foreach (var mv in GetValidMoves(r, c, p))
                        list.Add(new Tuple<int,int,int,int>(r, c, mv.Item1, mv.Item2));
                }
            return list;
        }
    }
}
