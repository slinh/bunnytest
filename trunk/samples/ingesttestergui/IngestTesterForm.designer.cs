namespace IngestTesterGui
{
    partial class IngestTesterForm
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
            this.components = new System.ComponentModel.Container();
            this.mAppCredentialsGroupbox = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.mPasswordText = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.mUsernameText = new System.Windows.Forms.TextBox();
            this.mIngestListListbox = new System.Windows.Forms.ListBox();
            this.mStreamTasksTimer = new System.Windows.Forms.Timer(this.components);
            this.mStartIngestTestButton = new System.Windows.Forms.Button();
            this.mSkipIngestServerButton = new System.Windows.Forms.Button();
            this.mCancelIngestTestButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.mIngestTestStatusText = new System.Windows.Forms.Label();
            this.mStatusLabel = new System.Windows.Forms.Label();
            this.mBestServerLabel = new System.Windows.Forms.Label();
            this.mAppCredentialsGroupbox.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // mAppCredentialsGroupbox
            // 
            this.mAppCredentialsGroupbox.Controls.Add(this.label3);
            this.mAppCredentialsGroupbox.Controls.Add(this.mPasswordText);
            this.mAppCredentialsGroupbox.Controls.Add(this.label2);
            this.mAppCredentialsGroupbox.Controls.Add(this.mUsernameText);
            this.mAppCredentialsGroupbox.Location = new System.Drawing.Point(4, 21);
            this.mAppCredentialsGroupbox.Name = "mAppCredentialsGroupbox";
            this.mAppCredentialsGroupbox.Size = new System.Drawing.Size(524, 71);
            this.mAppCredentialsGroupbox.TabIndex = 17;
            this.mAppCredentialsGroupbox.TabStop = false;
            this.mAppCredentialsGroupbox.Text = "Twitch App Credentials";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(267, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(88, 13);
            this.label3.TabIndex = 34;
            this.label3.Text = "Twitch Password";
            // 
            // mPasswordText
            // 
            this.mPasswordText.Location = new System.Drawing.Point(267, 32);
            this.mPasswordText.Name = "mPasswordText";
            this.mPasswordText.PasswordChar = '*';
            this.mPasswordText.Size = new System.Drawing.Size(244, 20);
            this.mPasswordText.TabIndex = 33;
            this.mPasswordText.Text = "alecttv";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 17);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(90, 13);
            this.label2.TabIndex = 32;
            this.label2.Text = "Twitch Username";
            // 
            // mUsernameText
            // 
            this.mUsernameText.Location = new System.Drawing.Point(15, 32);
            this.mUsernameText.Name = "mUsernameText";
            this.mUsernameText.Size = new System.Drawing.Size(246, 20);
            this.mUsernameText.TabIndex = 31;
            this.mUsernameText.Text = "alecistestingtwitch";
            // 
            // mIngestListListbox
            // 
            this.mIngestListListbox.FormattingEnabled = true;
            this.mIngestListListbox.Location = new System.Drawing.Point(17, 49);
            this.mIngestListListbox.Name = "mIngestListListbox";
            this.mIngestListListbox.Size = new System.Drawing.Size(496, 225);
            this.mIngestListListbox.TabIndex = 26;
            // 
            // mStreamTasksTimer
            // 
            this.mStreamTasksTimer.Enabled = true;
            this.mStreamTasksTimer.Interval = 1;
            this.mStreamTasksTimer.Tick += new System.EventHandler(this.mStreamTasksTimer_Tick);
            // 
            // mStartIngestTestButton
            // 
            this.mStartIngestTestButton.Location = new System.Drawing.Point(17, 19);
            this.mStartIngestTestButton.Name = "mStartIngestTestButton";
            this.mStartIngestTestButton.Size = new System.Drawing.Size(123, 23);
            this.mStartIngestTestButton.TabIndex = 22;
            this.mStartIngestTestButton.Text = "Start Ingest Test";
            this.mStartIngestTestButton.UseVisualStyleBackColor = true;
            this.mStartIngestTestButton.Click += new System.EventHandler(this.mStartIngestTestButton_Click);
            // 
            // mSkipIngestServerButton
            // 
            this.mSkipIngestServerButton.Enabled = false;
            this.mSkipIngestServerButton.Location = new System.Drawing.Point(275, 19);
            this.mSkipIngestServerButton.Name = "mSkipIngestServerButton";
            this.mSkipIngestServerButton.Size = new System.Drawing.Size(122, 23);
            this.mSkipIngestServerButton.TabIndex = 24;
            this.mSkipIngestServerButton.Text = "Skip Current Server";
            this.mSkipIngestServerButton.UseVisualStyleBackColor = true;
            this.mSkipIngestServerButton.Click += new System.EventHandler(this.mSkipIngestServerButton_Click);
            // 
            // mCancelIngestTestButton
            // 
            this.mCancelIngestTestButton.Enabled = false;
            this.mCancelIngestTestButton.Location = new System.Drawing.Point(146, 20);
            this.mCancelIngestTestButton.Name = "mCancelIngestTestButton";
            this.mCancelIngestTestButton.Size = new System.Drawing.Size(123, 23);
            this.mCancelIngestTestButton.TabIndex = 23;
            this.mCancelIngestTestButton.Text = "Cancel Ingest Test";
            this.mCancelIngestTestButton.UseVisualStyleBackColor = true;
            this.mCancelIngestTestButton.Click += new System.EventHandler(this.mCancelIngestTestButton_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.mIngestTestStatusText);
            this.groupBox1.Controls.Add(this.mIngestListListbox);
            this.groupBox1.Controls.Add(this.mCancelIngestTestButton);
            this.groupBox1.Controls.Add(this.mSkipIngestServerButton);
            this.groupBox1.Controls.Add(this.mStartIngestTestButton);
            this.groupBox1.Location = new System.Drawing.Point(4, 98);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(524, 298);
            this.groupBox1.TabIndex = 27;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Ingest Tester";
            // 
            // mIngestTestStatusText
            // 
            this.mIngestTestStatusText.BackColor = System.Drawing.SystemColors.Control;
            this.mIngestTestStatusText.Location = new System.Drawing.Point(17, 277);
            this.mIngestTestStatusText.Name = "mIngestTestStatusText";
            this.mIngestTestStatusText.Size = new System.Drawing.Size(494, 18);
            this.mIngestTestStatusText.TabIndex = 27;
            // 
            // mStatusLabel
            // 
            this.mStatusLabel.Location = new System.Drawing.Point(4, 3);
            this.mStatusLabel.Name = "mStatusLabel";
            this.mStatusLabel.Size = new System.Drawing.Size(524, 15);
            this.mStatusLabel.TabIndex = 28;
            this.mStatusLabel.Text = "Status - Uninitialized";
            this.mStatusLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // mBestServerLabel
            // 
            this.mBestServerLabel.AutoSize = true;
            this.mBestServerLabel.Location = new System.Drawing.Point(12, 410);
            this.mBestServerLabel.Name = "mBestServerLabel";
            this.mBestServerLabel.Size = new System.Drawing.Size(0, 13);
            this.mBestServerLabel.TabIndex = 29;
            // 
            // IngestTesterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(535, 429);
            this.Controls.Add(this.mBestServerLabel);
            this.Controls.Add(this.mStatusLabel);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.mAppCredentialsGroupbox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "IngestTesterForm";
            this.Text = "Twitch Ingest Tester";
            this.mAppCredentialsGroupbox.ResumeLayout(false);
            this.mAppCredentialsGroupbox.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox mAppCredentialsGroupbox;
        private System.Windows.Forms.ListBox mIngestListListbox;
        private System.Windows.Forms.Timer mStreamTasksTimer;
        private System.Windows.Forms.Button mStartIngestTestButton;
        private System.Windows.Forms.Button mSkipIngestServerButton;
        private System.Windows.Forms.Button mCancelIngestTestButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label mStatusLabel;
        private System.Windows.Forms.Label mBestServerLabel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox mPasswordText;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox mUsernameText;
        private System.Windows.Forms.Label mIngestTestStatusText;

    }
}

