namespace WinformsSample
{
    partial class LoginForm
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
            this.mOkButton = new System.Windows.Forms.Button();
            this.mCancelButton = new System.Windows.Forms.Button();
            this.mUserNameText = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.mPasswordLabel = new System.Windows.Forms.Label();
            this.mPasswordText = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // mOkButton
            // 
            this.mOkButton.Location = new System.Drawing.Point(220, 107);
            this.mOkButton.Name = "mOkButton";
            this.mOkButton.Size = new System.Drawing.Size(75, 23);
            this.mOkButton.TabIndex = 0;
            this.mOkButton.Text = "OK";
            this.mOkButton.UseVisualStyleBackColor = true;
            this.mOkButton.Click += new System.EventHandler(this.mOkButton_Click);
            // 
            // mCancelButton
            // 
            this.mCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.mCancelButton.Location = new System.Drawing.Point(139, 107);
            this.mCancelButton.Name = "mCancelButton";
            this.mCancelButton.Size = new System.Drawing.Size(75, 23);
            this.mCancelButton.TabIndex = 1;
            this.mCancelButton.Text = "Cancel";
            this.mCancelButton.UseVisualStyleBackColor = true;
            this.mCancelButton.Click += new System.EventHandler(this.mCancelButton_Click);
            // 
            // mUserNameText
            // 
            this.mUserNameText.Location = new System.Drawing.Point(12, 26);
            this.mUserNameText.Name = "mUserNameText";
            this.mUserNameText.Size = new System.Drawing.Size(283, 20);
            this.mUserNameText.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "User Name";
            // 
            // mPasswordLabel
            // 
            this.mPasswordLabel.AutoSize = true;
            this.mPasswordLabel.Location = new System.Drawing.Point(12, 50);
            this.mPasswordLabel.Name = "mPasswordLabel";
            this.mPasswordLabel.Size = new System.Drawing.Size(53, 13);
            this.mPasswordLabel.TabIndex = 5;
            this.mPasswordLabel.Text = "Password";
            // 
            // mPasswordText
            // 
            this.mPasswordText.Location = new System.Drawing.Point(12, 69);
            this.mPasswordText.Name = "mPasswordText";
            this.mPasswordText.Size = new System.Drawing.Size(283, 20);
            this.mPasswordText.TabIndex = 1;
            // 
            // LoginForm
            // 
            this.AcceptButton = this.mOkButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.mCancelButton;
            this.ClientSize = new System.Drawing.Size(307, 142);
            this.Controls.Add(this.mPasswordLabel);
            this.Controls.Add(this.mPasswordText);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.mUserNameText);
            this.Controls.Add(this.mCancelButton);
            this.Controls.Add(this.mOkButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Login";
            this.Load += new System.EventHandler(this.LoginForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button mOkButton;
        private System.Windows.Forms.Button mCancelButton;
        private System.Windows.Forms.TextBox mUserNameText;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label mPasswordLabel;
        private System.Windows.Forms.TextBox mPasswordText;
    }
}