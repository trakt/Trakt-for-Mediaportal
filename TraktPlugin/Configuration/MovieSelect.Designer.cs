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
            this.SuspendLayout();
            // 
            // checkedListBoxMovies
            // 
            this.checkedListBoxMovies.FormattingEnabled = true;
            this.checkedListBoxMovies.Location = new System.Drawing.Point(12, 12);
            this.checkedListBoxMovies.Name = "checkedListBoxMovies";
            this.checkedListBoxMovies.ScrollAlwaysVisible = true;
            this.checkedListBoxMovies.Size = new System.Drawing.Size(269, 334);
            this.checkedListBoxMovies.TabIndex = 0;
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(206, 352);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 1;
            this.btnOk.Text = "&OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // MovieSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(293, 384);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.checkedListBoxMovies);
            this.Name = "MovieSelect";
            this.ShowIcon = false;
            this.Text = "Add/Remove Movies";
            this.Load += new System.EventHandler(this.MovieSelect_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckedListBox checkedListBoxMovies;
        private System.Windows.Forms.Button btnOk;
    }
}