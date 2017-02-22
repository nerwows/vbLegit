Imports System.Runtime.InteropServices, System.Threading, System.Security.Cryptography, System.Text, Microsoft.DirectX, Microsoft.DirectX.Direct3D

Public Class frmOverlay

#Region "If u make p2c ruski hax, delete this"

    '===BY nerWoWs==='

    'Thanks to:
    'https://www.unknowncheats.me/forum/counterstrike-global-offensive/139780-vb-net-external-esp-recoil-crosshair.html - FattyXP for this base.
    'https://msdn.microsoft.com/en-us/library/aa289496(v=vs.71).aspx - Microsoft, for Threading update tutorial <3
    'https://www.unknowncheats.me/forum/1523292-post2.html- IISP33DII for GetWeaponID code + Weapon list <3

    'Useful:
    'https://msdn.microsoft.com/en-us/library/windows/desktop/bb323981(v=vs.85).aspx - If you are interested in DirectX things.

#End Region

#Region "Declares"

    ' Here we declare the function calls we make to Windows API's using System.Runtime.InteropServices

    <DllImport("dwmapi.dll")>
    Private Shared Function DwmExtendFrameIntoClientArea(ByVal hwnd As IntPtr, ByRef margins As Margins) As Integer
    End Function
    <DllImport("user32", EntryPoint:="GetWindowLong")>
    Public Shared Function GetWindowLong(ByVal hWnd As IntPtr, ByVal nIndex As GWL) As Integer
    End Function

    <DllImport("user32", EntryPoint:="SetWindowLong")>
    Public Shared Function SetWindowLong(ByVal hWnd As IntPtr, ByVal nIndex As GWL, ByVal dsNewLong As WS_EX) As Integer
    End Function

    <DllImport("user32.dll", EntryPoint:="SetLayeredWindowAttributes")>
    Public Shared Function SetLayeredWindowAttributes(ByVal hWnd As IntPtr, ByVal crKey As Integer, ByVal alpha As Byte, ByVal dwFlags As LWA) As Boolean
    End Function

#End Region

#Region "ANTI"

    Public Shared Function GetUniqueKey(maxSize As Integer) As String
        Dim chars As Char() = New Char(61) {}
        chars = "abcdefghijklmnopqrstuvwxyzåäöABCDEFGHIJKLMNOPQRSTUVWXYZÅÄÖ1234567890".ToCharArray()
        Dim data As Byte() = New Byte(0) {}
        Dim crypto As New RNGCryptoServiceProvider()
        crypto.GetNonZeroBytes(data)
        data = New Byte(maxSize - 1) {}
        crypto.GetNonZeroBytes(data)
        Dim result As New StringBuilder(maxSize)
        For Each b As Byte In data
            result.Append(chars(b Mod (chars.Length)))
        Next
        Return result.ToString()
    End Function

    Public Function GetRandom(ByVal Min As Integer, ByVal Max As Integer) As Integer
        Dim Generator As System.Random = New System.Random()
        Return Generator.Next(Min, Max)
    End Function

#End Region

#Region "Variables and Constants"

    'Offsets
    'Replace offset's start 0x with &H, and you are good to go.

    'Usually changes every update
    Const m_dwLocalPlayer As Integer = &HAA66D4
    Const m_dwEntityList As Integer = &H4AC9154
    Const m_dwForceAttack As Integer = &H2F0911C

    'Usually does not change at all
    Const m_hActiveWeapon As Integer = &H2EE8
    Const m_iItemDefinitionIndex As Integer = &H2F88
    Const m_iTeamNum As Integer = &HF0
    Const m_iHealth As Integer = &HFC
    Const m_bDormant As Integer = &HE9
    Const m_bIsDefusing As Integer = &H38A4
    Const m_vecPunch As Integer = &H301C

    ' Direct3D Variables
    Dim device As Direct3D.Device
    Dim dxLine As Direct3D.Line
    Dim dxFont As Direct3D.Font
    Dim marg As Margins

    Dim _mem As New ProcM("csgo")

    ' Program Variables
    Dim dx As System.Threading.Thread

    Dim bRecoilCross As Boolean = 1
    Dim bNoscopeCross As Boolean = 1
    Dim bDefAlarm As Boolean = 1
    Dim bAutoPistol As Boolean = 1

    Dim DrawMenu As Boolean = 1

    Dim _initalStyle As Integer
    Private _client As Integer

    Const MAXPLAYERS As Integer = 64
    Dim xTo, yTo, d1y, d1x As Integer

#End Region

#Region "Types, Structs, Defines, Enums"
    ' Random shit we need to define to use
    Public Structure Margins
        Dim Left, Right, Top, Bottom As Integer
    End Structure

    Public Enum GWL As Integer
        ExStyle = -20
    End Enum

    Public Enum WS_EX As Integer
        Transparent = &H20
        Layered = &H80000
    End Enum

    Public Enum LWA As Integer
        ColorKey = &H1
        Alpha = &H2
    End Enum

#End Region

#Region "Drawing Shits"

    'Draws text with that cool shadow effect.
    Public Sub _ShadowText(text As String, Position As Point, Colour As Color)
        Try
            dxFont.DrawText(Nothing, text, New Point(Position.X + 1, Position.Y + 1), Color.Black)
            dxFont.DrawText(Nothing, text, Position, Colour)
        Catch ex As Exception : End Try
    End Sub

    'Draw a w pixel wide line of color from x1/y1 to x2/y2.
    Public Sub DrawLine(x1 As Single, y1 As Single, x2 As Single, y2 As Single, w As Single, Colour As Color)
        Dim vLine As Vector2() = New Vector2(1) {New Vector2(x1, y1), New Vector2(x2, y2)}
        Try
            dxLine.GlLines = True
            dxLine.Antialias = False
            dxLine.Width = w
            dxLine.Begin()
            dxLine.Draw(vLine, Colour.ToArgb())
            dxLine.End()
        Catch ex As Exception : End Try
    End Sub

    ' Sets the transparent color to Colour
    Private Shared Function SetTransparency(Alpha As Integer, Colour As Color) As Color
        Try
            Return Color.FromArgb(Alpha, Colour.R, Colour.G, Colour.B)
        Catch ex As Exception : End Try
    End Function

    'Draws a Solid box of Colour
    Public Sub DrawFilledBox(x As Single, y As Single, w As Single, h As Single, Colour As System.Drawing.Color)
        Dim vLine As Vector2() = New Vector2(1) {}
        Try
            dxLine.GlLines = True
            dxLine.Antialias = False
            dxLine.Width = 1

            vLine(0).X = x + w / 2
            vLine(0).Y = y
            vLine(1).X = x + w / 2
            vLine(1).Y = y + h

            dxLine.Begin()
            dxLine.Draw(vLine, Colour.ToArgb())
            dxLine.End()
        Catch ex As Exception : End Try
    End Sub

    ' Draws a TransparentBox, with Colour as the transparency color
    Public Sub DrawTransparentBox(x As Single, y As Single, w As Single, h As Single, Transparency As Integer, Colour As System.Drawing.Color)
        Dim vLine As Vector2() = New Vector2(1) {}
        Try
            dxLine.GlLines = True
            dxLine.Antialias = False
            dxLine.Width = w

            vLine(0).X = x + w / 2
            vLine(0).Y = y
            vLine(1).X = x + w / 2
            vLine(1).Y = y + h
            Dim halfTransparent As Color = SetTransparency(Transparency, Colour)
            dxLine.Begin()
            dxLine.Draw(vLine, halfTransparent.ToArgb())
            dxLine.End()
        Catch ex As Exception : End Try
    End Sub

#End Region

#Region "Hooking Related"

    ' This region is things related to the Keyboard Hook, for turning on/off things and exiting the hack.

    <DllImport("user32.dll")>
    Private Shared Function SetWindowsHookEx(idHook As Integer, callback As LowLevelKeyboardProc, hInstance As IntPtr, threadId As UInteger) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function UnhookWindowsHookEx(hInstance As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function CallNextHookEx(idHook As IntPtr, nCode As Integer, wParam As Integer, lParam As IntPtr) As IntPtr
    End Function

    <DllImport("kernel32.dll")>
    Private Shared Function LoadLibrary(lpFileName As String) As IntPtr
    End Function

    Private Delegate Function LowLevelKeyboardProc(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr

    <DllImport("user32.dll")>
    Private Shared Function GetAsyncKeyState(vKey As Integer) As Short
    End Function

    Const WH_KEYBOARD_LL As Integer = 13
    Const WM_KEYDOWN As Integer = &H100

    Private _proc As LowLevelKeyboardProc = AddressOf hookProc

    Private Shared hhook As IntPtr = IntPtr.Zero

    Public Sub SetHook()
        Dim hInstance As IntPtr = LoadLibrary("User32")
        hhook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hInstance, 0)
    End Sub

    Public Shared Sub UnHook()
        UnhookWindowsHookEx(hhook)
    End Sub

    Public Function hookProc(code As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
        Dim vkCode As Integer = Marshal.ReadInt32(lParam)
        Dim ValidKeyDown As Boolean = False

        If vkCode = Keys.Insert.GetHashCode() OrElse
            vkCode = Keys.Home.GetHashCode() OrElse
            vkCode = Keys.End.GetHashCode() OrElse
            vkCode = Keys.Delete.GetHashCode() OrElse
            vkCode = Keys.PageUp.GetHashCode() OrElse
            vkCode = Keys.PageDown.GetHashCode() Then
            ValidKeyDown = True
        End If

        If code >= 0 AndAlso wParam = WM_KEYDOWN AndAlso ValidKeyDown Then
            If vkCode = Keys.Insert.GetHashCode() Then bAutoPistol = Not bAutoPistol
            If vkCode = Keys.PageUp.GetHashCode() Then bRecoilCross = Not bRecoilCross
            If vkCode = Keys.PageDown.GetHashCode() Then bNoscopeCross = Not bNoscopeCross
            If vkCode = Keys.Home.GetHashCode() Then DrawMenu = Not DrawMenu
            If vkCode = Keys.Delete.GetHashCode() Then Application.Exit()
            If vkCode = Keys.End.GetHashCode() Then bDefAlarm = Not bDefAlarm
            Return 1
        Else
            Return CallNextHookEx(hhook, code, CInt(wParam), lParam)
        End If

    End Function

#End Region

#Region "Form Subroutines"

    Private Sub frmOverLay_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        UnHook()
        device.Dispose()
        End
    End Sub

    Public Sub New()
        MyBase.New()

        'This call is required by the Windows Form Designer.
        InitializeComponent()
        'Add any initialization after the InitializeComponent() call

    End Sub

    Private Sub frmOverlay_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        ' Set instance unique form title. I recommend changing the string of chars in GetUniqueKey
        ' around build to build to reduce pattern scanning on it, if you use it for future hacks
        ' since this function and string are public in 3 projects in 2 languages that I know of.

        Me.Text = GetUniqueKey(GetRandom(12, 72))

        ' Get form initalStyle, to add to the call later
        _initalStyle = GetWindowLong(Me.Handle, GWL.ExStyle)
        ' Set all the new Styles we need to make the form Transparent and Click-Through
        SetWindowLong(Me.Handle, GWL.ExStyle, _initalStyle Or WS_EX.Layered Or WS_EX.Transparent)
        SetLayeredWindowAttributes(Me.Handle, 0, 255 * 0.7, LWA.Alpha)
        ' Set our Keyboard Hook
        SetHook()

        initDX()

        _mem.StartProcess()
        _client = _mem.DllImageAddress("client.dll")

        dx = New Thread(New ThreadStart(AddressOf Me.drawDX))
        dx.IsBackground = True
        dx.Start()

        DefAlarm_StartTask()
        AutoPistol_StartTask()

        xTo = Width / 2
        yTo = Height / 2

        d1y = Me.Height / 90
        d1x = Me.Width / 90
    End Sub

    Private Sub frmOverlay_Paint(sender As Object, e As PaintEventArgs) Handles Me.Paint
        marg.Left = 0
        marg.Top = 0
        marg.Right = Me.Width
        marg.Bottom = Me.Height
        DwmExtendFrameIntoClientArea(Me.Handle, marg)
    End Sub
#End Region

#Region "DirectX Related"
    Sub initDX()
        Dim present As New PresentParameters
        'present.BackBufferCount = 1
        present.Windowed = True
        present.SwapEffect = SwapEffect.Discard
        present.BackBufferFormat = Direct3D.Format.A8R8G8B8
        present.BackBufferCount = 1
        present.BackBufferHeight = Me.Height
        present.BackBufferWidth = Me.Width

        device = New Device(0, DeviceType.Hardware, Me.Handle, CreateFlags.HardwareVertexProcessing, present)
        dxLine = New Direct3D.Line(Me.device)
        dxFont = New Direct3D.Font(Me.device, New System.Drawing.Font("Tahoma", 9, FontStyle.Regular))
        device.RenderState.CullMode = Cull.None

    End Sub

    Sub drawDX()
        Do
            ' Do things Direct3D needs to do so we can draw on the buffer
            device.Clear(Direct3D.ClearFlags.Target, Color.FromArgb(0, 0, 0, 0), 1.0F, 0)
            device.RenderState.ZBufferEnable = False
            device.RenderState.Lighting = False
            device.RenderState.CullMode = Cull.None
            device.Transform.Projection = Matrix.OrthoOffCenterLH(0, Me.Width, Me.Height, 0, 0, 1)
            device.BeginScene()
            ' START DRAWING!

            'Draw a little box thing
            If DrawMenu Then
                DrawTransparentBox(0, 500, 125, 45, 175, Color.Black)
                If bRecoilCross Then
                    _ShadowText("Recoil Cross: " + bRecoilCross.ToString(), New Point(3, 500), Color.DarkGreen)
                Else
                    _ShadowText("Recoil Cross: " + bRecoilCross.ToString(), New Point(3, 500), Color.DarkRed)
                End If

                If bNoscopeCross Then
                    _ShadowText("NoScope Cross: " + bNoscopeCross.ToString(), New Point(3, 510), Color.DarkGreen)
                Else
                    _ShadowText("NoScope Cross: " + bNoscopeCross.ToString(), New Point(3, 510), Color.DarkRed)
                End If

                If bAutoPistol Then
                    _ShadowText("Auto Pistol: " + bAutoPistol.ToString(), New Point(3, 520), Color.DarkGreen)
                Else
                    _ShadowText("Auto Pistol: " + bAutoPistol.ToString(), New Point(3, 520), Color.DarkRed)
                End If

                If bDefAlarm Then
                    _ShadowText("Defuse Alarm: " + bDefAlarm.ToString(), New Point(3, 530), Color.DarkGreen)
                Else
                    _ShadowText("Defuse Alarm: " + bDefAlarm.ToString(), New Point(3, 530), Color.DarkRed)
                End If
            Else
                'Cause we dont want to draw, right?
            End If

            If _mem.CheckProcess Then

                If GetAsyncKeyState(&H1) <> 0 And bRecoilCross Then
                    Try
                        Dim drX As Integer = xTo - (d1x * (GetPunch().y))
                        Dim drY As Integer = yTo + (d1y * (GetPunch().x))

                        DrawLine(drX - 6, drY, drX + 6, drY, 1, Color.Red)
                        DrawLine(drX, drY - 6, drX, drY + 6, 1, Color.Red)
                    Catch ex As Exception : End Try
                End If

                If bNoscopeCross And GetWeaponID(GetLocalPlayer) = 9 OrElse
                    bNoscopeCross And GetWeaponID(GetLocalPlayer) = 11 OrElse
                    bNoscopeCross And GetWeaponID(GetLocalPlayer) = 38 OrElse
                    bNoscopeCross And GetWeaponID(GetLocalPlayer) = 40 Then
                    Try
                        ' +
                        'DrawLine(xTo - 6, yTo, xTo + 6, yTo, 1, Color.Aqua)
                        'DrawLine(xTo, yTo - 6, xTo, yTo + 6, 1, Color.Aqua)

                        ' X
                        'DrawLine(xTo - 6, yTo - 6, xTo + 6, yTo + 6, 1, Color.Aqua)
                        'DrawLine(xTo + 6, yTo - 6, xTo - 6, yTo + 6, 1, Color.Aqua)

                        ' ◻
                        DrawLine(xTo - 6, yTo + 6, xTo + 6, yTo + 6, 1, Color.Aqua)
                        DrawLine(xTo + 6, yTo - 6, xTo - 6, yTo - 6, 1, Color.Aqua)
                        DrawLine(xTo - 6, yTo - 6, xTo - 6, yTo + 6, 1, Color.Aqua)
                        DrawLine(xTo + 6, yTo + 6, xTo + 6, yTo - 6, 1, Color.Aqua)
                    Catch ex As Exception : End Try
                End If

            End If



            device.EndScene()
            device.Present()

            Thread.Sleep(100)
        Loop
    End Sub
#End Region

#Region "Defuse Alarm"

    Public Sub DefAlarm()
        While bDefAlarm ' While bDefAlarm(Boolean DefAlarm) is true, program is going to execute code bellow.
            'In the under, we scan entitys with cycle.
            For i As Integer = 0 To MAXPLAYERS
                'In the under, we are checking, if entity is from the same team, as our player.
                'Then we check, if entity is dormant (google, what is dormant, if you don't already know).
                'Then we check if entity has >0 HP (failproof), and if it is defusing, we *barp*
                If GetEntTeam(EntList(i)) <> GetEntTeam(GetLocalPlayer) And Not GetEntDormant(EntList(i)) And GetEntHealth(EntList(i)) > 0 And GetEntDefusing(EntList(i)) Then
                    Console.Beep()
                    'Or you can use your own alert-sound instead:
                    'My.Computer.Audio.Play(My.Resources.<filename>, AudioPlayMode.WaitToComplete)
                    'Sound file needs to be WAV if i'm correct.
                End If
            Next
            Thread.Sleep(50)
            'We need to thread sleep, because we are programming, not cooking: https://www.youtube.com/watch?v=kMVLeSNZEBY
            'The bigger value is in the Thread.Sleep(), the less CPU we use, BUT 
            'The bigger value is, the bigger delay we get on function execution.
        End While
    End Sub

#Region "Defuse Alarm tasks" 'https://msdn.microsoft.com/en-us/library/aa289496(v=vs.71).aspx
    Public DefAlarm_CancelThread As New System.Threading.ManualResetEvent(False)
    Public DefAlarm_ThreadisCanceled As New System.Threading.ManualResetEvent(False)

    Public Sub DefAlarm_StartTask()
        Dim DefAlarmTh As New System.Threading.Thread(AddressOf DefAlarm)
        DefAlarm_CancelThread.Reset()
        DefAlarm_ThreadisCanceled.Reset()
        DefAlarmTh.Start()
        'MsgBox("Thread Started")
    End Sub

    Public Sub DefAlarm_CancelTask()
        DefAlarm_CancelThread.Set()
        If DefAlarm_ThreadisCanceled.WaitOne(1, False) Then
            'MsgBox("The thread has stopped.")
        Else
            'MsgBox("The thread could not be stopped.")
        End If
    End Sub

#End Region

#End Region

#Region "AutoPistol"

    '    Dim WEAPON_DEAGLE As Integer = 1
    '    Dim WEAPON_DUAL As Integer = 2
    '    Dim WEAPON_FIVE7 As Integer = 3
    '    Dim WEAPON_GLOCK As Integer = 4
    '    Dim WEAPON_AK47 As Integer = 7
    '    Dim WEAPON_AUG As Integer = 8
    '    Dim WEAPON_AWP As Integer = 9
    '    Dim WEAPON_FAMAS As Integer = 10
    '    Dim WEAPON_G3SG1 As Integer = 11
    '    Dim WEAPON_GALIL As Integer = 13
    '    Dim WEAPON_M249 As Integer = 14
    '    Dim WEAPON_M4A1 As Integer = 16
    '    Dim WEAPON_MAC10 As Integer = 17
    '    Dim WEAPON_P90 As Integer = 19
    '    Dim WEAPON_UMP As Integer = 24
    '    Dim WEAPON_XM1014 As Integer = 25
    '    Dim WEAPON_BIZON As Integer = 26
    '    Dim WEAPON_MAG7 As Integer = 27
    '    Dim WEAPON_NEGEV As Integer = 28
    '    Dim WEAPON_SAWEDOFF As Integer = 29
    '    Dim WEAPON_TEC9 As Integer = 30
    '    Dim WEAPON_TASER As Integer = 31
    '    Dim WEAPON_HKP2000 As Integer = 32
    '    Dim WEAPON_MP7 As Integer = 33
    '    Dim WEAPON_MP9 As Integer = 34
    '    Dim WEAPON_NOVA As Integer = 35
    '    Dim WEAPON_P250_CZ75 As Integer = 36
    '    Dim WEAPON_SCAR20 As Integer = 38
    '    Dim WEAPON_SG553 As Integer = 39
    '    Dim WEAPON_SSG08 As Integer = 40
    '    Dim WEAPON_KNIFEGG As Integer = 41
    '    Dim WEAPON_KNIFE As Integer = 42
    '    Dim WEAPON_FLASHBANG As Integer = 43
    '    Dim WEAPON_HEGRENADE As Integer = 44
    '    Dim WEAPON_SMOKE As Integer = 45
    '    Dim WEAPON_T_MOLOTOV As Integer = 46
    '    Dim WEAPON_DECOY As Integer = 47
    '    Dim WEAPON_CT_MOLOTOV As Integer = 48
    '    Dim WEAPON_C4 As Integer = 49

    Public Sub AutoPistol()
        While bAutoPistol 'While bAutoPistol (Boolean bAutoPistol) is true, we continue to execute our code.
            'So we check, if our player has specific gun in hand, and if he has it,
            'we continue to execute the code.
            If GetWeaponID(GetLocalPlayer) = 1 OrElse
                GetWeaponID(GetLocalPlayer) = 2 OrElse
                GetWeaponID(GetLocalPlayer) = 3 OrElse
                GetWeaponID(GetLocalPlayer) = 4 OrElse
                GetWeaponID(GetLocalPlayer) = 30 OrElse
                GetWeaponID(GetLocalPlayer) = 32 Then

                If GetAsyncKeyState(&H1) Then 'So if we are pressing Mouse1 button,
                    AttackMem()               'We continue to execute the code, we attack.
                End If
            Else
            End If
            Thread.Sleep(1) 'We need to thread sleep, because we are programming, not cooking: https://www.youtube.com/watch?v=kMVLeSNZEBY
        End While
    End Sub

#Region "Autopistol start/stop taskai" 'https://msdn.microsoft.com/en-us/library/aa289496(v=vs.71).aspx
    Public AutoPistol_CancelThread As New System.Threading.ManualResetEvent(False)
    Public AutoPistol_ThreadisCanceled As New System.Threading.ManualResetEvent(False)

    Public Sub AutoPistol_StartTask()
        Dim AutoPistolTh As New System.Threading.Thread(AddressOf AutoPistol)
        AutoPistol_CancelThread.Reset()
        AutoPistol_ThreadisCanceled.Reset()
        AutoPistolTh.Start()
        'MsgBox("Thread Started")
    End Sub

    Public Sub AutoPistol_CancelTask()
        AutoPistol_CancelThread.Set()
        If AutoPistol_ThreadisCanceled.WaitOne(1, False) Then
            'MsgBox("The thread has stopped.")
        Else
            'MsgBox("The thread could not be stopped.")
        End If
    End Sub

#End Region

#End Region

#Region "GET"

    Function GetLocalPlayer()
        Return _mem.rdInt(_client + m_dwLocalPlayer)
    End Function

    Function EntList(i As Integer)
        Return _mem.rdInt(_client + m_dwEntityList + ((i - 1) * 16))
    End Function

    Function GetEntTeam(x As Integer)
        Return _mem.rdInt(x + m_iTeamNum)
    End Function

    Function GetEntHealth(x As Integer)
        Return _mem.rdInt(x + m_iHealth)
    End Function

    Function GetEntDefusing(x As Integer) As Boolean
        Return _mem.rdInt(x + m_bIsDefusing)
    End Function

    Function GetEntDormant(x As Integer) As Boolean
        Return _mem.rdInt(x + m_bDormant)
    End Function

    Public Function GetPunch()
        Dim buffer As Byte() = New Byte(4) {}
        buffer = _mem.rdMem(GetLocalPlayer() + m_vecPunch, 8)
        Return New Vector3(BitConverter.ToSingle(buffer, 0), BitConverter.ToSingle(buffer, 4), 0)
    End Function

    Function GetWeaponID(EntPtr As Integer) As Integer
        Dim WeaponIndex As Integer = _mem.rdInt(EntPtr + m_hActiveWeapon) And &HFFFF
        Return _mem.rdInt(EntList(WeaponIndex) + m_iItemDefinitionIndex)
    End Function

#End Region

#Region "SET"

    Sub AttackMem()
        _mem.WrtInt(_client + m_dwForceAttack, 1) 'We "Click" the button, but using memory, to avoid lags.
        Threading.Thread.Sleep(1) 'We sleep a little bit, to avoid bugs. [We can sleep even more...]
        _mem.WrtInt(_client + m_dwForceAttack, 0) 'We "Release" the button, but using memory, to avoid lags.
    End Sub

#End Region

End Class
