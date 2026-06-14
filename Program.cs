using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlphaZero
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            BotDifficulty chosen;
            using (var dlg = new DifficultyForm())
            {
                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;
                chosen = dlg.ChosenDifficulty;
            }

            var game = new Form1();
            game.SetDifficulty(chosen);
            Application.Run(game);
        }
    }
}
