//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License 
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using Gurux.Common;

namespace Gurux.Net
{
#if WINDOWS_PHONE
    class ReceiveThread
    {
        public ManualResetEvent Closing;
        GXNet m_Parent;
        Socket m_Socket;        

        public ReceiveThread(GXNet parent, Socket socket)
        {
            Closing = new ManualResetEvent(false);
            m_Parent = parent;
            m_Socket = socket;            
        }
        /// <summary>
        /// Receive data from the server using the established socket connection
        /// </summary>
        /// <returns>The data received from the server</returns>
        public void Receive()
        {
            try
            {
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                socketEventArg.RemoteEndPoint = m_Socket.RemoteEndPoint;
                AutoResetEvent received = new AutoResetEvent(false);
                socketEventArg.SetBuffer(m_Parent.ReceiveBuffer, 0, m_Parent.ReceiveBuffer.Length);
                // Inline event handler for the Completed event.
                // Note: This even handler was implemented inline in order to make this method self-contained.
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                {
                    if (e.SocketError == SocketError.Success)
                    {
                        m_Parent.m_BytesReceived += (uint)e.BytesTransferred;
                        if (m_Parent.IsSynchronous)
                        {
                            lock (m_Parent.m_syncBase.m_ReceivedSync)
                            {
                                m_Parent.m_syncBase.AppendData(m_Parent.ReceiveBuffer, e.Offset, e.BytesTransferred);                                
                                m_Parent.m_syncBase.m_ReceivedEvent.Set();
                            }
                        }
                        else if (m_Parent.m_OnReceived != null)
                        {
                            m_Parent.m_syncBase.m_ReceivedSize = 0;
                            byte[] data = new byte[e.BytesTransferred];
                            Array.Copy(m_Parent.ReceiveBuffer, data, e.BytesTransferred);
                            m_Parent.m_OnReceived(this, new ReceiveEventArgs(data, m_Socket.RemoteEndPoint.ToString()));
                        }
                    }
                    else if (e.SocketError != SocketError.OperationAborted)
                    {
                        throw new SocketException((int)e.SocketError);
                    }
                    received.Set();
                });
                do
                {
                    // Make an asynchronous Receive request over the socket
                    m_Socket.ReceiveAsync(socketEventArg);
                    EventWaitHandle.WaitAny(new EventWaitHandle[] { Closing, received });
                }
                while (!Closing.WaitOne(0));
                socketEventArg.Dispose();
            }
            catch (Exception ex)
            {
                if (m_Parent.m_OnError != null)
                {
                    m_Parent.m_OnError(m_Parent, ex);
                }
            }
        }
    }
#endif
}
