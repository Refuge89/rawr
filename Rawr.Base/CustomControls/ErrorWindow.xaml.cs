﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Rawr
{
    public partial class ErrorWindow : ChildWindow
    {
        private string _ErrorMessage = "";
        private string _StackTrace = "";
        private string _SuggestedFix = "";
        private string _Info = "";

        public string ErrorMessage
        {
            get { return _ErrorMessage != "" ? _ErrorMessage : "No Error Message"; }
            set
            {
                _ErrorMessage = value;
                TB_ErrorMessage.Text = ErrorMessage;
            }
        }
        public string StackTrace
        {
            get { return _StackTrace != "" ? _StackTrace : "No Stack Trace"; }
            set
            {
                _StackTrace = value;
                TB_StackTrace.Text = StackTrace;
                //if (StackTrace == "No Stack Trace") { LB_StackTrace.Visibility = TB_StackTrace.Visibility = Visibility.Collapsed; }
            }
        }
        public string SuggestedFix
        {
            get { return _SuggestedFix != "" ? _SuggestedFix : "No Suggested Fix"; }
            set
            {
                _SuggestedFix = value;
                TB_SuggestedFix.Text = SuggestedFix;
            }
        }
        public string Info
        {
            get { return _Info != "" ? _Info : "No Additional Info"; }
            set
            {
                _Info = value;
                TB_Info.Text = Info;
            }
        }

        public ErrorWindow()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void BT_CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(string.Format("I have performed the Suggested Fix and continue to receive this error. "
                        + "If I am posting this message without having performed the Suggested Fix I am aware that "
                        + "I am consuming the Developer(s) time in managing my Issue unnecessarily."
                        + "\r\n\r\n== Error Message ==\r\n{0}\r\n\r\n== StackTrace ==\r\n{1}"
                        + "\r\n\r\n== These are the Steps that I have tried ==\r\n[Please fill in steps here]",
                        ErrorMessage, StackTrace));
        }
    }
}
