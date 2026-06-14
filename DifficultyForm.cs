using System;
using System.Drawing;
using System.Windows.Forms;

namespace AlphaZero
{
    public partial class DifficultyForm : Form
    {
        public BotDifficulty ChosenDifficulty { get; private set; } = BotDifficulty.Medium;

        public DifficultyForm()
        {
            InitializeComponent();
        }

        private void btnEasy_Click(object sender, EventArgs e)
        {
            ChosenDifficulty = BotDifficulty.Easy;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnMedium_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Will be available soon!", "Coming Soon",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnHard_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Will be available soon!", "Coming Soon",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DifficultyForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK)
                Application.Exit();
        }

        private void lblSubtitle_Click(object sender, EventArgs e)
        {

        }
    }
}
