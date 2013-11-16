using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IngestTesterGui
{
    public partial class IngestTesterForm: Form
    {
        protected Twitch.BroadcastController mBroadcastController = new Twitch.BroadcastController();
        protected Twitch.IngestTester mIngestTester = null;
        protected String mTwitchUsername = "";
        protected String mTwitchPassword = "";
        protected bool mBeginIngestTest = false;

        static protected String mClientId = "swug4pkjx9y9emgvbsr5bq5aneytm4s";
        static protected String mClientSecret = "qs5ihbacb5mt9iw5omjdx3p6nohasbh";

        public IngestTesterForm()
        {
            InitializeComponent();

            mBroadcastController.AuthTokenRequestComplete += this.HandleAuthTokenRequestComplete;
            mBroadcastController.BroadcastStateChanged += this.HandleBroadcastStateChanged;
            mBroadcastController.LoginAttemptComplete += this.HandleLoginAttemptComplete;
            mBroadcastController.IngestListReceived += this.HandleIngestListReceived;

        }

        protected void HandleAuthTokenRequestComplete(Twitch.ErrorCode result, Twitch.AuthToken authToken)
        {
            if (Twitch.Error.Failed(result))
            {
                MessageBox.Show("Auth token request failed, please check your credentials and CA cert file path and try again.");
                mBroadcastController.ShutdownTwitch();
            }
        }

        protected void HandleBroadcastStateChanged(Twitch.BroadcastController.BroadcastState state)
        {
            String stateString = state.ToString();
            if (state == Twitch.BroadcastController.BroadcastState.ReadyToBroadcast)
            {
                // If reached the ReadyToBroadcast state from "Start Ingest Test" button press
                if (mBeginIngestTest)
                {
                    mBeginIngestTest = false;
                    if (BeginIngestTest())
                    {
                        stateString = "Ingest Testing";
                    }
                    else
                    {
                        MessageBox.Show("Failed to start ingest tester, please try again");
                        stateString = "Error";
                    }
                }
            }
            mStatusLabel.Text = "Status - " + stateString;
        }

        protected void HandleLoginAttemptComplete(Twitch.ErrorCode result)
        {
            if (Twitch.Error.Failed(result))
            {
                MessageBox.Show("Login failed, please try initializing again");
                mBroadcastController.ShutdownTwitch();
            }
        }

        protected class IngestListEntry
        {
            protected Twitch.IngestServer mServer = null;
            public bool Default { get; set; }
            public bool Selected { get; set; }

            public Twitch.IngestServer Server
            {
                get { return mServer; }
            }

            public IngestListEntry(Twitch.IngestServer server)
            {
                mServer = server;
            }

            public override string ToString()
            {
                string str = mServer.ServerName;
                if (this.Default) str += " (Default)";
                if (this.Selected) str = "* " + str;
                str += " - " + mServer.BitrateKbps + " kbps";

                return str;
            }
        }

        protected void HandleIngestListReceived(Twitch.IngestList list)
        {
            mIngestListListbox.Items.Clear();

            for (int i = 0; i < list.Servers.Length; ++i)
            {
                IngestListEntry item = new IngestListEntry(list.Servers[i]);
                item.Default = item.Server == list.DefaultServer;
                item.Selected = item.Server == mBroadcastController.IngestServer;
                mIngestListListbox.Items.Add(item);
            }
        }

        protected void RefreshListbox(ListBox listbox)
        {
            typeof(ListBox).InvokeMember("RefreshItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.InvokeMethod, null, listbox, new object[] { });
        }

        private void mStreamTasksTimer_Tick(object sender, EventArgs e)
        {
            mBroadcastController.Update();

            if (mIngestTester != null)
            {
                if (mIngestTester.State == Twitch.IngestTester.TestState.TestingServer)
                {
                    mIngestTestStatusText.Text = "[" + (int)(mIngestTester.TotalProgress * 100) + "%] " + mIngestTester.State.ToString() + ": " + mIngestTester.CurrentServer.ServerName + "... " + mIngestTester.CurrentServer.BitrateKbps + " kbps [" + (int)(mIngestTester.ServerProgress * 100) + "%]";
                    RefreshListbox(mIngestListListbox);
                }
            }
        }

        private void mStartIngestTestButton_Click(object sender, EventArgs e)
        {
            if (mUsernameText.Text == "")
            {
                MessageBox.Show("Please provide a Twitch username");
                return;
            }
            else if (mPasswordText.Text == "")
            {
                MessageBox.Show("Please provide a Twitch password");
                return;
            }

            // On first call, initialize Twitch sdk
            if (!mBroadcastController.IsInitialized)
            {
                mBroadcastController.ClientId = mClientId;
                mBroadcastController.ClientSecret = mClientSecret;
                mBroadcastController.CaCertFilePath = "";
                mBroadcastController.InitializeTwitch();
            }

            // Request a new auth token if the provided Twitch username has changed.
            // If have to request new auth token, don't begin ingest testing until callback
            // completes
            if (mUsernameText.Text != mTwitchUsername || mPasswordText.Text != mTwitchPassword)
            {
                if (!mBroadcastController.RequestAuthToken(mUsernameText.Text, mPasswordText.Text))
                {
                    mBeginIngestTest = false;
                }
                else
                {
                    mTwitchUsername = mUsernameText.Text;
                    mTwitchPassword = mPasswordText.Text;
                    mBeginIngestTest = true;
                }
            }
            else
            {
                BeginIngestTest();
            }
        }

        void mIngestTester_OnTestStateChanged(Twitch.IngestTester source, Twitch.IngestTester.TestState state)
        {
            mIngestTestStatusText.Text = "[" + (int)(mIngestTester.TotalProgress * 100) + "%] " + state.ToString();

            switch (state)
            {
                case Twitch.IngestTester.TestState.ConnectingToServer:
                {
                    mIngestTestStatusText.Text += ": " + source.CurrentServer.ServerName + "...";
                    break;
                }
                case Twitch.IngestTester.TestState.TestingServer:
                case Twitch.IngestTester.TestState.DoneTestingServer:
                {
                    mIngestTestStatusText.Text += ": " + source.CurrentServer.ServerName + "... " + source.CurrentServer.BitrateKbps + " kbps";
                    break;
                }
                case Twitch.IngestTester.TestState.Finished:
                {
                    String bestServerName = "";
                    float bestBitrate = 0;
                    Twitch.IngestServer curServer;

                    for (int i = 0; i < mIngestListListbox.Items.Count; i++)
                    {
                        curServer = ((IngestListEntry) mIngestListListbox.Items[i]).Server;
                        if (curServer.BitrateKbps > bestBitrate)
                        {
                            bestBitrate = curServer.BitrateKbps;
                            bestServerName = curServer.ServerName;
                        }
                    }
                    mBestServerLabel.Text = "Best server - " + bestServerName;

                    mSkipIngestServerButton.Enabled = false;
                    mCancelIngestTestButton.Enabled = false;

                    mIngestTester.OnTestStateChanged -= mIngestTester_OnTestStateChanged;
                    mIngestTester = null;
                    break;
                }
                case Twitch.IngestTester.TestState.Cancelled:
                {
                    mSkipIngestServerButton.Enabled = false;
                    mCancelIngestTestButton.Enabled = false;

                    mIngestTester.OnTestStateChanged -= mIngestTester_OnTestStateChanged;
                    mIngestTester = null;
                    break;
                }
                default:
                {
                    break;
                }
            }

            RefreshListbox(mIngestListListbox);
        }

        private void mSkipIngestServerButton_Click(object sender, EventArgs e)
        {
            if (mIngestTester == null)
            {
                return;
            }

            mIngestTester.SkipCurrentServer();
        }

        private void mCancelIngestTestButton_Click(object sender, EventArgs e)
        {
            if (mIngestTester == null)
            {
                return;
            }

            mIngestTester.Cancel();
        }

        private void mShutdownButton_Click(object sender, EventArgs e)
        {
            mBroadcastController.ShutdownTwitch();
        }

        private bool BeginIngestTest()
        {
            if (mIngestTester == null)
            {
                mIngestTester = mBroadcastController.StartIngestTest();
                if (mIngestTester != null)
                {
                    mCancelIngestTestButton.Enabled = true;
                    mSkipIngestServerButton.Enabled = true;
                    mIngestTester.OnTestStateChanged += mIngestTester_OnTestStateChanged;
                    return true;
                }
                mStartIngestTestButton.Enabled = true;
            }

            return false;
        }
    }
}
