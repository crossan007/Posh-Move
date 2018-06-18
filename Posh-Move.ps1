Function Send {
    <#
        .SYNOPSIS
            Sends a file using a passphrase to a recipient
    #>
    Param(
        $File,
        $Passphrase
    )

    $Localhost = [System.Net.dns]::GetHostEntry([system.net.dns]::GetHostName())

    $ipAddress = $Localhost.AddressList[0];  
    $remoteEP = [System.Net.IPEndPoint]::new($ipAddress,11000)

    $SenderSocket = [System.Net.Sockets.Socket]::new($ipAddress.AddressFamily, [System.Net.Sockets.SocketType]::Stream, [System.Net.Sockets.ProtocolType]::Tcp )

    $SenderSocket.Connect($remoteEP)

    $FileInfo  = @{
        FullName = $File.FullName
        Bytes = $File.length
    }

    $msg = [System.Text.Encoding]::ASCII.GetBytes("$($FileInfo | ConvertTo-JSON)<EOF>")
    $mStream = [System.IO.MemoryStream]::new($msg)
    $bytesSent = $SenderSocket.Send($msg);  

    $nets = [System.Net.Sockets.NetworkStream]::new($SenderSocket)

    $file = [System.IO.FileStream]::new($($File.FullName), [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read)
    $file.CopyTo($nets)
    start-sleep -Milliseconds 100
    $file.close()
    $nets.Flush()
    $nets.close()




}


Function Receive {
    Param(
        $ReceivePath
    )
    $bytes = [System.Byte[]]::new(1024)
    $Localhost = [System.Net.dns]::GetHostEntry([system.net.dns]::GetHostName())
    $ipAddress = $Localhost.AddressList[0];  
    $LocalEP = [System.Net.IPEndPoint]::new($ipAddress,11000)
    $receiveSocket = [System.Net.Sockets.Socket]::new($ipAddress.AddressFamily, [System.Net.Sockets.SocketType]::Stream, [System.Net.Sockets.ProtocolType]::Tcp )

    try {
        $receiveSocket.bind($LocalEP)
        $receiveSocket.Listen(1)    
    
        
        write-host "Waiting for a connection..."
    
        $handler = $receiveSocket.Accept();  
        $data = "";  
    
        while ($true) {
            $bytesRec = $handler.Receive($bytes);  
            $data += [System.Text.Encoding]::ASCII.GetString($bytes,0,$bytesRec);  
            if ($data.IndexOf("<EOF>") -gt -1) {  
                break
            }  
        }  
    
        $FileInfo = $data.substring(0, ($data.length-5)) | ConvertFrom-Json
        
        $FileName = [System.IO.Path]::GetFileName($FileInfo.FullName)
        Write-Host $FileName
        $OutFile = [System.IO.Path]::Combine($ReceivePath,$FileName)
        Write-Host "Receiving File: $($FileInfo.FullName) to $OutFile"

        $fileStream = [System.IO.FileStream]::new($OutFile,[System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite)
        $pos = 0
        while ($pos -lt $($FileInfo.Bytes)) {
            $bytesRec = $handler.Receive($bytes);  
            Write-Host "Received $BytesRec Bytes"
            $pos += $bytesRec
            $fileStream.Write($bytes,0,$bytesRec)
        }  
        $fileStream.Close()
        Write-Host "Done receiving File: $($FileInfo.FullName). Cleaning up"
       

    
    
        #Echo the data back to the client.  
        #byte[] msg = Encoding.ASCII.GetBytes(data); 
       
    }
    catch {
        throw $_
    }
    finally {
        $fileStream.close()
        Write-Host "Streams Closed" 
        $handler.Shutdown([System.Net.Sockets.SocketShutdown]::Both)  
        $handler.Close()
    }
   
    


}