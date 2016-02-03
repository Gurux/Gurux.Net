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
using Gurux.Net.Properties;

namespace Gurux.Net
{    
    /// <summary>
    /// The GXNet component determines methods that make the communication possible using Internet. 
    /// See help in http://www.gurux.org/Gurux.Net
    /// </summary>
    public class GXNet : IGXMedia, IGXVirtualMedia, INotifyPropertyChanged, IDisposable
    {
#if WINDOWS_PHONE
        ReceiveThread m_Receiver;
        Thread m_ReceiverThread;
#endif
        
        bool isVirtual, isVirtualOpen, isClone;
        // Define a timeout in milliseconds for each asynchronous call. If a response is not received within this 
        // timeout period, the call is aborted.
        const int TIMEOUT_MILLISECONDS = 5000;
        internal byte[] ReceiveBuffer = new byte[1024];
        /// <summary>
        /// Used protocol.
        /// </summary>
        NetworkType communicationProtocol;
        /// <summary>
        /// Connection host name.
        /// </summary>
        string hostAddress;
        /// <summary>
        /// Host port.
        /// </summary>
        int port;
        /// <summary>
        /// Is this server or client.
        /// </summary>
        internal bool isServer;
        internal GXSynchronousMediaBase syncBase;
        /// <summary>
        /// Server or client socket.
        /// </summary>
        Socket socket = null;
        internal Dictionary<Socket, byte[]> m_ServerDataBuffers = new Dictionary<Socket, byte[]>();        
        readonly object m_Synchronous = new object();
        private object m_sync = new object();

        // Signaling object used to notify when an asynchronous operation is completed
        static ManualResetEvent m_clientDone = new ManualResetEvent(false);

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXNet()
        {
            syncBase = new GXSynchronousMediaBase(1024);
            ConfigurableSettings = AvailableMediaSettings.All;
            Protocol = NetworkType.Tcp;            
        }

        /// <summary>
        /// Client Constructor.
        /// </summary>
        /// <param name="protocol">Used protocol.</param>
        /// <param name="hostName">Host name.</param>
        /// <param name="connectionPort">Client connection port.</param>
        public GXNet(NetworkType protocol, string hostName, int connectionPort)
            : this()
        {
            communicationProtocol = protocol;
            hostAddress = hostName;
            port = connectionPort;            
        }

        /// <summary>
        /// Constructor used when server is started.
        /// </summary>
        /// <param name="protocol">Used protocol.</param>
        /// <param name="listeningPort">Server listening port.</param>
        public GXNet(NetworkType protocol, int listeningPort) 
            : this()
        {
            isServer = true;
            protocol = communicationProtocol;
            port = listeningPort;         
        }

        /// <summary>
        /// Make clone from Network component.
        /// </summary>
        /// <remarks>
        /// This can be used in server side if server 
        /// want to start communicating with client using syncronous communication.
        /// Clone do not close connection.
        /// </remarks>
        public GXNet Clone()
        {
            GXNet net = new GXNet();
            net.isServer = isServer;
            net.syncBase = new GXSynchronousMediaBase(1024);
            net.ConfigurableSettings = ConfigurableSettings;
            net.Protocol = Protocol;
            net.isServer = isServer;
            net.hostAddress = hostAddress;
            net.port = port;
            net.socket = socket;
            net.Trace = Trace;
            net.m_ServerDataBuffers = m_ServerDataBuffers;
            net.isClone = true;
            return net;
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
                return syncBase.Trace;
            }
            set
            {
                syncBase.Trace = value;
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
            if (Trace >= TraceLevel.Error && m_OnTrace != null)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Error, ex, null));
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

        /// <inheritdoc cref="IGXMedia.Tag"/>
        public object Tag
        {
            get;
            set;
        }

        /// <inheritdoc cref="IGXMedia.MediaContainer"/>
        IGXMediaContainer IGXMedia.MediaContainer
        {
            get;
            set;
        }

        /// <inheritdoc cref="IGXMedia.SyncRoot"/>
        [Browsable(false), ReadOnly(true)]
        public object SyncRoot
        {
            get
            {
                //In some special cases when binary serialization is used this might be null
                //after deserialize. Just set it.
                if (m_sync == null)
                {
                    m_sync = new object();
                }
                return m_sync;
            }
        }

        /// <inheritdoc cref="IGXVirtualMedia.Virtual"/>
        bool IGXVirtualMedia.Virtual
        {
            get
            {
                return isVirtual;
            }
            set
            {
                isVirtual = value;
            }
        }

        /// <summary>
        /// Occurs when a property value is asked.
        /// </summary>
        event GetPropertyValueEventHandler IGXVirtualMedia.OnGetPropertyValue
        {
            add
            {
                m_OnGetPropertyValue += value;
            }
            remove
            {
                m_OnGetPropertyValue -= value;
            }
        }

         /// <summary>
        /// Occurs when data is sent on virtual mode.
        /// </summary>
        event ReceivedEventHandler IGXVirtualMedia.OnDataSend
        {
            add
            {
                m_OnDataSend += value;
            }
            remove
            {
                m_OnDataSend -= value;
            }
        }

        /// <summary>
        /// Called when new data is received to the virtual media.
        /// </summary>
        /// <param name="data">received data</param>
        /// <param name="sender">Data sender.</param>
        void IGXVirtualMedia.DataReceived(byte[] data, string sender)
        {
            HandleReceivedData(data.Length, data, sender);
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
        /// If data is send synchronously use Synchronous</remarks>
        /// <seealso cref="OnReceived"/>
        /// <seealso cref="Synchronous"/>
        public void Send(object data, string receiver)
        {
            if (socket == null && !isVirtualOpen)
            {
                throw new Exception(Resources.InvalidConnection);
            }
            if (!this.isServer)
            {
                byte[] value = Gurux.Common.GXCommon.GetAsByteArray(data);
                if (Trace == TraceLevel.Verbose && m_OnTrace != null)
                {
                    m_OnTrace(this, new TraceEventArgs(TraceTypes.Sent, value, receiver));
                }
                //Reset last position if Eop is used.
                lock (syncBase.receivedSync)
                {
                    syncBase.lastPosition = 0;
                }
                if (!isVirtual)
                {
                    // Create SocketAsyncEventArgs context object
                    SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                    SocketError err = 0;
                    try
                    {
                        // Set properties on context object
                        socketEventArg.RemoteEndPoint = socket.RemoteEndPoint;
                        socketEventArg.UserToken = null;
                        // Inline event handler for the Completed event.
                        // Note: This event handler was implemented inline in order to make this method self-contained.
                        socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                        {
                            err = e.SocketError;
                            m_clientDone.Set();
                        });

                        socketEventArg.SetBuffer(value, 0, value.Length);
                        // Sets the state of the event to non-signaled, causing threads to block
                        m_clientDone.Reset();
                        // Make an asynchronous Send request over the socket
                        socket.SendAsync(socketEventArg);

                        // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
                        // If no response comes back within this time then proceed
                        m_clientDone.WaitOne(TIMEOUT_MILLISECONDS);
                    }
                    finally
                    {
                        socketEventArg.Dispose();
                    }
                    if (err != 0)
                    {
                        throw new SocketException((int)err);
                    }
                }
                else
                {
                    m_OnDataSend(this, new ReceiveEventArgs(data, receiver));
                }
                this.BytesSent += (ulong)value.Length;
            }
            else
            {
#if !WINDOWS_PHONE
                Socket client = null;
                foreach (var it in m_ServerDataBuffers)
                {
                    if (it.Key.RemoteEndPoint.ToString() == receiver)
                    {
                        client = it.Key;
                        break;
                    }
                }
                if (client == null)
                {
                    throw new Exception(Resources.InvalidClient);
                }
                byte[] value = Gurux.Common.GXCommon.GetAsByteArray(data);
                client.Send(value);
                this.BytesSent += (ulong)value.Length;
#endif
            }            
        }

        void NotifyMediaStateChange(MediaState state)
        {
            if (Trace >= TraceLevel.Info && m_OnTrace != null)
            {
                m_OnTrace(this, new TraceEventArgs(TraceTypes.Info, state, null));
            }
            if (m_OnMediaStateChange != null)
            {
                m_OnMediaStateChange(this, new MediaStateEventArgs(state));
            }                
        }

#if !WINDOWS_PHONE
        /// <summary>
        /// New data is received.
        /// </summary>
        /// <param name="result"></param>
        void RecieveComplete(IAsyncResult result)
        {
            Socket socket = null;
            try
            {
                int bytes = 0;
                byte[] buff = null;
                socket = result.AsyncState as Socket;
                if (socket.Connected)
                {
                    bytes = socket.EndReceive(result);
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
                }
                if (this.Server)
                {
                    string sender = socket.RemoteEndPoint.ToString();
                    if (bytes == 0)
                    {
                        //Client has left.
                        DisconnectClient(sender);
                        return;
                    }
                }
                if (bytes != 0)
                {
                    string sender = socket.RemoteEndPoint.ToString();
                    bytes = HandleReceivedData(bytes, buff, sender);
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
                            m_OnClientDisconnected(this, new ConnectionEventArgs(socket.RemoteEndPoint.ToString()));
                        }
                    }
                    else
                    {
                        syncBase.Exception = ex;
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

        private int HandleReceivedData(int bytes, byte[] buff, string sender)
        {
            BytesReceived += (uint)bytes;
            if (this.IsSynchronous)
            {
                TraceEventArgs arg = null;
                lock (syncBase.receivedSync)
                {
                    int index = syncBase.receivedSize;
                    syncBase.AppendData(buff, 0, bytes);
                    if (bytes != 0 && Trace == TraceLevel.Verbose && m_OnTrace != null)
                    {
                        arg = new TraceEventArgs(TraceTypes.Received, buff, 0, bytes, null);
                        m_OnTrace(this, arg);
                    }
                    if (bytes != 0 && Eop != null) //Search Eop if given.
                    {
                        if (Eop is Array)
                        {
                            foreach (object eop in (Array)Eop)
                            {
                                bytes = GXCommon.IndexOf(syncBase.m_Received, GXCommon.GetAsByteArray(eop), index, syncBase.receivedSize);
                                if (bytes != -1)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            bytes = GXCommon.IndexOf(syncBase.m_Received, GXCommon.GetAsByteArray(Eop), index, syncBase.receivedSize);
                        }
                    }
                    if (bytes != -1)
                    {
                        syncBase.receivedEvent.Set();
                    }
                }
            }
            else
            {
                if (m_OnReceived != null)
                {
                    syncBase.receivedSize = 0;
                    byte[] data = new byte[bytes];
                    Array.Copy(buff, data, bytes);
                    if (Trace == TraceLevel.Verbose && m_OnTrace != null)
                    {
                        m_OnTrace(this, new TraceEventArgs(TraceTypes.Received, data, null));
                    }
                    m_OnReceived(this, new ReceiveEventArgs(data, sender));
                }
                else if (Trace == TraceLevel.Verbose && m_OnTrace != null)
                {
                    m_OnTrace(this, new TraceEventArgs(TraceTypes.Received, buff, 0, bytes, null));
                }
            }
            return bytes;
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
				if (socket == null)
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
                        if (socket != null)
                        {
                            workerSocket = socket.EndAccept(result);
                            ConnectionEventArgs e = new ConnectionEventArgs(workerSocket.RemoteEndPoint.ToString());
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
                                workerSocket.BeginReceive(buff, 0, buff.Length,
                                    SocketFlags.None, new AsyncCallback(RecieveComplete), workerSocket);
                            }
                        }
                    }
                    // Wait other clients.
                    if (socket != null)
                    {
                        socket.BeginAccept(new AsyncCallback(OnClientConnect), null);
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
                        m_OnClientDisconnected(this, new ConnectionEventArgs(workerSocket.RemoteEndPoint.ToString()));
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
                lock (syncBase.receivedSync)
                {
                    syncBase.lastPosition = 0;
                }                
                EndPoint ep = null;
                NotifyMediaStateChange(MediaState.Opening);
                AddressFamily family = this.UseIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
                if (!this.isServer && !isVirtual)
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
                        IPHostEntry host = Dns.GetHostEntry(hostAddress);
                        foreach (IPAddress ip in host.AddressList)
                        {
                            if ((ip.AddressFamily == AddressFamily.InterNetworkV6 && this.UseIPv6) ||
                                ip.AddressFamily == AddressFamily.InterNetwork && !this.UseIPv6)
                            {
                                ep = new IPEndPoint(ip, port);
                                break;
                            }
                        }
                    }
                    if (ep == null)
                    {
                        ep = new IPEndPoint(address, port);
                    }
                }
                // Create a stream-based, TCP socket using the InterNetwork Address Family.                 
                if (communicationProtocol == NetworkType.Tcp)
                {
                    if (!isVirtual)
                    {
                        socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
                    }
                }
                else if (communicationProtocol == NetworkType.Udp)
                {
                    if (!isVirtual)
                    {
                        socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
                    }
                }
                else
                {
                    throw new ArgumentException(Resources.ProtocolTxt);
                }
                if (!this.isServer)
                {
#if WINDOWS_PHONE 
                    // Create DnsEndPoint. The hostName and port are passed in to this method.
                    ep = new DnsEndPoint(HostName, Port);
#else
                    if (Trace >= TraceLevel.Info && m_OnTrace != null)
                    {
                        string str = string.Format("{0} {1} {2} {3} {4} {5} {6}", 
                            Resources.ClientSettings, 
                            Resources.ProtocolTxt, 
                            communicationProtocol.ToString(), 
                            Resources.HostNameTxt, 
                            hostAddress, 
                            Resources.PortTxt, 
                            port.ToString());
                        m_OnTrace(this, new TraceEventArgs(TraceTypes.Info, str, null));
                    }
#endif
                    // Create a SocketAsyncEventArgs object to be used in the connection request
                    SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                    socketEventArg.RemoteEndPoint = ep;
                    SocketError err = 0;
                    try
                    {
                        // Inline event handler for the Completed event.
                        // Note: This event handler was implemented inline in order to make this method self-contained.
                        if (!isVirtual)
                        {
                            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                            {
                                err = e.SocketError;
                                m_clientDone.Set();
                            });

                            // Sets the state of the event to nonsignaled, causing threads to block
                            m_clientDone.Reset();

                            // Make an asynchronous Connect request over the socket
                            socket.ConnectAsync(socketEventArg);

                            // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
                            // If no response comes back within this time then proceed
                            m_clientDone.WaitOne(TIMEOUT_MILLISECONDS);
                            if (err != 0)
                            {
                                throw new SocketException((int)err);
                            }
                        }
                    }
                    finally
                    {
                        if (!isVirtual)
                        {
                            socketEventArg.Dispose();
                        }
                    }

#if WINDOWS_PHONE
                    m_Receiver = new ReceiveThread(this, m_Socket, m_syncBase.m_Received);
                    m_ReceiverThread = new Thread(new ThreadStart(m_Receiver.Receive));
                    m_ReceiverThread.IsBackground = true;
                    m_ReceiverThread.Start();
#else
                    if (!isVirtual)
                    {
                        socket.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length,
                                                                        SocketFlags.None, new AsyncCallback(RecieveComplete), socket);
                    }
                    isVirtualOpen = true;
#endif
                }
                else
                {
#if !WINDOWS_PHONE
                    if (Trace >= TraceLevel.Info && m_OnTrace != null)
                    {
                        string str = string.Format("{0} {1} {2} {3} {4}",
                                    Resources.ServerSettings,
                                    Resources.ProtocolTxt,
                                    communicationProtocol,
                                    Resources.PortTxt,
                                    port);
                        m_OnTrace(this, new TraceEventArgs(TraceTypes.Info, str, null));
                    }
                    if (!isVirtual)
                    {
                        IPEndPoint ipLocal = new IPEndPoint(this.UseIPv6 ? IPAddress.IPv6Any : IPAddress.Any, port);
                        // Bind to local IP Address...
                        socket.Bind(ipLocal);
                        // Start listening...
                        socket.Listen(4);
                        // Create the call back for any client connections...
                        socket.BeginAccept(new AsyncCallback(OnClientConnect), null);
                    }
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
        public void Close()
        {
            if (!isClone && (socket != null || isVirtualOpen))
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
                    if (isVirtualOpen || socket.Connected)
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
                        if (socket != null)
                        {
                            socket.Close();
                        }
                    }
                    catch
                    {
                        //Ignore all errors on close.
                    }
                    isVirtualOpen = false;
                    NotifyMediaStateChange(MediaState.Closed);
                    socket = null;
                    BytesSent = BytesReceived = 0;
                    syncBase.receivedSize = 0;
                    syncBase.receivedEvent.Set();
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
                return socket != null || isVirtualOpen;
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
                if (isVirtual && m_OnGetPropertyValue != null)
                {
                    string value = m_OnGetPropertyValue("Protocol");                    
                    if (value != null)
                    {
                        return (NetworkType)int.Parse(value);
                    }                    
                }
                return communicationProtocol;
            }
            set
            {
                if (communicationProtocol != value)
                {
                    communicationProtocol = value;
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
                if (isVirtual && m_OnGetPropertyValue != null)
                {
                    string value = m_OnGetPropertyValue("HostName");
                    if (value != null)
                    {
                        return value;
                    }                    
                }
                return hostAddress;
            }
            set
            {
                if (hostAddress != value)
                {
                    hostAddress = value;
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
                if (isVirtual && m_OnGetPropertyValue != null)
                {
                    string value = m_OnGetPropertyValue("Port");
                    if (value != null)
                    {
                        return int.Parse(value);
                    }                    
                }
                return port;
            }
            set
            {
                if (port != value)
                {
                    port = value;
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
                if (isVirtual && m_OnGetPropertyValue != null)
                {
                    string value = m_OnGetPropertyValue("Server");
                    if (value != null)
                    {
                        return bool.Parse(value);
                    }                    
                }
                return isServer;
            }
            set
            {
                if (isServer != value)
                {
                    isServer = value;
                    NotifyPropertyChanged("Server");
                }
            }
        }
#endif

        /// <inheritdoc cref="IGXMedia.Receive"/>       
        public bool Receive<T>(ReceiveParameters<T> args)
        {
            return syncBase.Receive(args);
        }

        /// <summary>
        /// Sent byte count.
        /// </summary>
        /// <seealso cref="BytesReceived">BytesReceived</seealso>
        /// <seealso cref="ResetByteCounters">ResetByteCounters</seealso>
        [Browsable(false)]
        public UInt64 BytesSent
        {
            get;
            private set;
        }

        /// <summary>
        /// Received byte count.
        /// </summary>
        /// <seealso cref="BytesSent">BytesSent</seealso>
        /// <seealso cref="ResetByteCounters">ResetByteCounters</seealso>
        [Browsable(false)]
        public UInt64 BytesReceived
        {
            get;
            private set;
        }

        /// <summary>
        /// Resets BytesReceived and BytesSent counters.
        /// </summary>
        /// <seealso cref="BytesSent">BytesSent</seealso>
        /// <seealso cref="BytesReceived">BytesReceived</seealso>
        public void ResetByteCounters()
        {
            BytesSent = BytesReceived = 0;
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
                if (it.Key.RemoteEndPoint.ToString() == address)
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
                clients[++pos] = it.Key.RemoteEndPoint.ToString();
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
                if (isServer)
                {
                    tmp = "<Server>" + (isServer ? "1" : "0") + "</Server>" + Environment.NewLine;
                }
                if (!string.IsNullOrEmpty(hostAddress))
                {
                    tmp += "<IP>" + hostAddress + "</IP>" + Environment.NewLine;
                }
                if (port != 0)
                {
                    tmp += "<Port>" + port + "</Port>" + Environment.NewLine;
                }
                if (communicationProtocol != NetworkType.Tcp)
                {
                    tmp += "<Protocol>" + (int)communicationProtocol + "</Protocol>" + Environment.NewLine;
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
                                        communicationProtocol = (NetworkType)xmlReader.ReadElementContentAs(typeof(int), null);
                                        break;
                                    case "Port":
                                        port = (int)(xmlReader.ReadElementContentAs(typeof(int), null));
                                        break;
                                    case "Server":
                                        isServer = (int)xmlReader.ReadElementContentAs(typeof(int), null) == 1;
                                        break;
                                    case "IP":
                                        hostAddress = (string)xmlReader.ReadElementContentAs(typeof(string), null);
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
        GetPropertyValueEventHandler m_OnGetPropertyValue;
#if !WINDOWS_PHONE 
        ClientConnectedEventHandler m_OnClientConnected;
        ClientDisconnectedEventHandler m_OnClientDisconnected;        
#endif
        internal ErrorEventHandler m_OnError;
        internal ReceivedEventHandler m_OnReceived;
        ReceivedEventHandler m_OnDataSend;

        #region IGXMedia Members

        void IGXMedia.Copy(object target)
        {
            GXNet tmp = (GXNet)target;
            port = tmp.port;
            hostAddress = tmp.hostAddress;
            communicationProtocol = tmp.communicationProtocol;
#if !WINDOWS_PHONE 
            Server = tmp.Server;
#endif            
        }

        string IGXMedia.Name
        {
            get
            {
                string tmp;
                tmp = HostName + " " + port;
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
            return new Gurux.Shared.PropertiesForm(PropertiesForm, Resources.SettingsTxt, IsOpen).ShowDialog(parent) == System.Windows.Forms.DialogResult.OK;
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
            lock (syncBase.receivedSync)
            {
                syncBase.receivedSize = 0;
            }
        }

        /// <inheritdoc cref="IGXMedia.Validate"/>
        public void Validate()
        {
            if (port == 0)
            {
                throw new Exception(Resources.InvalidPortName);
            }
            if (!isServer && string.IsNullOrEmpty(hostAddress))
            {
                throw new Exception(Resources.InvalidHostName);
            }
        }

        /// <inheritdoc cref="IGXMedia.Eop"/>
        public object Eop
        {
            get;
            set;
        }

        /// <inheritdoc cref="IGXMedia.ConfigurableSettings"/>
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
