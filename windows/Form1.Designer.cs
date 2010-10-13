namespace TweetWall
{
    partial class Form1
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
            this.tweetList = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.apple2srcImage = new System.Windows.Forms.PictureBox();
            this.apple2LoresImage = new System.Windows.Forms.PictureBox();
            this.apple2HiresImage = new System.Windows.Forms.PictureBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.Source = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.apple2srcImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.apple2LoresImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.apple2HiresImage)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tweetList
            // 
            this.tweetList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.tweetList.FullRowSelect = true;
            this.tweetList.Location = new System.Drawing.Point(21, 185);
            this.tweetList.Name = "tweetList";
            this.tweetList.Size = new System.Drawing.Size(380, 224);
            this.tweetList.TabIndex = 0;
            this.tweetList.UseCompatibleStateImageBehavior = false;
            this.tweetList.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "From";
            this.columnHeader1.Width = 113;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Message";
            this.columnHeader2.Width = 250;
            // 
            // apple2srcImage
            // 
            this.apple2srcImage.Location = new System.Drawing.Point(8, 19);
            this.apple2srcImage.Name = "apple2srcImage";
            this.apple2srcImage.Size = new System.Drawing.Size(120, 120);
            this.apple2srcImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.apple2srcImage.TabIndex = 3;
            this.apple2srcImage.TabStop = false;
            // 
            // apple2LoresImage
            // 
            this.apple2LoresImage.Location = new System.Drawing.Point(134, 19);
            this.apple2LoresImage.Name = "apple2LoresImage";
            this.apple2LoresImage.Size = new System.Drawing.Size(120, 120);
            this.apple2LoresImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.apple2LoresImage.TabIndex = 4;
            this.apple2LoresImage.TabStop = false;
            // 
            // apple2HiresImage
            // 
            this.apple2HiresImage.Location = new System.Drawing.Point(260, 19);
            this.apple2HiresImage.Name = "apple2HiresImage";
            this.apple2HiresImage.Size = new System.Drawing.Size(120, 120);
            this.apple2HiresImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.apple2HiresImage.TabIndex = 8;
            this.apple2HiresImage.TabStop = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.Source);
            this.groupBox1.Controls.Add(this.apple2srcImage);
            this.groupBox1.Controls.Add(this.apple2LoresImage);
            this.groupBox1.Controls.Add(this.apple2HiresImage);
            this.groupBox1.Location = new System.Drawing.Point(13, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(405, 415);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Apple //t";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(296, 142);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(40, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "HIRES";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(170, 141);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "LORES";
            // 
            // Source
            // 
            this.Source.AutoSize = true;
            this.Source.Location = new System.Drawing.Point(52, 142);
            this.Source.Name = "Source";
            this.Source.Size = new System.Drawing.Size(41, 13);
            this.Source.TabIndex = 9;
            this.Source.Text = "Source";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(435, 439);
            this.Controls.Add(this.tweetList);
            this.Controls.Add(this.groupBox1);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.apple2srcImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.apple2LoresImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.apple2HiresImage)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView tweetList;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.PictureBox apple2srcImage;
        private System.Windows.Forms.PictureBox apple2LoresImage;
        private System.Windows.Forms.PictureBox apple2HiresImage;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label Source;
    }
}

