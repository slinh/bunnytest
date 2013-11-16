using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinformsSample
{
    public partial class LoginForm : Form
    {
        private bool mShowPasswordField = true;

        public LoginForm()
        {
            InitializeComponent();
        }

        public bool ShowPasswordField
        {
            get { return mShowPasswordField; }
            set { mShowPasswordField = value; }
        }

        public string UserName
        {
            get { return mUserNameText.Text; }
            set { mUserNameText.Text = value; }
        }

        public string Password
        {
            get { return mPasswordText.Text; }
            set { mPasswordText.Text = value; }
        }

        private void mOkButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        private void mCancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            if (!mShowPasswordField)
            {
                mPasswordText.Text = "";
            }

            mPasswordText.Enabled = mShowPasswordField;
            mPasswordLabel.Enabled = mShowPasswordField;
        }
    }
}
