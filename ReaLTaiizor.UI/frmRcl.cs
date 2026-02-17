using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Runtime.InteropServices;
using RCL;
using ReaLTaiizor.Colors;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Util;
using ReaLTaiizor.Manager;

namespace ReaLTaiizor.UI
{

    public partial class frmRcl : MaterialForm
    {
        // P/Invoke declarations for creating rounded corners
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("User32.dll", EntryPoint = "SetWindowRgn")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        private readonly MaterialSkinManager materialManager;
        public readonly int progressval = 10;
        static readonly string nl = System.Environment.NewLine;
        readonly string addEfsCommentMessage = "Please add proper rationale" + nl + "if No or Not Applicable option selected";
        readonly string addCdrCommentMessage = "Please add proper rationale" + nl + "if No or Not Applicable option selected";
        readonly string selectCdrOptionMessage = "Please select one option" + nl + "Yes | No | Not Applicable";
        readonly string selectEfsOptionMessage = "Please select one option" + nl + "Yes | No | Not Applicable";
        readonly MessageBoxButtons buttonsOk = MessageBoxButtons.OK;
        DialogResult result1;
        private int colorSchemeIndex;
        public bool cdrComboBoxFlag1 = false;
        public bool cdrComboBoxFlag2 = false;
        public bool cdrComboBoxFlag3 = false;
        public bool cdrComboBoxFlag4 = false;
        public bool cdrComboBoxFlag5 = false;
        public bool cdrComboBoxFlag6 = false;
        public bool cdrComboBoxFlag7 = false;
        public bool cdrComboBoxFlag8 = false;

        public bool efsComboBoxFlag1 = false;
        public bool efsComboBoxFlag2 = false;
        public bool efsComboBoxFlag3 = false;
        public bool efsComboBoxFlag4 = false;
        public bool efsComboBoxFlag5 = false;
        public bool efsComboBoxFlag6 = false;

        public frmRcl()
        {
            InitializeComponent();
            tableLayoutPanel1.BackColor = System.Drawing.Color.White;
            tableLayoutPanel2.BackColor = System.Drawing.Color.White;

            // --- ADD THIS BLOCK to style the CP1/CP2 tab headers ---
            if (systemSpecInnerTabControl != null)
            {
                systemSpecInnerTabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
                systemSpecInnerTabControl.SizeMode = TabSizeMode.Fixed;
                systemSpecInnerTabControl.ItemSize = new Size(120, 30); // Adjust width/height as needed
                systemSpecInnerTabControl.DrawItem += SystemSpecInnerTabControl_DrawItem;
            }
            // --- END ADD ---

            // Owner draw for the other tab control
            efsTabInnerTabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            efsTabInnerTabControl.DrawItem += EfsTabInnerTabControl_DrawItem;

            // Adjust tab header size (width & height) for 12pt bold captions
            AdjustEfsTabHeaderSize();

            console.Visible = false;

            CheckforUpdate();

            materialManager = MaterialSkinManager.Instance;
            materialManager.EnforceBackcolorOnAllComponents = false;
            materialManager.AddFormToManage(this);
            materialManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialManager.ColorScheme = new MaterialColorScheme(MaterialPrimary.Indigo500, MaterialPrimary.Indigo700, MaterialPrimary.Indigo100, MaterialAccent.Pink200, MaterialTextShade.WHITE);
            console.Text = DataLogger.logString;
            
            // Apply rounded corners to the form
            ApplyRoundedCorners();
        }

        // Method to apply rounded corners to the form
        private void ApplyRoundedCorners()
        {
            int cornerRadius = 20; // Adjust the radius as needed for more or less rounded corners
            IntPtr roundedRegion = CreateRoundRectRgn(0, 0, Width, Height, cornerRadius, cornerRadius);
            SetWindowRgn(Handle, roundedRegion, true);
        }

        // Override OnResize to maintain rounded corners when form is resized
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // Reapply rounded corners when form is resized
            ApplyRoundedCorners();
        }

        // Dynamically compute a suitable width & height for tab headers
        private void AdjustEfsTabHeaderSize()
        {
            if (efsTabInnerTabControl == null || efsTabInnerTabControl.TabPages.Count == 0)
                return;

            // Desired font
            using var font = new Font(efsTabInnerTabControl.Font.FontFamily, 12f, FontStyle.Bold);
            int maxWidth = 0;

            foreach (TabPage tp in efsTabInnerTabControl.TabPages)
            {
                // Measure single line text
                var sz = TextRenderer.MeasureText(tp.Text, font, new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.SingleLine);
                // Add horizontal padding
                int w = sz.Width + 32; // 16px left + 16px right
                if (w > maxWidth) maxWidth = w;
            }

            // Minimum width safeguard
            maxWidth = Math.Max(maxWidth, 140);

            // Height: font height + vertical padding
            int height = font.Height + 14; // 7px top + 7px bottom

            efsTabInnerTabControl.SizeMode = TabSizeMode.Fixed;
            efsTabInnerTabControl.ItemSize = new Size(maxWidth, height);
        }

        // DrawItem event handler for bold tab headers (12pt)
        private void EfsTabInnerTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabControl = (TabControl)sender;
            var tabPage = tabControl.TabPages[e.Index];

            using var font = new Font(tabControl.Font.FontFamily, 12f, FontStyle.Bold);

            // Background (respect selection)
            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color back = isSelected ? SystemColors.ControlLightLight : SystemColors.Control;
            using (var b = new SolidBrush(back))
                e.Graphics.FillRectangle(b, e.Bounds);

            // Text color
            Color textColor = isSelected ? SystemColors.ControlText : SystemColors.ControlText;

            TextRenderer.DrawText(
                e.Graphics,
                tabPage.Text,
                font,
                e.Bounds,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
                e.DrawFocusRectangle();
        }

        private void CheckforUpdate()
        {
            String RemoteVersion, LocalVersion = "";
            DataLogger.logString += "Test \n";

            if (!isNWavailable())
            {
                DataLogger.logString += ("Unable to connect to Server");
                return;
            }

            string fileUrl = @"file:\\bhlinn60.apac.nsn-net.net\NEWAirphone_Bangalore\AP-BLR2\RCL\update.xml";
            string currentDirectory = Directory.GetCurrentDirectory();
            string localPath = Path.Combine(currentDirectory, "update_new.xml");
            Uri fileUri = new Uri(fileUrl);

            // Try downloading the update file. If it fails, log and continue execution.
            try
            {
                // Create a FileWebRequest object
                FileWebRequest request = (FileWebRequest)WebRequest.Create(fileUri);

                using (FileWebResponse response = (FileWebResponse)request.GetResponse())
                {
                    // Get the response stream
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        if (responseStream == null)
                        {
                            throw new InvalidOperationException("No response stream received when checking for updates.");
                        }

                        // Create a file stream to save the file
                        using (FileStream fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write))
                        {
                            // Read the response stream and write to the file stream
                            responseStream.CopyTo(fileStream);
                        }
                    }
                }

                DataLogger.logString += "Version File downloaded successfully!";
                DataLogger.logString += "\n";
            }
            catch (WebException wex)
            {
                DataLogger.logString += ("Unable to access update file: " + wex.Message + "\n");

                // Show an error window; user clicks OK to acknowledge and the app proceeds
                try
                {
                    MessageBox.Show("Unable to access update server. Skipping update check." + "\n" + wex.Message, "Update Check", buttonsOk, MessageBoxIcon.Warning);
                }
                catch
                {
                    // If MessageBox fails for any reason, ignore and continue
                }

                // Ensure any partial file is removed
                try { if (File.Exists(localPath)) File.Delete(localPath); } catch { }

                // Proceed with application execution after user acknowledges
                return;
            }
            catch (Exception ex)
            {
                DataLogger.logString += ("Error while checking for updates: " + ex.Message + "\n");

                try
                {
                    MessageBox.Show("Error while checking for updates. Skipping update check." + "\n" + ex.Message, "Update Check", buttonsOk, MessageBoxIcon.Warning);
                }
                catch
                {
                    // ignore
                }

                try { if (File.Exists(localPath)) File.Delete(localPath); } catch { }
                return; // proceed with application execution without update
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(localPath);
            XmlNode node = doc.SelectSingleNode("//AppVersion");
            if (node != null)
            {
                DataLogger.logString += ("Server AppVersion Value: " + node.InnerText);
                DataLogger.logString += "\n";
                RemoteVersion = node.InnerText;
            }
            else
            {
                DataLogger.logString += ("FAIL: AppVersion Value: ");
                DataLogger.logString += "\n";
                MessageBox.Show("Failed to Check Update", "NW Error", buttonsOk, MessageBoxIcon.Error);
                return;
            }

            string localPath_old = Path.Combine(currentDirectory, "update.xml");
            doc.Load(localPath_old);
            XmlNode node_local = doc.SelectSingleNode("//AppVersion");
            if (node_local != null)
            {
                DataLogger.logString += ("Local AppVersion Value: " + node_local.InnerText);
                DataLogger.logString += "\n";
                LocalVersion = node_local.InnerText;
            }
            else
            {
                DataLogger.logString += ("FAIL: AppVersion Value: ");
                DataLogger.logString += "\n";
                MessageBox.Show("Failed to Check Update", "NW Error", buttonsOk, MessageBoxIcon.Error);
                return;
            }
            if (LocalVersion != RemoteVersion)
            {
                DataLogger.logString += ("App Update is Needed");
                DataLogger.logString += "\n";
                MessageBox.Show("Tool Update is Mandatory. Update will take upto 2 minutes. Don't disconnect VPN", "New version Available", buttonsOk, MessageBoxIcon.Warning);
                DownloadFile();
            }
            File.Delete(Path.Combine(currentDirectory, "update_new.xml"));
            return;
        }

        private bool isNWavailable()
        {
            string server = "bhlinn60.apac.nsn-net.net";
            Ping ping = new Ping();

            try
            {
                PingReply reply = ping.Send(server);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to Check for Update!!! Make sure you are connected to VPN", ex.Message, buttonsOk, MessageBoxIcon.Error);
                return false;
            }
        }

        private void materialButton1_Click(object sender, EventArgs e)
        {
            materialManager.Theme = materialManager.Theme == MaterialSkinManager.Themes.DARK ? MaterialSkinManager.Themes.LIGHT : MaterialSkinManager.Themes.DARK;
            updateColor();
        }

        private void updateColor()
        {
            switch (colorSchemeIndex)
            {
                case 0:
                    materialManager.ColorScheme = new MaterialColorScheme(
                        materialManager.Theme == MaterialSkinManager.Themes.DARK ? MaterialPrimary.Teal500 : MaterialPrimary.Indigo500,
                        materialManager.Theme == MaterialSkinManager.Themes.DARK ? MaterialPrimary.Teal700 : MaterialPrimary.Indigo700,
                        materialManager.Theme == MaterialSkinManager.Themes.DARK ? MaterialPrimary.Teal200 : MaterialPrimary.Indigo100,
                        MaterialAccent.Pink200,
                        MaterialTextShade.WHITE);
                    break;
                case 1:
                    materialManager.ColorScheme = new MaterialColorScheme(
                        MaterialPrimary.Green600,
                        MaterialPrimary.Green700,
                        MaterialPrimary.Green200,
                        MaterialAccent.Red100,
                        MaterialTextShade.WHITE);
                    break;
                case 2:
                    materialManager.ColorScheme = new MaterialColorScheme(
                        MaterialPrimary.BlueGrey800,
                        MaterialPrimary.BlueGrey900,
                        MaterialPrimary.BlueGrey500,
                        MaterialAccent.LightBlue200,
                        MaterialTextShade.WHITE);
                    break;
            }
            Invalidate();
        }


        private void efsBtnSubmit_Click(object sender, EventArgs e)
        {

            if (efstxtReviewers.TextLength == 0)
            {
                result1 = MessageBox.Show("Please add reviewers name", "Mandatory reviewers", buttonsOk, MessageBoxIcon.Warning);
                if (result1 == DialogResult.OK)
                {
                    efstxtReviewers.Focus();
                    return;
                }
                else
                {
                    // Do something  
                }
            }

            void ShowMessageAndFocus(ComboBox comboBox, TextBox textBox, string message, Label label)
            {
                var result = MessageBox.Show(message, label.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (result == DialogResult.OK)
                {
                    if (comboBox != null)
                    {
                        comboBox.Focus();
                    }
                    else if (textBox != null)
                    {
                        textBox.Focus();
                    }
                }
                else
                {
                    // Do something
                }
            }

            Boolean ValidateEfsComboBoxAndTextBox(ComboBox comboBox, TextBox textBox, string selectMessage, string addCommentMessage, Label label)
            {
                String text = textBox.Text;
                int wordCount = text.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                if (comboBox.Text.Equals("Select"))
                {
                    ShowMessageAndFocus(null, textBox, "Please Select the Status", label);
                    return false;
                }
                else if (wordCount <= 2 && (comboBox.SelectedIndex.Equals(1) || comboBox.SelectedIndex.Equals(2)))
                {
                    ShowMessageAndFocus(null, textBox, addEfsCommentMessage, label);
                    return false;
                }
                return true;
            }

            Boolean ValidateEfsComboBoxAndTextBoxYes(ComboBox comboBox, TextBox textBox, Label label)
            {
                String text = textBox.Text;
                int wordCount = text.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                if (wordCount < 1 && (comboBox.SelectedIndex.Equals(0)))
                {
                    ShowMessageAndFocus(null, textBox, "Please update Comments Section", label);
                    return false;
                }
                return true;
            }

            ComboBox[] comboBoxes = { efsComboBox1, efsComboBox2, efsComboBox3, efsComboBox4, efsComboBox5, efsComboBox6, efsComboBox7, efsComboBox7, efsComboBox9, efsComboBox10 };
            TextBox[] textBoxes = { efstxt1, efstxt2, efstxt3, efstxt4, efstxt5, efstxt6, efstxt7, efstxt8, efstxt9, efstxt10 };
            Label[] labels = { efslbl1, efslbl2, efslbl3, efslbl4, efslbl5, efslbl6, efslbl7, efslbl8, efslbl9, efslbl10 };

            for (int i = 0; i < comboBoxes.Length; i++)
            {
                if (!ValidateEfsComboBoxAndTextBox(comboBoxes[i], textBoxes[i], selectEfsOptionMessage, addEfsCommentMessage, labels[i]))
                {
                    return;
                }
            }
            if (!ValidateEfsComboBoxAndTextBoxYes(efsComboBox1, efstxt1, efslbl1))
                return;
            if (!ValidateEfsComboBoxAndTextBoxYes(efsComboBox2, efstxt2, efslbl2))
                return;
            if (!ValidateEfsComboBoxAndTextBoxYes(efsComboBox3, efstxt3, efslbl3))
                return;
            if (!ValidateEfsComboBoxAndTextBoxYes(efsComboBox4, efstxt4, efslbl4))
                return;
            if (!ValidateEfsComboBoxAndTextBoxYes(efsComboBox5, efstxt5, efslbl5))
                return;

            efsReviewResult("EFS in CP2 Ready State");
        }

        private void cp2BtnSubmit_Click(object sender, EventArgs e)
        {

            if (cp2txtReviewers.TextLength == 0)
            {
                result1 = MessageBox.Show("Please add reviewers name", "Mandatory reviewers", buttonsOk, MessageBoxIcon.Warning);
                if (result1 == DialogResult.OK)
                {
                    cp2txtReviewers.Focus();
                    return;
                }
                else
                {
                    // Do something  
                }
            }

            void ShowMessageAndFocus(ComboBox comboBox, TextBox textBox, string message, Label label)
            {
                var result = MessageBox.Show(message, label.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (result == DialogResult.OK)
                {
                    if (comboBox != null)
                    {
                        comboBox.Focus();
                    }
                    else if (textBox != null)
                    {
                        textBox.Focus();
                    }
                }
                else
                {
                    // Do something
                }
            }

            Boolean ValidateCP2ComboBoxAndTextBox(ComboBox comboBox, TextBox textBox, string selectMessage, string addCommentMessage, Label label)
            {
                String text = textBox.Text;
                int wordCount = text.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                if (comboBox.Text.Equals("Select"))
                {
                    ShowMessageAndFocus(null, textBox, "Please Select the Status", label);
                    return false;
                }
                else if (wordCount <= 2 && (comboBox.SelectedIndex.Equals(1) || comboBox.SelectedIndex.Equals(2)))
                {
                    ShowMessageAndFocus(null, textBox, addEfsCommentMessage, label);
                    return false;
                }
                return true;
            }

            Boolean ValidateCP2ComboBoxAndTextBoxYes(ComboBox comboBox, TextBox textBox, Label label)
            {
                String text = textBox.Text;
                int wordCount = text.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                if (wordCount < 1 && (comboBox.SelectedIndex.Equals(0)))
                {
                    ShowMessageAndFocus(null, textBox, "Please update Comments Section", label);
                    return false;
                }
                return true;
            }

            ComboBox[] comboBoxes = { cp2ComboBox1, cp2ComboBox2, cp2ComboBox3, cp2ComboBox4, cp2ComboBox5, cp2ComboBox6, cp2ComboBox7, cp2ComboBox7, cp2ComboBox9, cp2ComboBox10 };
            TextBox[] textBoxes = { cp2txt1, cp2txt2, cp2txt3, cp2txt4, cp2txt5, cp2txt6, cp2txt7, cp2txt8, cp2txt9, cp2txt10 };
            Label[] labels = { cp2lbl1, cp2lbl2, cp2lbl3, cp2lbl4, cp2lbl5, cp2lbl6, cp2lbl7, cp2lbl8, cp2lbl9, cp2lbl10 };

            for (int i = 0; i < comboBoxes.Length; i++)
            {
                if (!ValidateCP2ComboBoxAndTextBox(comboBoxes[i], textBoxes[i], selectEfsOptionMessage, addEfsCommentMessage, labels[i]))
                {
                    return;
                }
            }
            if (!ValidateCP2ComboBoxAndTextBoxYes(cp2ComboBox1, cp2txt1, cp2lbl1))
                return;
            if (!ValidateCP2ComboBoxAndTextBoxYes(cp2ComboBox2, cp2txt2, cp2lbl2))
                return;
            if (!ValidateCP2ComboBoxAndTextBoxYes(cp2ComboBox3, cp2txt3, cp2lbl3))
                return;
            if (!ValidateCP2ComboBoxAndTextBoxYes(cp2ComboBox4, cp2txt4, cp2lbl4))
                return;
            if (!ValidateCP2ComboBoxAndTextBoxYes(cp2ComboBox5, cp2txt5, cp2lbl5))
                return;

            efsReviewResult("EFS in CP2 Approved State");
        }

        private void cp3BtnSubmit_Click(object sender, EventArgs e)
        {

            if (cp3txtReviewers.TextLength == 0)
            {
                result1 = MessageBox.Show("Please add reviewers name", "Mandatory reviewers", buttonsOk, MessageBoxIcon.Warning);
                if (result1 == DialogResult.OK)
                {
                    cp3txtReviewers.Focus();
                    return;
                }
                else
                {
                    // Do something  
                }
            }

            void ShowMessageAndFocus(ComboBox comboBox, TextBox textBox, string message, Label label)
            {
                var result = MessageBox.Show(message, label.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (result == DialogResult.OK)
                {
                    if (comboBox != null)
                    {
                        comboBox.Focus();
                    }
                    else if (textBox != null)
                    {
                        textBox.Focus();
                    }
                }
                else
                {
                    // Do something
                }
            }

            Boolean ValidateCP3ComboBoxAndTextBox(ComboBox comboBox, TextBox textBox, string selectMessage, string addCommentMessage, Label label)
            {
                String text = textBox.Text;
                int wordCount = text.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                if (comboBox.Text.Equals("Select"))
                {
                    ShowMessageAndFocus(null, textBox, "Please Select the Status", label);
                    return false;
                }
                else if (wordCount <= 2 && (comboBox.SelectedIndex.Equals(1) || comboBox.SelectedIndex.Equals(2)))
                {
                    ShowMessageAndFocus(null, textBox, addEfsCommentMessage, label);
                    return false;
                }
                return true;
            }

            Boolean ValidateCP3ComboBoxAndTextBoxYes(ComboBox comboBox, TextBox textBox, Label label)
            {
                String text = textBox.Text;
                int wordCount = text.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                if (wordCount < 1 && (comboBox.SelectedIndex.Equals(0)))
                {
                    ShowMessageAndFocus(null, textBox, "Please update Comments Section", label);
                    return false;
                }
                return true;
            }

            ComboBox[] comboBoxes = { cp3ComboBox1, cp3ComboBox2, cp3ComboBox3, cp3ComboBox4, cp3ComboBox5, cp3ComboBox6, cp3ComboBox7 };
            TextBox[] textBoxes = { cp3txt1, cp3txt2, cp3txt3, cp3txt4, cp3txt5, cp3txt6, cp3txt7 };
            Label[] labels = { cp3lbl1, cp3lbl2, cp3lbl3, cp3lbl4, cp3lbl5, cp3lbl6, cp3lbl7 };

            for (int i = 0; i < comboBoxes.Length; i++)
            {
                if (!ValidateCP3ComboBoxAndTextBox(comboBoxes[i], textBoxes[i], selectEfsOptionMessage, addEfsCommentMessage, labels[i]))
                {
                    return;
                }
            }
            if (!ValidateCP3ComboBoxAndTextBoxYes(cp3ComboBox1, cp3txt1, cp3lbl1))
                return;

            efsReviewResult("EFS in CP3 Approved State");
        }

        public void DownloadFile()
        {
            String fileUrl = @"file:\\bhlinn60.apac.nsn-net.net\NEWAirphone_Bangalore\AP-BLR2\RCL\RCL.zip";
            string destinationPath = Path.Combine(Directory.GetCurrentDirectory(), "RCL.zip");
            FileWebRequest request = (FileWebRequest)WebRequest.Create(fileUrl);
            Uri fileUri = new Uri(fileUrl);

            try
            {
                using (FileWebResponse response = (FileWebResponse)request.GetResponse())
                {
                    long totalBytes = response.ContentLength;

                    // Show progress dialog
                    using (var progressForm = new ProgressDialog("Downloading RCL.zip", "Downloading update file..."))
                    {
                        progressForm.Show();
                        Application.DoEvents();

                        // Get the response stream
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            // Create a file stream to save the file
                            using (FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                            {
                                // Read the response stream in chunks and update progress
                                ReadStreamWithProgress(responseStream, fileStream, totalBytes, progressForm);
                            }
                        }
                    }

                    RunUpdater();
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error downloading update: " + ex.Message, "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReadStreamWithProgress(Stream sourceStream, FileStream destinationStream, long totalBytes, ProgressDialog progressDialog)
        {
            const int bufferSize = 8192; // 8KB chunks
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            long totalBytesRead = 0;

            while ((bytesRead = sourceStream.Read(buffer, 0, bufferSize)) > 0)
            {
                destinationStream.Write(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;

                // Update progress
                if (totalBytes > 0)
                {
                    int percentage = (int)((totalBytesRead * 100) / totalBytes);
                    progressDialog.UpdateProgress(percentage, $"Downloading: {totalBytesRead / (1024 * 1024)} MB / {totalBytes / (1024 * 1024)} MB");
                    Application.DoEvents(); // Keep UI responsive
                }
            }

            progressDialog.UpdateProgress(100, "Download complete!");
            System.Threading.Thread.Sleep(500); // Brief pause to show completion
        }

        private void RunUpdater()
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            // Specify the executable name
            string executableName = "Updater.exe";

            // Combine the directory and executable name to get the full path
            string executablePath = Path.Combine(currentDirectory, executableName);

            // Create a new process start info
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = currentDirectory
            };
            Process process = new Process
            {
                StartInfo = startInfo
            };
            process.Start();
        }

        public void cdrReviewResult()
        {
            StringBuilder builder = new("||" + cdrlblhdr1.Text + "||" + cdrlblhdr2.Text + "||" + cdrlblhdr3.Text + "||" + "\n");

            for (int i = 1; i <= 11; i++)
            {
                var label = this.Controls.Find("cdrlbl" + i, true).FirstOrDefault() as Label;
                var comboBox = this.Controls.Find("cdrComboBox" + i, true).FirstOrDefault() as ComboBox;
                var textBox = this.Controls.Find("cdrtxt" + i, true).FirstOrDefault() as TextBox;

                if (label != null && comboBox != null && textBox != null)
                {
                    builder.AppendFormat("|" + label.Text);
                    if (comboBox.Text.Equals("No") || comboBox.Text.Equals("Not Applicable"))
                    {
                        builder.AppendFormat("||" + comboBox.SelectedItem);
                    }
                    else
                    {
                        builder.AppendFormat("|" + comboBox.SelectedItem);
                    }
                    builder.AppendFormat("|" + textBox.Text + " |\n");
                }
            }

            string batchOperationResults = builder.ToString();
            //DialogResult mresult = MaterialMessageBox.Show(batchOperationResults, "Review Result");
            result1 = MessageBox.Show(batchOperationResults + "Click OK to copy contents", "Paste contents to Jira Description", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result1 == DialogResult.OK)
            {
                Clipboard.SetText(batchOperationResults);
                return;
            }
            else
            {
                // Do something  
            }

            return;

        }

        public void efsReviewResult(String efsState)
        {
            StringBuilder builder = new("||" + efsState + "||" + "\n");
            builder.AppendFormat("||" + efslblhdr1.Text + "||" + efslblhdr2.Text + "||" + efslblhdr3.Text + "||" + "\n");

            for (int i = 1; i <= 9; i++)
            {
                var label = this.Controls.Find("efslbl" + i, true).FirstOrDefault() as Label;
                var comboBox = this.Controls.Find("efsComboBox" + i, true).FirstOrDefault() as ComboBox;
                var textBox = this.Controls.Find("efstxt" + i, true).FirstOrDefault() as TextBox;

                if (label != null && comboBox != null && textBox != null)
                {
                    builder.AppendFormat("|" + label.Text);
                    if (comboBox.Text.Equals("No") || comboBox.Text.Equals("Not Applicable"))
                    {
                        builder.AppendFormat("||" + comboBox.SelectedItem);
                    }
                    else
                    {
                        builder.AppendFormat("|" + comboBox.SelectedItem);
                    }
                    builder.AppendFormat("|" + textBox.Text + " |\n");
                }
            }

            builder.AppendFormat("**Reviewers:**  " + efstxtReviewers.Text + " \n\n");

            string batchOperationResults = builder.ToString();
            //DialogResult mresult = MaterialMessageBox.Show(batchOperationResults, "Review Result");
            result1 = MessageBox.Show(batchOperationResults + "Click OK to copy contents", "Paste contents to Jira Description", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result1 == DialogResult.OK)
            {
                Clipboard.SetText(batchOperationResults);
                return;
            }
            else
            {
                // Do something  
            }

            return;

        }

        private void efsBtnReset_Click(object sender, EventArgs e)
        {
            // Defensive: Ensure "Select" is present, add if missing
            EnsureSelectItem(efsComboBox1);
            EnsureSelectItem(efsComboBox2);
            EnsureSelectItem(efsComboBox3);
            EnsureSelectItem(efsComboBox4);
            EnsureSelectItem(efsComboBox5);
            EnsureSelectItem(efsComboBox6);
            EnsureSelectItem(efsComboBox7);
            EnsureSelectItem(efsComboBox8);
            EnsureSelectItem(efsComboBox9);
            EnsureSelectItem(efsComboBox10);

            // Now reliably set to "Select"
            SetComboBoxToSelect(efsComboBox1);
            SetComboBoxToSelect(efsComboBox2);
            SetComboBoxToSelect(efsComboBox3);
            SetComboBoxToSelect(efsComboBox4);
            SetComboBoxToSelect(efsComboBox5);
            SetComboBoxToSelect(efsComboBox6);
            SetComboBoxToSelect(efsComboBox7);
            SetComboBoxToSelect(efsComboBox8);
            SetComboBoxToSelect(efsComboBox9);
            SetComboBoxToSelect(efsComboBox10);

            // Clear associated TextBoxes
            efstxt1.Text = "";
            efstxt2.Text = "";
            efstxt3.Text = "";
            efstxt4.Text = "";
            efstxt5.Text = "";
            efstxt6.Text = "";
            efstxt7.Text = "";
            efstxt8.Text = "";
            efstxt9.Text = "";
            efstxt10.Text = "";
            efstxtReviewers.Text = "";

            // Reset flags
            efsComboBoxFlag1 = false;
            efsComboBoxFlag2 = false;
            efsComboBoxFlag3 = false;
            efsComboBoxFlag4 = false;
            efsComboBoxFlag5 = false;
            efsComboBoxFlag6 = false;
        }

        private void efsBtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cp2BtnReset_Click(object sender, EventArgs e)
        {
            // Defensive: Ensure "Select" is present, add if missing
            EnsureSelectItem(cp2ComboBox1);
            EnsureSelectItem(cp2ComboBox2);
            EnsureSelectItem(cp2ComboBox3);
            EnsureSelectItem(cp2ComboBox4);
            EnsureSelectItem(cp2ComboBox5);
            EnsureSelectItem(cp2ComboBox6);
            EnsureSelectItem(cp2ComboBox7);
            EnsureSelectItem(cp2ComboBox8);
            EnsureSelectItem(cp2ComboBox9);
            EnsureSelectItem(cp2ComboBox10);

            // Now reliably set to "Select"
            SetComboBoxToSelect(cp2ComboBox1);
            SetComboBoxToSelect(cp2ComboBox2);
            SetComboBoxToSelect(cp2ComboBox3);
            SetComboBoxToSelect(cp2ComboBox4);
            SetComboBoxToSelect(cp2ComboBox5);
            SetComboBoxToSelect(cp2ComboBox6);
            SetComboBoxToSelect(cp2ComboBox7);
            SetComboBoxToSelect(cp2ComboBox8);
            SetComboBoxToSelect(cp2ComboBox9);
            SetComboBoxToSelect(cp2ComboBox10);

            // Clear associated TextBoxes
            cp2txt1.Text = "";
            cp2txt2.Text = "";
            cp2txt3.Text = "";
            cp2txt4.Text = "";
            cp2txt5.Text = "";
            cp2txt6.Text = "";
            cp2txt7.Text = "";
            cp2txt8.Text = "";
            cp2txt9.Text = "";
            cp2txt10.Text = "";
            cp2txtReviewers.Text = "";
        }

        private void cp2BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cp3BtnReset_Click(object sender, EventArgs e)
        {
            // Defensive: Ensure "Select" is present, add if missing
            EnsureSelectItem(cp3ComboBox1);
            EnsureSelectItem(cp3ComboBox2);
            EnsureSelectItem(cp3ComboBox3);
            EnsureSelectItem(cp3ComboBox4);
            EnsureSelectItem(cp3ComboBox5);
            EnsureSelectItem(cp3ComboBox6);
            EnsureSelectItem(cp3ComboBox7);

            // Now reliably set to "Select"
            SetComboBoxToSelect(cp3ComboBox1);
            SetComboBoxToSelect(cp3ComboBox2);
            SetComboBoxToSelect(cp3ComboBox3);
            SetComboBoxToSelect(cp3ComboBox4);
            SetComboBoxToSelect(cp3ComboBox5);
            SetComboBoxToSelect(cp3ComboBox6);
            SetComboBoxToSelect(cp3ComboBox7);

            // Clear associated TextBoxes
            cp3txt1.Text = "";
            cp3txt2.Text = "";
            cp3txt3.Text = "";
            cp3txt4.Text = "";
            cp3txt5.Text = "";
            cp3txt6.Text = "";
            cp3txt7.Text = "";
            cp3txtReviewers.Text = "";
        }

        private void cp3BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cdrBtnReset_Click(object sender, EventArgs e)
        {
            // Defensive: Ensure "Select" is present, add if missing
            EnsureSelectItem(cdrComboBox1);
            EnsureSelectItem(cdrComboBox2);
            EnsureSelectItem(cdrComboBox3);
            EnsureSelectItem(cdrComboBox4);
            EnsureSelectItem(cdrComboBox5);
            EnsureSelectItem(cdrComboBox6);
            EnsureSelectItem(cdrComboBox7);
            EnsureSelectItem(cdrComboBox8);
            EnsureSelectItem(cdrComboBox9);
            EnsureSelectItem(cdrComboBox10);

            // Now reliably set to "Select"
            SetComboBoxToSelect(cdrComboBox1);
            SetComboBoxToSelect(cdrComboBox2);
            SetComboBoxToSelect(cdrComboBox3);
            SetComboBoxToSelect(cdrComboBox4);
            SetComboBoxToSelect(cdrComboBox5);
            SetComboBoxToSelect(cdrComboBox6);
            SetComboBoxToSelect(cdrComboBox7);
            SetComboBoxToSelect(cdrComboBox8);
            SetComboBoxToSelect(cdrComboBox9);
            SetComboBoxToSelect(cdrComboBox10);

            // Clear associated TextBoxes
            cdrtxt1.Text = "";
            cdrtxt2.Text = "";
            cdrtxt3.Text = "";
            cdrtxt4.Text = "";
            cdrtxt5.Text = "";
            cdrtxt6.Text = "";
            cdrtxt7.Text = "";
            cdrtxt8.Text = "";
            cdrtxt9.Text = "";
            cdrtxt10.Text = "";

            // Reset flags
            cdrComboBoxFlag1 = false;
            cdrComboBoxFlag2 = false;
            cdrComboBoxFlag3 = false;
            cdrComboBoxFlag4 = false;
            cdrComboBoxFlag5 = false;
            cdrComboBoxFlag6 = false;
            cdrComboBoxFlag7 = false;
            cdrComboBoxFlag8 = false;
        }

        private void cdrBtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cdrBtnSubmit_Click(object sender, EventArgs e)
        {
            // Reviewed by Area Guards
            if (cdrComboBox1.Text.Equals("Select"))
            {
                result1 = MessageBox.Show(selectCdrOptionMessage, cdrlbl1.Text, buttonsOk, MessageBoxIcon.Warning);
                if (result1 == DialogResult.OK)
                {
                    cdrComboBox1.Focus();
                    return;
                }
            }

            // Code Is Applicable and Coding is Done
            if (cdrComboBox2.Text.Equals("Select"))
            {
                result1 = MessageBox.Show(selectCdrOptionMessage, cdrlbl2.Text, buttonsOk, MessageBoxIcon.Warning);
                if (result1 == DialogResult.OK)
                {
                    cdrComboBox2.Focus();
                    return;
                }
            }
            else if (cdrtxt2.TextLength < 10)
            {
                if (cdrComboBox2.Text.Equals("Yes"))
                {
                    result1 = MessageBox.Show("Please add reviewer name and gerrit link" + nl, cdrlbl2.Text, buttonsOk, MessageBoxIcon.Warning);
                }
                else
                {
                    result1 = MessageBox.Show(addCdrCommentMessage, cdrlbl2.Text, buttonsOk, MessageBoxIcon.Warning);
                }

                if (result1 == DialogResult.OK)
                {
                    cdrtxt2.Focus();
                    return;
                }
            }

            // Create arrays for your ComboBoxes, TextBoxes, and Labels
            ComboBox[] comboBoxes = { cdrComboBox3, cdrComboBox4, cdrComboBox5, cdrComboBox6, cdrComboBox7, cdrComboBox8, cdrComboBox9, cdrComboBox10 };
            TextBox[] textBoxes = { cdrtxt3, cdrtxt4, cdrtxt5, cdrtxt6, cdrtxt7, cdrtxt8, cdrtxt9, cdrtxt10 };
            Label[] labels = { cdrlbl3, cdrlbl4, cdrlbl5, cdrlbl6, cdrlbl7, cdrlbl8, cdrlbl9, cdrlbl10 };

            // Loop through each pair and validate
            for (int i = 0; i < comboBoxes.Length; i++)
            {
                String text = textBoxes[i].Text;
                int wordCount = text.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                if (comboBoxes[i].Text.Equals("Select"))
                {
                    result1 = MessageBox.Show(selectCdrOptionMessage, labels[i].Text, buttonsOk, MessageBoxIcon.Warning);
                    if (result1 == DialogResult.OK)
                    {
                        comboBoxes[i].Focus();
                        return;
                    }
                }
                else if ((wordCount <= 2) && (comboBoxes[i].Text.Equals("No") || comboBoxes[i].Text.Equals("Not Applicable")))
                {
                    result1 = MessageBox.Show(addCdrCommentMessage, labels[i].Text, buttonsOk, MessageBoxIcon.Warning);
                    if (result1 == DialogResult.OK)
                    {
                        textBoxes[i].Focus();
                        return;
                    }
                    else
                    {
                        // Do something  
                    }
                }
            }



            // DFMEA is done, If not By when it is planned
            if (cdrComboBox11.Text.Equals("Select"))
            {
                result1 = MessageBox.Show(selectCdrOptionMessage, cdrlbl11.Text, buttonsOk, MessageBoxIcon.Warning);
                if (result1 == DialogResult.OK)
                {
                    cdrComboBox11.Focus();
                    return;
                }
            }
            else if (cdrtxt11.TextLength == 0)
            {
                if (cdrComboBox11.Text.Equals("Yes"))
                {
                    result1 = MessageBox.Show("Please add DFMEA link" + nl, cdrlbl11.Text, buttonsOk, MessageBoxIcon.Warning);
                }
                else if (cdrComboBox11.Text.Equals("No"))
                {
                    result1 = MessageBox.Show("Please add by when DFMEA will be done", cdrlbl11.Text, buttonsOk, MessageBoxIcon.Warning);
                }
                else
                {
                    result1 = MessageBox.Show(addCdrCommentMessage, cdrlbl11.Text, buttonsOk, MessageBoxIcon.Warning);
                }

                if (result1 == DialogResult.OK)
                {
                    cdrtxt11.Focus();
                    return;
                }
            }

            cdrReviewResult();
        }

        private void cdrLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Clipboard.SetText("https://confluence.ext.net.nokia.com/display/AirPhoneWTS/Airphone+DOD+Review+Checklist+Tool");
        }

        private void efslink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Clipboard.SetText("https://confluence.ext.net.nokia.com/display/AirPhoneWTS/Airphone+DOD+Review+Checklist+Tool");
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void efslbl1_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanelBody_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel8_Paint(object sender, PaintEventArgs e)
        {

        }

        private void cdrlbl6_Click(object sender, EventArgs e)
        {

        }

        private void cdrlbl8_Click(object sender, EventArgs e)
        {

        }

        private void cdrlbl11_Click(object sender, EventArgs e)
        {

        }

        private void materialLabel50_Click(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void materialButton1_Click_1(object sender, EventArgs e)
        {

        }

        private void specBtnReset_Click(object sender, EventArgs e)
        {
            // Defensive: Ensure "Select" is present, add if missing
            EnsureSelectItem(specComboBox1);
            EnsureSelectItem(specComboBox2);
            EnsureSelectItem(specComboBox3);
            EnsureSelectItem(specComboBox4);
            EnsureSelectItem(specComboBox5);
            EnsureSelectItem(specComboBox6);
            EnsureSelectItem(specComboBox7);

            // Now reliably set to "Select"
            SetComboBoxToSelect(specComboBox1);
            SetComboBoxToSelect(specComboBox2);
            SetComboBoxToSelect(specComboBox3);
            SetComboBoxToSelect(specComboBox4);
            SetComboBoxToSelect(specComboBox5);
            SetComboBoxToSelect(specComboBox6);
            SetComboBoxToSelect(specComboBox7);

            // Clear associated TextBoxes
            spectxt1.Text = "";
            spectxt2.Text = "";
            spectxt3.Text = "";
            spectxt4.Text = "";
            spectxt5.Text = "";
            spectxt6.Text = "";
            spectxt7.Text = "";
        }

        private void EnsureSelectItem(ComboBox comboBox)
        {
            bool found = false;
            foreach (var item in comboBox.Items)
            {
                if (item.ToString().Equals("Select", StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                comboBox.Items.Insert(0, "Select");
            }
        }

        private void SetComboBoxToSelect(ComboBox comboBox)
        {
            int selectIndex = -1;
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i].ToString().Equals("Select", StringComparison.OrdinalIgnoreCase))
                {
                    selectIndex = i;
                    break;
                }
            }
            if (selectIndex >= 0)
            {
                comboBox.SelectedIndex = selectIndex;
            }
            else
            {
                comboBox.SelectedIndex = 0;
            }
        }

        private void specBtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void specBtnSubmit_Click(object sender, EventArgs e)
        {
            // Example: If you have a reviewers textbox for spec, validate it here.
            // If not, you can remove this block.
            // if (specTxtReviewers.TextLength == 0)
            // {
            //     result1 = MessageBox.Show("Please add reviewers name", "Mandatory reviewers", buttonsOk, MessageBoxIcon.Warning);
            //     if (result1 == DialogResult.OK)
            //     {
            //         specTxtReviewers.Focus();
            //         return;
            //     }
            // }

            void ShowMessageAndFocus(ComboBox comboBox, TextBox textBox, string message, Label label)
            {
                var result = MessageBox.Show(message, label.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (result == DialogResult.OK)
                {
                    if (comboBox != null)
                    {
                        comboBox.Focus();
                    }
                    else if (textBox != null)
                    {
                        textBox.Focus();
                    }
                }
            }

            Boolean ValidateSpecComboBoxAndTextBox(ComboBox comboBox, TextBox textBox, string selectMessage, string addCommentMessage, Label label)
            {
                String text = textBox.Text;
                int wordCount = text.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                if (comboBox.Text.Equals("Select"))
                {
                    ShowMessageAndFocus(comboBox, textBox, "Please Select the Status", label);
                    return false;
                }
                else if (wordCount <= 2 && (comboBox.SelectedIndex == 1 || comboBox.SelectedIndex == 2))
                {
                    ShowMessageAndFocus(comboBox, textBox, addCommentMessage, label);
                    return false;
                }
                return true;
            }

            // Arrays for your ComboBoxes, TextBoxes, and Labels
            ComboBox[] comboBoxes = { specComboBox1, specComboBox2, specComboBox3, specComboBox4, specComboBox5, specComboBox6, specComboBox7 };
            TextBox[] textBoxes = { spectxt1, spectxt2, spectxt3, spectxt4, spectxt5, spectxt6, spectxt7 };
            Label[] labels = { speclbl1, speclbl2, speclbl3, speclbl4, speclbl5, speclbl6, speclbl7 };

            for (int i = 0; i < comboBoxes.Length; i++)
            {
                if (!ValidateSpecComboBoxAndTextBox(comboBoxes[i], textBoxes[i], selectEfsOptionMessage, addEfsCommentMessage, labels[i]))
                {
                    return;
                }
            }

            // If you want to show a summary/result, implement it here.
            // For example, you can copy the logic from efsReviewResult and adapt for spec.
            StringBuilder builder = new StringBuilder("||Review Criteria||Status||Comments||\n");
            for (int i = 0; i < comboBoxes.Length; i++)
            {
                builder.AppendFormat("|{0}", labels[i].Text);
                if (comboBoxes[i].Text.Equals("No") || comboBoxes[i].Text.Equals("Not Applicable"))
                {
                    builder.AppendFormat("||{0}", comboBoxes[i].SelectedItem);
                }
                else
                {
                    builder.AppendFormat("|{0}", comboBoxes[i].SelectedItem);
                }
                builder.AppendFormat("|{0} |\n", textBoxes[i].Text);
            }

            string batchOperationResults = builder.ToString();
            result1 = MessageBox.Show(batchOperationResults + "Click OK to copy contents", "Paste contents to Jira Description", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result1 == DialogResult.OK)
            {
                Clipboard.SetText(batchOperationResults);
                return;
            }
        }

        private void materialLabel10_Click(object sender, EventArgs e)
        {

        }

        private void specBtnReset2_Click(object sender, EventArgs e)
        {
            // Defensive: Ensure "Select" is present, add if missing
            EnsureSelectItem(specComboBox1_2);

            // Now reliably set to "Select"
            SetComboBoxToSelect(specComboBox1_2);

            // Clear associated TextBoxes
            spectxt1_2.Text = "";
        }

        private void specBtnSubmit2_Click(object sender, EventArgs e)
        {
            // If you have a reviewers textbox for spec2, validate it here.
            // If not, you can remove this block.
            // if (specTxtReviewers2.TextLength == 0)
            // {
            //     result1 = MessageBox.Show("Please add reviewers name", "Mandatory reviewers", buttonsOk, MessageBoxIcon.Warning);
            //     if (result1 == DialogResult.OK)
            //     {
            //         specTxtReviewers2.Focus();
            //         return;
            //     }
            // }

            void ShowMessageAndFocus(ComboBox comboBox, TextBox textBox, string message, Label label)
            {
                var result = MessageBox.Show(message, label.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (result == DialogResult.OK)
                {
                    if (comboBox != null)
                    {
                        comboBox.Focus();
                    }
                    else if (textBox != null)
                    {
                        textBox.Focus();
                    }
                }
            }

            Boolean ValidateSpecComboBoxAndTextBox(ComboBox comboBox, TextBox textBox, string selectMessage, string addCommentMessage, Label label)
            {
                String text = textBox.Text;
                int wordCount = text.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                if (comboBox.Text.Equals("Select"))
                {
                    ShowMessageAndFocus(comboBox, textBox, "Please Select the Status", label);
                    return false;
                }
                else if (wordCount <= 2 && (comboBox.SelectedIndex == 1 || comboBox.SelectedIndex == 2))
                {
                    ShowMessageAndFocus(comboBox, textBox, addCommentMessage, label);
                    return false;
                }
                return true;
            }

            // Arrays for your ComboBoxes, TextBoxes, and Labels for CP2
            ComboBox[] comboBoxes = { specComboBox1_2 };
            TextBox[] textBoxes = { spectxt1_2 };
            Label[] labels = { label7 };

            for (int i = 0; i < comboBoxes.Length; i++)
            {
                if (!ValidateSpecComboBoxAndTextBox(comboBoxes[i], textBoxes[i], selectEfsOptionMessage, addEfsCommentMessage, labels[i]))
                {
                    return;
                }
            }

            // Show summary/result for CP2
            StringBuilder builder = new StringBuilder("||Review Criteria||Status||Comments||\n");
            for (int i = 0; i < comboBoxes.Length; i++)
            {
                builder.AppendFormat("|{0}", labels[i].Text);
                if (comboBoxes[i].Text.Equals("No") || comboBoxes[i].Text.Equals("Not Applicable"))
                {
                    builder.AppendFormat("||{0}", comboBoxes[i].SelectedItem);
                }
                else
                {
                    builder.AppendFormat("|{0}", comboBoxes[i].SelectedItem);
                }
                builder.AppendFormat("|{0} |\n", textBoxes[i].Text);
            }

            string batchOperationResults = builder.ToString();
            result1 = MessageBox.Show(batchOperationResults + "Click OK to copy contents", "Paste contents to Jira Description", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result1 == DialogResult.OK)
            {
                Clipboard.SetText(batchOperationResults);
                return;
            }
        }

        private void specBtnCancel2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void materialLabel46_Click(object sender, EventArgs e)
        {

        }

        private void cdrlbl5_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel8_Paint_1(object sender, PaintEventArgs e)
        {

        }

        // Reuse to bold all headers in a TabControl. If fontSize <= 0, it keeps the current size.
        private void BoldAllTabHeaders(TabControl tabControl, float fontSize = 0f)
        {
            if (tabControl == null) return;
            var currentFont = tabControl.Font;
            float newSize = fontSize > 0 ? fontSize : currentFont.Size;

            // Avoid creating a new font object if it's already correct
            if (currentFont.Bold && Math.Abs(currentFont.Size - newSize) < 0.1f) return;

            tabControl.Font = new Font(currentFont.FontFamily, newSize, FontStyle.Bold);
        }

        private void SystemSpecInnerTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabControl = (TabControl)sender;
            var tabPage = tabControl.TabPages[e.Index];

            // Use a bold, size 12 font for the header
            using (var font = new Font(tabControl.Font.FontFamily, 12f, FontStyle.Bold))
            {
                // Define drawing area
                Rectangle tabArea = tabControl.GetTabRect(e.Index);
                
                // Draw the background
                var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                using (var backBrush = new SolidBrush(isSelected ? SystemColors.ControlLightLight : SystemColors.Control))
                {
                    e.Graphics.FillRectangle(backBrush, tabArea);
                }

                // Draw the text
                TextRenderer.DrawText(
                    e.Graphics,
                    tabPage.Text,
                    font,
                    tabArea,
                    SystemColors.ControlText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            // Draw the focus rectangle if the tab has focus
            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
            {
                e.DrawFocusRectangle();
            }
        }
    }
}