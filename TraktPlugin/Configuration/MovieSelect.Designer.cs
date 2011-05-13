namespace TraktPlugin
{
    partial class MovieSelect
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.checkedListBoxMovies = new System.Windows.Forms.CheckedListBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.lbSelectMoviesLabel = new System.Windows.Forms.Label();
            this.btnFolderRestrictions = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // checkedListBoxMovies
            // 
            this.checkedListBoxMovies.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.checkedListBoxMovies.FormattingEnabled = true;
            this.checkedListBoxMovies.Location = new System.Drawing.Point(12, 27);
            this.checkedListBoxMovies.Name = "checkedListBoxMovies";
            this.checkedListBoxMovies.ScrollAlwaysVisible = true;
            this.checkedListBoxMovies.Size = new System.Drawing.Size(269, 304);
            this.checkedListBoxMovies.TabIndex = 0;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(206, 337);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 1;
            this.btnOk.Text = "&OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // lbSelectMoviesLabel
            // 
            this.lbSelectMoviesLabel.AutoSize = true;
            this.lbSelectMoviesLabel.Location = new System.Drawing.Point(12, 9);
            this.lbSelectMoviesLabel.Name = "lbSelectMoviesLabel";
            this.lbSelectMoviesLabel.Size = new System.Drawing.Size(173, 13);
            this.lbSelectMoviesLabel.TabIndex = 2;
            this.lbSelectMoviesLabel.Text = "Select Movies to Ignore from Trakt:";
            // 
            // btnFolderRestrictions
            // 
            this.btnFolderRestrictions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnFolderRestrictions.Location = new System.Drawing.Point(12, 337);
            this.btnFolderRestrictions.Name = "btnFolderRestrictions";
            this.btnFolderRestrictions.Size = new System.Drawing.Size(137, 23);
            this.btnFolderRestrictions.TabIndex = 3;
            this.btnFolderRestrictions.Text = "&Folder Restrictions...";
            this.btnFolderRestrictions.UseVisualStyleBackColor = true;
            this.btnFolderRestrictions.Click += new System.EventHandler(this.btnFolderRestrictions_Click);
            // 
            // MovieSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(293, 368);
            this.Controls.Add(this.btnFolderRestrictions);
            this.Controls.Add(this.lbSelectMoviesLabel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.checkedListBoxMovies);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(309, 406);
            this.Name = "MovieSelect";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add/Remove Movies";
            this.Load += new System.EventHandler(this.MovieSelect_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckedListBox checkedListBoxMovies;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label lbSelectMoviesLabel;
        private System.Windows.Forms.Button btnFolderRestrictions;
    }
}