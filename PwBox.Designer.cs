namespace API_TestSuite_GUI
{
    partial class PwBox
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

        public string getUsr
        {
            get { return usrBox.Text; }
        }

        public string getPswd
        {
            get { return pswdBox.Text; }
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.usrBox = new System.Windows.Forms.TextBox();
            this.pswdBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.OKbutton = new System.Windows.Forms.Button();
            this.notOKButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // usrBox
            // 
            this.usrBox.Location = new System.Drawing.Point(80, 25);
            this.usrBox.Name = "usrBox";
            this.usrBox.Size = new System.Drawing.Size(100, 20);
            this.usrBox.TabIndex = 0;
            this.usrBox.Text = "admin";
            // 
            // pswdBox
            // 
            this.pswdBox.Location = new System.Drawing.Point(80, 51);
            this.pswdBox.Name = "pswdBox";
            this.pswdBox.PasswordChar = '*';
            this.pswdBox.Size = new System.Drawing.Size(100, 20);
            this.pswdBox.TabIndex = 1;
            this.pswdBox.Text = "admin";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Username: ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Password: ";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(17, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(161, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Please enter AQ user credentials";
            // 
            // OKbutton
            // 
            this.OKbutton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKbutton.Location = new System.Drawing.Point(15, 82);
            this.OKbutton.Name = "OKbutton";
            this.OKbutton.Size = new System.Drawing.Size(75, 23);
            this.OKbutton.TabIndex = 5;
            this.OKbutton.Text = "OK";
            this.OKbutton.UseVisualStyleBackColor = true;
            this.OKbutton.Click += new System.EventHandler(this.OKbutton_Click);
            // 
            // notOKButton
            // 
            this.notOKButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.notOKButton.Location = new System.Drawing.Point(102, 82);
            this.notOKButton.Name = "notOKButton";
            this.notOKButton.Size = new System.Drawing.Size(75, 23);
            this.notOKButton.TabIndex = 6;
            this.notOKButton.Text = "Cancel";
            this.notOKButton.UseVisualStyleBackColor = true;
            // 
            // PwBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(192, 117);
            this.ControlBox = false;
            this.Controls.Add(this.notOKButton);
            this.Controls.Add(this.OKbutton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pswdBox);
            this.Controls.Add(this.usrBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PwBox";
            this.Text = "AQUARIUS login";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox usrBox;
        private System.Windows.Forms.TextBox pswdBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        public System.Windows.Forms.Button OKbutton;
        private System.Windows.Forms.Button notOKButton;
    }
}