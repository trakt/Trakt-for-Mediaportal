namespace TraktPlugin
{
    partial class SeriesSelect
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
            this.checkedListBoxSeries = new System.Windows.Forms.CheckedListBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.chkBoxToggleAll = new System.Windows.Forms.CheckBox();
            this.labelSeriesSelected = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // checkedListBoxSeries
            // 
            this.checkedListBoxSeries.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.checkedListBoxSeries.FormattingEnabled = true;
            this.checkedListBoxSeries.Location = new System.Drawing.Point(13, 28);
            this.checkedListBoxSeries.Name = "checkedListBoxSeries";
            this.checkedListBoxSeries.ScrollAlwaysVisible = true;
            this.checkedListBoxSeries.Size = new System.Drawing.Size(272, 304);
            this.checkedListBoxSeries.TabIndex = 0;
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(210, 340);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 1;
            this.buttonOK.Text = "&OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // chkBoxToggleAll
            // 
            this.chkBoxToggleAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkBoxToggleAll.AutoSize = true;
            this.chkBoxToggleAll.Location = new System.Drawing.Point(13, 344);
            this.chkBoxToggleAll.Name = "chkBoxToggleAll";
            this.chkBoxToggleAll.Size = new System.Drawing.Size(70, 17);
            this.chkBoxToggleAll.TabIndex = 3;
            this.chkBoxToggleAll.Text = "Select &All";
            this.chkBoxToggleAll.UseVisualStyleBackColor = true;
            this.chkBoxToggleAll.CheckedChanged += new System.EventHandler(this.chkBoxToggleAll_CheckedChanged);
            // 
            // labelSeriesSelected
            // 
            this.labelSeriesSelected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.labelSeriesSelected.AutoSize = true;
            this.labelSeriesSelected.Location = new System.Drawing.Point(98, 345);
            this.labelSeriesSelected.Name = "labelSeriesSelected";
            this.labelSeriesSelected.Size = new System.Drawing.Size(93, 13);
            this.labelSeriesSelected.TabIndex = 2;
            this.labelSeriesSelected.Text = "0: Series Selected";
            this.labelSeriesSelected.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(161, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Select series to ignore from trakt:";
            // 
            // SeriesSelect
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(297, 385);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chkBoxToggleAll);
            this.Controls.Add(this.labelSeriesSelected);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.checkedListBoxSeries);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(313, 423);
            this.Name = "SeriesSelect";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add/Remove Series";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckedListBox checkedListBoxSeries;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.CheckBox chkBoxToggleAll;
        private System.Windows.Forms.Label labelSeriesSelected;
        private System.Windows.Forms.Label label1;
    }
}