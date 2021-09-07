using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sip2Form
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void connectBtn_Click(object sender, EventArgs e)
        {
            try
            {
                using (var sip = new Sip2Connection(ipAddress.Text, int.Parse(port.Text)))
                {

                    LogMessage($"Trying connect to {ipAddress.Text} {port.Text} ...");
                    sip.Open();
                    LogMessage($"Succesfully connected to {ipAddress.Text} {port.Text}");

                }
            }
            catch (AggregateException ae)
            {
                foreach (var inner in ae.InnerExceptions)
                {
                    LogMessage(inner.Message, Color.Red);
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message, Color.Red);
            }

        }
        private void authorizeBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var instId = institutionId.Text;
                using (var sip = new Sip2Connection(ipAddress.Text, int.Parse(port.Text)))
                {
                    LogMessage($"Trying connect to {ipAddress.Text} {port.Text} ...");
                    sip.Open();
                    LogMessage($"Succesfully connected to {ipAddress.Text} {port.Text}", Color.Green);
                    var seqNum = 0;
                    var response = "";
                    if (!string.IsNullOrEmpty(loginUser.Text) && !string.IsNullOrEmpty(loginPwd.Text))
                    {

                        var loginMessage = $"{Sip2Command.Login}00{Sip2Command.FidLoginUid}{loginUser.Text}|{Sip2Command.FidLoginPwd}{loginPwd.Text}|{Sip2Command.FidLocationCode}{locationCode.Text}|{Sip2Command.FidSeq}{seqNum++}{Sip2Command.FidCksum}";
                        LogMessage($"Sent login request: {loginMessage.WithCheckSum()}");
                        response = sip.SendMessage(loginMessage.WithCheckSum());
                        LogMessage($"Login response: {response}");

                        if (!IsLoginSuccessful(response))
                        {
                            throw new Exception($"Handshake with server failed! Server response - {response}");
                        }
                    }

                    if (string.IsNullOrEmpty(instId))
                    {
                        var statusMessage = $"{Sip2Command.ScStatus}0000{Sip2Command.Version}{Sip2Command.FidSeq}{seqNum++}{Sip2Command.FidCksum}";
                        LogMessage($"Sent SCS status request: {statusMessage.WithCheckSum()}");

                        response = sip.SendMessage(statusMessage.WithCheckSum());
                        LogMessage($"Status response: {response}");

                        instId = GetInstitutionId(response);
                    }

                    var dateFld = DateTime.Now.ToString(Sip2Command.SipDatetime);
                    var patronMessage = $"{Sip2Command.PatronInfo}001{dateFld}{Sip2Command.Summary}{Sip2Command.FidInstId}{instId}|{Sip2Command.FidPatronId}{barcode.Text}|{Sip2Command.FidTerminalPwd}{loginPwd.Text}|{Sip2Command.FidPatronPwd}{pin.Text}|{Sip2Command.FidSeq}{seqNum}{Sip2Command.FidCksum}";

                    LogMessage($"Sent user info request: {patronMessage.WithCheckSum()}");

                    response = sip.SendMessage(patronMessage.WithCheckSum());
                    LogMessage($"User info response: {response}");

                    if (ValidatePatronInfoFromResponse(response))
                    {
                        LogMessage($"Barcode and PIN are OK!", Color.Green);
                    }
                    else
                    {
                        LogMessage("Invalid Patron barcode or PIN", Color.Red);
                    }
                }

            }
            catch (AggregateException ae)
            {
                foreach (var inner in ae.InnerExceptions)
                {
                    LogMessage(inner.Message, Color.Red);
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message, Color.Red);
            }
        }

        private static bool IsLoginSuccessful(string response)
        {
            return response.StartsWith($"{Sip2Command.LoginResp}{Sip2Command.Ok}", StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool ValidatePatronInfoFromResponse(string response)
        {
            if (!response.StartsWith(Sip2Command.PatronInfoResp, StringComparison.InvariantCultureIgnoreCase))
                throw new Exception($"Patron information response did not match expected result. Response - {response}");

            var isAuthorized = response.Contains($"{Sip2Command.FidValidPatron}{Sip2Command.FidYes}") && response.Contains($"{Sip2Command.FidValidPatronPwd}{Sip2Command.FidYes}");
            return isAuthorized;
        }

        private static string GetInstitutionId(string response)
        {
            if (!response.StartsWith(Sip2Command.AcsStatus, StringComparison.InvariantCultureIgnoreCase))
                throw new Exception($"ACS status response did not match expected result. Response - {response}");

            var re = new Regex(Sip2Command.SipStatusRegex);
            var match = re.Match(response);
            if (!match.Success) { throw new Exception($"ACS status response did not match expected result. Response - {response}"); }
            return match.Groups["id"].Value;
        }

        private void LogMessage(string message, Color color = default)
        {
            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;

            logBox.SelectionColor = color;
            logBox.AppendText($"{message}\n");
            logBox.SelectionColor = logBox.ForeColor;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            logBox.Clear();
        }
    }
}
