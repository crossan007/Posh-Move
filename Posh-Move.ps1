Function Load-Types {
    $references = @("System.Xml", "LZ4")
    $Source = Get-Content -Path "$PSScriptRoot\\Program.cs" -Raw
    Add-Type -Path "$PSScriptRoot\\LZ4.dll"
    Add-Type -ReferencedAssemblies $references -TypeDefinition $Source -Language CSharp 
}
Function Send-File {
    <#
        .SYNOPSIS
            Sends a file using a passphrase to a recipient
    #>
    Param(
        [Parameter(Mandatory=$true)]
        $File,
        [Parameter(Mandatory=$true)]
        $Passphrase,
        [Parameter(Mandatory=$true)]
        $TargetAddress,
        [Parameter(Mandatory=$true)]
        $TargetPort
    )
    Invoke-Command -ScriptBlock {
        Param(
            $file, $TargetAddress, $TargetPort, $Passphrase
        )
        Load-Types
        [PoshMove.Utils]::Send($file, $TargetAddress, $TargetPort, $Passphrase)
    } -ArgumentList @($file, $TargetAddress, $TargetPort, $Passphrase)
   

}

Function Receive-File {
    Param(
        [Parameter(Mandatory=$true)]
        $ReceivePath,
        [Parameter(Mandatory=$true)]
        $ListenPort,
        [Parameter(Mandatory=$true)]
        $Passphrase
    )
    Invoke-Command -ScriptBlock {
        Param(
            $ReceivePath, $ListenPort, $Passphrase
        )
        Load-Types
        [PoshMove.Utils]::Receive($ReceivePath, $ListenPort, $Passphrase)
    } -ArgumentList @($ReceivePath, $ListenPort, $Passphrase)
}