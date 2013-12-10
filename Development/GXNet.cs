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
using System.ComponentModel;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Xml;
using Gurux.Shared;
using Gurux.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Gurux.Net
{    
    /// <summary>
    /// The GXNet component determines methods that make the communication possible using Internet. 
    /// </summary>
    /// <seealso href="../Net/useNet.html">Using from .NET</seealso>
    /// <seealso href="../Net/useVB.html">Using from VB</seealso> 
    public class GXNet : IGXMedia, INotifyPropertyChanged, IDisposable
    {
#if WINDOWS_PHONE
        ReceiveThread m_Receiver;
        Thread m_ReceiverThread;
#endif
        // Define a timeout in milliseconds for each asynchronous call. If a response is not received within this 
        // timeout period, the call is aborted.
        const int TIMEOUT_MILLISECONDS = 5000;
        internal byte[] ReceiveBuffer = new byte[1024];
        NetworkType m_Protocol;
        string m_HostName;
        int m_Port;
        internal bool m_Server;
        internal GXSynchronousMediaBase m_syncBase;
        Socket m_Socket = null;
        internal UInt64 m_BytesReceived = 0;
        UInt64 m_BytesSend = 0;        
        internal Dictionary<Socket, byte[]> m_ServerDataBuffers = new Dictionary<Socket, byte[]>();        
        readonly object m_Synchronous = new object();
        TraceLevel m_Trace;

        // Signaling object used to notify when an asynchronous operation is completed
        static ManualResetEvent m_clientDone = new ManualResetEvent(false);

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXNet()
        {
            m_syncBase = new GXSynchronousMediaBase(1024);
            ConfigurableSettings = AvailableMediaSettings.All;
            Protocol = NetworkType.Tcp;            
        }

        /// <summary>
        /// Client Constructor.
        /// </summary>
        /// <param name="protocol">Used protocol.</param>
        /// <param name="hostName">Host name.</param>
        /// <param name="port">Client port.</param>
        public GXNet(NetworkType protocol, string hostName, int port)
            : this()
        {            
            Protocol = protocol;
            HostName = hostName;
            Port = port;
        }

        /// <summary>
        /// Constructor used when server is started.
        /// </summary>
        /// <param name="protocol">Used protocol.</param>
        /// <param name="port">Server port.</param>
        public GXNet(NetworkType protocol, int port) 
            : this()
        {
            this.Server = true;
            Protocol = protocol;            
            Port = port;
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~GXNet()
        {
            if (IsOpen)
            {
                Close();
            }
        }       

        /// <summary>
        /// Is IPv6 used. Default is False (IPv4).
        /// </summary>        
        public bool UseIPv6
        {
            get;
            set;
        }

        /// <summary>
        /// What level of tracing is used.
        /// </summary>
        public TraceLevel Trace
        {
            get
            {
                return m_Trace;
            }
            set
            {
                m_Trace = m_syncBase.Trace = value;
            }
        }
        private void NotifyPropertyChanged(string info)
        {
            if (m_OnPropertyChanged != null)
            {
                m_OnPropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        void NotifyError(Exception ex)
        {
            if (m_OnError != null)
            {
                m_OnError(this, ex);
            }
            if (m_Trace >= TraceLevel.Error && m_OnTrace != null)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Error, ex));
            }
        }

        /// <inheritdoc cref="IGXMedia.ConfigurableSettings"/>
        public AvailableMediaSettings ConfigurableSettings
        {
            get
            {
                return (AvailableMediaSettings)((IGXMedia)this).ConfigurableSettings;
            }
            set
            {
                ((IGXMedia)this).ConfigurableSettings = (int)value;
            }
        }

        /// <summary>   
        /// Displays the copyright of the control, user license, and version information, in a dialog box. 
        /// </summary>  
        public void AboutBox()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends data asynchronously. <br/>
        /// No reply from the receiver, whether or not the operation was successful, is expected.
        /// </summary>
        /// <param name="data">Data to send to the device.</param>
        /// <param name="receiver">IP address of the receiver (optional).</param>
        /// <remarks>Reply data is received through OnReceived event.<br/>
        /// If data is send synchronously use </remarks>
        /// <example>
        /// <code>
        /// 'Send ASCII (text) string to the device.
        /// dim dataToSend as string
        /// GXNet1.Send("Test", null)
        /// 
        /// 'Send byte array to the device.
        /// dim dataToSend as string
        /// GXNet1.Send(new byte[]{"01, 02, 03, 04}, null)
        /// 
        /// 'Send byte to the device.
        /// dim dataToSend
        /// dataToSend = ""
        /// GXNet1.Send((byte) 55, null)
        /// </code>
        /// </example>			
        /// <seealso cref="OnReceived">OnReceived</seealso>
        /// <seealso cref="Open">Open</seealso>
        /// <seealso cref="Close">Close</seealso> 
        /// <example>
        /// <code lang="csharp" source="..\\GXNet csharp Sample\\Form1.cs" region="Send" />                
        /// </example>
        public void Send(object data, string receiver)
        {
            if (m_Socket == null)
            {
                throw new Exception("Invalid connection.");
            }
            if (!this.m_Server)
            {
				byte[] value = Gurux.Common.GXCommon.GetAsByteArray(data);
                if (m_Trace == TraceLevel.Verbose && m_OnTrace != null)
                {
                    m_OnTrace(this, new TraceEventArgs(TraceTypes.Sent, value));
                }
                //Reset last position if Eop is used.
                lock (m_syncBase.m_ReceivedSync)
                {
                    m_syncBase.m_LastPosition = 0;
                }
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();

                // Set properties on context object
                socketEventArg.RemoteEndPoint = m_Socket.RemoteEndPoint;
                socketEventArg.UserToken = null;
                SocketError err = 0;
                // Inline event handler for the Completed event.
                // Note: This event handler was implemented inline in order to make this method self-contained.
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                {
                    err = e.SocketError;
                    m_clientDone.Set();
                });                
                
                socketEventArg.SetBuffer(value, 0, value.Length);
                // Sets the state of the event to nonsignaled, causing threads to block
                m_clientDone.Reset();
                // Make an asynchronous Send request over the socket
                m_Socket.SendAsync(socketEventArg);

                // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
                // If no response comes back within this time then proceed
                m_clientDone.WaitOne(TIMEOUT_MILLISECONDS);
                if (err != 0)
                {
                    throw new SocketException((int)err);
                }
                this.m_BytesSend += (ulong)value.Length;
            }
            else
            {
#if !WINDOWS_PHONE
                Socket client = null;
                foreach (var it in m_ServerDataBuffers)
                {
                    if (it.Key.LocalEndPoint.ToString() == receiver)
                    {
                        client = it.Key;
                        break;
                    }
                }
                if (client == null)
                {
                    throw new Exception("Invalid client.");
                }
                byte[] value = Gurux.Common.GXCommon.GetAsByteArray(data);
                client.Send(value);
                this.m_BytesSend += (ulong)value.Length;                
#endif
            }
        }

        void NotifyMediaStateChange(MediaState state)
        {
            if (m_Trace >= TraceLevel.Info && m_OnTrace != null)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Info, state));
            }
            if (m_OnMediaStateChange != null)
            {
                m_OnMediaStateChange(this, new MediaStateEventArgs(state));
            }                
        }

#if !WINDOWS_PHONE
        void RecieveComplete(IAsyncResult result)
        {
            Socket socket = null;
            try
            {
                socket = result.AsyncState as Socket;
                if (socket.Connected)
                {
                    byte[] buff = null;
                    int bytes = socket.EndReceive(result);
                    if (this.Server)
                    {
                        if (m_ServerDataBuffers.ContainsKey(socket))
                        {
                            buff = m_ServerDataBuffers[socket];
                        }
                    }
                    else
                    {
                        buff = ReceiveBuffer;
                    }
                    if (bytes == 0)
                    {
                        //Client has left.
                        DisconnectClient(socket.LocalEndPoint.ToString());
                        return;
                    }                    
                    m_BytesReceived += (uint)bytes;
                    if (this.IsSynchronous)
                    {
                        TraceEventArgs arg = null;
                        lock (m_syncBase.m_ReceivedSync)
                        {
                            int index = m_syncBase.m_ReceivedSize;
                            m_syncBase.AppendData(buff, 0, bytes);                            
                            if (bytes != 0 && Eop != null) //Search Eop if given.
                            {
                                if (Eop is Array)
                                {
                                    foreach (object eop in (Array)Eop)
                                    {
                                        bytes = GXCommon.IndexOf(m_syncBase.m_Received, GXCommon.GetAsByteArray(eop), index, m_syncBase.m_ReceivedSize);
                                        if (bytes != -1)
                                        {
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    bytes = GXCommon.IndexOf(m_syncBase.m_Received, GXCommon.GetAsByteArray(Eop), index, m_syncBase.m_ReceivedSize);
                                }
                            }
                            if (bytes != -1)
                            {
                                m_syncBase.m_ReceivedEvent.Set();
                                if (bytes != 0 && m_Trace == TraceLevel.Verbose && m_OnTrace != null)
                                {
                                    arg = new TraceEventArgs(TraceTypes.Received, m_syncBase.m_Received, index, bytes - index);
                                }
                            }
                        }
                        if (arg != null)
                        {
                            m_OnTrace(this, arg);
                        }
                    }
                    else
                    {
                        if (m_OnReceived != null)
                        {
                            m_syncBase.m_ReceivedSize = 0;
                            byte[] data = new byte[bytes];
                            Array.Copy(buff, data, bytes);
                            m_OnReceived(this, new ReceiveEventArgs(data, socket.LocalEndPoint.ToString()));
                        }
                        if (m_Trace == TraceLevel.Verbose && m_OnTrace != null)
                        {
                            m_OnTrace(this, new TraceEventArgs(TraceTypes.Received, buff, 0, bytes));
                        }
                    }
                    if (socket.Connected)
                    {
                        socket.BeginReceive(buff, 0, buff.Length, SocketFlags.None, new AsyncCallback(RecieveComplete), socket);
                    }
                }
            }
            catch (SocketException ex)
            {
                //If client has close connection.
                if (ex.ErrorCode == 10054)
                {
                    if (Server)
                    {
                        m_ServerDataBuffers.Remove(socket);
                        if (m_OnClientDisconnected != null)
                        {
                            m_OnClientDisconnected(this, new ConnectionEventArgs(socket.LocalEndPoint.ToString()));
                        }
                    }
                    else
                    {
                        m_syncBase.Exception = ex;
                        Close();
                    }
                }
                else
                {
                    NotifyError(ex);
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }
        }              

        /// <summary>
        /// New client is connected to the server.
        /// </summary>
        /// <param name="result"></param>
        void OnClientConnect(IAsyncResult result)
        {            
            Socket workerSocket = null;
            try
            {
                //Server is closed.
				if (m_Socket == null)
                {                    
                    if (m_OnMediaStateChange != null)
                    {
                        Close();
                    }
                }
                else
                {
                    if (MaxClientCount != 0 && m_ServerDataBuffers.Count + 1 > MaxClientCount)
                    {
                        Close();
                    }
                    else
                    {
                        if (m_Socket != null)
                        {
                            workerSocket = m_Socket.EndAccept(result);
                            ConnectionEventArgs e = new ConnectionEventArgs(workerSocket.LocalEndPoint.ToString());
                            if (m_OnClientConnected != null)
                            {
                                m_OnClientConnected(this, e);
                                if (!e.Accept)
                                {
                                    workerSocket.Close();
                                }
                            }
                            if (e.Accept)
                            {
                                byte[] buff = new byte[1024];
                                m_ServerDataBuffers[workerSocket] = buff;
                                //Receive(workerSocket);
                                workerSocket.BeginReceive(buff, 0, buff.Length,
                                    SocketFlags.None, new AsyncCallback(RecieveComplete), workerSocket);
                            }
                        }
                    }
                    // Wait other clients.
                    if (m_Socket != null)
                    {
                        m_Socket.BeginAccept(new AsyncCallback(OnClientConnect), null);
                    }
                }
            }
            catch (ObjectDisposedException)//Server has left.
            {
                if (workerSocket != null)
                {
                    m_ServerDataBuffers.Remove(workerSocket);
                    if (m_OnClientDisconnected != null)
                    {
                        m_OnClientDisconnected(this, new ConnectionEventArgs(workerSocket.LocalEndPoint.ToString()));
                    }
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }
        }
#endif

        /// <summary>
        /// Opens the connection.
        /// </summary>
        /// <remarks>
        /// Protocol, Port and HostName must be set, before calling the Open method.
        /// </remarks>
        /// <example>
        /// <code lang="csharp">
        /// 'This example shows how to start client connection.
        /// //Set Protocol
        /// GXNet1.Protocol = ProtocolType.Tcp;
        /// //Set client port
        /// GXNet1.Port = 1234;
        /// //Set client name
        /// GXNet1.HostName = "localhost";
        /// //Make connection
        /// GXNet1.Open();
        /// //Send data
        /// GXNet1.Send("Hello World!", null);
        /// //The response is received after this through the OnReceived event.
        /// </code>
        /// </example>
        /// <seealso cref="Port">Port</seealso>
        /// <seealso cref="HostName">HostName</seealso>
        /// <seealso cref="Protocol">Protocol</seealso>
        /// <seealso cref="Server">Server</seealso>
        /// <seealso cref="Close">Close</seealso>
        public void Open()
        {
            Close();            
            try
            {
                lock (m_syncBase.m_ReceivedSync)
                {
                    m_syncBase.m_LastPosition = 0;
                }
                NotifyMediaStateChange(MediaState.Opening);
				AddressFamily family = this.UseIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;								
				EndPoint ep = null;
                if (!this.m_Server)
                {
					IPAddress address;
					if (IPAddress.TryParse(HostName, out address))
					{
						switch (address.AddressFamily)
						{
							case AddressFamily.InterNetwork:
								family = address.AddressFamily;
								break;
							case AddressFamily.InterNetworkV6:
								family = address.AddressFamily;
								break;
							default:
								family = address.AddressFamily;
								break;
						}
					}
					else
					{
						// Get host related information.
						IPHostEntry host = Dns.GetHostEntry(HostName);
						foreach (IPAddress ip in host.AddressList)
						{
							if ((ip.AddressFamily == AddressFamily.InterNetworkV6 && this.UseIPv6) ||
								ip.AddressFamily == AddressFamily.InterNetwork && !this.UseIPv6)
							{
								ep = new IPEndPoint(ip, Port);
								break;
							}
						}
					}
                    if (ep == null)
                    {
                        ep = new IPEndPoint(address, Port);
                    }
                }
                // Create a stream-based, TCP socket using the InterNetwork Address Family.                 
                if (Protocol == NetworkType.Tcp)
                {
                    m_Socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
                }
                else if (Protocol == NetworkType.Udp)
                {
                    m_Socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
                }
                else
                {
                    throw new ArgumentException("Protocol");
                }
                if (!this.m_Server)
                {                    
#if WINDOWS_PHONE 
                    // Create DnsEndPoint. The hostName and port are passed in to this method.
                    ep = new DnsEndPoint(HostName, Port);
#else                   					
                    if (m_Trace >= TraceLevel.Info && m_OnTrace != null)
                    {
                        m_OnTrace(this, new TraceEventArgs(TraceTypes.Info, "Client settings: Protocol: " + this.Protocol.ToString() + " Host: " + HostName + " Port: " + Port.ToString()));
                    }                        
#endif                    
                    // Create a SocketAsyncEventArgs object to be used in the connection request
                    SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                    socketEventArg.RemoteEndPoint = ep;
                    SocketError err = 0;
                    // Inline event handler for the Completed event.
                    // Note: This event handler was implemented inline in order to make this method self-contained.
                    socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                    {
                        err = e.SocketError;
                        m_clientDone.Set();
                    });

                    // Sets the state of the event to nonsignaled, causing threads to block
                    m_clientDone.Reset();

                    // Make an asynchronous Connect request over the socket
                    m_Socket.ConnectAsync(socketEventArg);

                    // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
                    // If no response comes back within this time then proceed
                    m_clientDone.WaitOne(TIMEOUT_MILLISECONDS);
                    if (err != 0)
                    {
                        throw new SocketException((int)err);
                    }
#if WINDOWS_PHONE
                    m_Receiver = new ReceiveThread(this, m_Socket, m_syncBase.m_Received);
                    m_ReceiverThread = new Thread(new ThreadStart(m_Receiver.Receive));
                    m_ReceiverThread.IsBackground = true;
                    m_ReceiverThread.Start();
#else
                    m_Socket.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length,
                                                                    SocketFlags.None, new AsyncCallback(RecieveComplete), m_Socket);
#endif
                }
                else
                {
#if !WINDOWS_PHONE
                    if (m_Trace >= TraceLevel.Info && m_OnTrace != null)
                    {
                        m_OnTrace(this, new TraceEventArgs(TraceTypes.Info, "Server settings: Protocol: " + this.Protocol.ToString() + " Port: " + Port.ToString()));
                    }
                    IPEndPoint ipLocal = new IPEndPoint(this.UseIPv6 ? IPAddress.IPv6Any : IPAddress.Any, Port);
                    // Bind to local IP Address...
                    m_Socket.Bind(ipLocal);
                    // Start listening...
                    m_Socket.Listen(4);
                    // Create the call back for any client connections...
                    m_Socket.BeginAccept(new AsyncCallback(OnClientConnect), null);                    
#endif
                }
                NotifyMediaStateChange(MediaState.Open);
            }
            catch
            {
                Close();
                throw;
            }

        }

        /// <inheritdoc cref="IGXMedia.Close"/>        
        /// <example>
        /// <code lang="csharp" source="..\\GXNet csharp Sample\\Form1.cs" region="Close" />
        /// </example>
        public void Close()
        {            
            if (m_Socket != null)
            {
#if WINDOWS_PHONE
                if (m_Receiver != null)
                {
                    m_Receiver.Closing.Set();
                    if (m_ReceiverThread.IsAlive)
                    {
                        m_ReceiverThread.Join();
                    }
                    m_Receiver = null;
                }
#endif                
                m_ServerDataBuffers.Clear();
                try
                {
                    if (m_Socket.Connected)
                    {
                        NotifyMediaStateChange(MediaState.Closing);                        
                    }
                }
                catch (Exception ex)
                {
                    NotifyError(ex);
                    throw;
                }
                finally
                {
                    try
                    {                        
                        m_Socket.Close();
                    }
                    catch
                    {
                        //Ignore all errors on close.
                    }
                    NotifyMediaStateChange(MediaState.Closed);
                    m_Socket = null;
                    m_BytesSend = m_BytesReceived = 0;
                    m_syncBase.m_ReceivedSize = 0;
                    m_syncBase.m_ReceivedEvent.Set();
                }
            }
        }
#if !WINDOWS_PHONE
        /// <inheritdoc cref="IGXMedia.PropertiesForm"/>
        public System.Windows.Forms.Form PropertiesForm
        {
            get
            {
                return new Settings(this);
            }
        }
#endif
        /// <inheritdoc cref="IGXMedia.IsOpen"/>
        /// <seealso cref="Open">Open</seealso>
        /// <seealso cref="Close">Close</seealso>
        [Browsable(false)]
        public bool IsOpen
        {
            get
            {
                return m_Socket != null;
            }
        }        

        /// <summary>
        /// Retrieves or sets the protocol.
        /// </summary>
        /// <remarks>
        /// Defaut protocol is UDP.
        /// </remarks>
        /// <value>
        /// Protocol
        /// </value>
        [DefaultValue(NetworkType.Udp)]
        [Category("Communication")]
        [Description("Retrieves or sets the protocol.")]
        public NetworkType Protocol
        {
            get
            {
                return m_Protocol;
            }
            set
            {
                if (m_Protocol != value)
                {
                    m_Protocol = value;
                    NotifyPropertyChanged("Protocol");
                }
            }
        }        

        /// <summary>
        /// Retrieves or sets the name or IP address of the host.
        /// </summary>
        /// <value>
        /// The name of the host.
        /// </value>
        /// <seealso cref="Open">Open</seealso>
        /// <seealso cref="Port">Port</seealso>
        /// <seealso cref="Protocol">Protocol</seealso>
        [DefaultValue("")]
        [Category("Communication")]
        [Description("Retrieves or sets the name or IP address of the host.")]
        public string HostName
        {
            get
            {
                return m_HostName;
            }
            set
            {
                if (m_HostName != value)
                {
                    m_HostName = value;
                    NotifyPropertyChanged("HostName");
                }                
            }
        }

        /// <summary>
        /// Retrieves or sets the host or server port number.
        /// </summary>
        /// <value>
        /// Host or server port number.
        /// </value>
        /// <seealso cref="Open">Open</seealso>
        /// <seealso cref="HostName">HostName</seealso>  	
        /// <seealso cref="Protocol">Protocol</seealso>
        [DefaultValue(0)]
        [Category("Communication")]
        [Description("Retrieves or sets the host port number.")]
        public int Port
        {
            get
            {
                return m_Port;
            }
            set
            {
                if (m_Port != value)
                {
                    m_Port = value;
                    NotifyPropertyChanged("Port");
                }
            }
        }

#if !WINDOWS_PHONE 
        /// <summary>
        /// Determines if the component is in server, or in client, mode.
        /// </summary>
        /// <value>
        /// True, if component is a server. False, if component is a client.
        /// </value>
        /// <seealso cref="Open">Open</seealso> 	
        [DefaultValue(false)]
        [Category("Communication")]
        [Description("Retrieves or sets the component to client or server mode.")]
        public bool Server
        {
            get
            {
                return m_Server;
            }
            set
            {
                if (m_Server != value)
                {
                    m_Server = value;
                    NotifyPropertyChanged("Server");
                }
            }
        }
#endif

        /// <inheritdoc cref="IGXMedia.Receive"/>
        /// <example>
        /// <code>
        /// 'Send long and wait until OK reply is received or 5 seconds.
        /// 'Data is returned as string.
        /// lock (GXNet1.Synchronous)
        /// {        
        ///     dim params as new Receiveparameters
        ///     params.Eop = "OK"
        ///     params.WaitTime = 10000
        ///     params.Type = typeof(string)
        ///     GXNet1.Send((byte) 0x13 , null)
        ///     GXNet1.Receive(params)
        ///     ' While all data is not received read more data.
        ///     ' This is done because reply data might include "OK" but all data is not read yet.
        ///     'While PacketIsNotCompleted
        ///         GXNet1.Receive(params)
        ///     'Wend
        /// }
        /// 
        /// 'Send data and wait until 4 bytes is received.
        /// 'Received data is received as long (Int32)
        /// lock (GXNet1.Synchronous)
        /// {           
        ///     dim params as new Receiveparameters
        ///     params.Count = 4
        ///     params.WaitTime = 10000
        ///     params.Type = typeof(string)
        ///     GXNet1.Send((byte) 0x13 , null)
        ///     GXNet1.Receive(params)
        /// }
        /// </code>
        /// </example>        
        public bool Receive<T>(ReceiveParameters<T> args)
        {
            return m_syncBase.Receive(args);
        }

        /// <summary>
        /// Sent byte count.
        /// </summary>
        /// <seealso cref="BytesReceived">BytesReceived</seealso>
        /// <seealso cref="ResetByteCounters">ResetByteCounters</seealso>
        [Browsable(false)]
        public UInt64 BytesSent
        {
            get
            {
                return m_BytesSend;
            }
        }

        /// <summary>
        /// Received byte count.
        /// </summary>
        /// <seealso cref="BytesSent">BytesSent</seealso>
        /// <seealso cref="ResetByteCounters">ResetByteCounters</seealso>
        [Browsable(false)]
        public UInt64 BytesReceived
        {
            get
            {
                return m_BytesReceived;
            }
        }

        /// <summary>
        /// Resets BytesReceived and BytesSent counters.
        /// </summary>
        /// <seealso cref="BytesSent">BytesSent</seealso>
        /// <seealso cref="BytesReceived">BytesReceived</seealso>
        public void ResetByteCounters()
        {
            m_BytesSend = m_BytesReceived = 0;
        }       

        /// <summary>
        /// Retrieves or sets maximum count of connected clients.
        /// </summary>
        [DefaultValue(0)]
        [Description("Retrieves or sets maximum count of connected clients.")]
        public int MaxClientCount
        {
            get;
            set;
        }      

#if !WINDOWS_PHONE
        /// <summary>
        /// Disconnect selected client.
        /// </summary>
        public void DisconnectClient(string address)
        {                        
            foreach (var it in m_ServerDataBuffers)
            {
                if (it.Key.LocalEndPoint.ToString() == address)
                {
                    m_ServerDataBuffers.Remove(it.Key);
                    it.Key.Shutdown(SocketShutdown.Both);                    
                    it.Key.Close();
                    if (m_OnClientDisconnected != null)
                    {
                        m_OnClientDisconnected(this, new ConnectionEventArgs(address));
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Returns list IP addresses and port numbers, of active clients in TCP/IP protocol.
        /// </summary>
        public string[] GetActiveClients()
        {
            string[] clients = new string[m_ServerDataBuffers.Count];
            int pos = -1;
            foreach (var it in m_ServerDataBuffers)
            {
                clients[++pos] = it.Key.LocalEndPoint.ToString();
            }
            return clients;
        }
#endif

        /// <summary>
        /// Media settings as a XML string.
        /// </summary>
        public string Settings
        {
            get
            {
                string tmp = "";
                if (m_Server)
                {
                    tmp = "<Server>" + (m_Server ? "1" : "0") + "</Server>" + Environment.NewLine;
                }
                if (!string.IsNullOrEmpty(HostName))
                {
                    tmp += "<IP>" + HostName + "</IP>" + Environment.NewLine;
                }
                if (Port != 0)
                {
                    tmp += "<Port>" + Port + "</Port>" + Environment.NewLine;
                }
                if (Protocol != NetworkType.Tcp)
                {
                    tmp += "<Protocol>" + (int)Protocol + "</Protocol>" + Environment.NewLine;
                }
                return tmp;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    XmlReaderSettings settings = new XmlReaderSettings(); ;
                    settings.ConformanceLevel = ConformanceLevel.Fragment;
                    using (XmlReader xmlReader = XmlReader.Create(new System.IO.StringReader(value), settings))
                    {
                        while (!xmlReader.EOF)
                        {
                            if (xmlReader.IsStartElement())
                            {
                                switch (xmlReader.Name)
                                {
                                    case "Protocol":
                                        Protocol = (NetworkType)xmlReader.ReadElementContentAs(typeof(int), null);
                                        break;
                                    case "Port":
                                        Port = (int)(xmlReader.ReadElementContentAs(typeof(int), null));
                                        break;
                                    case "Server":
                                        m_Server = (int)xmlReader.ReadElementContentAs(typeof(int), null) == 1;
                                        break;
                                    case "IP":
                                        HostName = (string)xmlReader.ReadElementContentAs(typeof(string), null);
                                        break;
                                }
                            }
                            else
                            {
                                xmlReader.Read();
                            }
                        }
                    }
                }
            }
        }          

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                m_OnPropertyChanged += value;
            }
            remove
            {
                m_OnPropertyChanged -= value;
            }
        }

        /// <summary>
        /// GXNet component sends received data through this method.
        /// </summary>
        [Description("GXNet component sends received data through this method.")]
        public event ReceivedEventHandler OnReceived
        {
            add
            {
                m_OnReceived += value;
            }
            remove
            {
                m_OnReceived -= value;
            }
        }

        /// <summary>
        /// Errors that occur after the connection is established, are sent through this method. 
        /// </summary>       
        [Description("Errors that occur after the connection is established, are sent through this method.")]
        public event ErrorEventHandler OnError
        {
            add
            {

                m_OnError += value;
            }
            remove
            {
                m_OnError -= value;
            }
        }

        /// <summary>
        /// Media component sends notification, when its state changes.
        /// </summary>       
        [Description("Media component sends notification, when its state changes.")]
        public event MediaStateChangeEventHandler OnMediaStateChange
        {
            add
            {
                m_OnMediaStateChange += value;
            }
            remove
            {
                m_OnMediaStateChange -= value;
            }
        }
#if WINDOWS_PHONE 
        event ClientConnectedEventHandler IGXMedia.OnClientConnected
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event ClientDisconnectedEventHandler IGXMedia.OnClientDisconnected
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }
#else
        /// <summary>
        /// Called when the client is establishing a connection with a Net Server.
        /// </summary>
        [Description("Called when the client is establishing a connection with a Net Server.")]
        public event ClientConnectedEventHandler OnClientConnected
        {
            add
            {
                m_OnClientConnected += value;
            }
            remove
            {
                m_OnClientConnected -= value;
            }
        }

        /// <summary>
        /// Called when the client has been disconnected from the network server.
        /// </summary>
        [Description("Called when the client has been disconnected from the network server.")]
        public event ClientDisconnectedEventHandler OnClientDisconnected
        {
            add
            {
                m_OnClientDisconnected += value;
            }
            remove
            {
                m_OnClientDisconnected -= value;
            }
        }
        
        /// <inheritdoc cref="TraceEventHandler"/>
        [Description("Called when the Media is sending or receiving data.")]
        public event TraceEventHandler OnTrace
        {
            add
            {
                m_OnTrace += value;
            }
            remove
            {
                m_OnTrace -= value;
            }
        }        

#endif
        //Events
        PropertyChangedEventHandler m_OnPropertyChanged;
        MediaStateChangeEventHandler m_OnMediaStateChange;
        TraceEventHandler m_OnTrace;
#if !WINDOWS_PHONE 
        ClientConnectedEventHandler m_OnClientConnected;
        ClientDisconnectedEventHandler m_OnClientDisconnected;        
#endif
        internal ErrorEventHandler m_OnError;
        internal ReceivedEventHandler m_OnReceived;

        #region IGXMedia Members

        void IGXMedia.Copy(object target)
        {
            GXNet tmp = (GXNet)target;
            Port = tmp.Port;
            HostName = tmp.HostName;
            Protocol = tmp.Protocol;
#if !WINDOWS_PHONE 
            Server = tmp.Server;
#endif            
        }

        string IGXMedia.Name
        {
            get
            {
                string tmp;
                tmp = HostName + " " + Port;
                if (Protocol == NetworkType.Udp)
                {
                    tmp += "UDP";
                }
                else
                {
                    tmp += "TCP/IP";
                }
                return tmp;
            }
        }

        string IGXMedia.MediaType
        {
            get 
            {
                return "Net";
            }
        }

        [System.Runtime.InteropServices.DllImport("wininet.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private extern static bool InternetGetConnectedState(ref InternetConnectionState lpdwFlags, int dwReserved);

        [Flags]
        private enum InternetConnectionState : int
        {
            Modem = 0x1,
            Lan = 0x2,
            Proxy = 0x4,
            RasInstalled = 0x10,
            Offline = 0x20,
            Configured = 0x40
        }

        bool Gurux.Common.IGXMedia.Enabled
        {
            get
            {
                bool isConnected = true;
                if (System.Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    InternetConnectionState flags = InternetConnectionState.Lan | InternetConnectionState.Configured;
                    isConnected = InternetGetConnectedState(ref flags, 0);
                }
                return isConnected;
            }
        }
#if !WINDOWS_PHONE
        /// <summary>
        /// Shows the network Properties dialog.
        /// </summary>
        /// <param name="parent">Owner window of the Properties dialog.</param>
        /// <returns>True, if the user has accepted the changes.</returns>
        /// <seealso cref="Port">Port</seealso>
        /// <seealso cref="HostName">HostName</seealso>
        /// <seealso cref="Protocol">Protocol</seealso>
        /// <seealso cref="Server">Server</seealso>        
        /// <seealso href="PropertiesDialog.html">Properties Dialog</seealso>
        public bool Properties(System.Windows.Forms.Form parent)
        {
            return new Gurux.Shared.PropertiesForm(PropertiesForm, Gurux.Net.Resources.SettingsTxt, IsOpen).ShowDialog(parent) == System.Windows.Forms.DialogResult.OK;
        }
#endif
        /// <inheritdoc cref="IGXMedia.Synchronous"/>
        public object Synchronous
        {
            get 
            {
                return m_Synchronous;
            }
        }

        /// <inheritdoc cref="IGXMedia.IsSynchronous"/>
        public bool IsSynchronous
        {
            get 
            {
                bool reserved = System.Threading.Monitor.TryEnter(m_Synchronous, 0);
                if (reserved)
                {
                    System.Threading.Monitor.Exit(m_Synchronous);
                }
                return !reserved;
            }
        }

        /// <inheritdoc cref="IGXMedia.ResetSynchronousBuffer"/>
        public void ResetSynchronousBuffer()
        {
            lock (m_syncBase.m_ReceivedSync)
            {
                m_syncBase.m_ReceivedSize = 0;
            }
        }

        /// <inheritdoc cref="IGXMedia.Validate"/>
        public void Validate()
        {
            if (Port == 0)
            {
                throw new Exception("Invalid port name.");
            }
            if (!m_Server && string.IsNullOrEmpty(HostName))
            {
                throw new Exception("Invalid host name.");
            }
        }

        /// <inheritdoc cref="IGXMedia.Eop"/>
        public object Eop
        {
            get;
            set;
        }

        int IGXMedia.ConfigurableSettings
        {
            get;
            set;
        }

        #endregion

        #region IDisposable Members

		/// <summary>
		/// Closes the connection.
		/// </summary>
        public void Dispose()
        {
            Close();
        }

        #endregion
    }
}
