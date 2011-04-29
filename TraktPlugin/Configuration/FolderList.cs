using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TraktPlugin
{
    public partial class FolderList : Form
    {
        List<string> _folders = new List<string>();
        public List<string> Folders 
        { 
            get { return _folders; }
            set { _folders = value; } 
        }

        public FolderList()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            // Load blocked folder list
            foreach (string folder in Folders)
            {
                listFolders.Items.Add(folder);
            }

            base.OnLoad(e);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddPathPopup pathPopup = new AddPathPopup();
            DialogResult result = pathPopup.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                string path = pathPopup.SelectedPath;

                if (!Directory.Exists(path))
                {
                    string message = "The path entered does not exist!";
                    MessageBox.Show(message, "Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // add path to list
                if (!listFolders.Items.Contains(path))
                    listFolders.Items.Add(path);              
            }

        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            // remove item from list
            if (listFolders.SelectedIndex < 0) return;
            listFolders.Items.RemoveAt(listFolders.SelectedIndex);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // save blocked folder list
            Folders.Clear();
            foreach (string folder in listFolders.Items)
            {
                Folders.Add(folder);
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
