' 查找 / 替换窗口（非模态，常驻）。
'
' 设计要点：
'   1. 用 Show() 而不是 ShowDialog()：打开后不阻塞主窗口，可以边看主文本边改；
'   2. 目标文本框由 MainUI 注入（Target），本窗口不持有主窗口引用，便于单独调试；
'   3. 每次命中都只是 Select + ScrollToCaret，不抢焦点，所以能一直按回车连续「下一个」。
Imports System
Imports System.Drawing
Imports System.Media
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Windows.Forms

Public Class Search

    ''' <summary>被查找 / 替换的文本框，由 MainUI 创建本窗口时注入。</summary>
    Public Target As TextBox

    Public Sub New()
        InitializeComponent()
    End Sub

    ' ---------- 占位提示 ----------

    Private Const EM_SETCUEBANNER As Integer = &H1501

    <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As String) As IntPtr
    End Function

    Private Sub Search_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            SendMessage(TxtFind.Handle, EM_SETCUEBANNER, CType(1, IntPtr), "查找内容")
            SendMessage(TxtReplace.Handle, EM_SETCUEBANNER, CType(1, IntPtr), "替换为")
        Catch ex As Exception
            Debug.WriteLine("[忽略] " & ex.Message)
        End Try
        SizeButtons()
        TxtFind.Focus()
    End Sub

    ''' <summary>
    ''' 四个按钮宽度一致 = 最长文案的像素宽 + 内边距（够用就好），高度固定 32（与主窗口一致）。
    ''' 左列贴输入框左边线，右列贴输入框右边线，中间空出来的就是间距。
    ''' </summary>
    Private Sub SizeButtons()
        Dim all As Button() = {BtnPrev, BtnNext, BtnReplace, BtnReplaceAll}
        Dim widest As Integer = 0
        For Each b As Button In all
            Dim sz As Size = TextRenderer.MeasureText(b.Text, b.Font)
            If sz.Width > widest Then widest = sz.Width
        Next
        widest += 20
        If widest < 76 Then widest = 76

        Const gap As Integer = 12
        BtnPrev.Size = New Size(widest, 32)
        BtnReplace.Size = New Size(widest, 32)
        BtnNext.Size = New Size(widest, 32)
        BtnReplaceAll.Size = New Size(widest, 32)

        ' 左列贴输入框左边线，右列贴右边线；按钮宽度不变，中间那段空白就是间距
        BtnPrev.Left = ShFind.Left
        BtnNext.Left = ShFind.Right - widest
        If BtnNext.Left < BtnPrev.Right + gap Then BtnNext.Left = BtnPrev.Right + gap

        BtnReplace.Left = ShReplace.Left
        BtnReplaceAll.Left = ShReplace.Right - widest
        If BtnReplaceAll.Left < BtnReplace.Right + gap Then BtnReplaceAll.Left = BtnReplace.Right + gap
    End Sub

    ' ---------- 对外接口（MainUI 调用） ----------

    ''' <summary>把主窗口里选中的单行文字带进查找框；不合适的选中不动。</summary>
    Public Sub SetFindText(text As String)
        If String.IsNullOrEmpty(text) Then Return
        If text.Length > 200 OrElse text.IndexOf(ChrW(10)) >= 0 OrElse text.IndexOf(ChrW(13)) >= 0 Then Return
        TxtFind.Text = text
        TxtFind.SelectAll()
    End Sub

    ''' <summary>Ctrl+F 聚焦查找框，Ctrl+H 聚焦替换框。</summary>
    Public Sub FocusReplace(replaceMode As Boolean)
        If replaceMode Then
            TxtReplace.Focus()
            TxtReplace.SelectAll()
        Else
            TxtFind.Focus()
            TxtFind.SelectAll()
        End If
    End Sub

    ''' <summary>
    ''' 按父窗口（Owner）居中。StartPosition=CenterParent 在「Show 出来的非模态窗体」上
    ''' 并不总是准（它会按创建那一刻的 Owner 位置算，而且不含边框），所以这里显式摆一次。
    ''' 只在首次显示时调用，用户拖动过之后再次 Ctrl+F 不会把窗口拽回去。
    ''' </summary>
    Public Sub CenterOnOwner()
        If Owner Is Nothing Then Return
        Dim host As Rectangle = Owner.Bounds
        Dim x As Integer = host.Left + (host.Width - Width) \ 2
        Dim y As Integer = host.Top + (host.Height - Height) \ 2

        ' 夹到显示器工作区里，父窗口贴边或比屏幕还大时不至于跑到屏幕外
        Dim area As Rectangle = Screen.FromControl(Me).WorkingArea
        If x + Width > area.Right Then x = area.Right - Width
        If y + Height > area.Bottom Then y = area.Bottom - Height
        If x < area.Left Then x = area.Left
        If y < area.Top Then y = area.Top

        Location = New Point(x, y)
    End Sub

    ' ---------- 内部工具 ----------

    ''' <summary>查找/替换一律忽略大小写（原来的复选框已去掉，默认就是这个行为）。</summary>
    Private ReadOnly Property Cmp As StringComparison
        Get
            Return StringComparison.OrdinalIgnoreCase
        End Get
    End Property

    Private Function Ready() As Boolean
        Return Target IsNot Nothing AndAlso Not Target.IsDisposed AndAlso TxtFind.Text.Length > 0
    End Function

    Private Sub Beep()
        Try
            SystemSounds.Hand.Play()
        Catch ex As Exception
            Debug.WriteLine("[忽略] " & ex.Message)
        End Try
    End Sub

    ''' <summary>选中并滚动到可见，但不抢焦点。</summary>
    Private Sub Highlight(index As Integer, length As Integer)
        Target.Select(index, length)
        Target.ScrollToCaret()
    End Sub

    ' ---------- 查找 ----------

    ''' <summary>从当前选区之后找下一个，到末尾绕回开头。返回是否命中。</summary>
    Public Function FindNext() As Boolean
        If Not Ready() Then Return False
        Dim needle As String = TxtFind.Text
        Dim hay As String = Target.Text
        If hay.Length = 0 Then Return False

        Dim start As Integer = Target.SelectionStart + Target.SelectionLength
        If start > hay.Length Then start = hay.Length
        Dim hit As Integer = hay.IndexOf(needle, start, Cmp)
        If hit < 0 Then hit = hay.IndexOf(needle, 0, Cmp)
        If hit < 0 Then
            Beep()
            Return False
        End If
        Highlight(hit, needle.Length)
        Return True
    End Function

    ''' <summary>从当前选区之前找上一个，到开头绕回末尾。</summary>
    Public Function FindPrev() As Boolean
        If Not Ready() Then Return False
        Dim needle As String = TxtFind.Text
        Dim hay As String = Target.Text
        If hay.Length = 0 Then Return False

        Dim start As Integer = Target.SelectionStart - 1
        If start >= hay.Length Then start = hay.Length - 1
        Dim hit As Integer = -1
        If start >= 0 Then hit = hay.LastIndexOf(needle, start, Cmp)
        If hit < 0 Then hit = hay.LastIndexOf(needle, Cmp)
        If hit < 0 Then
            Beep()
            Return False
        End If
        Highlight(hit, needle.Length)
        Return True
    End Function

    ' ---------- 替换 ----------

    ''' <summary>替换当前命中；当前选区不是命中就先跳到下一个命中。</summary>
    Public Sub ReplaceOne()
        If Not Ready() Then Return
        Dim needle As String = TxtFind.Text

        Dim isHit As Boolean = Target.SelectionLength = needle.Length AndAlso
                               String.Equals(Target.SelectedText, needle, Cmp)
        If Not isHit AndAlso Not FindNext() Then Return

        Dim at As Integer = Target.SelectionStart
        Target.SelectedText = TxtReplace.Text
        Target.Select(at + TxtReplace.Text.Length, 0)
        FindNext()
    End Sub

    ''' <summary>整篇替换，替换后弹一句数量；一处都没有就只响一声。</summary>
    Public Sub ReplaceAll()
        If Not Ready() Then Return
        Dim needle As String = TxtFind.Text
        Dim repl As String = TxtReplace.Text
        Dim hay As String = Target.Text
        If hay.Length = 0 Then Return

        Dim sb As New StringBuilder(hay.Length)
        Dim pos As Integer = 0
        Dim count As Integer = 0
        Do
            Dim hit As Integer = hay.IndexOf(needle, pos, Cmp)
            If hit < 0 Then Exit Do
            sb.Append(hay, pos, hit - pos)
            sb.Append(repl)
            pos = hit + needle.Length
            count += 1
        Loop
        If count = 0 Then
            Beep()
            Return
        End If
        sb.Append(hay, pos, hay.Length - pos)

        Dim caret As Integer = Target.SelectionStart
        Target.Text = sb.ToString()
        If caret > Target.TextLength Then caret = Target.TextLength
        Target.Select(caret, 0)
        MsgBox("已替换 " & count & " 处。", vbInformation, "全部替换")
    End Sub

    ' ---------- 事件 ----------

    Private Sub BtnPrev_Click(sender As Object, e As EventArgs) Handles BtnPrev.Click
        FindPrev()
    End Sub

    Private Sub BtnNext_Click(sender As Object, e As EventArgs) Handles BtnNext.Click
        FindNext()
    End Sub

    Private Sub BtnReplace_Click(sender As Object, e As EventArgs) Handles BtnReplace.Click
        ReplaceOne()
    End Sub

    Private Sub BtnReplaceAll_Click(sender As Object, e As EventArgs) Handles BtnReplaceAll.Click
        ReplaceAll()
    End Sub

    Private Sub TxtFind_KeyDown(sender As Object, e As KeyEventArgs) Handles TxtFind.KeyDown
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            e.Handled = True
            If e.Shift Then FindPrev() Else FindNext()
        ElseIf e.KeyCode = Keys.Escape Then
            e.SuppressKeyPress = True
            Close()
        End If
    End Sub

    Private Sub TxtReplace_KeyDown(sender As Object, e As KeyEventArgs) Handles TxtReplace.KeyDown
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            e.Handled = True
            ReplaceOne()
        ElseIf e.KeyCode = Keys.Escape Then
            e.SuppressKeyPress = True
            Close()
        End If
    End Sub

End Class
