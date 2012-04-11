// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System.Collections.Generic;
using System.Windows.Forms;
using System;

namespace fCraft.ServerGUI {
    sealed class ConsoleBox : TextBox {
        const int WM_KEYDOWN = 0x100;
        const int WM_SYSKEYDOWN = 0x104;
        public event Action OnCommand;
        readonly List<string> log = new List<string>();
        int logPointer;

        protected override bool ProcessCmdKey( ref Message msg, Keys keyData ) {
            if( !Enabled ) return base.ProcessCmdKey( ref msg, keyData );
            switch( keyData ) {
                case Keys.Up:
                    if( msg.Msg == WM_SYSKEYDOWN || msg.Msg == WM_KEYDOWN ) {
                        if( log.Count == 0 ) return true;
                        if( logPointer == -1 ) {
                            logPointer = log.Count - 1;
                        } else if( logPointer > 0 ) {
                            logPointer--;
                        }
                        Text = log[logPointer];
                        SelectAll();
                    }
                    return true;

                case Keys.Down:
                    if( msg.Msg == WM_SYSKEYDOWN || msg.Msg == WM_KEYDOWN ) {
                        if( log.Count == 0 || logPointer == -1 ) return true;
                        if( logPointer < log.Count - 1 ) {
                            logPointer++;
                        }
                        Text = log[logPointer];
                        SelectAll();
                    }
                    return true;

                case Keys.Enter:
                    if( msg.Msg == WM_SYSKEYDOWN || msg.Msg == WM_KEYDOWN ) {
                        if( Text.Trim().Length > 0 ) {
                            log.Add( Text );
                            if( log.Count > 100 ) log.RemoveAt( 0 );
                            logPointer = -1;
                            if( OnCommand != null ) OnCommand();
                        }
                    }
                    return true;

                case Keys.Escape:
                    if( msg.Msg == WM_SYSKEYDOWN || msg.Msg == WM_KEYDOWN ) {
                        logPointer = log.Count;
                        Text = "";
                    }
                    return base.ProcessCmdKey( ref msg, keyData );

                default:
                    return base.ProcessCmdKey( ref msg, keyData );
            }
        }
    }
}