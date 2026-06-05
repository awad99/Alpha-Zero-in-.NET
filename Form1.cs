using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AlphaZero
{
    public partial class Form1 : Form
    {
        private Button[,] gridButtons = new Button[8, 8];

        private readonly Color colorLightSquare = Color.FromArgb(240, 240, 240); 
        private readonly Color colorDarkSquare = Color.FromArgb(48, 53, 66);     
        private readonly Color colorHoverLight = Color.FromArgb(220, 220, 220); 
        private readonly Color colorHoverDark = Color.FromArgb(64, 71, 88);      

        private Dictionary<string, Image> pieceImages = new Dictionary<string, Image>();
        private Tuple<int, int> selectedSquare = null;
        private List<Tuple<int, int>> validMoves = new List<Tuple<int, int>>();
        private readonly Color colorValidMove = Color.FromArgb(144, 238, 144); // LightGreen

        private GameLogic gameLogic = new GameLogic();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadPieceImages();
            SetupFormLayout();
            CreateFullScreenChessboard();
        }

        private void LoadPieceImages()
        {
            string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            string[] pieceNames = { "wP", "wR", "wN", "wB", "wQ", "wK", "bP", "bR", "bN", "bB", "bQ", "bK" };
            
            foreach (var piece in pieceNames)
            {
                string filePath = Path.Combine(assetsPath, $"{piece}.png");
                if (File.Exists(filePath))
                {
                    try
                    {
                        pieceImages[piece] = Image.FromFile(filePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading {piece} image: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Missing chess piece image: {filePath}");
                }
            }
        }

        private void SetupFormLayout()
        {
            this.Text = "Chess Board";
            this.BackColor = Color.Black;
            
            // Maximize the form to fill the screen
            this.WindowState = FormWindowState.Maximized;
            this.DoubleBuffered = true;
        }

        private void CreateFullScreenChessboard()
        {
            // TableLayoutPanel to automatically size squares to fill the screen
            TableLayoutPanel boardTable = new TableLayoutPanel
            {
                RowCount = 8,
                ColumnCount = 8,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = Color.Black
            };

            // Set all columns to 12.5% width
            for (int col = 0; col < 8; col++)
            {
                boardTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            }

            // Set all rows to 12.5% height
            for (int row = 0; row < 8; row++)
            {
                boardTable.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            }

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    bool isLight = (row + col) % 2 == 0;
                    Color defaultColor = isLight ? colorLightSquare : colorDarkSquare;
                    Color hoverColor = isLight ? colorHoverLight : colorHoverDark;

                    Button btn = new Button
                    {
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = defaultColor,
                        Text = "", 
                        Tag = new Tuple<int, int>(row, col)
                    };
                    btn.FlatAppearance.BorderSize = 0;
                    btn.FlatAppearance.MouseOverBackColor = hoverColor;
                    btn.FlatAppearance.MouseDownBackColor = hoverColor;

                    // Set piece image if present
                    string piece = gameLogic.GetPieceAt(row, col);
                    if (piece != null && pieceImages.ContainsKey(piece))
                    {
                        btn.BackgroundImage = pieceImages[piece];
                        btn.BackgroundImageLayout = ImageLayout.Zoom;
                    }

                    btn.Click += Square_Click;

                    boardTable.Controls.Add(btn, col, row);
                    gridButtons[row, col] = btn;
                }
            }

            this.Controls.Add(boardTable);
        }

        private void Square_Click(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            Tuple<int, int> position = (Tuple<int, int>)clickedButton.Tag;
            int r = position.Item1;
            int c = position.Item2;

            char file = (char)('a' + c);
            int rank = 8 - r;

            if (selectedSquare == null)
            {
                // Select a piece
                string pieceAtSquare = gameLogic.GetPieceAt(r, c);
                if (pieceAtSquare != null)
                {
                    selectedSquare = position;
                    clickedButton.BackColor = Color.FromArgb(173, 216, 230); // Light blue highlight for selection
                    
                    validMoves = gameLogic.GetValidMoves(r, c, pieceAtSquare);
                    HighlightValidMoves();

                    // Show selection tooltip
                    string pieceName = gameLogic.GetFullPieceName(pieceAtSquare);
                    ToolTip toolTip = new ToolTip();
                    toolTip.Show($"Selected {pieceName} on {file}{rank}", clickedButton, clickedButton.Width / 2, clickedButton.Height / 2, 800);
                }
                else
                {
                    // Clicked empty square without selection
                    ToolTip toolTip = new ToolTip();
                    toolTip.Show($"Square: {file}{rank}", clickedButton, clickedButton.Width / 2, clickedButton.Height / 2, 800);
                }
            }
            else
            {
                int selRow = selectedSquare.Item1;
                int selCol = selectedSquare.Item2;

                if (selRow == r && selCol == c)
                {
                    // Deselect
                    ResetSquareColors();
                    selectedSquare = null;
                    validMoves.Clear();
                }
                else
                {
                    bool isValidMove = false;
                    foreach (var move in validMoves)
                    {
                        if (move.Item1 == r && move.Item2 == c)
                        {
                            isValidMove = true;
                            break;
                        }
                    }

                    if (isValidMove)
                    {
                        // Move piece
                        string piece = gameLogic.GetPieceAt(selRow, selCol);
                        gameLogic.MovePiece(selRow, selCol, r, c);

                        // Update UI buttons
                        UpdateSquareUI(selRow, selCol);
                        UpdateSquareUI(r, c);

                        ResetSquareColors();
                        selectedSquare = null;
                        validMoves.Clear();

                        // Tooltip for movement confirmation
                        string pieceName = gameLogic.GetFullPieceName(piece);
                        ToolTip toolTip = new ToolTip();
                        toolTip.Show($"Moved {pieceName} to {file}{rank}", clickedButton, clickedButton.Width / 2, clickedButton.Height / 2, 1000);
                    }
                    else
                    {
                        // Deselect if clicked on invalid square
                        ResetSquareColors();
                        selectedSquare = null;
                        validMoves.Clear();
                    }
                }
            }
        }

        private void HighlightValidMoves()
        {
            foreach (var move in validMoves)
            {
                int moveR = move.Item1;
                int moveC = move.Item2;
                gridButtons[moveR, moveC].BackColor = colorValidMove;
            }
        }

        private void UpdateSquareUI(int r, int c)
        {
            Button btn = gridButtons[r, c];
            string piece = gameLogic.GetPieceAt(r, c);
            if (piece != null && pieceImages.ContainsKey(piece))
            {
                btn.BackgroundImage = pieceImages[piece];
                btn.BackgroundImageLayout = ImageLayout.Zoom;
            }
            else
            {
                btn.BackgroundImage = null;
            }
        }

        private void ResetSquareColors()
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    bool isLight = (r + c) % 2 == 0;
                    gridButtons[r, c].BackColor = isLight ? colorLightSquare : colorDarkSquare;
                }
            }
        }
    }
}
