using System;
using System.Windows.Forms;

namespace TraktPlugin
{
    public partial class AddPathPopup : Form
    {
        public string SelectedPath { get; set; }

        public AddPathPopup()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.SelectedPath = SelectedPath;
            folderDialog.Description = "Select or create a New Folder from the list below:";
            DialogResult result = folderDialog.ShowDialog();
            if (result == DialogResult.OK)
                pathTextBox.Text = folderDialog.SelectedPath;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            SelectedPath = pathTextBox.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
