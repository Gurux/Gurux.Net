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

namespace Gurux.Net
{
    /// <summary>
    /// <p>Join the Gurux Community or follow <a href="https://twitter.com/guruxorg">@Gurux</a> for project updates.</p>
    /// <p>Open Source GXNet media component, made by Gurux Ltd, is a part of GXMedias set of media components, 
    /// which programming interfaces help you implement communication by chosen connection type. 
    /// Gurux media components also support the following connection types: serial port and terminal.</p>
    /// <p>For more info check out <a href="http://www.gurux.org/" title="Gurux">Gurux</a>.</p>
    /// <p>We are updating documentation on Gurux web page. </p>
    /// <p>If you have problems you can ask your questions in Gurux <a     /// href="http://www.gurux.org/forum">Forum</a>.</p>
    /// <h1><a name="simple-example" class="anchor" href="#simple-example"><span class="mini-icon mini-icon-link"></span></a>Simple example</h1>
    /// <p>Before use you must set following settings:</p>
    /// <ul>
    /// <li><see cref="Gurux.Net.GXNet.HostName"/></li>
    /// <li><see cref="Gurux.Net.GXNet.Port"/></li>
    /// <li><see cref="Gurux.Net.GXNet.Protocol"/></li>
    /// </ul><p>It is also good to listen following events:</p>
    /// <ul>
    /// <li><see cref="Gurux.Net.GXNet.OnError"/></li>
    /// <li><see cref="Gurux.Net.GXNet.OnReceived"/></li>
    /// <li><see cref="Gurux.Net.GXNet.OnMediaStateChange"/></li>
    /// </ul>
    /// <p>and if in server mode following events might be important.</p>
    /// <ul>
    /// <li>OnClientConnected</li>
    /// <li>OnClientDisconnected</li>
    /// </ul>
    /// <example>
    /// <code>
    /// GXNet cl = new GXNet();
    /// cl.HostName = "localhost";
    /// cl.Port = 1000;
    /// cl.Protocol = NetworkType.Tcp;
    /// cl.Open();
    /// </code>
    /// </example>
    /// Data is send with send command:
    /// <example>
    /// <code>
    /// cl.Send("Hello World!");
    /// </code>
    /// </example>
    /// In default mode received data is coming as asynchronously from OnReceived event.
    /// <example>
    /// <code>
    /// cl.OnReceived += new ReceivedEventHandler(this.OnReceived);
    /// </code>
    /// </example>
    /// Data can be send as syncronous if needed:
    /// <example>
    /// <code>
    /// lock (cl.Synchronous)
    /// {
    ///     string reply = "";
    ///     ReceiveParameters&lt;string&gt; p = new ReceiveParameters&lt;string&gt;()
    ///     //ReceiveParameters&lt;byte[]&gt; p = new ReceiveParameters&lt;byte[]&gt;()
    ///     {
    ///         //Wait time tells how long data is waited.
    ///         WaitTime = 1000,
    ///         //Eop tells End Of Packet charachter.
    ///         p.Eop = '\\r'
    ///     }
    ///     gxNet1.Send("Hello World!", null);
    ///     if (gxNet1.Receive(p))
    ///     {
    ///         reply = Convert.ToString(p.Reply)
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGenerated]
    class NamespaceDoc
    {
    }
}