Imports Microsoft.Win32
Imports System.IO
Imports System.IO.Compression
Imports System.Media
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' MainUI 的界面事件与模式行布局部分。
''' 由原 MainUI.vb 按成员边界拆出（Partial Class），方法体逐行未改。
''' </summary>
Partial Public Class MainUI

    <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Public Shared Function AppendMenu(hMenu As IntPtr, uFlags As MenuFlags, uIDNewItem As UIntPtr, lpNewItem As String) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    ''' <summary>
    ''' $2 模式：保存成 01 容器（只认 $2 静态钥匙）。压缩复选框只在这个模式下出现，
    ''' 默认不勾 —— 明文输入的默认出口是「$2 不压缩加密」。
    ''' </summary>
    ''' <summary>当前模式决定下拉显示什么：Vx 显示 V3/V5/V6，$2 显示 OS/BOARDINFO/OMCI，其余隐藏。</summary>
    Private Sub ApplyMode()
        If _modeLock Then Return
        _modeLock = True
        Try
            If RadVx.Checked Then
                FillModeCombo(New String() {"V3", "V5", "V6"}, "V" & BoardInfoVersion.ToString())
                CmbMode.Visible = True
            ElseIf RadD2.Checked Then
                FillModeCombo(New String() {"OS", "BOARDINFO", "OMCI"}, D2KeyType)
                CmbMode.Visible = True
            Else
                CmbMode.Visible = False
            End If
            ' 「动态」和「压缩」只服务 $2 模式：前者决定 01/02，后者决定压不压。
            ChkDynKey.Visible = RadD2.Checked
            ChkZip.Visible = RadD2.Checked
            ' 下载包只有两条路：$2（内层 01/02）和 $4（内层 03）。其余模式没有对应内层，
            ' 直接把「下载」藏起来，避免用户勾了却产出个四不像的包。
            ChkPolicy.Visible = RadD2.Checked OrElse RadD4.Checked
            If Not ChkPolicy.Visible Then ChkPolicy.Checked = False
            If Not ChkDynKey.Visible Then ChkDynKey.Checked = False
        Finally
            _modeLock = False
        End Try
        SyncZipState()
        UpdateSaveState()
        LayoutModeRow()
    End Sub

    Private Sub BtnCopy_Click(sender As Object, e As EventArgs) Handles BtnCopy.Click
        Try
            If TxtMain.TextLength > 0 Then
                Clipboard.SetText(TxtMain.Text)
            End If
        Catch ex As Exception
            ' 规范要求：不静默吞异常，至少在调试器里留痕
            Debug.WriteLine("[忽略] " & ex.GetType().Name & ": " & ex.Message)
        End Try
    End Sub

    Private Sub BtnOpen_Click(sender As Object, e As EventArgs) Handles BtnOpen.Click
        Try
            TxtMain.Clear()
            BoardInfoVersion = 3
            If OfdBoardinfo.ShowDialog = DialogResult.OK Then
                LoadConfig(OfdBoardinfo.FileName)
            End If
        Catch ex As Exception
            ' 规范要求：不静默吞异常，至少在调试器里留痕
            Debug.WriteLine("[忽略] " & ex.GetType().Name & ": " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' 「计算」：按当前模式把正文里的 $x…$ 字段解密 / 加密（Vx 模式下用于板信息里的 $2/$4 令牌）。
    ''' </summary>
    Private Sub BtnParse_Click(sender As Object, e As EventArgs) Handles BtnParse.Click
        Try
            ParseText()
        Catch ex As Exception
            ' 规范要求：不静默吞异常，至少在调试器里留痕
            Debug.WriteLine("[忽略] " & ex.GetType().Name & ": " & ex.Message)
        End Try
    End Sub

    Private Sub CmbMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles CmbMode.SelectedIndexChanged
        If _modeLock Then Return
        Try
            If RadVx.Checked Then
                Select Case CmbMode.Text
                    Case "V3" : BoardInfoVersion = 3
                    Case "V5" : BoardInfoVersion = 5
                    Case "V6" : BoardInfoVersion = 6
                End Select
            ElseIf RadD2.Checked Then
                D2KeyType = CmbMode.Text
            End If
        Catch ex As Exception
            ' 规范要求：不静默吞异常，至少在调试器里留痕
            Debug.WriteLine("[忽略] " & ex.GetType().Name & ": " & ex.Message)
        End Try
    End Sub

    ''' <summary>按给定项重建下拉并选中指定值，期间屏蔽选择事件。</summary>
    Private Sub FillModeCombo(items As String(), selected As String)
        _modeLock = True
        Try
            CmbMode.Items.Clear()
            CmbMode.Items.AddRange(items)
            Dim i As Integer = Array.IndexOf(items, selected)
            CmbMode.SelectedIndex = If(i >= 0, i, 0)
        Finally
            _modeLock = False
        End Try
    End Sub

    Public Function GetEnvironmentVersion() As Integer
        Try
            Dim ndpKey As RegistryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", False)
            If ndpKey IsNot Nothing Then
                Return CInt(ndpKey.GetValue("Release", "0"))
            End If
            Return 0
        Catch ex As Exception
            Return 0
        End Try
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.Winapi, SetLastError:=True)>
    Public Shared Function GetSystemMenu(hWnd As IntPtr, <MarshalAs(UnmanagedType.Bool)> bRevert As Boolean) As IntPtr
    End Function

    ''' <summary>
    ''' 底部模式行布局：下拉贴容器左边线、压缩/下载贴容器右边线，五个模式单选作为一组整体居中。
    ''' 「容器两边」取 TxtMain 的左右边界，所以这一行和上面的文本框严格对齐。
    ''' 单选居中后再夹一次，保证任何模式下都不会顶到下拉或复选框。
    ''' </summary>
    Private Sub LayoutModeRow()
        Const gap As Integer = 12        ' 模式单选 / 复选框之间
        Const gapCombo As Integer = 10   ' 下拉与第一个模式单选之间
        Const centerY As Integer = 537   ' 整行视觉中线（下拉 28 高、单选 24 高、复选框 23 高，统一按它对齐）

        Dim leftEdge As Integer = TxtMain.Left        ' 容器左边线（12）
        Dim rightEdge As Integer = TxtMain.Right      ' 容器右边线（772）

        ' ---- 右侧复选框：从右往左依次是 下载 / 压缩 / 动态 ----
        ChkPolicy.Left = rightEdge - RowItemWidth(ChkPolicy)
        ChkPolicy.Top = centerY - CInt(ChkPolicy.Height / 2)

        ChkZip.Left = ChkPolicy.Left - gap - RowItemWidth(ChkZip)
        ChkZip.Top = centerY - CInt(ChkZip.Height / 2)

        ChkDynKey.Left = ChkZip.Left - gap - RowItemWidth(ChkDynKey)
        ChkDynKey.Top = centerY - CInt(ChkDynKey.Height / 2)

        ' ---- 左侧下拉：贴左边线 ----
        CmbMode.Left = leftEdge
        CmbMode.Top = centerY - CInt(CmbMode.Height / 2)

        ' ---- 中间单选：五个算一组，按客户区中线居中 ----
        Dim radios As Control() = {RadVx, RadD1, RadD2, RadD3, RadD4}
        Dim total As Integer = 0
        Dim i As Integer = 0
        For Each c As Control In radios
            total += RowItemWidth(c)
            If i > 0 Then total += gap
            i += 1
        Next

        Dim x As Integer = CInt((ClientSize.Width - total) / 2)

        ' 夹在「下拉右边」和「压缩/下载左边」之间，避免重叠
        Dim minX As Integer = If(CmbMode.Visible, CmbMode.Right + gapCombo, leftEdge)
        Dim blocker As Control = If(ChkDynKey.Visible, ChkDynKey, If(ChkZip.Visible, ChkZip, ChkPolicy))
        Dim maxX As Integer = blocker.Left - gap - total
        If x > maxX Then x = maxX
        If x < minX Then x = minX

        For Each c As Control In radios
            c.Left = x
            c.Top = centerY - CInt(c.Height / 2)
            x += RowItemWidth(c) + gap
        Next
    End Sub

    Private Sub LblVersion_Click(sender As Object, e As EventArgs) Handles LblVersion.Click
        Try
            Process.Start(Application.ExecutablePath)
        Catch ex As Exception
            ' 规范要求：不静默吞异常，至少在调试器里留痕
            Debug.WriteLine("[忽略] " & ex.GetType().Name & ": " & ex.Message)
        End Try
    End Sub

    Private Sub MainUI_DragDrop(sender As Object, e As DragEventArgs) Handles Me.DragDrop
        Try
            Dim dropped As String() = TryCast(e.Data.GetData(DataFormats.FileDrop), String())
            If dropped IsNot Nothing AndAlso dropped.Length > 0 Then LoadConfig(dropped(0))
        Catch ex As Exception
            ' 规范要求：不静默吞异常，至少在调试器里留痕
            Debug.WriteLine("[忽略] " & ex.GetType().Name & ": " & ex.Message)
        End Try
    End Sub

    Private Sub MainUI_DragEnter(sender As Object, e As DragEventArgs) Handles Me.DragEnter
        Try
            If e.Data.GetDataPresent(DataFormats.FileDrop) = True Then
                e.Effect = DragDropEffects.Copy
            Else
                e.Effect = DragDropEffects.None
            End If
        Catch ex As Exception
            ' 规范要求：不静默吞异常，至少在调试器里留痕
            Debug.WriteLine("[忽略] " & ex.GetType().Name & ": " & ex.Message)
        End Try
    End Sub

    Private Sub MainUI_Load(sender As Object, e As EventArgs) Handles Me.Load
        Try
            ' 冷启动时 D4KeyStore / D4Password 还是字段默认值（空数组）。上游原版没有这一步初始化，
            ' 首次打开 $4 容器会在 HW_D4Decrypt 的 Array.Copy(block, 8, D4KeyStore, 0, 4) 处越界，
            ' 异常被 LoadConfig 的 Catch 吞成「文件打开失败」——表现就是「必须先操作一次别的文件才解得开」。
            ' 启动即置成与「一次 $4 处理结束后」完全相同的状态：默认槽 (1,2) + $2 静态钥匙。
            HW_D4Reset()

            Dim VersionStrings As String() = Application.ProductVersion.ToString.Split("."c)
            LblVersion.Text = "HW Dollar4 版本: " & VersionStrings(0) & "." & VersionStrings(1) & "." & VersionStrings(2) & " (20" & VersionStrings(3).Substring(0, 2) & "." & VersionStrings(3).Substring(2, 2) & ")"

            ' 原版 MainUI.vb:855 同样有一句。TextBox 默认 HideSelection=True：一旦失焦，
            ' 选区就被系统隐藏。查找窗口是常驻且持有焦点的，没有这一句就完全看不到命中的位置。
            TxtMain.HideSelection = False

            ' 下拉项改为按模式动态填充，启动时先建立一次
            ApplyMode()

            If GetEnvironmentVersion() < 461808 Or LblVersion.Font.Size > 12 Then
                MsgBox("若要运行此应用程序，您必须首先安装 .NET Framework 的以下版本之一:" & vbCrLf & " v4.7.2", vbCritical, "HW Dollar4 - .NET Framework 初始化错误")
                End
            End If

            ' aescrypt2.exe 与 libmbedtls.dll 的运行时校验已移除：两者对应的功能
            ' 现由纯托管的 HwAescrypt2 / HwGcm 完成，程序不再依赖任何外部原生组件。

            ' GetSystemMenu 只取一次句柄复用，避免三次独立调用
            Dim SysMenu As IntPtr = GetSystemMenu(Handle, False)
            If SysMenu <> IntPtr.Zero Then
                AppendMenu(SysMenu, MenuFlags.MF_SEPARATOR, New UIntPtr(CUInt(&H1FFD)), "SEPARATOR")
                AppendMenu(SysMenu, MenuFlags.MF_STRING, New UIntPtr(CUInt(&H1FFE)), "开源软件使用声明(&O)")
                AppendMenu(SysMenu, MenuFlags.MF_STRING, New UIntPtr(CUInt(&H1FFF)), "关于(&A)")
            End If
        Catch ex As Exception
            ' 规范要求：不静默吞异常，至少在调试器里留痕
            Debug.WriteLine("[忽略] " & ex.GetType().Name & ": " & ex.Message)
        End Try
    End Sub

    Private Sub MainUI_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        Try
            LayoutModeRow()
        Catch
            Debug.WriteLine("[忽略] 已忽略一个异常")
        End Try
    End Sub

    Private Sub ModeRadio_CheckedChanged(sender As Object, e As EventArgs) Handles RadVx.CheckedChanged, RadD1.CheckedChanged, RadD2.CheckedChanged, RadD3.CheckedChanged, RadD4.CheckedChanged
        Try
            If _modeLock Then Return
            If Not RadVx.Checked Then BoardInfoVersion = 3
            If RadD1.Checked OrElse RadD3.Checked Then HW_D4Reset()
            ApplyMode()
        Catch ex As Exception
            ' 规范要求：不静默吞异常，至少在调试器里留痕
            Debug.WriteLine("[忽略] " & ex.GetType().Name & ": " & ex.Message)
        End Try
    End Sub

    ''' <summary>AutoSize 控件在布局前 Width 还是设计器里的旧值，取实测偏大者。</summary>
    Private Function RowItemWidth(c As Control) As Integer
        Dim w As Integer = c.Width
        If c.AutoSize Then
            Dim measured As Integer = c.PreferredSize.Width
            If measured > w Then w = measured
        End If
        Return w
    End Function

    ''' <summary>
    ''' 非模态显示查找/替换窗口。Ctrl+F 聚焦查找框，Ctrl+H 聚焦替换框；
    ''' 主窗口里选中了单行文字会自动带进去当查找内容。
    ''' </summary>
    Private Sub ShowSearchWindow(replaceMode As Boolean)
        Try
            Dim fresh As Boolean = False
            If _searchForm Is Nothing OrElse _searchForm.IsDisposed Then
                _searchForm = New Search()
                _searchForm.Target = TxtMain
                _searchForm.Owner = Me
                fresh = True
            End If
            If Not _searchForm.Visible Then
                _searchForm.Show()
                ' 只有新建那次才摆位置：用户挪过窗口后，再按 Ctrl+F 不该把它拽回来
                If fresh Then _searchForm.CenterOnOwner()
            Else
                _searchForm.Activate()
            End If
            _searchForm.SetFindText(TxtMain.SelectedText)
            _searchForm.FocusReplace(replaceMode)
        Catch
            Debug.WriteLine("[忽略] 已忽略一个异常")
        End Try
    End Sub

    ''' <summary>外部改了 BoardInfoVersion / D2KeyType 后，把下拉同步过来。</summary>
    Private Sub SyncModeCombo()
        ApplyMode()
    End Sub

    ''' <summary>
    ''' 正文**整段**就是一个 $x…$ 字段时，底部模式单选自动跳到对应项。
    ''' 只认「整段就是一个字段」：XML 里零散出现 $2 属性不会乱跳，提示信息以 '[' 开头也不跳。
    ''' Focused 判断是必要的 —— 打开文件时 LoadConfig 会把令牌原样塞进文本框，
    ''' 那种情况不该按正文内容改模式（V5/V6 板信息必须停在 Vx）。
    ''' </summary>
    Private Sub TxtMain_TextChanged(sender As Object, e As EventArgs) Handles TxtMain.TextChanged
        Try
            UpdateSaveState()
            If Not TxtMain.Focused Then Return
            Dim s As String = TxtMain.Text.Trim()
            If s.Length < 3 OrElse Not s.EndsWith("$") Then Return
            Select Case s.Substring(0, 2)
                Case "$1"
                    RadD1.Checked = True
                Case "$2"
                    RadD2.Checked = True
                Case "$3"
                    RadD3.Checked = True
                Case "$4"
                    RadD4.Checked = True
            End Select
        Catch
            Debug.WriteLine("[忽略] 已忽略一个异常")
        End Try
    End Sub

End Class
