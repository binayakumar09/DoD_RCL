using System;
using System.Windows.Forms;
using System.Drawing;

namespace ReaLTaiizor.UI
{
    /// <summary>
    /// A simple dialog form that displays download progress with a progress bar and status label.
    /// </summary>
    public class ProgressDialog : Form
    {
        private ProgressBar progressBar;
        private Label statusLabel;
        private Label percentageLabel;

        public ProgressDialog(string title, string message)
        {
            InitializeComponents(title, message);
        }

        private void InitializeComponents(string title, string message)
        {
            // Form settings
            this.Text = title;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(400, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.TopMost = true;

            // Status label
            statusLabel = new Label
            {
                Text = message,
                Location = new Point(20, 20),
                Size = new Size(360, 30),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(statusLabel);

            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(20, 60),
                Size = new Size(360, 25),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            this.Controls.Add(progressBar);

            // Percentage label
            percentageLabel = new Label
            {
                Text = "0%",
                Location = new Point(20, 95),
                Size = new Size(360, 30),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = SystemColors.ControlText
            };
            this.Controls.Add(percentageLabel);
        }

        /// <summary>
        /// Updates the progress bar and status text.
        /// </summary>
        /// <param name="percentage">Progress percentage (0-100)</param>
        /// <param name="message">Status message to display</param>
        public void UpdateProgress(int percentage, string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new System.Action(() => UpdateProgress(percentage, message)));
                return;
            }

            // Clamp percentage between 0 and 100
            percentage = Math.Max(0, Math.Min(100, percentage));

            progressBar.Value = percentage;
            percentageLabel.Text = $"{percentage}% - {message}";
            this.Refresh();
        }

        /// <summary>
        /// Updates just the status message without changing progress.
        /// </summary>
        /// <param name="message">Status message to display</param>
        public void UpdateMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new System.Action(() => UpdateMessage(message)));
                return;
            }

            statusLabel.Text = message;
            this.Refresh();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                progressBar?.Dispose();
                statusLabel?.Dispose();
                percentageLabel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
