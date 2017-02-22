Imports System.Runtime.InteropServices, System.Text


Friend Class ProcM

#Region "Variables"

    Protected BaseAddress As Integer
    Protected MyProcess As Process
    Protected myProcessModule As ProcessModule
    Protected processHandle As Integer
    Protected ProcessName As String

#End Region

#Region "Methods / Main functions"

    Public Sub New(pProcessName As String)
        Me.ProcessName = pProcessName
    End Sub

    Public Function CheckProcess() As Boolean
        Dim pProcess As Process()
        pProcess = Process.GetProcesses()
        Dim procHandle As Integer = 0
        For Each temp As Process In pProcess
            If temp.ProcessName.ToUpper() = Me.ProcessName.ToUpper() Then
                'Me.MyProcess = temp
                ' Me.processHandle = OpenProcess(2035711, False, temp.Id)

                Return True
            End If
        Next
        Return False
    End Function

    Public Function CutString(mystring As String) As String
        Dim chArray As Char() = mystring.ToCharArray()
        Dim str As String = ""
        For i As Integer = 0 To mystring.Length - 1
            Try
                If (chArray(i) = " "c) AndAlso (chArray(i + 1) = " "c) Then
                    Return str
                End If
                If chArray(i) = ControlChars.NullChar Then
                    Return str
                End If
            Catch ex As IndexOutOfRangeException
                chArray(i) = " "c
                Dim exp As String = ex.ToString()
            End Try
            str = str & chArray(i).ToString()
        Next
        Return mystring.TrimEnd(New Char() {"0"c})
    End Function

    Public Function DllImageAddress(dllname As String) As Integer
        Try
            Dim modules As ProcessModuleCollection = Me.MyProcess.Modules

            For Each procmodule As ProcessModule In modules
                If dllname = procmodule.ModuleName Then
                    Return CInt(procmodule.BaseAddress)
                End If
            Next
            Return -1
        Catch ex As IndexOutOfRangeException
            Dim exp As String = ex.ToString()
            MessageBox.Show(exp)
            Return -1
        Catch ex As System.ComponentModel.Win32Exception
            Dim exp As String = ex.ToString()
            Return -1
        End Try
    End Function

    Public Function ImageAddress(pOffset As Integer) As Integer
        Me.BaseAddress = 0
        Me.myProcessModule = Me.MyProcess.MainModule
        Me.BaseAddress = CInt(Me.myProcessModule.BaseAddress)
        Return (pOffset + Me.BaseAddress)
    End Function

    Public Function StartProcess() As Boolean
        If Me.ProcessName <> "" Then
            Dim pProcess As Process()
            pProcess = Process.GetProcesses()
            Dim tempProcess As Process
            For Each tempProcess In pProcess
                If tempProcess.ProcessName.ToUpper() = Me.ProcessName.ToUpper() Then
                    Dim tempHandle As Integer
                    Me.MyProcess = tempProcess
                    tempHandle = OpenProcess(ProcessAccessFlags.All, False, tempProcess.Id)
                    Me.processHandle = tempHandle

                    ' MessageBox.Show(tempHandle.ToString)
                    Return True
                End If
            Next

            If Me.processHandle = 0 Then
                MessageBox.Show(Me.ProcessName & " is not running. Start " & Me.ProcessName & " and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand)
                frmOverlay.Close()
            End If
        Else
            MessageBox.Show("Define process name first!")
            frmOverlay.Close()
        End If
    End Function

    Public Function ReadMemory(Of T)(ByVal address As Integer, ByVal length As Integer, ByVal unicodeString As Boolean) As T
        Dim buffer() As Byte
        If GetType(T) Is GetType(String) Then
            If unicodeString Then buffer = New Byte(length * 2 - 1) {} Else buffer = New Byte(length - 1) {}
        ElseIf GetType(T) Is GetType(Byte()) Then
            buffer = New Byte(length - 1) {}
        Else
            buffer = New Byte(Marshal.SizeOf(GetType(T)) - 1) {}
        End If
        If Not CheckProcess() Then Return Nothing
        Dim success As Boolean = ReadProcessMemory(processHandle, New IntPtr(address), buffer, New IntPtr(buffer.Length), IntPtr.Zero)
        If Not success Then Return Nothing
        If GetType(T) Is GetType(Byte()) Then Return CType(CType(buffer, Object), T)
        If GetType(T) Is GetType(String) Then
            If unicodeString Then Return CType(CType(Encoding.Unicode.GetString(buffer), Object), T)
            Return CType(CType(Encoding.ASCII.GetString(buffer), Object), T)
        End If
        Dim gcHandle As GCHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned)
        Dim returnObject As T
        returnObject = CType(Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject, GetType(T)), T)
        gcHandle.Free()
        Return returnObject
    End Function

#End Region

#Region "DLL Imports"

    <DllImport("kernel32.dll")>
    Public Shared Function OpenProcess(processAccess As ProcessAccessFlags, bInheritHandle As Boolean, processId As Integer) As IntPtr
    End Function

    <DllImport("kernel32.dll")>
    Public Shared Function OpenProcess(processAccess As Integer, bInheritHandle As Boolean, processId As Integer) As IntPtr
    End Function

    <DllImport("user32.dll", EntryPoint:="FindWindow", SetLastError:=True)>
    Public Shared Function FindWindowByCaption(ZeroOnly As Integer, lpWindowName As String) As Integer
    End Function

    <DllImport("kernel32.dll")>
    Public Shared Function CloseHandle(hObject As Integer) As Boolean
    End Function

    <DllImport("kernel32.dll")>
    Public Shared Function VirtualProtectEx(hProcess As Integer, lpAddress As Integer, dwSize As Integer, flNewProtect As UInteger, ByRef lpflOldProtect As UInteger) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function PostMessage(hWnd As IntPtr, Msg As UInt32, wParam As Integer, lParam As Integer) As Boolean
    End Function
    <DllImport("USER32.DLL")>
    Public Shared Function SetForegroundWindow(hWnd As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Sub keybd_event(bVk As Byte, bScan As Byte, dwFlags As Integer, dwExtraInfo As Integer)
    End Sub

#End Region

#Region "Read Memory"

    <DllImport("kernel32.dll")>
    Public Shared Function ReadProcessMemory(hProcess As Integer, lpBaseAddress As Integer, buffer As Byte(), size As Integer, lpNumberOfBytesrd As Integer) As Boolean
    End Function

    Public Function rdByte(pOffset As Integer) As Byte
        Dim buffer As Byte() = New Byte(0) {}
        ReadProcessMemory(Me.processHandle, pOffset, buffer, 1, 0)
        Return buffer(0)
    End Function

    Public Function rdByte(AddToImageAddress As Boolean, pOffset As Integer) As Byte
        Dim buffer As Byte() = New Byte(0) {}
        Dim lpBaseAddress As Integer = If(AddToImageAddress, Me.ImageAddress(pOffset), pOffset)
        ReadProcessMemory(Me.processHandle, lpBaseAddress, buffer, 1, 0)
        Return buffer(0)
    End Function

    Public Function rdByte([Module] As String, pOffset As Integer) As Byte
        Dim buffer As Byte() = New Byte(0) {}
        ReadProcessMemory(Me.processHandle, Me.DllImageAddress([Module]) + pOffset, buffer, 1, 0)
        Return buffer(0)
    End Function

    Public Function rdFloat(pOffset As Integer) As Single
        Return BitConverter.ToSingle(Me.rdMem(pOffset, 4), 0)
    End Function

    Public Function rdFloat(AddToImageAddress As Boolean, pOffset As Integer) As Single
        Return BitConverter.ToSingle(Me.rdMem(pOffset, 4, AddToImageAddress), 0)
    End Function

    Public Function rdFloat([Module] As String, pOffset As Integer) As Single
        Return BitConverter.ToSingle(Me.rdMem(Me.DllImageAddress([Module]) + pOffset, 4), 0)
    End Function

    Public Function rdInt(pOffset As Integer) As Integer
        Return BitConverter.ToInt32(Me.rdMem(pOffset, 4), 0)
    End Function

    Public Function rdInt(AddToImageAddress As Boolean, pOffset As Integer) As Integer
        Return BitConverter.ToInt32(Me.rdMem(pOffset, 4, AddToImageAddress), 0)
    End Function

    Public Function rdInt([Module] As String, pOffset As Integer) As Integer
        Return BitConverter.ToInt32(Me.rdMem(Me.DllImageAddress([Module]) + pOffset, 4), 0)
    End Function

    Public Function rdMem(pOffset As Integer, pSize As Integer) As Byte()
        Dim buffer As Byte() = New Byte(pSize - 1) {}
        ReadProcessMemory(Me.processHandle, pOffset, buffer, pSize, 0)
        Return buffer
    End Function

    Public Function rdMem(pOffset As Integer, pSize As Integer, AddToImageAddress As Boolean) As Byte()
        Dim buffer As Byte() = New Byte(pSize - 1) {}
        Dim lpBaseAddress As Integer = If(AddToImageAddress, Me.ImageAddress(pOffset), pOffset)
        ReadProcessMemory(Me.processHandle, lpBaseAddress, buffer, pSize, 0)
        Return buffer
    End Function

    Public Function rdShort(pOffset As Integer) As Short
        Return BitConverter.ToInt16(Me.rdMem(pOffset, 2), 0)
    End Function

    Public Function rdShort(AddToImageAddress As Boolean, pOffset As Integer) As Short
        Return BitConverter.ToInt16(Me.rdMem(pOffset, 2, AddToImageAddress), 0)
    End Function

    Public Function rdShort([Module] As String, pOffset As Integer) As Short
        Return BitConverter.ToInt16(Me.rdMem(Me.DllImageAddress([Module]) + pOffset, 2), 0)
    End Function

    Public Function rdStringAscii(pOffset As Integer, pSize As Integer) As String
        Return Me.CutString(Encoding.ASCII.GetString(Me.rdMem(pOffset, pSize)))
    End Function

    Public Function rdStringAscii(AddToImageAddress As Boolean, pOffset As Integer, pSize As Integer) As String
        Return Me.CutString(Encoding.ASCII.GetString(Me.rdMem(pOffset, pSize, AddToImageAddress)))
    End Function

    Public Function rdStringAscii([Module] As String, pOffset As Integer, pSize As Integer) As String
        Return Me.CutString(Encoding.ASCII.GetString(Me.rdMem(Me.DllImageAddress([Module]) + pOffset, pSize)))
    End Function

    Public Function rdStringUnicode(pOffset As Integer, pSize As Integer) As String
        Return Me.CutString(Encoding.Unicode.GetString(Me.rdMem(pOffset, pSize)))
    End Function

    Public Function rdStringUnicode(AddToImageAddress As Boolean, pOffset As Integer, pSize As Integer) As String
        Return Me.CutString(Encoding.Unicode.GetString(Me.rdMem(pOffset, pSize, AddToImageAddress)))
    End Function

    Public Function rdStringUnicode([Module] As String, pOffset As Integer, pSize As Integer) As String
        Return Me.CutString(Encoding.Unicode.GetString(Me.rdMem(Me.DllImageAddress([Module]) + pOffset, pSize)))
    End Function

    Public Function rdUInt(pOffset As Integer) As UInteger
        Return BitConverter.ToUInt32(Me.rdMem(pOffset, 4), 0)
    End Function

    Public Function rdUInt(AddToImageAddress As Boolean, pOffset As Integer) As UInteger
        Return BitConverter.ToUInt32(Me.rdMem(pOffset, 4, AddToImageAddress), 0)
    End Function

    Public Function rdUInt([Module] As String, pOffset As Integer) As UInteger
        Return BitConverter.ToUInt32(Me.rdMem(Me.DllImageAddress([Module]) + pOffset, 4), 0)
    End Function

    Public Function rdDouble(pOffset As Integer) As Double
        Return BitConverter.ToDouble(Me.rdMem(pOffset, 8), 0)
    End Function

    Public Function rdLong(pOffset As Integer) As Single
        Return BitConverter.ToInt64(Me.rdMem(pOffset, 8), 0)
    End Function

    Public Function rdLong(AddToImageAddress As Boolean, pOffset As Integer) As Single
        Return BitConverter.ToInt64(Me.rdMem(pOffset, 8, AddToImageAddress), 0)
    End Function

    Public Function rdLong([Module] As String, pOffset As Integer) As Single
        Return BitConverter.ToInt64(Me.rdMem(Me.DllImageAddress([Module]) + pOffset, 8), 0)
    End Function

#End Region

#Region "Write Memory"

    <DllImport("kernel32.dll")>
    Public Shared Function WriteProcessMemory(hProcess As Integer, lpBaseAddress As Integer, buffer As Byte(), size As Integer, lpNumberOfBytesWritten As Integer) As Boolean
    End Function

    Public Sub WrtByte(pOffset As Integer, pBytes As Byte)
        Me.WrtMem(pOffset, BitConverter.GetBytes(CShort(pBytes)))
    End Sub

    Public Sub WrtByte(AddToImageAddress As Boolean, pOffset As Integer, pBytes As Byte)
        Me.WrtMem(pOffset, BitConverter.GetBytes(CShort(pBytes)), AddToImageAddress)
    End Sub

    Public Sub WrtByte([Module] As String, pOffset As Integer, pBytes As Byte)
        Me.WrtMem(Me.DllImageAddress([Module]) + pOffset, BitConverter.GetBytes(CShort(pBytes)))
    End Sub

    Public Sub WrtDouble(pOffset As Integer, pBytes As Double)
        Me.WrtMem(pOffset, BitConverter.GetBytes(pBytes))
    End Sub

    Public Sub WrtDouble(AddToImageAddress As Boolean, pOffset As Integer, pBytes As Double)
        Me.WrtMem(pOffset, BitConverter.GetBytes(pBytes), AddToImageAddress)
    End Sub

    Public Sub WrtDouble([Module] As String, pOffset As Integer, pBytes As Double)
        Me.WrtMem(Me.DllImageAddress([Module]) + pOffset, BitConverter.GetBytes(pBytes))
    End Sub

    Public Sub WrtFloat(pOffset As Integer, pBytes As Single)
        Me.WrtMem(pOffset, BitConverter.GetBytes(pBytes))
    End Sub

    Public Sub WrtFloat(AddToImageAddress As Boolean, pOffset As Integer, pBytes As Single)
        Me.WrtMem(pOffset, BitConverter.GetBytes(pBytes), AddToImageAddress)
    End Sub

    Public Sub WrtFloat([Module] As String, pOffset As Integer, pBytes As Single)
        Me.WrtMem(Me.DllImageAddress([Module]) + pOffset, BitConverter.GetBytes(pBytes))
    End Sub

    Public Sub WrtInt(pOffset As Integer, pBytes As Integer)
        Me.WrtMem(pOffset, BitConverter.GetBytes(pBytes))
    End Sub

    Public Sub WrtInt(AddToImageAddress As Boolean, pOffset As Integer, pBytes As Integer)
        Me.WrtMem(pOffset, BitConverter.GetBytes(pBytes), AddToImageAddress)
    End Sub

    Public Sub WrtInt([Module] As String, pOffset As Integer, pBytes As Integer)
        Me.WrtMem(Me.DllImageAddress([Module]) + pOffset, BitConverter.GetBytes(pBytes))
    End Sub

    Public Sub WrtMem(pOffset As Integer, pBytes As Byte())
        WriteProcessMemory(Me.processHandle, pOffset, pBytes, pBytes.Length, 0)
    End Sub

    Public Sub WrtMem(pOffset As Integer, pBytes As Byte(), AddToImageAddress As Boolean)
        Dim lpBaseAddress As Integer = If(AddToImageAddress, Me.ImageAddress(pOffset), pOffset)
        WriteProcessMemory(Me.processHandle, lpBaseAddress, pBytes, pBytes.Length, 0)
    End Sub

    Public Sub WrtShort(pOffset As Integer, pBytes As Short)
        Me.WrtMem(pOffset, BitConverter.GetBytes(pBytes))
    End Sub

    Public Sub WrtShort(AddToImageAddress As Boolean, pOffset As Integer, pBytes As Short)
        Me.WrtMem(pOffset, BitConverter.GetBytes(pBytes), AddToImageAddress)
    End Sub

    Public Sub WrtShort([Module] As String, pOffset As Integer, pBytes As Short)
        Me.WrtMem(Me.DllImageAddress([Module]) + pOffset, BitConverter.GetBytes(pBytes))
    End Sub

    Public Sub WrtStringAscii(pOffset As Integer, pBytes As String)
        Me.WrtMem(pOffset, Encoding.ASCII.GetBytes(pBytes & Convert.ToString(vbNullChar)))
    End Sub

    Public Sub WrtStringAscii(AddToImageAddress As Boolean, pOffset As Integer, pBytes As String)
        Me.WrtMem(pOffset, Encoding.ASCII.GetBytes(pBytes & Convert.ToString(vbNullChar)), AddToImageAddress)
    End Sub

    Public Sub WrtStringAscii([Module] As String, pOffset As Integer, pBytes As String)
        Me.WrtMem(Me.DllImageAddress([Module]) + pOffset, Encoding.ASCII.GetBytes(pBytes & Convert.ToString(vbNullChar)))
    End Sub

    Public Sub WrtStringUnicode(pOffset As Integer, pBytes As String)
        Me.WrtMem(pOffset, Encoding.Unicode.GetBytes(pBytes & Convert.ToString(vbNullChar)))
    End Sub

    Public Sub WrtStringUnicode(AddToImageAddress As Boolean, pOffset As Integer, pBytes As String)
        Me.WrtMem(pOffset, Encoding.Unicode.GetBytes(pBytes & Convert.ToString(vbNullChar)), AddToImageAddress)
    End Sub

    Public Sub WrtStringUnicode([Module] As String, pOffset As Integer, pBytes As String)
        Me.WrtMem(Me.DllImageAddress([Module]) + pOffset, Encoding.Unicode.GetBytes(pBytes & Convert.ToString(vbNullChar)))
    End Sub

    Public Sub WrtUInt(pOffset As Integer, pBytes As UInteger)
        Me.WrtMem(pOffset, BitConverter.GetBytes(pBytes))
    End Sub

    Public Sub WrtUInt(AddToImageAddress As Boolean, pOffset As Integer, pBytes As UInteger)
        Me.WrtMem(pOffset, BitConverter.GetBytes(pBytes), AddToImageAddress)
    End Sub

    Public Sub WrtUInt([Module] As String, pOffset As Integer, pBytes As UInteger)
        Me.WrtMem(Me.DllImageAddress([Module]) + pOffset, BitConverter.GetBytes(pBytes))
    End Sub

#End Region

#Region "Enums"

    <Flags>
    Public Enum ProcessAccessFlags As UInteger
        All = 2035711
        CreateThread = 2
        DupHandle = 64
        QueryInformation = 1024
        SetInformation = 512
        Synchronize = 1048576
        Terminate = 1
        VMOperation = 8
        VMRead = 16
        VMWrt = 32
    End Enum


#End Region

End Class

