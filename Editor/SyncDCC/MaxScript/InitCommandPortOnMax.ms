global port = 6001
global bufferSize = 4096

Fn BackgroundTcpListenerDoWork theSender theEvent = 
(
	IPAddress = DotNetClass "System.Net.IPAddress"
	theIPAddress = IPAddress.Any
	theTcpListener = DotNetObject "System.Net.Sockets.TcpListener" theIPAddress port
	theTcpListener.Start()
	
	theString="";
	
	while theString!="exit" do
	(
		theSocket = theTcpListener.AcceptSocket()
		
		theByteStream = DotNetObject "System.Byte[]" bufferSize
		
		theSocket.Receive theByteStream
		Encoding = DotnetClass "System.Text.Encoding"
		theString = trimright (Encoding.ASCII.GetString(theByteStream))
			
		print theString

		if theString!="" do (try(execute theString)catch(print "Error Occured!") )
		
		theSocket.Close()

	)
	theTcpListener.Stop()
)

BackgroundWorker = DotNetObject "System.ComponentModel.BackgroundWorker"
DotNet.AddEventHandler BackgroundWorker "DoWork" BackgroundTcpListenerDoWork 
BackgroundWorker.WorkerSupportsCancellation = true
BackgroundWorker.RunWorkerAsync()
