using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace AlphaZero
{
    // Double-buffered panel — eliminates flicker during animation
    public class ChessPanel : Panel
    {
        public ChessPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            UpdateStyles();
        }
    }

    public partial class Form1 : Form
    {
        private GameLogic gameLogic = new GameLogic();
        private Dictionary<string, Image> pieceImages = new Dictionary<string, Image>();

        private ChessPanel boardPanel;

        // Selection
        private int selRow = -1, selCol = -1;
        private List<Tuple<int, int>> validMoves = new List<Tuple<int, int>>();

        // Animation
        private System.Windows.Forms.Timer animTimer;
        private bool   animating;
        private int    aFromR, aFromC, aToR, aToC;
        private string aPiece;
        private float  aProgress;
        private const float STEP = 0.1f;

        // Board geometry — fills the entire panel
        private int SqW { get { return boardPanel.Width  / 8; } }
        private int SqH { get { return boardPanel.Height / 8; } }

        // Colors
        private static readonly Color CLight = Color.FromArgb(240, 217, 181);
        private static readonly Color CDark  = Color.FromArgb(181, 136,  99);

        public Form1() { InitializeComponent(); }

        private void Form1_Load(object sender, EventArgs e)
        {
            Text        = "Chess";
            WindowState = FormWindowState.Maximized;
            BackColor   = Color.FromArgb(30, 30, 30);

            LoadImages();
            BuildUI();

            animTimer          = new System.Windows.Forms.Timer { Interval = 12 };
            animTimer.Tick    += OnTick;
        }

        private void LoadImages()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            foreach (var n in new[] { "wP","wR","wN","wB","wQ","wK","bP","bR","bN","bB","bQ","bK" })
            {
                string p = Path.Combine(dir, n + ".png");
                if (File.Exists(p)) try { pieceImages[n] = Image.FromFile(p); } catch { }
            }
        }

        private void BuildUI()
        {
            boardPanel            = new ChessPanel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30) };
            boardPanel.Paint     += OnPaint;
            boardPanel.MouseClick += OnClick;
            boardPanel.Resize    += (s, ev) => boardPanel.Invalidate();

            Controls.Add(boardPanel);
        }

        // ── Animation ─────────────────────────────────────────────────────────
        private void OnTick(object sender, EventArgs e)
        {
            aProgress += STEP;
            if (aProgress >= 1f)
            {
                aProgress = 1f;
                animTimer.Stop();
                animating = false;
                if (gameLogic.PromotionPending)
                    DoPromotion();
                else
                    RefreshStatus();
            }
            boardPanel.Invalidate();
        }

        private void RefreshStatus()
        {
            boardPanel.Invalidate();
            CheckEnd();
        }

        private void CheckEnd()
        {
            if (gameLogic.Result == GameResult.Ongoing) return;
            string msg;
            switch (gameLogic.Result)
            {
                case GameResult.WhiteWins:                 msg = "Checkmate! White wins!"; break;
                case GameResult.BlackWins:                 msg = "Checkmate! Black wins!"; break;
                case GameResult.DrawStalemate:             msg = "Stalemate — Draw!";      break;
                case GameResult.DrawFiftyMoveRule:         msg = "50-move rule — Draw!";   break;
                case GameResult.DrawThreefoldRepetition:   msg = "Threefold — Draw!";      break;
                case GameResult.DrawInsufficientMaterial:  msg = "Insufficient material — Draw!"; break;
                default:                                   msg = "Game Over!";              break;
            }
            if (MessageBox.Show(msg + "\n\nPlay again?", "Game Over",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                NewGame();
        }

        private void DoPromotion()
        {
            string color = gameLogic.IsWhiteTurn ? "w" : "b";
            using (var dlg = new PromotionDialog(color, pieceImages))
            {
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ShowDialog(this);
                gameLogic.ConfirmPromotion(dlg.Chosen ?? color + "Q");
            }
            boardPanel.Invalidate();
            RefreshStatus();
        }

        // ── Drawing ───────────────────────────────────────────────────────────
        private void OnPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode     = SmoothingMode.AntiAlias;

            int sw = SqW, sh = SqH;
            if (sw <= 4 || sh <= 4) return;

            bool wCheck = gameLogic.IsKingInCheck(true);
            bool bCheck = gameLogic.IsKingInCheck(false);

            // ── Squares ───────────────────────────────────────────────────────
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    bool light = (r + c) % 2 == 0;
                    Color col  = light ? CLight : CDark;

                    // Last move
                    if (gameLogic.LastMoveFrom != null &&
                        ((r == gameLogic.LastMoveFrom.Item1 && c == gameLogic.LastMoveFrom.Item2) ||
                         (r == gameLogic.LastMoveTo.Item1   && c == gameLogic.LastMoveTo.Item2)))
                        col = Blend(col, Color.FromArgb(210, 190, 40), 0.45f);

                    // Selection
                    if (r == selRow && c == selCol)
                        col = Blend(col, Color.FromArgb(80, 130, 230), 0.55f);

                    var rect = new Rectangle(c * sw, r * sh, sw, sh);
                    using (var b = new SolidBrush(col)) g.FillRectangle(b, rect);

                    // King in check — red fill
                    string sp = gameLogic.GetPieceAt(r, c);
                    if ((sp == "wK" && wCheck) || (sp == "bK" && bCheck))
                        using (var b = new SolidBrush(Color.FromArgb(120, 220, 30, 30)))
                            g.FillRectangle(b, rect);

                    // Valid-move indicators
                    if (IsValid(r, c))
                    {
                        if (sp == null)
                        {
                            int dsw = sw / 3, dsh = sh / 3;
                            using (var b = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                                g.FillEllipse(b, c*sw+(sw-dsw)/2, r*sh+(sh-dsh)/2, dsw, dsh);
                        }
                        else
                        {
                            int padw = sw / 10, padh = sh / 10;
                            using (var p = new Pen(Color.FromArgb(130, 0, 0, 0), 4))
                                g.DrawEllipse(p, c*sw+padw, r*sh+padh, sw-padw*2, sh-padh*2);
                        }
                    }
                }
            }

            // ── Coordinates ───────────────────────────────────────────────────
            float fs = Math.Max(7f, Math.Min(sw, sh) * 0.16f);
            using (var font = new Font("Segoe UI", fs, FontStyle.Bold))
            {
                for (int r = 0; r < 8; r++)
                {
                    bool light = (r % 2 == 0);
                    using (var b = new SolidBrush(light ? CDark : CLight))
                        g.DrawString((8 - r).ToString(), font, b, 2, r * sh + 2);
                }
                for (int c = 0; c < 8; c++)
                {
                    bool light = (c % 2 == 1);
                    string lbl = ((char)('a' + c)).ToString();
                    SizeF ts   = g.MeasureString(lbl, font);
                    using (var b = new SolidBrush(light ? CDark : CLight))
                        g.DrawString(lbl, font, b, c*sw+sw-ts.Width-2, 7*sh+sh-ts.Height);
                }
            }

            // ── Pieces (skip animated piece destination) ──────────────────────
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (animating && r == aToR && c == aToC) continue;
                    string p = gameLogic.GetPieceAt(r, c);
                    if (p != null && pieceImages.ContainsKey(p))
                    {
                        int padw = sw / 11, padh = sh / 11;
                        g.DrawImage(pieceImages[p], c*sw+padw, r*sh+padh, sw-padw*2, sh-padh*2);
                    }
                }
            }

            // ── Animated piece ────────────────────────────────────────────────
            if (animating && aPiece != null && pieceImages.ContainsKey(aPiece))
            {
                float t   = 1f - (1f - aProgress) * (1f - aProgress);
                float px  = aFromC * sw + (aToC - aFromC) * sw * t;
                float py  = aFromR * sh + (aToR - aFromR) * sh * t;
                int   padw = sw / 11, padh = sh / 11;
                g.DrawImage(pieceImages[aPiece], px + padw, py + padh, sw - padw*2, sh - padh*2);
            }
        }

        // ── Input ─────────────────────────────────────────────────────────────
        private void OnClick(object sender, MouseEventArgs e)
        {
            if (animating || gameLogic.Result != GameResult.Ongoing) return;
            int c = e.X / SqW;
            int r = e.Y / SqH;
            if (r < 0 || r > 7 || c < 0 || c > 7) return;
            HandleClick(r, c);
        }

        private void HandleClick(int r, int c)
        {
            string piece = gameLogic.GetPieceAt(r, c);
            bool own = piece != null && (gameLogic.IsWhiteTurn ? piece[0] == 'w' : piece[0] == 'b');

            if (selRow < 0)
            {
                if (own) Select(r, c);
            }
            else if (r == selRow && c == selCol)
            {
                Deselect();
            }
            else if (own)
            {
                Select(r, c);
            }
            else if (IsValid(r, c))
            {
                Move(selRow, selCol, r, c);
            }
            else
            {
                Deselect();
            }
        }

        private void Select(int r, int c)
        {
            selRow = r; selCol = c;
            validMoves = gameLogic.GetValidMoves(r, c, gameLogic.GetPieceAt(r, c));
            boardPanel.Invalidate();
        }

        private void Deselect()
        {
            selRow = selCol = -1;
            validMoves.Clear();
            boardPanel.Invalidate();
        }

        private bool IsValid(int r, int c)
        {
            foreach (var m in validMoves)
                if (m.Item1 == r && m.Item2 == c) return true;
            return false;
        }

        private void Move(int fr, int fc, int tr, int tc)
        {
            aPiece = gameLogic.GetPieceAt(fr, fc);
            aFromR = fr; aFromC = fc; aToR = tr; aToC = tc;
            aProgress = 0f; animating = true;

            gameLogic.MovePiece(fr, fc, tr, tc);
            Deselect();
            animTimer.Start();
        }

        private void NewGame()
        {
            animTimer.Stop(); animating = false;
            gameLogic.ResetGame();
            Deselect();
            boardPanel.Invalidate();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static Color Blend(Color a, Color b, float t)
        {
            return Color.FromArgb(
                Clamp((int)(a.R + (b.R - a.R) * t)),
                Clamp((int)(a.G + (b.G - a.G) * t)),
                Clamp((int)(a.B + (b.B - a.B) * t)));
        }
        private static int Clamp(int v) { return v < 0 ? 0 : v > 255 ? 255 : v; }
    }

    // ── Promotion dialog ──────────────────────────────────────────────────────
    public class PromotionDialog : Form
    {
        public string Chosen { get; private set; }

        public PromotionDialog(string color, Dictionary<string, Image> images)
        {
            Text            = "Promote Pawn";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(36, 36, 48);
            Size            = new Size(360, 130);

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, Padding = new Padding(8),
                BackColor = Color.FromArgb(36, 36, 48), FlowDirection = FlowDirection.LeftToRight
            };

            foreach (var type in new[] { "Q", "R", "B", "N" })
            {
                string code = color + type;
                var btn = new Button
                {
                    Size = new Size(78, 78), FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(55, 55, 75), Cursor = Cursors.Hand, Tag = code
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 120);
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 110);
                if (images.ContainsKey(code))
                {
                    btn.BackgroundImage = images[code];
                    btn.BackgroundImageLayout = ImageLayout.Zoom;
                }
                btn.Click += (s, ev) =>
                {
                    Chosen = (string)((Button)s).Tag;
                    DialogResult = DialogResult.OK;
                    Close();
                };
                flow.Controls.Add(btn);
            }
            Controls.Add(flow);
        }
    }
}
