namespace WinformsSample
{
    partial class SampleForm
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
            this.mStreamTasksTimer = new System.Windows.Forms.Timer(this.components);
            this.mSubmitFrameTimer = new System.Windows.Forms.Timer(this.components);
            this.mChatTimer = new System.Windows.Forms.Timer(this.components);
            this.ChatTab = new System.Windows.Forms.TabPage();
            this.mChatMessagesGroupbox = new System.Windows.Forms.GroupBox();
            this.mChatSendButton = new System.Windows.Forms.Button();
            this.mChatInputTextbox = new System.Windows.Forms.TextBox();
            this.mChatMessagesTextbox = new System.Windows.Forms.TextBox();
            this.mChatUsersListbox = new System.Windows.Forms.ListBox();
            this.mChatConnectionGroupbox = new System.Windows.Forms.GroupBox();
            this.label9 = new System.Windows.Forms.Label();
            this.mChatDisconnectButton = new System.Windows.Forms.Button();
            this.mChatChannelText = new System.Windows.Forms.TextBox();
            this.mChatConnectAnonymousButton = new System.Windows.Forms.Button();
            this.mChatConnectButton = new System.Windows.Forms.Button();
            this.mChatStatusLabel = new System.Windows.Forms.Label();
            this.StreamTab = new System.Windows.Forms.TabPage();
            this.mEnableAudioCheckbox = new System.Windows.Forms.CheckBox();
            this.mLoginGroupbox = new System.Windows.Forms.GroupBox();
            this.mSetAuthTokenButton = new System.Windows.Forms.Button();
            this.mRequestAuthTokenButton = new System.Windows.Forms.Button();
            this.mAdvancedBroadcastGroupbox = new System.Windows.Forms.GroupBox();
            this.mStartButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.mFramesPerSecondSelector = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.mResolutionCombo = new System.Windows.Forms.ComboBox();
            this.mBroadcastInfoGroupbox = new System.Windows.Forms.GroupBox();
            this.mChannelNameText = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.mArchivingStateCheckbox = new System.Windows.Forms.CheckBox();
            this.mGameNameText = new System.Windows.Forms.TextBox();
            this.mStreamTitleText = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.mSetStreamInfoButton = new System.Windows.Forms.Button();
            this.mAudioGroupbox = new System.Windows.Forms.GroupBox();
            this.mSystemVolumeText = new System.Windows.Forms.TextBox();
            this.mMicVolumeText = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.mDecreaseSysVolumeButton = new System.Windows.Forms.Button();
            this.mIncreaseSysVolumeButton = new System.Windows.Forms.Button();
            this.mDecreaseMicVolumeButton = new System.Windows.Forms.Button();
            this.mIncreaseMicVolumeButton = new System.Windows.Forms.Button();
            this.mIngestTestingGroupbox = new System.Windows.Forms.GroupBox();
            this.mIngestListListbox = new System.Windows.Forms.ListBox();
            this.mSkipIngestServerButton = new System.Windows.Forms.Button();
            this.mIngestTestStatusText = new System.Windows.Forms.TextBox();
            this.mCancelIngestTestButton = new System.Windows.Forms.Button();
            this.mStartIngestTestButton = new System.Windows.Forms.Button();
            this.mGameNameListGroupbox = new System.Windows.Forms.GroupBox();
            this.mGameNameListTextbox = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.mGameNameListButton = new System.Windows.Forms.Button();
            this.mMetaDataGroup = new System.Windows.Forms.GroupBox();
            this.mSendEndSpanMetaDataButton = new System.Windows.Forms.Button();
            this.mSendStartSpanMetaDataButton = new System.Windows.Forms.Button();
            this.mSendActionMetaDataButton = new System.Windows.Forms.Button();
            this.mBroadcastControlsGroupbox = new System.Windows.Forms.GroupBox();
            this.mResumeButton = new System.Windows.Forms.Button();
            this.mStopButton = new System.Windows.Forms.Button();
            this.mRunCommercialButton = new System.Windows.Forms.Button();
            this.mPauseButton = new System.Windows.Forms.Button();
            this.mSimpleBroadcastGroupbox = new System.Windows.Forms.GroupBox();
            this.label14 = new System.Windows.Forms.Label();
            this.mAspectRatioCombo = new System.Windows.Forms.ComboBox();
            this.mMaxKbpsLabel = new System.Windows.Forms.Label();
            this.mMaxKbpsTrackbar = new System.Windows.Forms.TrackBar();
            this.label11 = new System.Windows.Forms.Label();
            this.mStartButtonRecommended = new System.Windows.Forms.Button();
            this.mInitializationGroupbox = new System.Windows.Forms.GroupBox();
            this.mClientSecret = new System.Windows.Forms.Label();
            this.mClientSecretText = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.mClientIdText = new System.Windows.Forms.TextBox();
            this.mShutdownButton = new System.Windows.Forms.Button();
            this.mInitButton = new System.Windows.Forms.Button();
            this.Tabs = new System.Windows.Forms.TabControl();
            this.mEmoticonModeCombobox = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.ChatTab.SuspendLayout();
            this.mChatMessagesGroupbox.SuspendLayout();
            this.mChatConnectionGroupbox.SuspendLayout();
            this.StreamTab.SuspendLayout();
            this.mLoginGroupbox.SuspendLayout();
            this.mAdvancedBroadcastGroupbox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mFramesPerSecondSelector)).BeginInit();
            this.mBroadcastInfoGroupbox.SuspendLayout();
            this.mAudioGroupbox.SuspendLayout();
            this.mIngestTestingGroupbox.SuspendLayout();
            this.mGameNameListGroupbox.SuspendLayout();
            this.mMetaDataGroup.SuspendLayout();
            this.mBroadcastControlsGroupbox.SuspendLayout();
            this.mSimpleBroadcastGroupbox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mMaxKbpsTrackbar)).BeginInit();
            this.mInitializationGroupbox.SuspendLayout();
            this.Tabs.SuspendLayout();
            this.SuspendLayout();
            // 
            // mStreamTasksTimer
            // 
            this.mStreamTasksTimer.Enabled = true;
            this.mStreamTasksTimer.Interval = 1;
            this.mStreamTasksTimer.Tick += new System.EventHandler(this.StreamTasksTimer_Tick);
            // 
            // mSubmitFrameTimer
            // 
            this.mSubmitFrameTimer.Interval = 16;
            this.mSubmitFrameTimer.Tick += new System.EventHandler(this.mSubmitFrameTimer_Tick);
            // 
            // mChatTimer
            // 
            this.mChatTimer.Enabled = true;
            this.mChatTimer.Interval = 250;
            this.mChatTimer.Tick += new System.EventHandler(this.mChatTimer_Tick);
            // 
            // ChatTab
            // 
            this.ChatTab.Controls.Add(this.mChatMessagesGroupbox);
            this.ChatTab.Controls.Add(this.mChatConnectionGroupbox);
            this.ChatTab.Controls.Add(this.mChatStatusLabel);
            this.ChatTab.Location = new System.Drawing.Point(4, 22);
            this.ChatTab.Name = "ChatTab";
            this.ChatTab.Padding = new System.Windows.Forms.Padding(3);
            this.ChatTab.Size = new System.Drawing.Size(878, 714);
            this.ChatTab.TabIndex = 1;
            this.ChatTab.Text = "Chat";
            this.ChatTab.UseVisualStyleBackColor = true;
            // 
            // mChatMessagesGroupbox
            // 
            this.mChatMessagesGroupbox.Controls.Add(this.mChatSendButton);
            this.mChatMessagesGroupbox.Controls.Add(this.mChatInputTextbox);
            this.mChatMessagesGroupbox.Controls.Add(this.mChatMessagesTextbox);
            this.mChatMessagesGroupbox.Controls.Add(this.mChatUsersListbox);
            this.mChatMessagesGroupbox.Location = new System.Drawing.Point(14, 12);
            this.mChatMessagesGroupbox.Name = "mChatMessagesGroupbox";
            this.mChatMessagesGroupbox.Size = new System.Drawing.Size(856, 491);
            this.mChatMessagesGroupbox.TabIndex = 13;
            this.mChatMessagesGroupbox.TabStop = false;
            this.mChatMessagesGroupbox.Text = "Chat Messages";
            // 
            // mChatSendButton
            // 
            this.mChatSendButton.Location = new System.Drawing.Point(546, 457);
            this.mChatSendButton.Name = "mChatSendButton";
            this.mChatSendButton.Size = new System.Drawing.Size(90, 23);
            this.mChatSendButton.TabIndex = 17;
            this.mChatSendButton.Text = "Send";
            this.mChatSendButton.UseVisualStyleBackColor = true;
            this.mChatSendButton.Click += new System.EventHandler(this.mChatSendButton_Click);
            // 
            // mChatInputTextbox
            // 
            this.mChatInputTextbox.Location = new System.Drawing.Point(6, 459);
            this.mChatInputTextbox.Name = "mChatInputTextbox";
            this.mChatInputTextbox.Size = new System.Drawing.Size(534, 20);
            this.mChatInputTextbox.TabIndex = 16;
            // 
            // mChatMessagesTextbox
            // 
            this.mChatMessagesTextbox.Location = new System.Drawing.Point(6, 19);
            this.mChatMessagesTextbox.Multiline = true;
            this.mChatMessagesTextbox.Name = "mChatMessagesTextbox";
            this.mChatMessagesTextbox.ReadOnly = true;
            this.mChatMessagesTextbox.Size = new System.Drawing.Size(630, 433);
            this.mChatMessagesTextbox.TabIndex = 14;
            // 
            // mChatUsersListbox
            // 
            this.mChatUsersListbox.FormattingEnabled = true;
            this.mChatUsersListbox.Location = new System.Drawing.Point(642, 19);
            this.mChatUsersListbox.Name = "mChatUsersListbox";
            this.mChatUsersListbox.Size = new System.Drawing.Size(208, 433);
            this.mChatUsersListbox.TabIndex = 15;
            // 
            // mChatConnectionGroupbox
            // 
            this.mChatConnectionGroupbox.Controls.Add(this.label4);
            this.mChatConnectionGroupbox.Controls.Add(this.mEmoticonModeCombobox);
            this.mChatConnectionGroupbox.Controls.Add(this.label9);
            this.mChatConnectionGroupbox.Controls.Add(this.mChatDisconnectButton);
            this.mChatConnectionGroupbox.Controls.Add(this.mChatChannelText);
            this.mChatConnectionGroupbox.Controls.Add(this.mChatConnectAnonymousButton);
            this.mChatConnectionGroupbox.Controls.Add(this.mChatConnectButton);
            this.mChatConnectionGroupbox.Location = new System.Drawing.Point(14, 509);
            this.mChatConnectionGroupbox.Name = "mChatConnectionGroupbox";
            this.mChatConnectionGroupbox.Size = new System.Drawing.Size(856, 109);
            this.mChatConnectionGroupbox.TabIndex = 12;
            this.mChatConnectionGroupbox.TabStop = false;
            this.mChatConnectionGroupbox.Text = "Chat Connection";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(130, 29);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(46, 13);
            this.label9.TabIndex = 14;
            this.label9.Text = "Channel";
            // 
            // mChatDisconnectButton
            // 
            this.mChatDisconnectButton.Location = new System.Drawing.Point(6, 77);
            this.mChatDisconnectButton.Name = "mChatDisconnectButton";
            this.mChatDisconnectButton.Size = new System.Drawing.Size(118, 23);
            this.mChatDisconnectButton.TabIndex = 13;
            this.mChatDisconnectButton.Text = "Disconnect";
            this.mChatDisconnectButton.UseVisualStyleBackColor = true;
            this.mChatDisconnectButton.Click += new System.EventHandler(this.mChatDisconnectButton_Click);
            // 
            // mChatChannelText
            // 
            this.mChatChannelText.Location = new System.Drawing.Point(129, 48);
            this.mChatChannelText.Name = "mChatChannelText";
            this.mChatChannelText.Size = new System.Drawing.Size(181, 20);
            this.mChatChannelText.TabIndex = 12;
            // 
            // mChatConnectAnonymousButton
            // 
            this.mChatConnectAnonymousButton.Location = new System.Drawing.Point(6, 48);
            this.mChatConnectAnonymousButton.Name = "mChatConnectAnonymousButton";
            this.mChatConnectAnonymousButton.Size = new System.Drawing.Size(118, 23);
            this.mChatConnectAnonymousButton.TabIndex = 11;
            this.mChatConnectAnonymousButton.Text = "Connect Anonymous";
            this.mChatConnectAnonymousButton.UseVisualStyleBackColor = true;
            this.mChatConnectAnonymousButton.Click += new System.EventHandler(this.mChatConnectAnonymous_Click);
            // 
            // mChatConnectButton
            // 
            this.mChatConnectButton.Location = new System.Drawing.Point(6, 19);
            this.mChatConnectButton.Name = "mChatConnectButton";
            this.mChatConnectButton.Size = new System.Drawing.Size(118, 23);
            this.mChatConnectButton.TabIndex = 10;
            this.mChatConnectButton.Text = "Connect";
            this.mChatConnectButton.UseVisualStyleBackColor = true;
            this.mChatConnectButton.Click += new System.EventHandler(this.mChatConnectButton_Click);
            // 
            // mChatStatusLabel
            // 
            this.mChatStatusLabel.AutoSize = true;
            this.mChatStatusLabel.Location = new System.Drawing.Point(3, 726);
            this.mChatStatusLabel.Name = "mChatStatusLabel";
            this.mChatStatusLabel.Size = new System.Drawing.Size(37, 13);
            this.mChatStatusLabel.TabIndex = 10;
            this.mChatStatusLabel.Text = "Status";
            // 
            // StreamTab
            // 
            this.StreamTab.Controls.Add(this.mEnableAudioCheckbox);
            this.StreamTab.Controls.Add(this.mLoginGroupbox);
            this.StreamTab.Controls.Add(this.mAdvancedBroadcastGroupbox);
            this.StreamTab.Controls.Add(this.mBroadcastInfoGroupbox);
            this.StreamTab.Controls.Add(this.mAudioGroupbox);
            this.StreamTab.Controls.Add(this.mIngestTestingGroupbox);
            this.StreamTab.Controls.Add(this.mGameNameListGroupbox);
            this.StreamTab.Controls.Add(this.mMetaDataGroup);
            this.StreamTab.Controls.Add(this.mBroadcastControlsGroupbox);
            this.StreamTab.Controls.Add(this.mSimpleBroadcastGroupbox);
            this.StreamTab.Controls.Add(this.mInitializationGroupbox);
            this.StreamTab.Location = new System.Drawing.Point(4, 22);
            this.StreamTab.Name = "StreamTab";
            this.StreamTab.Padding = new System.Windows.Forms.Padding(3);
            this.StreamTab.Size = new System.Drawing.Size(878, 714);
            this.StreamTab.TabIndex = 0;
            this.StreamTab.Text = "Stream";
            this.StreamTab.UseVisualStyleBackColor = true;
            // 
            // mEnableAudioCheckbox
            // 
            this.mEnableAudioCheckbox.AutoSize = true;
            this.mEnableAudioCheckbox.Checked = true;
            this.mEnableAudioCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.mEnableAudioCheckbox.Location = new System.Drawing.Point(182, 291);
            this.mEnableAudioCheckbox.Name = "mEnableAudioCheckbox";
            this.mEnableAudioCheckbox.Size = new System.Drawing.Size(89, 17);
            this.mEnableAudioCheckbox.TabIndex = 65;
            this.mEnableAudioCheckbox.Text = "Enable Audio";
            this.mEnableAudioCheckbox.UseVisualStyleBackColor = true;
            // 
            // mLoginGroupbox
            // 
            this.mLoginGroupbox.Controls.Add(this.mSetAuthTokenButton);
            this.mLoginGroupbox.Controls.Add(this.mRequestAuthTokenButton);
            this.mLoginGroupbox.Location = new System.Drawing.Point(375, 13);
            this.mLoginGroupbox.Name = "mLoginGroupbox";
            this.mLoginGroupbox.Size = new System.Drawing.Size(156, 113);
            this.mLoginGroupbox.TabIndex = 64;
            this.mLoginGroupbox.TabStop = false;
            this.mLoginGroupbox.Text = "Login";
            // 
            // mSetAuthTokenButton
            // 
            this.mSetAuthTokenButton.Location = new System.Drawing.Point(11, 52);
            this.mSetAuthTokenButton.Name = "mSetAuthTokenButton";
            this.mSetAuthTokenButton.Size = new System.Drawing.Size(133, 23);
            this.mSetAuthTokenButton.TabIndex = 28;
            this.mSetAuthTokenButton.Text = "Set Existing AuthToken";
            this.mSetAuthTokenButton.UseVisualStyleBackColor = true;
            this.mSetAuthTokenButton.Click += new System.EventHandler(this.mSetAuthTokenButton_Click);
            // 
            // mRequestAuthTokenButton
            // 
            this.mRequestAuthTokenButton.Location = new System.Drawing.Point(11, 22);
            this.mRequestAuthTokenButton.Name = "mRequestAuthTokenButton";
            this.mRequestAuthTokenButton.Size = new System.Drawing.Size(133, 23);
            this.mRequestAuthTokenButton.TabIndex = 27;
            this.mRequestAuthTokenButton.Text = "RequestAuthToken";
            this.mRequestAuthTokenButton.UseVisualStyleBackColor = true;
            this.mRequestAuthTokenButton.Click += new System.EventHandler(this.mRequestAuthTokenButton_Click);
            // 
            // mAdvancedBroadcastGroupbox
            // 
            this.mAdvancedBroadcastGroupbox.Controls.Add(this.mStartButton);
            this.mAdvancedBroadcastGroupbox.Controls.Add(this.label2);
            this.mAdvancedBroadcastGroupbox.Controls.Add(this.mFramesPerSecondSelector);
            this.mAdvancedBroadcastGroupbox.Controls.Add(this.label1);
            this.mAdvancedBroadcastGroupbox.Controls.Add(this.mResolutionCombo);
            this.mAdvancedBroadcastGroupbox.Location = new System.Drawing.Point(182, 131);
            this.mAdvancedBroadcastGroupbox.Name = "mAdvancedBroadcastGroupbox";
            this.mAdvancedBroadcastGroupbox.Size = new System.Drawing.Size(165, 141);
            this.mAdvancedBroadcastGroupbox.TabIndex = 63;
            this.mAdvancedBroadcastGroupbox.TabStop = false;
            this.mAdvancedBroadcastGroupbox.Text = "Advanced Broadcast Initiation";
            // 
            // mStartButton
            // 
            this.mStartButton.Location = new System.Drawing.Point(16, 108);
            this.mStartButton.Name = "mStartButton";
            this.mStartButton.Size = new System.Drawing.Size(133, 23);
            this.mStartButton.TabIndex = 25;
            this.mStartButton.Text = "Start";
            this.mStartButton.UseVisualStyleBackColor = true;
            this.mStartButton.Click += new System.EventHandler(this.mStartButton_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 66);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 13);
            this.label2.TabIndex = 24;
            this.label2.Text = "Frames Per Second";
            // 
            // mFramesPerSecondSelector
            // 
            this.mFramesPerSecondSelector.Location = new System.Drawing.Point(17, 82);
            this.mFramesPerSecondSelector.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.mFramesPerSecondSelector.Minimum = new decimal(new int[] {
            12,
            0,
            0,
            0});
            this.mFramesPerSecondSelector.Name = "mFramesPerSecondSelector";
            this.mFramesPerSecondSelector.Size = new System.Drawing.Size(56, 20);
            this.mFramesPerSecondSelector.TabIndex = 23;
            this.mFramesPerSecondSelector.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 22;
            this.label1.Text = "Resolution";
            // 
            // mResolutionCombo
            // 
            this.mResolutionCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.mResolutionCombo.FormattingEnabled = true;
            this.mResolutionCombo.Items.AddRange(new object[] {
            "640x480",
            "1024x1200",
            "1920x1080"});
            this.mResolutionCombo.Location = new System.Drawing.Point(14, 37);
            this.mResolutionCombo.Name = "mResolutionCombo";
            this.mResolutionCombo.Size = new System.Drawing.Size(136, 21);
            this.mResolutionCombo.TabIndex = 21;
            // 
            // mBroadcastInfoGroupbox
            // 
            this.mBroadcastInfoGroupbox.Controls.Add(this.mChannelNameText);
            this.mBroadcastInfoGroupbox.Controls.Add(this.label7);
            this.mBroadcastInfoGroupbox.Controls.Add(this.label8);
            this.mBroadcastInfoGroupbox.Controls.Add(this.mArchivingStateCheckbox);
            this.mBroadcastInfoGroupbox.Controls.Add(this.mGameNameText);
            this.mBroadcastInfoGroupbox.Controls.Add(this.mStreamTitleText);
            this.mBroadcastInfoGroupbox.Controls.Add(this.label6);
            this.mBroadcastInfoGroupbox.Controls.Add(this.label5);
            this.mBroadcastInfoGroupbox.Controls.Add(this.mSetStreamInfoButton);
            this.mBroadcastInfoGroupbox.Location = new System.Drawing.Point(564, 14);
            this.mBroadcastInfoGroupbox.Name = "mBroadcastInfoGroupbox";
            this.mBroadcastInfoGroupbox.Size = new System.Drawing.Size(293, 232);
            this.mBroadcastInfoGroupbox.TabIndex = 62;
            this.mBroadcastInfoGroupbox.TabStop = false;
            this.mBroadcastInfoGroupbox.Text = "Broadcast Info";
            // 
            // mChannelNameText
            // 
            this.mChannelNameText.Location = new System.Drawing.Point(14, 43);
            this.mChannelNameText.Name = "mChannelNameText";
            this.mChannelNameText.Size = new System.Drawing.Size(265, 20);
            this.mChannelNameText.TabIndex = 44;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(14, 23);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(46, 13);
            this.label7.TabIndex = 45;
            this.label7.Text = "Channel";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(17, 184);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(79, 13);
            this.label8.TabIndex = 43;
            this.label8.Text = "Archiving State";
            // 
            // mArchivingStateCheckbox
            // 
            this.mArchivingStateCheckbox.AutoSize = true;
            this.mArchivingStateCheckbox.Enabled = false;
            this.mArchivingStateCheckbox.Location = new System.Drawing.Point(20, 203);
            this.mArchivingStateCheckbox.Name = "mArchivingStateCheckbox";
            this.mArchivingStateCheckbox.Size = new System.Drawing.Size(98, 17);
            this.mArchivingStateCheckbox.TabIndex = 42;
            this.mArchivingStateCheckbox.Text = "Archiving State";
            this.mArchivingStateCheckbox.UseVisualStyleBackColor = true;
            // 
            // mGameNameText
            // 
            this.mGameNameText.Location = new System.Drawing.Point(14, 142);
            this.mGameNameText.Name = "mGameNameText";
            this.mGameNameText.Size = new System.Drawing.Size(265, 20);
            this.mGameNameText.TabIndex = 38;
            this.mGameNameText.Text = "Fun Game";
            // 
            // mStreamTitleText
            // 
            this.mStreamTitleText.Location = new System.Drawing.Point(14, 92);
            this.mStreamTitleText.Name = "mStreamTitleText";
            this.mStreamTitleText.Size = new System.Drawing.Size(265, 20);
            this.mStreamTitleText.TabIndex = 36;
            this.mStreamTitleText.Text = "My Wicked Stream!!!!!";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(14, 122);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(66, 13);
            this.label6.TabIndex = 39;
            this.label6.Text = "Game Name";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 72);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(63, 13);
            this.label5.TabIndex = 37;
            this.label5.Text = "Stream Title";
            // 
            // mSetStreamInfoButton
            // 
            this.mSetStreamInfoButton.Location = new System.Drawing.Point(170, 168);
            this.mSetStreamInfoButton.Name = "mSetStreamInfoButton";
            this.mSetStreamInfoButton.Size = new System.Drawing.Size(109, 23);
            this.mSetStreamInfoButton.TabIndex = 35;
            this.mSetStreamInfoButton.Text = "Set Stream Info";
            this.mSetStreamInfoButton.UseVisualStyleBackColor = true;
            this.mSetStreamInfoButton.Click += new System.EventHandler(this.mSetStreamInfoButton_Click);
            // 
            // mAudioGroupbox
            // 
            this.mAudioGroupbox.Controls.Add(this.mSystemVolumeText);
            this.mAudioGroupbox.Controls.Add(this.mMicVolumeText);
            this.mAudioGroupbox.Controls.Add(this.label13);
            this.mAudioGroupbox.Controls.Add(this.label12);
            this.mAudioGroupbox.Controls.Add(this.mDecreaseSysVolumeButton);
            this.mAudioGroupbox.Controls.Add(this.mIncreaseSysVolumeButton);
            this.mAudioGroupbox.Controls.Add(this.mDecreaseMicVolumeButton);
            this.mAudioGroupbox.Controls.Add(this.mIncreaseMicVolumeButton);
            this.mAudioGroupbox.Location = new System.Drawing.Point(564, 252);
            this.mAudioGroupbox.Name = "mAudioGroupbox";
            this.mAudioGroupbox.Size = new System.Drawing.Size(209, 103);
            this.mAudioGroupbox.TabIndex = 61;
            this.mAudioGroupbox.TabStop = false;
            this.mAudioGroupbox.Text = "Audio";
            // 
            // mSystemVolumeText
            // 
            this.mSystemVolumeText.Enabled = false;
            this.mSystemVolumeText.Location = new System.Drawing.Point(22, 67);
            this.mSystemVolumeText.Name = "mSystemVolumeText";
            this.mSystemVolumeText.Size = new System.Drawing.Size(100, 20);
            this.mSystemVolumeText.TabIndex = 60;
            // 
            // mMicVolumeText
            // 
            this.mMicVolumeText.Enabled = false;
            this.mMicVolumeText.Location = new System.Drawing.Point(22, 28);
            this.mMicVolumeText.Name = "mMicVolumeText";
            this.mMicVolumeText.Size = new System.Drawing.Size(100, 20);
            this.mMicVolumeText.TabIndex = 58;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(19, 51);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(79, 13);
            this.label13.TabIndex = 59;
            this.label13.Text = "System Volume";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(19, 12);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(62, 13);
            this.label12.TabIndex = 57;
            this.label12.Text = "Mic Volume";
            // 
            // mDecreaseSysVolumeButton
            // 
            this.mDecreaseSysVolumeButton.Location = new System.Drawing.Point(158, 64);
            this.mDecreaseSysVolumeButton.Name = "mDecreaseSysVolumeButton";
            this.mDecreaseSysVolumeButton.Size = new System.Drawing.Size(24, 24);
            this.mDecreaseSysVolumeButton.TabIndex = 56;
            this.mDecreaseSysVolumeButton.Text = "-";
            this.mDecreaseSysVolumeButton.UseVisualStyleBackColor = true;
            this.mDecreaseSysVolumeButton.Click += new System.EventHandler(this.mDecreaseSysVolumeButton_Click);
            // 
            // mIncreaseSysVolumeButton
            // 
            this.mIncreaseSysVolumeButton.Location = new System.Drawing.Point(128, 64);
            this.mIncreaseSysVolumeButton.Name = "mIncreaseSysVolumeButton";
            this.mIncreaseSysVolumeButton.Size = new System.Drawing.Size(24, 24);
            this.mIncreaseSysVolumeButton.TabIndex = 55;
            this.mIncreaseSysVolumeButton.Text = "+";
            this.mIncreaseSysVolumeButton.UseVisualStyleBackColor = true;
            this.mIncreaseSysVolumeButton.Click += new System.EventHandler(this.mIncreaseSysVolumeButton_Click);
            // 
            // mDecreaseMicVolumeButton
            // 
            this.mDecreaseMicVolumeButton.Location = new System.Drawing.Point(158, 26);
            this.mDecreaseMicVolumeButton.Name = "mDecreaseMicVolumeButton";
            this.mDecreaseMicVolumeButton.Size = new System.Drawing.Size(24, 24);
            this.mDecreaseMicVolumeButton.TabIndex = 54;
            this.mDecreaseMicVolumeButton.Text = "-";
            this.mDecreaseMicVolumeButton.UseVisualStyleBackColor = true;
            this.mDecreaseMicVolumeButton.Click += new System.EventHandler(this.mDecreaseMicVolumeButton_Click);
            // 
            // mIncreaseMicVolumeButton
            // 
            this.mIncreaseMicVolumeButton.Location = new System.Drawing.Point(128, 26);
            this.mIncreaseMicVolumeButton.Name = "mIncreaseMicVolumeButton";
            this.mIncreaseMicVolumeButton.Size = new System.Drawing.Size(24, 24);
            this.mIncreaseMicVolumeButton.TabIndex = 53;
            this.mIncreaseMicVolumeButton.Text = "+";
            this.mIncreaseMicVolumeButton.UseVisualStyleBackColor = true;
            this.mIncreaseMicVolumeButton.Click += new System.EventHandler(this.mIncreaseMicVolumeButton_Click);
            // 
            // mIngestTestingGroupbox
            // 
            this.mIngestTestingGroupbox.Controls.Add(this.mIngestListListbox);
            this.mIngestTestingGroupbox.Controls.Add(this.mSkipIngestServerButton);
            this.mIngestTestingGroupbox.Controls.Add(this.mIngestTestStatusText);
            this.mIngestTestingGroupbox.Controls.Add(this.mCancelIngestTestButton);
            this.mIngestTestingGroupbox.Controls.Add(this.mStartIngestTestButton);
            this.mIngestTestingGroupbox.Location = new System.Drawing.Point(8, 335);
            this.mIngestTestingGroupbox.Name = "mIngestTestingGroupbox";
            this.mIngestTestingGroupbox.Size = new System.Drawing.Size(467, 368);
            this.mIngestTestingGroupbox.TabIndex = 60;
            this.mIngestTestingGroupbox.TabStop = false;
            this.mIngestTestingGroupbox.Text = "Ingest Testing";
            // 
            // mIngestListListbox
            // 
            this.mIngestListListbox.FormattingEnabled = true;
            this.mIngestListListbox.Location = new System.Drawing.Point(18, 115);
            this.mIngestListListbox.Name = "mIngestListListbox";
            this.mIngestListListbox.Size = new System.Drawing.Size(426, 238);
            this.mIngestListListbox.TabIndex = 62;
            // 
            // mSkipIngestServerButton
            // 
            this.mSkipIngestServerButton.Location = new System.Drawing.Point(151, 61);
            this.mSkipIngestServerButton.Name = "mSkipIngestServerButton";
            this.mSkipIngestServerButton.Size = new System.Drawing.Size(127, 23);
            this.mSkipIngestServerButton.TabIndex = 61;
            this.mSkipIngestServerButton.Text = "Skip Server";
            this.mSkipIngestServerButton.UseVisualStyleBackColor = true;
            this.mSkipIngestServerButton.Click += new System.EventHandler(this.mSkipIngestServerButton_Click);
            // 
            // mIngestTestStatusText
            // 
            this.mIngestTestStatusText.Enabled = false;
            this.mIngestTestStatusText.Location = new System.Drawing.Point(18, 89);
            this.mIngestTestStatusText.Name = "mIngestTestStatusText";
            this.mIngestTestStatusText.Size = new System.Drawing.Size(426, 20);
            this.mIngestTestStatusText.TabIndex = 60;
            // 
            // mCancelIngestTestButton
            // 
            this.mCancelIngestTestButton.Location = new System.Drawing.Point(18, 61);
            this.mCancelIngestTestButton.Name = "mCancelIngestTestButton";
            this.mCancelIngestTestButton.Size = new System.Drawing.Size(127, 23);
            this.mCancelIngestTestButton.TabIndex = 59;
            this.mCancelIngestTestButton.Text = "Cancel Ingest Test";
            this.mCancelIngestTestButton.UseVisualStyleBackColor = true;
            this.mCancelIngestTestButton.Click += new System.EventHandler(this.mCancelIngestTestButton_Click);
            // 
            // mStartIngestTestButton
            // 
            this.mStartIngestTestButton.Location = new System.Drawing.Point(18, 32);
            this.mStartIngestTestButton.Name = "mStartIngestTestButton";
            this.mStartIngestTestButton.Size = new System.Drawing.Size(127, 23);
            this.mStartIngestTestButton.TabIndex = 58;
            this.mStartIngestTestButton.Text = "Start Ingest Test";
            this.mStartIngestTestButton.UseVisualStyleBackColor = true;
            this.mStartIngestTestButton.Click += new System.EventHandler(this.mStartIngestTestButton_Click);
            // 
            // mGameNameListGroupbox
            // 
            this.mGameNameListGroupbox.Controls.Add(this.mGameNameListTextbox);
            this.mGameNameListGroupbox.Controls.Add(this.label10);
            this.mGameNameListGroupbox.Controls.Add(this.mGameNameListButton);
            this.mGameNameListGroupbox.Location = new System.Drawing.Point(564, 488);
            this.mGameNameListGroupbox.Name = "mGameNameListGroupbox";
            this.mGameNameListGroupbox.Size = new System.Drawing.Size(158, 110);
            this.mGameNameListGroupbox.TabIndex = 59;
            this.mGameNameListGroupbox.TabStop = false;
            this.mGameNameListGroupbox.Text = "Game Name List";
            // 
            // mGameNameListTextbox
            // 
            this.mGameNameListTextbox.Location = new System.Drawing.Point(17, 40);
            this.mGameNameListTextbox.Name = "mGameNameListTextbox";
            this.mGameNameListTextbox.Size = new System.Drawing.Size(127, 20);
            this.mGameNameListTextbox.TabIndex = 46;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(14, 24);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(85, 13);
            this.label10.TabIndex = 47;
            this.label10.Text = "Game Name List";
            // 
            // mGameNameListButton
            // 
            this.mGameNameListButton.Location = new System.Drawing.Point(17, 66);
            this.mGameNameListButton.Name = "mGameNameListButton";
            this.mGameNameListButton.Size = new System.Drawing.Size(127, 23);
            this.mGameNameListButton.TabIndex = 45;
            this.mGameNameListButton.Text = "Get Game Names";
            this.mGameNameListButton.UseVisualStyleBackColor = true;
            this.mGameNameListButton.Click += new System.EventHandler(this.mGameNameListButton_Click);
            // 
            // mMetaDataGroup
            // 
            this.mMetaDataGroup.Controls.Add(this.mSendEndSpanMetaDataButton);
            this.mMetaDataGroup.Controls.Add(this.mSendStartSpanMetaDataButton);
            this.mMetaDataGroup.Controls.Add(this.mSendActionMetaDataButton);
            this.mMetaDataGroup.Enabled = false;
            this.mMetaDataGroup.Location = new System.Drawing.Point(564, 367);
            this.mMetaDataGroup.Name = "mMetaDataGroup";
            this.mMetaDataGroup.Size = new System.Drawing.Size(179, 115);
            this.mMetaDataGroup.TabIndex = 58;
            this.mMetaDataGroup.TabStop = false;
            this.mMetaDataGroup.Text = "Meta Data";
            // 
            // mSendEndSpanMetaDataButton
            // 
            this.mSendEndSpanMetaDataButton.Location = new System.Drawing.Point(12, 80);
            this.mSendEndSpanMetaDataButton.Name = "mSendEndSpanMetaDataButton";
            this.mSendEndSpanMetaDataButton.Size = new System.Drawing.Size(153, 23);
            this.mSendEndSpanMetaDataButton.TabIndex = 42;
            this.mSendEndSpanMetaDataButton.Text = "SendEndSpanMetaData\r\n";
            this.mSendEndSpanMetaDataButton.UseVisualStyleBackColor = true;
            this.mSendEndSpanMetaDataButton.Click += new System.EventHandler(this.mSendEndSpanMetaDataButton_Click);
            // 
            // mSendStartSpanMetaDataButton
            // 
            this.mSendStartSpanMetaDataButton.Location = new System.Drawing.Point(12, 50);
            this.mSendStartSpanMetaDataButton.Name = "mSendStartSpanMetaDataButton";
            this.mSendStartSpanMetaDataButton.Size = new System.Drawing.Size(153, 23);
            this.mSendStartSpanMetaDataButton.TabIndex = 41;
            this.mSendStartSpanMetaDataButton.Text = "SendStartSpanMetaData\r\n";
            this.mSendStartSpanMetaDataButton.UseVisualStyleBackColor = true;
            this.mSendStartSpanMetaDataButton.Click += new System.EventHandler(this.mSendStartSpanMetaDataButton_Click);
            // 
            // mSendActionMetaDataButton
            // 
            this.mSendActionMetaDataButton.Location = new System.Drawing.Point(12, 19);
            this.mSendActionMetaDataButton.Name = "mSendActionMetaDataButton";
            this.mSendActionMetaDataButton.Size = new System.Drawing.Size(153, 23);
            this.mSendActionMetaDataButton.TabIndex = 40;
            this.mSendActionMetaDataButton.Text = "SendActionMetaData";
            this.mSendActionMetaDataButton.UseVisualStyleBackColor = true;
            this.mSendActionMetaDataButton.Click += new System.EventHandler(this.mSendActionMetaDataButton_Click);
            // 
            // mBroadcastControlsGroupbox
            // 
            this.mBroadcastControlsGroupbox.Controls.Add(this.mResumeButton);
            this.mBroadcastControlsGroupbox.Controls.Add(this.mStopButton);
            this.mBroadcastControlsGroupbox.Controls.Add(this.mRunCommercialButton);
            this.mBroadcastControlsGroupbox.Controls.Add(this.mPauseButton);
            this.mBroadcastControlsGroupbox.Location = new System.Drawing.Point(375, 134);
            this.mBroadcastControlsGroupbox.Name = "mBroadcastControlsGroupbox";
            this.mBroadcastControlsGroupbox.Size = new System.Drawing.Size(156, 138);
            this.mBroadcastControlsGroupbox.TabIndex = 23;
            this.mBroadcastControlsGroupbox.TabStop = false;
            this.mBroadcastControlsGroupbox.Text = "Broadcast Controls";
            // 
            // mResumeButton
            // 
            this.mResumeButton.Location = new System.Drawing.Point(6, 77);
            this.mResumeButton.Name = "mResumeButton";
            this.mResumeButton.Size = new System.Drawing.Size(133, 23);
            this.mResumeButton.TabIndex = 26;
            this.mResumeButton.Text = "Resume";
            this.mResumeButton.UseVisualStyleBackColor = true;
            this.mResumeButton.Click += new System.EventHandler(this.mResumeButton_Click);
            // 
            // mStopButton
            // 
            this.mStopButton.Location = new System.Drawing.Point(6, 19);
            this.mStopButton.Name = "mStopButton";
            this.mStopButton.Size = new System.Drawing.Size(133, 23);
            this.mStopButton.TabIndex = 25;
            this.mStopButton.Text = "Stop";
            this.mStopButton.UseVisualStyleBackColor = true;
            this.mStopButton.Click += new System.EventHandler(this.mStopButton_Click);
            // 
            // mRunCommercialButton
            // 
            this.mRunCommercialButton.Location = new System.Drawing.Point(6, 106);
            this.mRunCommercialButton.Name = "mRunCommercialButton";
            this.mRunCommercialButton.Size = new System.Drawing.Size(133, 23);
            this.mRunCommercialButton.TabIndex = 24;
            this.mRunCommercialButton.Text = "Run Commercial";
            this.mRunCommercialButton.UseVisualStyleBackColor = true;
            this.mRunCommercialButton.Click += new System.EventHandler(this.mRunCommercialButton_Click);
            // 
            // mPauseButton
            // 
            this.mPauseButton.Location = new System.Drawing.Point(6, 48);
            this.mPauseButton.Name = "mPauseButton";
            this.mPauseButton.Size = new System.Drawing.Size(133, 23);
            this.mPauseButton.TabIndex = 23;
            this.mPauseButton.Text = "Pause";
            this.mPauseButton.UseVisualStyleBackColor = true;
            this.mPauseButton.Click += new System.EventHandler(this.mPauseButton_Click);
            // 
            // mSimpleBroadcastGroupbox
            // 
            this.mSimpleBroadcastGroupbox.Controls.Add(this.label14);
            this.mSimpleBroadcastGroupbox.Controls.Add(this.mAspectRatioCombo);
            this.mSimpleBroadcastGroupbox.Controls.Add(this.mMaxKbpsLabel);
            this.mSimpleBroadcastGroupbox.Controls.Add(this.mMaxKbpsTrackbar);
            this.mSimpleBroadcastGroupbox.Controls.Add(this.label11);
            this.mSimpleBroadcastGroupbox.Controls.Add(this.mStartButtonRecommended);
            this.mSimpleBroadcastGroupbox.Location = new System.Drawing.Point(8, 131);
            this.mSimpleBroadcastGroupbox.Name = "mSimpleBroadcastGroupbox";
            this.mSimpleBroadcastGroupbox.Size = new System.Drawing.Size(156, 192);
            this.mSimpleBroadcastGroupbox.TabIndex = 17;
            this.mSimpleBroadcastGroupbox.TabStop = false;
            this.mSimpleBroadcastGroupbox.Text = "Simple Broadcast Initiation";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(17, 117);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(68, 13);
            this.label14.TabIndex = 27;
            this.label14.Text = "Aspect Ratio";
            // 
            // mAspectRatioCombo
            // 
            this.mAspectRatioCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.mAspectRatioCombo.FormattingEnabled = true;
            this.mAspectRatioCombo.Items.AddRange(new object[] {
            "16:9",
            "1:1",
            "3:2 ",
            "4:3"});
            this.mAspectRatioCombo.Location = new System.Drawing.Point(17, 132);
            this.mAspectRatioCombo.Name = "mAspectRatioCombo";
            this.mAspectRatioCombo.Size = new System.Drawing.Size(121, 21);
            this.mAspectRatioCombo.TabIndex = 26;
            // 
            // mMaxKbpsLabel
            // 
            this.mMaxKbpsLabel.AutoSize = true;
            this.mMaxKbpsLabel.Enabled = false;
            this.mMaxKbpsLabel.Location = new System.Drawing.Point(17, 89);
            this.mMaxKbpsLabel.Name = "mMaxKbpsLabel";
            this.mMaxKbpsLabel.Size = new System.Drawing.Size(31, 13);
            this.mMaxKbpsLabel.TabIndex = 25;
            this.mMaxKbpsLabel.Text = "Kbps";
            // 
            // mMaxKbpsTrackbar
            // 
            this.mMaxKbpsTrackbar.Location = new System.Drawing.Point(15, 38);
            this.mMaxKbpsTrackbar.Maximum = 3500;
            this.mMaxKbpsTrackbar.Minimum = 500;
            this.mMaxKbpsTrackbar.Name = "mMaxKbpsTrackbar";
            this.mMaxKbpsTrackbar.Size = new System.Drawing.Size(126, 45);
            this.mMaxKbpsTrackbar.TabIndex = 24;
            this.mMaxKbpsTrackbar.Value = 500;
            this.mMaxKbpsTrackbar.ValueChanged += new System.EventHandler(this.mMaxKbpsTrackbar_ValueChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(17, 22);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(53, 13);
            this.label11.TabIndex = 23;
            this.label11.Text = "Max kbps";
            // 
            // mStartButtonRecommended
            // 
            this.mStartButtonRecommended.Location = new System.Drawing.Point(11, 160);
            this.mStartButtonRecommended.Name = "mStartButtonRecommended";
            this.mStartButtonRecommended.Size = new System.Drawing.Size(133, 23);
            this.mStartButtonRecommended.TabIndex = 21;
            this.mStartButtonRecommended.Text = "Start";
            this.mStartButtonRecommended.UseVisualStyleBackColor = true;
            this.mStartButtonRecommended.Click += new System.EventHandler(this.mStartButtonRecommended_Click);
            // 
            // mInitializationGroupbox
            // 
            this.mInitializationGroupbox.Controls.Add(this.mClientSecret);
            this.mInitializationGroupbox.Controls.Add(this.mClientSecretText);
            this.mInitializationGroupbox.Controls.Add(this.label3);
            this.mInitializationGroupbox.Controls.Add(this.mClientIdText);
            this.mInitializationGroupbox.Controls.Add(this.mShutdownButton);
            this.mInitializationGroupbox.Controls.Add(this.mInitButton);
            this.mInitializationGroupbox.Location = new System.Drawing.Point(8, 6);
            this.mInitializationGroupbox.Name = "mInitializationGroupbox";
            this.mInitializationGroupbox.Size = new System.Drawing.Size(361, 120);
            this.mInitializationGroupbox.TabIndex = 16;
            this.mInitializationGroupbox.TabStop = false;
            this.mInitializationGroupbox.Text = "Initialization";
            // 
            // mClientSecret
            // 
            this.mClientSecret.AutoSize = true;
            this.mClientSecret.Location = new System.Drawing.Point(14, 64);
            this.mClientSecret.Name = "mClientSecret";
            this.mClientSecret.Size = new System.Drawing.Size(67, 13);
            this.mClientSecret.TabIndex = 20;
            this.mClientSecret.Text = "Client Secret";
            // 
            // mClientSecretText
            // 
            this.mClientSecretText.Location = new System.Drawing.Point(15, 80);
            this.mClientSecretText.Name = "mClientSecretText";
            this.mClientSecretText.Size = new System.Drawing.Size(235, 20);
            this.mClientSecretText.TabIndex = 19;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 23);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(47, 13);
            this.label3.TabIndex = 18;
            this.label3.Text = "Client ID";
            // 
            // mClientIdText
            // 
            this.mClientIdText.Location = new System.Drawing.Point(15, 39);
            this.mClientIdText.Name = "mClientIdText";
            this.mClientIdText.Size = new System.Drawing.Size(235, 20);
            this.mClientIdText.TabIndex = 10;
            // 
            // mShutdownButton
            // 
            this.mShutdownButton.Location = new System.Drawing.Point(256, 64);
            this.mShutdownButton.Name = "mShutdownButton";
            this.mShutdownButton.Size = new System.Drawing.Size(97, 23);
            this.mShutdownButton.TabIndex = 9;
            this.mShutdownButton.Text = "Shutdown";
            this.mShutdownButton.UseVisualStyleBackColor = true;
            this.mShutdownButton.Click += new System.EventHandler(this.ShutdownButton_Click);
            // 
            // mInitButton
            // 
            this.mInitButton.Location = new System.Drawing.Point(256, 38);
            this.mInitButton.Name = "mInitButton";
            this.mInitButton.Size = new System.Drawing.Size(97, 23);
            this.mInitButton.TabIndex = 8;
            this.mInitButton.Text = "Init";
            this.mInitButton.UseVisualStyleBackColor = true;
            this.mInitButton.Click += new System.EventHandler(this.InitButton_Click);
            // 
            // Tabs
            // 
            this.Tabs.Controls.Add(this.StreamTab);
            this.Tabs.Controls.Add(this.ChatTab);
            this.Tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Tabs.ItemSize = new System.Drawing.Size(45, 18);
            this.Tabs.Location = new System.Drawing.Point(0, 0);
            this.Tabs.Name = "Tabs";
            this.Tabs.SelectedIndex = 0;
            this.Tabs.Size = new System.Drawing.Size(886, 740);
            this.Tabs.TabIndex = 6;
            // 
            // mEmoticonModeCombobox
            // 
            this.mEmoticonModeCombobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.mEmoticonModeCombobox.FormattingEnabled = true;
            this.mEmoticonModeCombobox.Location = new System.Drawing.Point(348, 38);
            this.mEmoticonModeCombobox.Name = "mEmoticonModeCombobox";
            this.mEmoticonModeCombobox.Size = new System.Drawing.Size(126, 21);
            this.mEmoticonModeCombobox.TabIndex = 15;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(348, 19);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(81, 13);
            this.label4.TabIndex = 16;
            this.label4.Text = "Emoticon Mode";
            // 
            // SampleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(886, 740);
            this.Controls.Add(this.Tabs);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "SampleForm";
            this.Text = "Twitch Winforms Sample";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SampleForm_FormClosing);
            this.ChatTab.ResumeLayout(false);
            this.ChatTab.PerformLayout();
            this.mChatMessagesGroupbox.ResumeLayout(false);
            this.mChatMessagesGroupbox.PerformLayout();
            this.mChatConnectionGroupbox.ResumeLayout(false);
            this.mChatConnectionGroupbox.PerformLayout();
            this.StreamTab.ResumeLayout(false);
            this.StreamTab.PerformLayout();
            this.mLoginGroupbox.ResumeLayout(false);
            this.mAdvancedBroadcastGroupbox.ResumeLayout(false);
            this.mAdvancedBroadcastGroupbox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mFramesPerSecondSelector)).EndInit();
            this.mBroadcastInfoGroupbox.ResumeLayout(false);
            this.mBroadcastInfoGroupbox.PerformLayout();
            this.mAudioGroupbox.ResumeLayout(false);
            this.mAudioGroupbox.PerformLayout();
            this.mIngestTestingGroupbox.ResumeLayout(false);
            this.mIngestTestingGroupbox.PerformLayout();
            this.mGameNameListGroupbox.ResumeLayout(false);
            this.mGameNameListGroupbox.PerformLayout();
            this.mMetaDataGroup.ResumeLayout(false);
            this.mBroadcastControlsGroupbox.ResumeLayout(false);
            this.mSimpleBroadcastGroupbox.ResumeLayout(false);
            this.mSimpleBroadcastGroupbox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mMaxKbpsTrackbar)).EndInit();
            this.mInitializationGroupbox.ResumeLayout(false);
            this.mInitializationGroupbox.PerformLayout();
            this.Tabs.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer mStreamTasksTimer;
        private System.Windows.Forms.Timer mSubmitFrameTimer;
        private System.Windows.Forms.Timer mChatTimer;
        private System.Windows.Forms.TabPage ChatTab;
        private System.Windows.Forms.Label mChatStatusLabel;
        private System.Windows.Forms.TabPage StreamTab;
        private System.Windows.Forms.GroupBox mMetaDataGroup;
        private System.Windows.Forms.Button mSendEndSpanMetaDataButton;
        private System.Windows.Forms.Button mSendStartSpanMetaDataButton;
        private System.Windows.Forms.Button mSendActionMetaDataButton;
        private System.Windows.Forms.GroupBox mBroadcastControlsGroupbox;
        private System.Windows.Forms.Button mResumeButton;
        private System.Windows.Forms.Button mStopButton;
        private System.Windows.Forms.Button mRunCommercialButton;
        private System.Windows.Forms.Button mPauseButton;
        private System.Windows.Forms.GroupBox mSimpleBroadcastGroupbox;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button mStartButtonRecommended;
        private System.Windows.Forms.GroupBox mInitializationGroupbox;
        private System.Windows.Forms.Label mClientSecret;
        private System.Windows.Forms.TextBox mClientSecretText;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox mClientIdText;
        private System.Windows.Forms.Button mShutdownButton;
        private System.Windows.Forms.Button mInitButton;
        private System.Windows.Forms.TabControl Tabs;
        private System.Windows.Forms.GroupBox mAudioGroupbox;
        private System.Windows.Forms.TextBox mSystemVolumeText;
        private System.Windows.Forms.TextBox mMicVolumeText;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Button mDecreaseSysVolumeButton;
        private System.Windows.Forms.Button mIncreaseSysVolumeButton;
        private System.Windows.Forms.Button mDecreaseMicVolumeButton;
        private System.Windows.Forms.Button mIncreaseMicVolumeButton;
        private System.Windows.Forms.GroupBox mIngestTestingGroupbox;
        private System.Windows.Forms.ListBox mIngestListListbox;
        private System.Windows.Forms.Button mSkipIngestServerButton;
        private System.Windows.Forms.TextBox mIngestTestStatusText;
        private System.Windows.Forms.Button mCancelIngestTestButton;
        private System.Windows.Forms.Button mStartIngestTestButton;
        private System.Windows.Forms.GroupBox mGameNameListGroupbox;
        private System.Windows.Forms.TextBox mGameNameListTextbox;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button mGameNameListButton;
        private System.Windows.Forms.GroupBox mBroadcastInfoGroupbox;
        private System.Windows.Forms.TextBox mGameNameText;
        private System.Windows.Forms.TextBox mStreamTitleText;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button mSetStreamInfoButton;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox mArchivingStateCheckbox;
        private System.Windows.Forms.GroupBox mAdvancedBroadcastGroupbox;
        private System.Windows.Forms.Button mStartButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown mFramesPerSecondSelector;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox mResolutionCombo;
        private System.Windows.Forms.TrackBar mMaxKbpsTrackbar;
        private System.Windows.Forms.Label mMaxKbpsLabel;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ComboBox mAspectRatioCombo;
        private System.Windows.Forms.GroupBox mLoginGroupbox;
        private System.Windows.Forms.Button mRequestAuthTokenButton;
        private System.Windows.Forms.Button mSetAuthTokenButton;
        private System.Windows.Forms.TextBox mChannelNameText;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.GroupBox mChatConnectionGroupbox;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button mChatDisconnectButton;
        private System.Windows.Forms.TextBox mChatChannelText;
        private System.Windows.Forms.Button mChatConnectAnonymousButton;
        private System.Windows.Forms.Button mChatConnectButton;
        private System.Windows.Forms.GroupBox mChatMessagesGroupbox;
        private System.Windows.Forms.TextBox mChatMessagesTextbox;
        private System.Windows.Forms.ListBox mChatUsersListbox;
        private System.Windows.Forms.Button mChatSendButton;
        private System.Windows.Forms.TextBox mChatInputTextbox;
        private System.Windows.Forms.CheckBox mEnableAudioCheckbox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox mEmoticonModeCombobox;

    }
}

