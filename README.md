See An [Gurux](http://www.gurux.org/ "Gurux") for an overview.

Join the Gurux Community or follow [@Gurux](https://twitter.com/guruxorg "@Gurux") for project updates.

Open Source GXNet media component, made by Gurux Ltd, is a part of GXMedias set of media components, which programming interfaces help you implement communication by chosen connection type. Gurux media components also support the following connection types: serial port and terminal.

For more info check out [Gurux](http://www.gurux.org/ "Gurux").

We are updating documentation on Gurux web page. 

If you have problems you can ask your questions in Gurux [Forum](http://www.gurux.org/forum).

Build
=========================== 
If you want to build example you need Nuget package manager for Visual Studio.
You can get it here:
https://visualstudiogallery.msdn.microsoft.com/27077b70-9dad-4c64-adcf-c7cf6bc9970c

Simple example
=========================== 
Before use you must set following settings:
* HostName
* Port
* Protocol

It is also good to listen following events:
* OnError
* OnReceived
* OnMediaStateChange

and if in server mode following events might be important.
* OnClientConnected
* OnClientDisconnected                

```csharp

GXNet cl = new GXNet();
cl.HostName = "localhost";
cl.Port = 1000;
cl.Protocol = NetworkType.Tcp;
cl.Open();

```

Data is send with send command:

```csharp
cl.Send("Hello World!");
```
In default mode received data is coming as asynchronously from OnReceived event.

```csharp
cl.OnReceived += new ReceivedEventHandler(this.OnReceived);

```
Data can be send as syncronous if needed:

```csharp
lock (cl.Synchronous)
{
    string reply = "";
    ReceiveParameters<string> p = new ReceiveParameters<string>()
    //ReceiveParameters<byte[]> p = new ReceiveParameters<byte[]>()
    {
       //Wait time tells how long data is waited.
       WaitTime = 1000,
       //Eop tells End Of Packet charachter.
       Eop = '\r'
    };
    cl.Send("Hello World!", null);
    if (gxNet1.Receive(p))
    {
	reply = Convert.ToString(p.Reply);
    }
}
```