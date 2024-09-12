using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ReaLTaiizor.Colors;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Util;

namespace ReaLTaiizor.UI
{
    public partial class frmRcl : MaterialForm
    {
        private readonly MaterialManager materialManager;
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

            // Initialize MaterialManager
            materialManager = MaterialManager.Instance;

            // Set this to false to disable backcolor enforcing on non-materialSkin components
            // This HAS to be set before the AddFormToManage()
            materialManager.EnforceBackcolorOnAllComponents = true;

            // MaterialManager properties
            materialManager.AddFormToManage(this);
            materialManager.Theme = MaterialManager.Themes.LIGHT;
            materialManager.ColorScheme = new MaterialColorScheme(MaterialPrimary.Indigo500, MaterialPrimary.Indigo700, MaterialPrimary.Indigo100, MaterialAccent.Pink200, MaterialTextShade.WHITE);
        }


        private void materialButton1_Click(object sender, EventArgs e)
        {
            materialManager.Theme = materialManager.Theme == MaterialManager.Themes.DARK ? MaterialManager.Themes.LIGHT : MaterialManager.Themes.DARK;
            updateColor();
        }



        private void updateColor()
        {
            switch (colorSchemeIndex)
            {
                case 0:
                    materialManager.ColorScheme = new MaterialColorScheme(
                        materialManager.Theme == MaterialManager.Themes.DARK ? MaterialPrimary.Teal500 : MaterialPrimary.Indigo500,
                        materialManager.Theme == MaterialManager.Themes.DARK ? MaterialPrimary.Teal700 : MaterialPrimary.Indigo700,
                        materialManager.Theme == MaterialManager.Themes.DARK ? MaterialPrimary.Teal200 : MaterialPrimary.Indigo100,
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
                    ShowMessageAndFocus(null, textBox, addCommentMessage, label);
                    return false;
                }
                return true;
            }

            ComboBox[] comboBoxes = { efsComboBox1, efsComboBox2, efsComboBox3, efsComboBox4, efsComboBox5, efsComboBox6, efsComboBox7, efsComboBox7, efsComboBox9 };
            TextBox[] textBoxes = { efstxt1, efstxt2, efstxt3, efstxt4, efstxt5, efstxt6, efstxt7, efstxt8, efstxt9 };
            Label[] labels = { efslbl1, efslbl2, efslbl3, efslbl4, efslbl5, efslbl6, efslbl7, efslbl8, efslbl9 };

            for (int i = 0; i < comboBoxes.Length; i++)
            {
                if (!ValidateEfsComboBoxAndTextBox(comboBoxes[i], textBoxes[i], selectEfsOptionMessage, addEfsCommentMessage, labels[i]))
                {
                    return;
                }
            }

            efsReviewResult();
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

        public void efsReviewResult()
        {
            StringBuilder builder = new("||" + efslblhdr1.Text + "||" + efslblhdr2.Text + "||" + efslblhdr3.Text + "||" + "\n");

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

            builder.AppendFormat("Reviewers:  " + efstxtReviewers.Text + " \n\n");

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

            efsComboBox1.DroppedDown = false;
            efsComboBox1.SelectedItem = "Select";
            efsComboBox1.SelectedIndex = 0;
            efsComboBox1.Focus();
            efstxt1.Text = "";

            efsComboBox2.DroppedDown = false;
            efsComboBox2.SelectedItem = "Select";
            efsComboBox2.SelectedIndex = 0;
            efsComboBox2.Focus();
            efstxt2.Text = "";

            efsComboBox3.DroppedDown = false;
            efsComboBox3.SelectedItem = "Select";
            efsComboBox3.SelectedIndex = 0;
            efsComboBox3.Focus();
            efstxt3.Text = "";

            efsComboBox4.DroppedDown = false;
            efsComboBox4.SelectedItem = "Select";
            efsComboBox4.SelectedIndex = 0;
            efsComboBox4.Focus();
            efstxt4.Text = "";

            efsComboBox5.DroppedDown = false;
            efsComboBox5.SelectedItem = "Select";
            efsComboBox5.SelectedIndex = 0;
            efsComboBox5.Focus();
            efstxt5.Text = "";

            efsComboBox6.DroppedDown = false;
            efsComboBox6.SelectedItem = "Select";
            efsComboBox6.SelectedIndex = 0;
            efsComboBox6.Focus();
            efstxt6.Text = "";

            efstxtReviewers.Text = "";

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

        private void cdrBtnReset_Click(object sender, EventArgs e)
        {

            cdrComboBox1.DroppedDown = false;
            cdrComboBox1.SelectedItem = "Select";
            cdrComboBox1.SelectedIndex = 0;
            cdrComboBox1.Focus();
            cdrtxt1.Text = "";

            cdrComboBox2.DroppedDown = false;
            cdrComboBox2.SelectedItem = "Select";
            cdrComboBox2.SelectedIndex = 0;
            cdrComboBox2.Focus();
            cdrtxt2.Text = "";

            cdrComboBox3.DroppedDown = false;
            cdrComboBox3.SelectedItem = "Select";
            cdrComboBox3.SelectedIndex = 0;
            cdrComboBox3.Focus();
            cdrtxt3.Text = "";

            cdrComboBox4.DroppedDown = false;
            cdrComboBox4.SelectedItem = "Select";
            cdrComboBox4.SelectedIndex = 0;
            cdrComboBox4.Focus();
            cdrtxt4.Text = "";

            cdrComboBox5.DroppedDown = false;
            cdrComboBox5.SelectedItem = "Select";
            cdrComboBox5.SelectedIndex = 0;
            cdrComboBox5.Focus();
            cdrtxt5.Text = "";

            cdrComboBox6.DroppedDown = false;
            cdrComboBox6.SelectedItem = "Select";
            cdrComboBox6.SelectedIndex = 0;
            cdrComboBox6.Focus();
            cdrtxt6.Text = "";

            cdrComboBox7.DroppedDown = false;
            cdrComboBox7.SelectedItem = "Select";
            cdrComboBox7.SelectedIndex = 0;
            cdrComboBox7.Focus();
            cdrtxt7.Text = "";

            cdrComboBox8.DroppedDown = false;
            cdrComboBox8.SelectedItem = "Select";
            cdrComboBox8.SelectedIndex = 0;
            cdrComboBox8.Focus();
            cdrtxt8.Text = "";

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
    }
}