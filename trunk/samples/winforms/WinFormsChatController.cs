using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Twitch;
using ErrorCode = Twitch.ErrorCode;

namespace Twitch.Chat
{
    public class WinFormsChatController : ChatController
    {
        #region Memeber Variables

        protected string m_ClientId = "";
        protected string m_ClientSecret = "";

        #endregion

        #region Properties

        public override string ClientId
        {
            get { return m_ClientId; }
            set { m_ClientId = value; }
        }

        public override string ClientSecret
        {
            get { return m_ClientSecret; }
            set { m_ClientSecret = value; }
        }

        #endregion

        public WinFormsChatController()
        {
            m_Core = new Twitch.Core(new Twitch.StandardCoreAPI());
            m_Chat = new Twitch.Chat.Chat(new Twitch.Chat.StandardChatAPI());
        }

        #region Error Handling

        protected override void CheckError(ErrorCode err)
        {
            if (err != ErrorCode.TTV_EC_SUCCESS)
            {
                System.Windows.Forms.MessageBox.Show(err.ToString());
            }
        }

        protected override void ReportError(string err)
        {
            System.Windows.Forms.MessageBox.Show(err, "Error");
        }

        protected override void ReportWarning(string err)
        {
            System.Windows.Forms.MessageBox.Show(err, "Warning");
        }

        #endregion
    }
}
