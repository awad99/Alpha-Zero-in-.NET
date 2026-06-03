using System;
using System.Drawing;
using System.Windows.Forms;

namespace AlphaZero
{
    public partial class Form1 : Form
    {
        // 2D Array of Button controls representing the board squares
        private Button[,] gridButtons = new Button[8, 8];

        // Custom Premium Colors
        private readonly Color colorLightSquare = Color.FromArgb(240, 240, 240); // Soft Light Cream
        private readonly Color colorDarkSquare = Color.FromArgb(48, 53, 66);     // Sleek Dark Gray-Blue
        private readonly Color colorHoverLight = Color.FromArgb(220, 220, 220);  // Hover color for light squares
        private readonly Color colorHoverDark = Color.FromArgb(64, 71, 88);      // Hover color for dark squares

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetupFormLayout();
            CreateFullScreenChessboard();
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
            int row = position.Item1;
            int col = position.Item2;

            char file = (char)('a' + col);
            int rank = 8 - row;

            // Display clicked square coordinate on standard output or ToolTip
            ToolTip toolTip = new ToolTip();
            toolTip.Show($"Square: {file}{rank}", clickedButton, clickedButton.Width / 2, clickedButton.Height / 2, 1000);
        }
    }
}
