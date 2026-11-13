Imports Microsoft.Win32
Imports System.IO
Imports System.IO.Compression
Imports System.Media
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' MainUI 的配置文件读取与字段解析部分。
''' 由原 MainUI.vb 按成员边界拆出（Partial Class），方法体逐行未改。
''' </summary>
Partial Public Class MainUI

    Private Sub LoadConfig(configPath As String)
        Try
            If My.Computer.FileSystem.FileExists(configPath) Then
                TxtMain.Clear()
                RadVx.Checked = True
                BoardInfoVersion = 3
                SyncModeCombo()
                ChkPolicy.Checked = False
                If Path.GetFileName(configPath) = "kmc_store_A" OrElse Path.GetFileName(configPath) = "kmc_store_B" Then
                    D4KeyStoreLast = Path.GetDirectoryName(configPath)
                    TxtMain.Text = KMCv2_GetAllMasterKey(configPath)
                    Exit Sub
                End If
                Dim InputBuffer As Byte() = My.Computer.FileSystem.ReadAllBytes(configPath)
                If InputBuffer(0) = &H67 AndAlso InputBuffer(1) = &H66 AndAlso InputBuffer(2) = &H63 AndAlso InputBuffer(3) = &H71 Then
                    Dim buf1 As Byte() = New Byte(InputBuffer.Length - 33) {}
                    Array.Copy(InputBuffer, 32, buf1, 0, InputBuffer.Length - 32)
                    Dim buf2 As Byte() = New Byte(3) {}
                    Array.Copy(InputBuffer, 4, buf2, 0, 4)
                    Dim Ctce8PayloadCRC As String = BytesToHex(buf2)
                    If Not BytesToHex(BitConverter.GetBytes(CRC32(buf1))) = Ctce8PayloadCRC OrElse Not BitConverter.ToInt32(InputBuffer, 8) = buf1.Length Then
                        TxtMain.Text = "[ 提示 ] CFG配置文件CRC检查失败！"
                        Exit Sub
                    End If
                    InputBuffer = UnGZip(buf1)
                ElseIf InputBuffer(0) = &H7 AndAlso InputBuffer(1) = &H12 AndAlso InputBuffer(2) = &H21 AndAlso InputBuffer(3) = &H20 Then
                    Dim n1 As Integer = CInt(GetLittleEndian32(InputBuffer, 52))
                    If n1 = 328 Then
                        Dim n2 As Integer = CInt(GetLittleEndian32(InputBuffer, 56))
                        Dim b1(n2 - 1) As Byte
                        Array.Copy(InputBuffer, 328, b1, 0, n2)
                        Dim b2(39) As Byte
                        Array.Copy(InputBuffer, 64, b2, 0, b2.Length)
                        If GetLittleEndian32(InputBuffer, 60) = 1 AndAlso GetLittleEndian32(InputBuffer, 320) = CRC32(b1) AndAlso GetLittleEndian32(InputBuffer, 324) = CRC32(b2) Then
                            If GetLittleEndian32(InputBuffer, 328) = 3 Then
                                Dim n3 As Integer = CInt(GetLittleEndian32(InputBuffer, 4))
                                PolicyPassword = ""
                                If Not (GetLittleEndian32(InputBuffer, 96) = 1 AndAlso GetLittleEndian32(InputBuffer, 100) = 2) Then
                                    Using dlgPolicy As New Policy()
                                        dlgPolicy.ShowDialog()
                                    End Using
                                End If
                                If PolicyPassword.Length = 0 Then
                                    D4KeyStore = New Byte(3) {}
                                Else
                                    If n3 = 1 Then
                                        D4Password = Encoding.UTF8.GetBytes(PolicyPassword)
                                        D4KeyStore = New Byte(3) {}
                                    ElseIf n3 = 2 Then
                                        Dim n4 As Integer = Array.IndexOf(InputBuffer, CByte(0), 12) - 12
                                        Dim b3(n4 - 1) As Byte
                                        Array.Copy(InputBuffer, 12, b3, 0, n4)
                                        D4Password = Encoding.UTF8.GetBytes(BytesToHex(PKCS5_PBKDF2_HMAC(Encoding.UTF8.GetBytes(PolicyPassword), b3, 10000, 16)).ToLower())
                                        D4KeyStore = New Byte(3) {}
                                    Else
                                        D4KeyStore = New Byte(3) {}
                                    End If
                                End If
                                TxtMain.Text = HW_D4DecryptXML(b1)
                                RadD4.Checked = True
                                ChkPolicy.Checked = True
                                PolicyPassword = ""
                                HW_D4Reset()
                                Exit Sub
                            Else
                                ChkPolicy.Checked = True
                                InputBuffer = b1
                            End If
                        Else
                            TxtMain.Text = "[ 提示 ] 下载配置文件CRC检查失败！"
                            Exit Sub
                        End If
                    Else
                        TxtMain.Text = "[ 提示 ] 文件打开失败！"
                        Exit Sub
                    End If
                End If
                If InputBuffer(0) = &H1B AndAlso InputBuffer(1) = &H5C AndAlso InputBuffer(2) = &H9F AndAlso InputBuffer(3) = &H3A AndAlso InputBuffer(4) = &H12 AndAlso InputBuffer(5) = &H3 AndAlso InputBuffer(6) = &H20 AndAlso InputBuffer(7) = &H20 Then
                    RadVx.Checked = True
                    BoardInfoVersion = 5
                    SyncModeCombo()
                ElseIf InputBuffer(3) = &H1B AndAlso InputBuffer(2) = &H5C AndAlso InputBuffer(1) = &H9F AndAlso InputBuffer(0) = &H3A AndAlso InputBuffer(7) = &H12 AndAlso InputBuffer(6) = &H3 AndAlso InputBuffer(5) = &H20 AndAlso InputBuffer(4) = &H20 Then
                    RadVx.Checked = True
                    BoardInfoVersion = 6
                    SyncModeCombo()
                ElseIf InputBuffer(0) = &H20 AndAlso InputBuffer(1) = &H22 AndAlso InputBuffer(2) = &H7 AndAlso InputBuffer(3) = &H9 Then
                    ' 原版守门（原 MainUI.vb:753）：口令/KSF 没准备好就不碰文件。
                    ' 这一段是「打开即解密落盘」——写完还会把原文件改名成 原名_0001，
                    ' 所以失败分支必须显式停下来，不能带着半截状态往下走。
                    If HW_D4SetPassword(Path.GetDirectoryName(configPath)) Then
                        Dim D4DecryptBuffer As Byte() = HW_D4Decrypt(InputBuffer)
                        If D4DecryptBuffer.Length > 0 Then
                            RenameConfig(configPath)
                            My.Computer.FileSystem.WriteAllBytes(configPath, D4DecryptBuffer, False)
                            TxtMain.Text = "[ 提示 ] 文件已保存！" & vbCrLf & configPath
                            RadD4.Checked = True
                        Else
                            TxtMain.Text = "[ 提示 ] 文件打开失败！"
                        End If
                    Else
                        TxtMain.Text = "[ 提示 ] KSF 打开失败！"
                    End If
                    HW_D4Reset()
                    Exit Sub
                ' 03 容器 = $4 容器：03 | CRC32 | D4 块（20 22 07 09 + Domain/Key + IV/Tag/Salt + 密文）。
                ElseIf InputBuffer(0) = &H3 AndAlso InputBuffer(1) = &H0 AndAlso InputBuffer(2) = &H0 AndAlso InputBuffer(3) = &H0 Then
                    Dim HeaderLength As Integer = 8
                    Dim buf3 As Byte() = New Byte(3) {}
                    Array.Copy(InputBuffer, 4, buf3, 0, 4)
                    Dim PayloadCRC As String = BytesToHex(buf3)
                    Dim Payload As Byte() = New Byte(InputBuffer.Length - HeaderLength - 1) {}
                    Array.Copy(InputBuffer, HeaderLength, Payload, 0, InputBuffer.Length - HeaderLength)
                    If Not BytesToHex(HW_CTREE_CRC32(Payload)) = PayloadCRC Then
                        TxtMain.Text = "[ 提示 ] XML配置文件CRC检查失败！"
                        Exit Sub
                    End If
                    ' 原版守门（原 MainUI.vb:768）+ 空结果判定：解不出来就停在「文件打开失败」，
                    ' 不要把一个空字符串塞进文本框，让人以为文件是空的。
                    If HW_D4SetPassword(Path.GetDirectoryName(configPath)) Then
                        Dim D4DecryptString As String = HW_D4DecryptXML(InputBuffer)
                        If D4DecryptString.Length > 0 Then
                            TxtMain.Text = D4DecryptString
                            RadD4.Checked = True
                        Else
                            TxtMain.Text = "[ 提示 ] 文件打开失败！"
                        End If
                    Else
                        TxtMain.Text = "[ 提示 ] KSF 打开失败！"
                    End If
                    HW_D4Reset()
                    Exit Sub
                ' 01 容器 = $2 静态容器（钥匙固定为 HW_CTREE_Key，不随文件走）；
                ' 02 容器 = $2 动态容器（头部带 $2 令牌，钥匙随文件走）。
                ElseIf (InputBuffer(0) = &H1 OrElse InputBuffer(0) = &H2) AndAlso InputBuffer(1) = &H0 AndAlso InputBuffer(2) = &H0 AndAlso InputBuffer(3) = &H0 Then
                    Dim buf4 As Byte() = New Byte(3) {}
                    Array.Copy(InputBuffer, 4, buf4, 0, 4)
                    Dim PayloadCRC As String = BytesToHex(buf4)

                    Dim HeaderLength As Integer = 8
                    Dim DecryptKey As String = HW_CTREE_Key
                    If InputBuffer(0) = &H2 Then
                        Array.Copy(InputBuffer, 8, buf4, 0, 4)
                        Dim DecryptKeyLength As Integer = BytesToInt32(buf4)
                        Dim DecryptKeyBuffer As Byte() = New Byte(DecryptKeyLength - 1) {}
                        Array.Copy(InputBuffer, 12, DecryptKeyBuffer, 0, DecryptKeyLength)
                        DecryptKey = HW_D2Decrypt(Encoding.UTF8.GetString(DecryptKeyBuffer))
                        HeaderLength = 12 + DecryptKeyLength
                    End If

                    Dim Payload As Byte() = New Byte(InputBuffer.Length - HeaderLength - 1) {}
                    Array.Copy(InputBuffer, HeaderLength, Payload, 0, InputBuffer.Length - HeaderLength)
                    If Not BytesToHex(HW_CTREE_CRC32(Payload)) = PayloadCRC Then
                        TxtMain.Text = "[ 提示 ] XML配置文件CRC检查失败！"
                        Exit Sub
                    End If

                    Dim DecryptBuffer As Byte() = AesCrypt2(Payload, 1, Encoding.UTF8.GetBytes(DecryptKey))
                    ' $2 系容器（01 静态 / 02 动态）都归到 $2 模式；压缩与否看正文开头有没有 1F 8B
                    Dim IsGzip As Boolean = DecryptBuffer.Length > 1 AndAlso DecryptBuffer(0) = &H1F AndAlso DecryptBuffer(1) = &H8B
                    RadD2.Checked = True
                    ChkZip.Checked = IsGzip
                    ' 首字节 02 = 动态容器（钥匙随文件），01 = 静态（钥匙固定）
                    ChkDynKey.Checked = (InputBuffer(0) = &H2)
                    If IsGzip Then
                        Dim UnzipBuffer As Byte() = UnGZip(DecryptBuffer)
                        TxtMain.Text = Encoding.UTF8.GetString(UnzipBuffer)
                    ElseIf DecryptBuffer.Length > 0 Then
                        TxtMain.Text = Encoding.UTF8.GetString(DecryptBuffer)
                    Else
                        TxtMain.Text = "[ 提示 ] 文件打开失败！"
                    End If
                    Exit Sub
                End If

                If BoardInfoVersion = 5 OrElse BoardInfoVersion = 6 Then
                    Dim DataBuffer As Byte() = New Byte(InputBuffer.Length - 21) {}
                    Array.Copy(InputBuffer, 20, DataBuffer, 0, InputBuffer.Length - 20)
                    InputBuffer = DataBuffer
                End If
                TxtMain.Text = Encoding.UTF8.GetString(InputBuffer)
                If BoardInfoVersion = 5 OrElse BoardInfoVersion = 6 Then
                    ' 板信息的正文本身就是 $2/$4 令牌，赋值时会触发 TxtMain_TextChanged 的自动跳转；
                    ' 这里把模式重新钉回 Vx，否则会跳到 $2/$4 模式、保存时丢掉 20 字节头。
                    RadVx.Checked = True
                    SyncModeCombo()
                    ParseText(Path.GetDirectoryName(configPath))
                End If
            End If
        Catch ex As Exception
            TxtMain.Text = "[ 提示 ] 文件打开失败！"
            HW_D4Reset()
        End Try
    End Sub

    Private Function ParseText(Optional folder As String = "") As Boolean
        Try
            If TxtMain.Text.StartsWith("$1") And TxtMain.Text.EndsWith("$") Then
                TxtMain.Text = HW_D1Decrypt(TxtMain.Text)
            ElseIf TxtMain.Text.StartsWith("$2") And TxtMain.Text.EndsWith("$") Then
                TxtMain.Text = HW_D2Decrypt(TxtMain.Text)
            ElseIf TxtMain.Text.StartsWith("$3") And TxtMain.Text.EndsWith("$") Then
                Dim s1 As String = TxtMain.Text
                s1 = s1.Substring(2, s1.Length - 3).Replace("&quot;", """").Replace("&amp;", "&").Replace("&apos;", "'").Replace("&lt;", "<").Replace("&gt;", ">")
                Dim n1 As Integer = CInt(Math.Floor(s1.Length / 20.0))
                Dim lst1 As New List(Of String)
                For item1 = 0 To n1 - 1
                    lst1.Add(s1.Substring(20 * item1, 20))
                Next
                Dim buf1 As Byte() = New Byte(lst1.Count * 16 - 1) {}
                For n2 As Integer = 0 To lst1.Count - 1
                    Array.Copy(HW_StrToBytes(lst1(n2)), 0, buf1, n2 * 16, 16)
                Next
                Dim D3Magic As Integer = BitConverter.ToInt32(buf1, 52)
                If Not D3Magic = 20220323 Then
                    HW_D4Reset()
                    Return False
                End If
                Dim D3Checksum As Boolean = buf1(14) <> 0
                Dim D3IV As Byte() = New Byte(15) {}
                Array.Copy(buf1, 16, D3IV, 0, D3IV.Length)
                Dim D3Salt As Byte() = New Byte(15) {}
                Array.Copy(buf1, 32, D3Salt, 0, D3Salt.Length)
                Dim DomainID As Integer = buf1(1) * 256 + buf1(0)
                Dim KeyID As Integer = buf1(3) * 256 + buf1(2)
                Dim D3Iterations As Integer = BitConverter.ToInt32(buf1, 48)

                ' 只定位并加载密钥库文件（不弹窗；原实现此处会先弹 Dollar4 窗口）
                D3LoadKeyStore(If(folder.Length > 1, folder, D4KeyStoreLast))
                Dim D3Password As Byte() = KMCv2_GetMasterKey(D4KeyStoreFile, DomainID, KeyID)

                Dim D3Plain As String = Nothing
                If D3Password.Length > 0 Then
                    ' ① 密钥库里有该 (Domain, Key) → 用它（与上游一致）
                    D3Plain = D3DecryptWith(D3Password, buf1, lst1.Count, D3IV, D3Salt, D3Iterations, D3Checksum)
                Else
                    ' ② 密钥库取不到 → 先试内置主密钥；**解得像正常文本才采用**，
                    '    解得乱码则不吞掉，继续走上游的「弹窗手输一机一密」。
                    Dim BuiltinPlain As String = D3DecryptWith(D3_Builtin_MasterKey, buf1, lst1.Count, D3IV, D3Salt, D3Iterations, D3Checksum)
                    If D3LooksLikeText(BuiltinPlain) Then
                        D3Plain = BuiltinPlain
                    Else
                        ' ③ 回退到上游行为：弹 Dollar4 窗口手输 HEX
                        D4Password = New Byte() {}
                        If Not HW_D4SetPassword() Then
                            HW_D4Reset()
                            Return False
                        End If
                        D3Plain = D3DecryptWith(D4Password, buf1, lst1.Count, D3IV, D3Salt, D3Iterations, D3Checksum)
                    End If
                End If

                If D3Plain Is Nothing Then
                    HW_D4Reset()
                    Return False
                End If
                TxtMain.Text = D3Plain
                ' 解出来之后停在 $3 模式，让底部能看出「这段原本是 $3 字段」。
                ' $3 只支持解密，所以这个模式只作标识，保存时会拦住。
                If Not RadVx.Checked Then RadD3.Checked = True
                HW_D4Reset()
            ElseIf TxtMain.Text.StartsWith("$4") And TxtMain.Text.EndsWith("$") Then
                If HW_D4SetPassword(If(folder.Length > 1, folder, D4KeyStoreLast)) Then
                    Dim D4Result As String = HW_D4DecryptString(TxtMain.Text)
                    If D4Result.Length > 0 Then
                        TxtMain.Text = D4Result
                        ' Vx 模式是板信息 V5/V6：负载可能是 $4 包裹的，但文件头必须继续按 V5/V6 写，
                        ' 所以在 Vx 下不要把模式单选切到 $4，否则保存会丢掉 20 字节头。
                        If Not RadVx.Checked Then RadD4.Checked = True
                        HW_D4Reset()
                        Return True
                    End If
                End If
                HW_D4Reset()
                Return False
            Else
                If TxtMain.Text.Length > 0 Then
                    If RadD4.Checked Then
                        Dim D4Result As String = HW_D4EncryptString(TxtMain.Text)
                        If D4Result.Length > 3 Then
                            TxtMain.Text = D4Result
                            Return True
                        End If
                        Return False
                    ElseIf RadVx.Checked AndAlso (BoardInfoVersion = 5 OrElse BoardInfoVersion = 6) Then
                        ' V5/V6 板信息的负载在真机上就是 $2 令牌（$2/hw_boardinfo 解开就是这种形态），
                        ' 原版 ParseText 对这两个版本固定走 HW_D2Encrypt，且第二参数省略 = 不转义
                        ' （obj.value 里的引号保持原样）。重构时漏了这一段，存出去是明文负载，
                        ' 原版打开只会看到一整串 $2…$ 或者干脆解析失败。
                        TxtMain.Text = HW_D2Encrypt(TxtMain.Text)
                    ElseIf RadD1.Checked Then
                        TxtMain.Text = HW_D1Encrypt(TxtMain.Text, True)
                    ElseIf RadD2.Checked Then
                        TxtMain.Text = HW_D2Encrypt(TxtMain.Text, True)
                    End If
                    ' Vx 模式不再对字段做 $1/$2/$4 加密：它只管 hw_boardinfo
                End If
            End If
            Return True
        Catch ex As Exception
            HW_D4Reset()
            Return False
        End Try
    End Function

    Private Sub RenameConfig(configPath As String)
        Dim n1 As Integer = 0
        Dim s1 As String = configPath
        While My.Computer.FileSystem.FileExists(s1)
            n1 += 1
            s1 = Path.GetDirectoryName(configPath) & "\" & Path.GetFileNameWithoutExtension(configPath) & "_" & n1.ToString().PadLeft(4, "0"c) & Path.GetExtension(configPath)
        End While
        My.Computer.FileSystem.MoveFile(configPath, s1)
    End Sub

End Class
