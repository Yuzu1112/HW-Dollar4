Imports Microsoft.Win32
Imports System.IO
Imports System.IO.Compression
Imports System.Media
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' MainUI 的保存与写出部分。
''' 由原 MainUI.vb 按成员边界拆出（Partial Class），方法体逐行未改。
''' </summary>
Partial Public Class MainUI

    Private Sub BtnSave_Click(sender As Object, e As EventArgs) Handles BtnSave.Click
        Try
            ' 按钮不置灰，所以这里拦：写不出正常文件时只弹原因，不落盘
            If _saveBlockReason.Length > 0 Then
                MsgBox(_saveBlockReason, vbInformation, "无法保存")
                Exit Sub
            End If

            If SFDBoardinfo.ShowDialog = DialogResult.OK Then
                Dim SaveBuffer As Byte() = Encoding.UTF8.GetBytes(TxtMain.Text)
                Dim SaveBufferHandled As Boolean = False
                Dim SaveBufferInvalid As Boolean = False
                ' $2 配置是 01 容器，设备不读 .hash，别在旁边多写一个文件
                Dim SkipHash As Boolean = False

                ' $3 只有解密，没有加密：明文状态下停在 $3 模式就直接拦下来，避免误写成明文。
                If RadD3.Checked AndAlso Not SaveBufferHandled Then
                    If Not (TxtMain.Text.StartsWith("$3") AndAlso TxtMain.Text.EndsWith("$")) Then
                        TxtMain.Text = "[ 提示 ] $3 只支持解密，无法加密保存！"
                        Exit Sub
                    End If
                    SkipHash = True   ' 字段级密文串，旁边不写 .hash
                End If

                If ChkPolicy.Checked AndAlso SaveBuffer(0) = &H3C Then
                    Dim buf1 As Byte() = New Byte() {}
                    If RadD4.Checked Then
                        ' 0712 包的内层 03 头写 (0,0)、口令用默认派生值 —— 真机下载包就是这个形态，
                        ' 设备按 (0,0) 走默认派生。这里显式指定，避免沿用上一次解密残留的密钥槽。
                        D4KeyStore = New Byte(3) {}
                        D4Password = HW_D2_Key
                        _keepKeySlot = True
                        Try
                            buf1 = HW_D4EncryptXML(TxtMain.Text)
                        Finally
                            _keepKeySlot = False
                        End Try
                        HW_D4Reset()
                    ElseIf RadD2.Checked Then
                        ' $2 模式 + 下载：勾「动态」→ 内层 02（随机钥匙随包走，恒 gzip）；
                        ' 不勾 → 内层 01（固定 HW_CTREE_Key，压不压缩看「压缩」）。
                        If ChkDynKey.Checked Then
                            buf1 = HW_D2DynamicXML(TxtMain.Text)
                        Else
                            buf1 = HW_D2StaticXML(TxtMain.Text, ChkZip.Checked)
                        End If
                    Else
                        ' 兜底：其它模式没有对应的内层形态，「下载」在那些模式下也是隐藏的。
                        TxtMain.Text = "[ 提示 ] 下载包只支持 $2 或 $4 模式！"
                        Exit Sub
                    End If

                    Dim b1(buf1.Length + 327) As Byte
                    b1(0) = &H7
                    b1(1) = &H12
                    b1(2) = &H21
                    b1(3) = &H20

                    b1(52) = &H48
                    b1(53) = &H1

                    Dim buf2 As Byte() = BitConverter.GetBytes(Convert.ToInt64(buf1.Length))
                    b1(56) = buf2(0)
                    b1(57) = buf2(1)
                    b1(58) = buf2(2)
                    b1(59) = buf2(3)

                    b1(96) = D4KeyStore(1)
                    b1(97) = D4KeyStore(0)
                    b1(100) = D4KeyStore(3)
                    b1(101) = D4KeyStore(2)

                    b1(60) = &H1
                    Dim b2(39) As Byte
                    Array.Copy(b1, 64, b2, 0, b2.Length)
                    Dim buf3 As Byte() = BitConverter.GetBytes(CRC32(b2))
                    Dim buf4 As Byte() = BitConverter.GetBytes(CRC32(buf1))

                    b1(320) = buf4(0)
                    b1(321) = buf4(1)
                    b1(322) = buf4(2)
                    b1(323) = buf4(3)

                    b1(324) = buf3(0)
                    b1(325) = buf3(1)
                    b1(326) = buf3(2)
                    b1(327) = buf3(3)

                    Array.Copy(buf1, 0, b1, 328, buf1.Length)

                    My.Computer.FileSystem.WriteAllBytes(SFDBoardinfo.FileName, b1, False)
                    TxtMain.Text = "[ 提示 ] 文件已保存！" & vbCrLf & SFDBoardinfo.FileName
                    Exit Sub
                Else
                    ChkPolicy.Checked = False
                End If

                If RadD4.Checked Then
                    If SaveBuffer(0) = &H3C Then
                        SaveBuffer = HW_D4EncryptXML(TxtMain.Text)
                        SaveBufferHandled = True
                    Else
                        If BoardInfoVersion = 3 Then BoardInfoVersion = 5
                    End If
                End If

                ' $2 模式：存成 01 容器。ChkZip 只在这个模式下出现，
                ' 勾着 = 正文先 gzip（压缩后加密），不勾 = 原样加密。
                If RadD2.Checked AndAlso Not SaveBufferHandled Then
                    If TxtMain.TextLength > 0 Then
                        ' 勾「动态」→ 02 容器（随机钥匙随文件走，恒 gzip）；
                        ' 不勾 → 01 容器（固定 HW_CTREE_Key，压不压缩看「压缩」）。
                        If ChkDynKey.Checked Then
                            SaveBuffer = HW_D2DynamicXML(TxtMain.Text)
                        Else
                            SaveBuffer = HW_D2StaticXML(TxtMain.Text, ChkZip.Checked)
                        End If
                        SaveBufferHandled = True
                        SkipHash = True
                    Else
                        SaveBufferInvalid = True
                    End If
                End If

                ' $1 是字段级加密，本身没有文件容器：把密文串直接写进文件。
                ' 文本框里已经是 $ 开头的密文就不再套一层，避免二次加密。
                ' $3 只支持解密（设备侧产物，加密参数无法从单向样本反推），不提供写出。
                If RadD1.Checked AndAlso Not SaveBufferHandled AndAlso TxtMain.TextLength > 0 AndAlso Not TxtMain.Text.StartsWith("$") Then
                    SaveBuffer = Encoding.UTF8.GetBytes(HW_D1Encrypt(TxtMain.Text, False))
                    SaveBufferHandled = True
                    SkipHash = True
                End If

                If RadVx.Checked AndAlso (BoardInfoVersion = 5 OrElse BoardInfoVersion = 6) AndAlso Not SaveBufferHandled Then
                    If Not ((TxtMain.Text.StartsWith("$2") OrElse TxtMain.Text.StartsWith("$4")) AndAlso TxtMain.Text.EndsWith("$")) Then
                        If ParseText() Then
                            SaveBuffer = Encoding.UTF8.GetBytes(TxtMain.Text)
                        Else
                            SaveBufferInvalid = True
                        End If
                    End If

                    Dim HeadBuffer As Byte() = New Byte(15) {}
                    If BoardInfoVersion = 5 Then
                        HeadBuffer(0) = &H1B
                        HeadBuffer(1) = &H5C
                        HeadBuffer(2) = &H9F
                        HeadBuffer(3) = &H3A
                        HeadBuffer(4) = &H12
                        HeadBuffer(5) = &H3
                        HeadBuffer(6) = &H20
                        HeadBuffer(7) = &H20
                    ElseIf BoardInfoVersion = 6 Then
                        HeadBuffer(0) = &H3A
                        HeadBuffer(1) = &H9F
                        HeadBuffer(2) = &H5C
                        HeadBuffer(3) = &H1B
                        HeadBuffer(4) = &H20
                        HeadBuffer(5) = &H20
                        HeadBuffer(6) = &H3
                        HeadBuffer(7) = &H12
                    End If

                    Dim buf5 As Byte() = BitConverter.GetBytes(Convert.ToInt64(SaveBuffer.Length))
                    If BoardInfoVersion = 5 Then
                        HeadBuffer(8) = buf5(0)
                        HeadBuffer(9) = buf5(1)
                        HeadBuffer(10) = buf5(2)
                        HeadBuffer(11) = buf5(3)
                    ElseIf BoardInfoVersion = 6 Then
                        HeadBuffer(8) = buf5(3)
                        HeadBuffer(9) = buf5(2)
                        HeadBuffer(10) = buf5(1)
                        HeadBuffer(11) = buf5(0)
                    End If

                    Dim buf6 As Byte() = BitConverter.GetBytes(CRC32(SaveBuffer))
                    If BoardInfoVersion = 5 Then
                        HeadBuffer(12) = buf6(0)
                        HeadBuffer(13) = buf6(1)
                        HeadBuffer(14) = buf6(2)
                        HeadBuffer(15) = buf6(3)
                    ElseIf BoardInfoVersion = 6 Then
                        HeadBuffer(12) = buf6(3)
                        HeadBuffer(13) = buf6(2)
                        HeadBuffer(14) = buf6(1)
                        HeadBuffer(15) = buf6(0)
                    End If

                    Dim DataBuffer As Byte() = New Byte(SaveBuffer.Length + 19) {}
                    Array.Copy(HeadBuffer, 0, DataBuffer, 0, HeadBuffer.Length)
                    Dim buf7 As Byte() = BitConverter.GetBytes(CRC32(HeadBuffer))
                    If BoardInfoVersion = 5 Then
                        DataBuffer(16) = buf7(0)
                        DataBuffer(17) = buf7(1)
                        DataBuffer(18) = buf7(2)
                        DataBuffer(19) = buf7(3)
                    ElseIf BoardInfoVersion = 6 Then
                        DataBuffer(16) = buf7(3)
                        DataBuffer(17) = buf7(2)
                        DataBuffer(18) = buf7(1)
                        DataBuffer(19) = buf7(0)
                    End If

                    Array.Copy(SaveBuffer, 0, DataBuffer, 20, SaveBuffer.Length)
                    SaveBuffer = DataBuffer
                End If

                If SaveBufferInvalid Then
                    TxtMain.Text = "[ 提示 ] 文件保存失败！"
                Else
                    My.Computer.FileSystem.WriteAllBytes(SFDBoardinfo.FileName, SaveBuffer, False)
                    If SkipHash Then
                        TxtMain.Text = "[ 提示 ] 文件已保存！" & vbCrLf & SFDBoardinfo.FileName
                    Else
                        Dim hmac_result As Byte()
                        Using hmac As New HMACSHA256(HW_D2_Key)
                            hmac_result = hmac.ComputeHash(SaveBuffer)
                        End Using
                        My.Computer.FileSystem.WriteAllBytes(SFDBoardinfo.FileName & ".hash", hmac_result, False)
                        TxtMain.Text = "[ 提示 ] 文件已保存！" & vbCrLf & SFDBoardinfo.FileName & vbCrLf & SFDBoardinfo.FileName & ".hash"
                    End If
                End If
            End If
        Catch ex As Exception
            TxtMain.Text = "[ 提示 ] 文件保存失败！"
        End Try
    End Sub

    ''' <summary>
    ''' 「动态」= 02 容器：钥匙随文件走，正文恒 gzip。勾上时把「压缩」强制勾上并禁用，
    ''' 免得出现「动态 + 不压缩」这种解包端不会遇到的组合。
    ''' </summary>
    Private Sub ChkDynKey_CheckedChanged(sender As Object, e As EventArgs) Handles ChkDynKey.CheckedChanged
        SyncZipState()
    End Sub

    ''' <summary>
    ''' 设备只认 gzip 过的 $2 下载包（内层 01），所以只在 $2 模式下做约束：
    ''' 勾「下载」自动补上「压缩」；取消「压缩」表示这一包不合法，连带取消「下载」。
    ''' $4 模式不参与 —— 它的内层 03 由 HW_D4EncryptXML 内部固定 gzip，跟这个复选框无关，
    ''' 而「压缩」在 $4 模式下本来也是隐藏的。
    ''' </summary>
    Private Sub ChkPolicy_CheckedChanged(sender As Object, e As EventArgs) Handles ChkPolicy.CheckedChanged
        Try
            SyncZipState()
        Catch
            Debug.WriteLine("[忽略] 已忽略一个异常")
        End Try
    End Sub

    Private Sub ChkZip_CheckedChanged(sender As Object, e As EventArgs) Handles ChkZip.CheckedChanged
        Try
            If Not ChkZip.Checked AndAlso RadD2.Checked AndAlso ChkPolicy.Checked Then ChkPolicy.Checked = False
        Catch
            Debug.WriteLine("[忽略] 已忽略一个异常")
        End Try
    End Sub

    ''' <summary>
    ''' 「压缩」什么时候必须勾且不可取消：
    ''' ① 勾了「动态」—— 02 容器正文恒 gzip；
    ''' ② 勾了「下载」—— 设备只认压缩过的下载包。
    ''' 两种情况都强制打勾并把控件置灰，避免出现解包端不会遇到的组合。
    ''' </summary>
    Private Sub SyncZipState()
        Try
            Dim locked As Boolean = ChkDynKey.Checked OrElse (RadD2.Checked AndAlso ChkPolicy.Checked)
            If locked Then ChkZip.Checked = True
            ChkZip.Enabled = Not locked
            UpdateSaveState()
        Catch
            Debug.WriteLine("[忽略] 已忽略一个异常")
        End Try
    End Sub

    ''' <summary>
    ''' 只记录「能不能存」，**不把按钮置灰** —— 按钮颜色与主窗口保持一致。
    ''' 不能存时点保存会弹出原因，不会悄悄写出一份格式不对的文件。
    ''' </summary>
    Private Sub UpdateSaveState()
        Try
            Dim reason As String = ""
            Dim text As String = TxtMain.Text
            If text.Length = 0 Then
                reason = "文本框是空的，没有可保存的内容。"
            Else
                Dim s As String = text.TrimStart()
                Dim isXml As Boolean = s.StartsWith("<")
                Dim isField As Boolean = s.StartsWith("$") AndAlso s.EndsWith("$")

                If RadD4.Checked Then
                    ' $4 只有两种可写形态：XML → 03 容器；$4 令牌 → 原样写出。
                    If Not (isXml OrElse (isField AndAlso s.StartsWith("$4"))) Then
                        reason = "$4 模式只能保存 XML（生成 $4 容器）或 $4 令牌本身；当前正文没有对应的落盘形态。"
                    End If
                ElseIf RadD3.Checked Then
                    ' $3 只支持解密：只有正文本身就是 $3 令牌时才允许原样写出。
                    If Not (isField AndAlso s.StartsWith("$3")) Then
                        reason = "$3 只支持解密，不能加密保存。"
                    End If
                End If

                ' 下载包必须是 XML（0712 分支的前提就是正文以 '<' 开头）
                If reason.Length = 0 AndAlso ChkPolicy.Checked AndAlso Not isXml Then
                    reason = "勾了「下载」时正文必须是 XML（0712 下载包的前提）。"
                End If
            End If

            _saveBlockReason = reason
            BtnSave.Enabled = True
            BtnSave.BackColor = Color.MediumBlue
            BtnSave.ForeColor = Color.White
        Catch
            Debug.WriteLine("[忽略] 已忽略一个异常")
        End Try
    End Sub

End Class
