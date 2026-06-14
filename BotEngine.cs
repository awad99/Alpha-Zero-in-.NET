using System;
using System.Collections.Generic;

namespace AlphaZero
{
    public enum BotDifficulty { Easy, Medium, Hard }

    public partial class GameLogic
    {
        private static readonly Random Rng = new Random();

        public Tuple<int,int,int,int> GetBestMove(BotDifficulty difficulty)
        {
            var moves = GetAllMoves(IsWhiteTurn ? 'w' : 'b');
            if (moves.Count == 0) return null;
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
