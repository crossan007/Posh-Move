Function Load-Types {

$references = @("System.Xml")
$Source = Get-Content -Path "C:\Users\ccrossan\source\repos\Encryption\Encryption\Program.cs" -Raw
    
Add-Type -ReferencedAssemblies $references -TypeDefinition $Source -Language CSharp 

}


Function Send {
    <#
        .SYNOPSIS
            Sends a file using a passphrase to a recipient
    #>
    Param(
        $File,
        $Passphrase
    )
    Load-Types
    [PoshMove.Utils]::Send($file,"127.0.0.1")

}


Function Receive {
    Param(
        $ReceivePath
    )
    Load-Types
    [PoshMove.Utils]::Receive("./recpt")
}