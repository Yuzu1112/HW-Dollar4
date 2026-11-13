Imports Microsoft.Win32
Imports System.IO
Imports System.IO.Compression
Imports System.Media
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' MainUI 的加密算法与字节原语部分。
''' 由原 MainUI.vb 按成员边界拆出（Partial Class），方法体逐行未改。
''' </summary>
Partial Public Class MainUI

    ''' <summary>
    ''' aescrypt2 加解密。mode 为 0 表示加密、1 表示解密，keyBytes 为用户密钥（原样使用，不做 hex 解码）。
    ''' 原实现通过外置 aescrypt2.exe 进程完成（需写 3 个临时文件、界面阻塞最长 10 秒），
    ''' 现改为纯托管 HwAescrypt2；签名与返回值语义保持不变：失败返回空数组。
    ''' </summary>
    Public Function AesCrypt2(data As Byte(), mode As Integer, keyBytes As Byte()) As Byte()
        If mode = 1 Then
            Dim plain As Byte() = HwAescrypt2.TryDecrypt(data, keyBytes)
            Return If(plain Is Nothing, New Byte() {}, plain)
        End If
        Return HwAescrypt2.Encrypt(data, keyBytes)
    End Function

    Public Shared Function BinToBytes(binaryText As String) As Byte()
        Dim n1 As Integer = CInt(binaryText.Length / 8.0)
        Dim buf1 As Byte() = New Byte(n1 - 1) {}
        For n2 As Integer = 0 To n1 - 1
            buf1(n2) = Convert.ToByte(binaryText.Substring(8 * n2, 8), 2)
        Next
        Return buf1
    End Function

    Public Shared Function BytesToBin(data() As Byte) As String
        Dim sb1 As New StringBuilder
        For Each item1 In data
            sb1.Append(Convert.ToString(item1, 2).PadLeft(8, "0"c))
        Next
        Return sb1.ToString
    End Function

    Public Shared Function BytesToHex(data As Byte()) As String
        Return BitConverter.ToString(data).Replace("-", "").ToUpper
    End Function

    Public Shared Function BytesToInt32(littleEndian As Byte()) As Integer
        Return Int(littleEndian(0)) + Int(littleEndian(1)) * 256 + Int(littleEndian(2)) * 65536 + Int(littleEndian(3)) * 16777216
    End Function

    Public Shared Function CRC32(data As Byte()) As UInteger
        Dim uarr1 As UInteger() = New UInteger(255) {}
        Dim u1 As UInteger = &HEDB88320UI
        For n1 As Integer = 0 To uarr1.Length - 1
            Dim u2 As UInteger = CUInt(n1)
            For n2 As Integer = 8 To 1 Step -1
                If (u2 And 1) = 1 Then
                    u2 = (u2 >> 1) Xor u1
                Else
                    u2 >>= 1
                End If
            Next
            uarr1(n1) = u2
        Next
        Dim u3 As UInteger = &HFFFFFFFFUI
        For n3 As Integer = 0 To data.Length - 1
            Dim b1 As Byte = CByte((u3 And &HFFUI) Xor CUInt(data(n3)))
            u3 = (u3 >> 8) Xor uarr1(b1)
        Next
        Return Not u3
    End Function

    ''' <summary>
    ''' 用给定主密钥解 $3 密文，成功返回明文（尾部零填充保留），失败返回 Nothing。
    ''' blockCount 为 $3 的总块数（含 4 块固定头）。
    ''' </summary>
    Private Function D3DecryptWith(master As Byte(), data As Byte(), blockCount As Integer, iv As Byte(), salt As Byte(), iterations As Integer, checksum As Boolean) As String
        Try
            If master Is Nothing OrElse master.Length = 0 Then Return Nothing
            Dim CipherBlocks As Integer = blockCount - 4 - If(checksum, 2, 0)
            If CipherBlocks <= 0 Then Return Nothing
            Dim D3Key As Byte() = PKCS5_PBKDF2_HMAC(master, salt, iterations, 32)
            If D3Key.Length = 0 Then Return Nothing
            Dim Cipher As Byte() = New Byte(CipherBlocks * 16 - 1) {}
            Array.Copy(data, 64, Cipher, 0, CipherBlocks * 16)
            Return Encoding.UTF8.GetString(DecryptAES(Cipher, D3Key, iv))
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' 仅定位并加载密钥库文件（<b>不弹任何窗口</b>）。查找顺序与原 HW_D4SetPassword 一致：
    ''' 指定/上次目录 → 程序目录，各自先 kmc_store_A 后 kmc_store_B。
    ''' </summary>
    Private Sub D3LoadKeyStore(Optional folder As String = "")
        Try
            If folder.Length > 1 AndAlso My.Computer.FileSystem.DirectoryExists(folder) Then D4KeyStoreLast = folder
            D4KeyStoreFile = New Byte() {}
            Dim BaseDir As String = If(D4KeyStoreLast.Length > 1, D4KeyStoreLast, Application.StartupPath)
            Dim Candidates As String() = {
                BaseDir & "\kmc_store_A",
                BaseDir & "\kmc_store_B",
                Application.StartupPath & "\kmc_store_A",
                Application.StartupPath & "\kmc_store_B"
            }
            For Each Candidate As String In Candidates
                If My.Computer.FileSystem.FileExists(Candidate) Then
                    D4KeyStoreFile = My.Computer.FileSystem.ReadAllBytes(Candidate)
                    Return
                End If
            Next
        Catch ex As Exception
            D4KeyStoreFile = New Byte() {}
        End Try
    End Sub

    ''' <summary>
    ''' 判断 $3 解密结果是否像正常文本，用于「内置主密钥是否真的适用」的判定。
    ''' 错误密钥解出的是随机字节，会出现替换字符（U+FFFD）或控制字符；据此拒绝，
    ''' 避免用错密钥产出静默乱码（尾部零填充放行）。
    ''' </summary>
    Private Function D3LooksLikeText(plain As String) As Boolean
        If plain Is Nothing OrElse plain.Length = 0 Then Return False
        Dim HasPrintable As Boolean = False
        For Each ch As Char In plain
            If ch = ChrW(0) Then Continue For
            If AscW(ch) = &HFFFD Then Return False
            If ch < ChrW(32) AndAlso ch <> vbTab AndAlso ch <> vbCr AndAlso ch <> vbLf Then Return False
            HasPrintable = True
        Next
        Return HasPrintable
    End Function

    Private Function DecryptAES(Input As Byte(), Key As Byte(), IV As Byte()) As Byte()
        Dim Decryptor As System.Security.Cryptography.Aes = System.Security.Cryptography.Aes.Create("AES")
        Decryptor.BlockSize = 128
        Decryptor.KeySize = 256
        Decryptor.Key = Key
        Decryptor.IV = IV
        Decryptor.Mode = CipherMode.CBC
        Decryptor.Padding = PaddingMode.Zeros
        Return Decryptor.CreateDecryptor().TransformFinalBlock(Input, 0, Input.Length)
    End Function

    Private Function EncryptAES(Input As Byte(), Key As Byte(), IV As Byte()) As Byte()
        Dim Encryptor As System.Security.Cryptography.Aes = System.Security.Cryptography.Aes.Create("AES")
        Encryptor.BlockSize = 128
        Encryptor.KeySize = 256
        Encryptor.Key = Key
        Encryptor.IV = IV
        Encryptor.Mode = CipherMode.CBC
        Encryptor.Padding = PaddingMode.Zeros
        Return Encryptor.CreateEncryptor().TransformFinalBlock(Input, 0, Input.Length)
    End Function

    Public Function GZip(data As Byte()) As Byte()
        Dim buf1 As Byte() = data
        Dim ms1 As New MemoryStream()
        Using item1 As New Compression.GZipStream(ms1, CompressionMode.Compress, True)
            item1.Write(buf1, 0, buf1.Length)
        End Using
        ms1.Position = 0
        Dim buf2 As Byte() = New Byte(CInt(ms1.Length) - 1) {}
        ms1.Read(buf2, 0, buf2.Length)
        Return buf2
    End Function

    Private Shared Function GetBigEndian32(buffer As Byte(), offset As Integer) As UInteger
        Dim b1(3) As Byte
        Array.Copy(buffer, offset, b1, 0, 4)
        If BitConverter.IsLittleEndian Then Array.Reverse(b1)
        Return BitConverter.ToUInt32(b1, 0)
    End Function

    Private Shared Function GetLittleEndian32(buffer As Byte(), offset As Integer) As UInteger
        Dim b1(3) As Byte
        Array.Copy(buffer, offset, b1, 0, 4)
        If Not BitConverter.IsLittleEndian Then Array.Reverse(b1)
        Return BitConverter.ToUInt32(b1, 0)
    End Function

    Public Shared Function HW_BytesToStr(bytes As Byte()) As String
        Dim buf1 As Byte() = New Byte(19) {}
        For item1 = 0 To 3
            Dim l1 As Long = BitConverter.ToUInt32(New Byte() {bytes(item1 * 4), bytes(item1 * 4 + 1), bytes(item1 * 4 + 2), bytes(item1 * 4 + 3)}, 0)
            Dim l2 As Long = 74805201
            For item3 = 4 To 0 Step -1
                buf1(item1 * 5 + item3) = CByte(Math.Floor(l1 / CDbl(l2)))
                l1 = l1 Mod l2
                l2 \= 93
            Next
        Next
        For item2 = 0 To 19
            If buf1(item2) = 30 Then
                buf1(item2) = 126
            Else
                buf1(item2) = CByte(buf1(item2) + 33)
            End If
        Next
        Return Encoding.UTF8.GetString(buf1)
    End Function

    Public Shared Function HW_CTREE_CRC(data As Byte(), crc As UInteger) As UInteger
        Dim u1 As UInteger = crc
        For n1 As Integer = 0 To data.Length - 1
            u1 = (CUInt(data(n1)) Or (u1 << 8)) Xor HW_CTREE_CRC_Table(CInt(u1 >> 24))
        Next
        For n2 As Integer = 4 To 1 Step -1
            u1 = HW_CTREE_CRC_Table(CInt(BitConverter.GetBytes(u1)(3))) Xor (u1 << 8)
        Next
        Return u1
    End Function

    Public Shared Function HW_CTREE_CRC32(data As Byte()) As Byte()
        Dim u1 As UInteger = &H0UI
        Dim n1 As Integer = 0
        While data.Length - n1 > 1024
            Dim buf1 As Byte() = New Byte(1024 - 1) {}
            Array.Copy(data, n1, buf1, 0, 1024)
            u1 = HW_CTREE_CRC(buf1, u1)
            n1 += 1024
        End While
        Dim buf2 As Byte() = New Byte(data.Length - n1 - 1) {}
        Array.Copy(data, n1, buf2, 0, data.Length - n1)
        u1 = HW_CTREE_CRC(buf2, u1)
        Return BitConverter.GetBytes(u1)
    End Function

    Public Function HW_D1Decrypt(token As String) As String
        token = token.Substring(2, token.Length - 3).Replace("&quot;", """").Replace("&amp;", "&").Replace("&apos;", "'").Replace("&lt;", "<").Replace("&gt;", ">")
        Dim n1 As Integer = CInt(Math.Floor(token.Length / 24.0))
        Dim lst1 As New List(Of String)
        For item1 = 0 To n1 - 1
            lst1.Add(token.Substring(24 * item1, 24))
        Next
        Dim bytes1 As New List(Of Byte)()
        Dim Decryptor As System.Security.Cryptography.Aes = System.Security.Cryptography.Aes.Create("AES")
        Decryptor.BlockSize = 128
        Decryptor.KeySize = 256
        Decryptor.Mode = CipherMode.ECB
        Decryptor.Padding = PaddingMode.None
        For item2 = 0 To lst1.Count - 1
            Decryptor.Key = HW_D1GetAESKey(lst1(item2))
            bytes1.AddRange(Decryptor.CreateDecryptor().TransformFinalBlock(HW_StrToBytes(lst1(item2)), 0, 16))
        Next
        Return Encoding.UTF8.GetString(bytes1.ToArray())
    End Function

    Public Function HW_D1Encrypt(text As String, Optional escape As Boolean = False) As String
        Dim buf1 As Byte() = Encoding.UTF8.GetBytes(text)
        Dim n1 As Integer = CInt(Math.Ceiling(buf1.Length / 16.0))
        If n1 = 0 Then n1 = 1
        Dim rnd1 As New Random
        Dim buf2 As Byte() = New Byte(15) {}
        Dim s1 As String = ""
        Dim Encryptor As System.Security.Cryptography.Aes = System.Security.Cryptography.Aes.Create("AES")
        Encryptor.BlockSize = 128
        Encryptor.KeySize = 256
        Encryptor.Mode = CipherMode.ECB
        Encryptor.Padding = PaddingMode.None
        For n2 As Integer = 0 To n1 - 1
            Dim b1(15) As Byte
            Dim n3 As Integer = buf1.Length - (16 * n2)
            Dim n4 As Integer = If(n3 > 16, 16, n3)
            If n4 > 0 Then Array.Copy(buf1, 16 * n2, b1, 0, n4)
            rnd1.NextBytes(buf2)
            Dim s2 As String = HW_BytesToStr(buf2)
            Encryptor.Key = HW_D1GetAESKey(s2)
            s1 += HW_BytesToStr(Encryptor.CreateEncryptor().TransformFinalBlock(b1, 0, 16)) + s2.Substring(s2.Length - 4, 4)
        Next
        s1 = "$1" & s1 & "$"
        If escape Then
            s1 = s1.Replace("&", "&amp;").Replace("""", "&quot;").Replace("'", "&apos;").Replace("<", "&lt;").Replace(">", "&gt;")
        End If
        Return s1
    End Function

    Public Function HW_D1GetAESKey(token As String) As Byte()
        Dim buf1 As Byte() = HW_D1_Key
        Dim buf2 As Byte() = Encoding.UTF8.GetBytes(token)
        For item1 = buf2.Length - 4 To buf2.Length - 1
            If buf2(item1) = 126 Then
                buf2(item1) = 30
            Else
                buf2(item1) = CByte(buf2(item1) - 33)
            End If
        Next
        buf1(11) = buf2(buf2.Length - 4)
        buf1(17) = buf2(buf2.Length - 3)
        buf1(23) = buf2(buf2.Length - 2)
        buf1(29) = buf2(buf2.Length - 1)
        Return buf1
    End Function

    Public Function HW_D2Decrypt(token As String) As String
        token = token.Substring(2, token.Length - 3).Replace("&quot;", """").Replace("&amp;", "&").Replace("&apos;", "'").Replace("&lt;", "<").Replace("&gt;", ">")
        Dim n1 As Integer = CInt(Math.Floor(token.Length / 20.0))
        Dim lst1 As New List(Of String)
        For item1 = 0 To n1 - 1
            lst1.Add(token.Substring(20 * item1, 20))
        Next
        Dim buf1 As Byte() = New Byte((lst1.Count - 1) * 16 - 1) {}
        For item2 = 0 To lst1.Count - 2
            Array.Copy(HW_StrToBytes(lst1(item2)), 0, buf1, item2 * 16, 16)
        Next
        Return Encoding.UTF8.GetString(DecryptAES(buf1, HW_D2GetAESCBCKey(), HW_StrToBytes(lst1(lst1.Count - 1))))
    End Function

    ''' <summary>
    ''' 02 容器（$2 动态钥匙）。与 01 的区别只有「钥匙从哪来」：
    ''' 每份文件现场生成一把随机 32 字符钥匙（A-Z0-9，同 HW-Patrina 的 GetRndString(32)），
    ''' 正文 gzip 后用这把钥匙做 aescrypt2，钥匙本身再用 $2 加密成令牌塞进文件头。
    ''' 布局：02 | CRC32(密文) LE | 令牌长度 LE | 令牌 | 密文 —— 与解包端逐字节对应。
    ''' </summary>
    Public Function HW_D2DynamicXML(text As String) As Byte()
        Dim chars As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
        Dim rnd As New Random
        Dim key As New System.Text.StringBuilder
        For i As Integer = 1 To 32
            key.Append(chars.Substring(rnd.Next(0, chars.Length), 1))
        Next

        Dim cipher As Byte() = AesCrypt2(GZip(Encoding.UTF8.GetBytes(text)), 0, Encoding.UTF8.GetBytes(key.ToString()))
        Dim token As Byte() = Encoding.UTF8.GetBytes(HW_D2Encrypt(key.ToString()))

        Dim r As Byte() = New Byte(cipher.Length + 12 + token.Length - 1) {}
        r(0) = &H2
        Array.Copy(HW_CTREE_CRC32(cipher), 0, r, 4, 4)
        Array.Copy(BitConverter.GetBytes(Convert.ToInt64(token.Length)), 0, r, 8, 4)
        Array.Copy(token, 0, r, 12, token.Length)
        Array.Copy(cipher, 0, r, 12 + token.Length, cipher.Length)
        Return r
    End Function

    Public Function HW_D2Encrypt(text As String, Optional escape As Boolean = False) As String
        Dim rnd1 As New Random
        Dim buf1 As Byte() = New Byte(15) {}
        rnd1.NextBytes(buf1)
        Dim buf2 As Byte() = EncryptAES(Encoding.UTF8.GetBytes(text), HW_D2GetAESCBCKey(), buf1)
        Dim n1 As Integer = CInt(Math.Floor(buf2.Length / 16.0))
        Dim s1 As String = ""
        For item1 = 0 To n1 - 1
            Dim buf3 As Byte() = New Byte(15) {}
            Array.Copy(buf2, item1 * 16, buf3, 0, 16)
            s1 += HW_BytesToStr(buf3)
        Next
        s1 = "$2" & s1 & HW_BytesToStr(buf1) & "$"
        If escape Then
            s1 = s1.Replace("&", "&amp;").Replace("""", "&quot;").Replace("'", "&apos;").Replace("<", "&lt;").Replace(">", "&gt;")
        End If
        Return s1
    End Function

    Public Function HW_D2EncryptXML(text As String) As Byte()
        Dim buf1 As Byte() = AesCrypt2(GZip(Encoding.UTF8.GetBytes(text)), 0, HW_D2_Key)
        Dim buf2 As Byte() = New Byte() {}
        Dim PayloadCRC As Byte() = HW_CTREE_CRC32(buf1)
        Dim D2EncryptKey As Byte() = Encoding.UTF8.GetBytes(HW_D2Encrypt(Encoding.UTF8.GetString(HW_D2_Key)))
        buf2 = New Byte(buf1.Length + 12 + D2EncryptKey.Length - 1) {}
        buf2(0) = &H2
        Array.Copy(BitConverter.GetBytes(Convert.ToInt64(D2EncryptKey.Length)), 0, buf2, 8, 4)
        Array.Copy(D2EncryptKey, 0, buf2, 12, D2EncryptKey.Length)
        Array.Copy(buf1, 0, buf2, 12 + D2EncryptKey.Length, buf1.Length)
        Array.Copy(PayloadCRC, 0, buf2, 4, PayloadCRC.Length)
        Return buf2
    End Function

    Public Function HW_D2GetAESCBCKey() As Byte()
        Dim s1 As String = D2KeyType
        If s1.Length = 0 Or s1.ToUpper() = "OS" Then
            s1 = "Df7!ui%s9(lmV1L8" 'SPEC_OS_AES_CBC_APP_STR from libhw_ssp_basic.so
        ElseIf s1.ToUpper() = "BOARDINFO" Then
            s1 = "BOARDINFO(lmV1L8" 'SPEC_BOARDINFO_AES_CBC_APP_STR from libsmp_api.so
        ElseIf s1.ToUpper() = "OMCI" Then
            s1 = "asdfghjkl!@#0320" 'SPEC_OMCI_AES_CBC_APP_STR from omci.elf
        End If
        Dim buf1 As Byte() = New Byte(HW_D2_Key.Length + s1.Length - 1) {}
        Dim buf2 As Byte() = Encoding.UTF8.GetBytes(s1)
        Array.Copy(HW_D2_Key, buf1, HW_D2_Key.Length)
        Array.Copy(buf2, 0, buf1, HW_D2_Key.Length, buf2.Length)
        Return New SHA256CryptoServiceProvider().ComputeHash(buf1)
    End Function

    ''' <summary>
    ''' 01 容器（$2 静态钥匙，Keystore D4K1）：8 字节头 + aescrypt2 正文。
    ''' compress=True 时正文先 gzip（压缩后加密容器），False 时原样（不压缩加密容器）。
    ''' 两者的区别只在正文开头有没有 1F 8B，解包侧靠这个魔数自己判断，不需要额外标记。
    ''' </summary>
    Public Function HW_D2StaticXML(text As String, compress As Boolean) As Byte()
        Dim raw As Byte() = Encoding.UTF8.GetBytes(text)
        Dim body As Byte() = If(compress, GZip(raw), raw)
        Dim cipher As Byte() = AesCrypt2(body, 0, Encoding.UTF8.GetBytes(HW_CTREE_Key))
        Dim r As Byte() = New Byte(cipher.Length + 8 - 1) {}
        r(0) = &H1
        Dim crc As Byte() = HW_CTREE_CRC32(cipher)
        Array.Copy(crc, 0, r, 4, 4)
        Array.Copy(cipher, 0, r, 8, cipher.Length)
        Return r
    End Function

    Public Function HW_D4Decrypt(block As Byte()) As Byte()
        Dim password As Byte() = D4Password
        Dim DomainID As Integer = block(8) * 256 + block(9)
        Dim KeyID As Integer = block(10) * 256 + block(11)
        ' 冷启动时 D4KeyStore 长度为 0。这里**只做只读判断，不改任何状态**：
        ' 密钥槽未初始化就按原版的硬编码 (1,2) 比较，这样 (1,2) 的块不会去查库、
        ' 也不会弹口令框，口令为空就直接返回空字节 → 与原版一致地「打开失败」。
        Dim SameAsKeyStore As Boolean
        If D4KeyStore Is Nothing OrElse D4KeyStore.Length < 4 Then
            SameAsKeyStore = (DomainID = 1 AndAlso KeyID = 2)
        Else
            SameAsKeyStore = (DomainID = D4KeyStore(0) * 256 + D4KeyStore(1)) AndAlso
                             (KeyID = D4KeyStore(2) * 256 + D4KeyStore(3))
        End If
        Dim KeyStoreIsZero As Boolean = D4KeyStore IsNot Nothing AndAlso D4KeyStore.Length >= 4 AndAlso
                                        GetBigEndian32(D4KeyStore, 0) = 0
        If Not SameAsKeyStore AndAlso Not KeyStoreIsZero Then
            password = KMCv2_GetMasterKey(D4KeyStoreFile, DomainID, KeyID)
            If password.Length = 0 Then
                D4Password = New Byte() {}
                HW_D4SetPassword()
                password = D4Password
            End If
        End If
        If password.Length = 0 Then Return New Byte() {}
        Dim D4IV As Byte() = New Byte(15) {}
        Dim D4Salt As Byte() = New Byte(15) {}
        ' 防御：槽位若被意外清空（长度为 0），下面这句 Array.Copy 会越界，整次解密被 Catch 吞掉。
        If D4KeyStore Is Nothing OrElse D4KeyStore.Length < 4 Then D4KeyStore = New Byte(3) {}
        Array.Copy(block, 8, D4KeyStore, 0, 4)
        Array.Copy(block, 12, D4IV, 0, 16)
        Dim tag As Byte() = New Byte(15) {}
        Array.Copy(block, 28, tag, 0, 16)
        Array.Copy(block, 44, D4Salt, 0, 16)
        Dim cipher_data As Byte() = New Byte(block.Length - 64 - 1) {}
        Array.Copy(block, 64, cipher_data, 0, cipher_data.Length)
        Dim decrypt_data As Byte() = HwGcm.DecryptPbkdf2(password, D4Salt, D4IV, cipher_data, tag)
        If decrypt_data IsNot Nothing Then
            Return decrypt_data
        Else
            Return New Byte() {}
        End If
    End Function

    Public Function HW_D4DecryptString(token As String) As String
        Return Encoding.UTF8.GetString(HW_D4Decrypt(Convert.FromBase64String(token.Substring(2, token.Length - 3))))
    End Function

    Public Function HW_D4DecryptXML(block As Byte()) As String
        Dim buf1 As Byte() = New Byte(block.Length - 8 - 1) {}
        Array.Copy(block, 8, buf1, 0, buf1.Length)
        Return Encoding.UTF8.GetString(UnGZip(HW_D4Decrypt(buf1)))
    End Function

    Public Function HW_D4Encrypt(block As Byte()) As Byte()
        ' 产出的就是 D4 块：20 22 07 09 | 4 字节保留 | Domain/Key | IV | Tag | Salt | 密文长度(BE) | 密文。
        ' 这块单独成文件就是「$4 整文件（20 22 07 09）」；前面套 03+CRC 就是「$4 容器」；
        ' base64 后再套 $4…$ 就是「$4 字段令牌」。三者载荷完全同构。
        '
        ' 加密侧的钥匙必须和头里标的 (Domain, Key) 对得上。D4KeyStore 会在**解密**时被改成
        ' 被解文件的 (Domain, Key)（HW_D4Decrypt 末尾那句 Array.Copy），而 D4Password 不会跟着
        ' 换成对应的主密钥 —— 于是「先解过一台 (10001,1) 的文件，再加密 $4」会写出
        ' 「标签 (10001,1) + 默认 $2 密钥」的错配产物，设备解不开。
        ' 这里统一归位到默认 (1,2) + 默认 $2 密钥，产出的就是「$4 静态」。
        ' 例外：0712 下载包的内层 03 要写 (0,0)，调用方会先置 _keepKeySlot 跳过归位。
        ' 注意这里不用 HW_D4Reset()：它连 D4KeyStoreFile（已缓存的 kmc_store 字节）一起清掉，
        ' 加密一次之后再去解 (Domain,Key) 的非默认文件就会又弹口令框。只归位这两项即可。
        If Not _keepKeySlot Then
            D4Password = HW_D2_Key
            D4KeyStore = New Byte(3) {&H0, &H1, &H0, &H2}
        End If

        Dim rnd1 As New Random
        Dim rnd2 As New Random
        Dim D4IV As Byte() = New Byte(15) {}
        rnd1.NextBytes(D4IV)
        Dim D4Salt As Byte() = New Byte(15) {}
        rnd2.NextBytes(D4Salt)
        Dim password As Byte() = D4Password
        Dim input_data As Byte() = New Byte(block.Length - 1) {}
        Array.Copy(block, input_data, block.Length)
        Dim tag As Byte() = Nothing
        Dim cipher_data As Byte() = HwGcm.EncryptPbkdf2(password, D4Salt, D4IV, input_data, tag)
        If cipher_data IsNot Nothing Then
            Dim D4Data As Byte() = New Byte(block.Length + 64 - 1) {}
            'Byte 1-8
            D4Data(0) = &H20
            D4Data(1) = &H22
            D4Data(2) = &H7
            D4Data(3) = &H9

            'Byte 9-60
            Array.Copy(D4KeyStore, 0, D4Data, 8, 4)
            Array.Copy(D4IV, 0, D4Data, 12, 16)
            Array.Copy(tag, 0, D4Data, 28, 16)
            Array.Copy(D4Salt, 0, D4Data, 44, 16)

            'Byte 61-64
            Dim D4DataSize As Byte() = BitConverter.GetBytes(cipher_data.Length)
            D4Data(60) = D4DataSize(3)
            D4Data(61) = D4DataSize(2)
            D4Data(62) = D4DataSize(1)
            D4Data(63) = D4DataSize(0)

            'Byte 65+
            Array.Copy(cipher_data, 0, D4Data, 64, cipher_data.Length)
            Return D4Data
        Else
            Return New Byte() {}
        End If
    End Function

    Public Function HW_D4EncryptString(text As String) As String
        Return "$4" & Convert.ToBase64String(HW_D4Encrypt(Encoding.UTF8.GetBytes(text))) & "$"
    End Function

    Public Function HW_D4EncryptXML(text As String) As Byte()
        Dim buf1 As Byte() = HW_D4Encrypt(GZip(Encoding.UTF8.GetBytes(text)))
        Dim buf2 As Byte() = New Byte(buf1.Length + 8 - 1) {}
        buf2(0) = &H3
        Dim buf3 As Byte() = HW_CTREE_CRC32(buf1)
        Array.Copy(buf3, 0, buf2, 4, buf3.Length)
        Array.Copy(buf1, 0, buf2, 8, buf1.Length)
        Return buf2
    End Function

    Public Sub HW_D4Reset()
        Try
            D4Password = HW_D2_Key
            D4KeyStore = New Byte(3) {&H0, &H1, &H0, &H2}
            D4KeyStoreFile = New Byte() {}
        Catch ex As Exception
            ' 规范要求：不静默吞异常，至少在调试器里留痕
            Debug.WriteLine("[忽略] " & ex.GetType().Name & ": " & ex.Message)
        End Try
    End Sub

    Public Function HW_D4SetPassword(Optional folder As String = "") As Boolean
        Try
            If folder.Length > 1 Then
                If My.Computer.FileSystem.DirectoryExists(folder) Then D4KeyStoreLast = folder
                Dim s1 As String = ""
                Dim s2 As String = folder & "\kmc_store_A"
                If Not My.Computer.FileSystem.FileExists(s2) Then
                    s2 = folder & "\kmc_store_B"
                    If Not My.Computer.FileSystem.FileExists(s2) Then
                        s2 = Application.StartupPath & "\kmc_store_A"
                        If Not My.Computer.FileSystem.FileExists(s2) Then
                            s2 = Application.StartupPath & "\kmc_store_B"
                            If Not My.Computer.FileSystem.FileExists(s2) Then
                                Return True
                            End If
                        End If
                    End If
                End If
                D4KeyStoreFile = My.Computer.FileSystem.ReadAllBytes(s2)
                Return True
            End If
            If D4Password.Length > 0 Then Return True
            Using dlgD4 As New Dollar4()
                dlgD4.ShowDialog()
            End Using
            If D4Password.Length > 0 Then Return True
            Return False
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Shared Function HW_StrToBytes(text As String) As Byte()
        Dim buf1 As Byte() = Encoding.UTF8.GetBytes(text)
        For item2 = 0 To 19
            If buf1(item2) = 126 Then
                buf1(item2) = 30
            Else
                buf1(item2) = CByte(buf1(item2) - 33)
            End If
        Next
        Dim buf2 As Byte() = New Byte(15) {}
        For item3 = 0 To 3
            Dim l1 As Long = 0
            Dim l2 As Long = 1
            For item1 = 0 To 4
                l1 += l2 * buf1(item3 * 5 + item1)
                l2 *= 93
            Next
            Dim buf3 As Byte() = BitConverter.GetBytes(l1)
            buf2(item3 * 4 + 0) = buf3(0)
            buf2(item3 * 4 + 1) = buf3(1)
            buf2(item3 * 4 + 2) = buf3(2)
            buf2(item3 * 4 + 3) = buf3(3)
        Next
        Return buf2
    End Function

    Public Shared Function HexToBytes(hex As String) As Byte()
        Return Enumerable.Range(0, hex.Length).Where(Function(x) x Mod 2 = 0).[Select](Function(x) Convert.ToByte(hex.Substring(x, 2), 16)).ToArray()
    End Function

    Public Function KMCv2_GetAllMasterKey(configPath As String) As String
        Dim s1 As String = "[ 提示 ] KSF 打开失败！"
        Try
            Dim KSFv2 As Byte() = My.Computer.FileSystem.ReadAllBytes(configPath)
            Dim s2 As String = "Domain" & vbTab & "| Key" & vbTab & "| Master Key (Hex)" & vbCrLf & "------------------------------------------------------------------------------------------------------------------------" & vbCrLf
            If KSFv2.Length < 32 Then Return s1
            For n1 As Integer = 0 To 33
                If KSFv2(n1) <> KSFv2_Magic(n1) Then Return s1
            Next
            Dim KSFv2_IterationCount As Integer = CInt(GetBigEndian32(KSFv2, 52))
            Dim KSFv2_RecordCount As Integer = CInt(GetBigEndian32(KSFv2, 184))
            Dim b1(31) As Byte
            Dim b2(31) As Byte
            Array.Copy(KSFv2, 56, b1, 0, 32)
            For n2 As Integer = 0 To 31
                b1(n2) = KSFv2(56 + n2) Xor KSFv2(88 + n2) Xor KSFv2_Mask(n2)
            Next
            Array.Copy(KSFv2, 152, b2, 0, 32)
            Dim KSFv2_RootMasterKey As Byte() = PKCS5_PBKDF2_HMAC(b1, b2, KSFv2_IterationCount, 32)
            For n3 As Integer = 0 To KSFv2_RecordCount - 1
                Dim n4 As Integer = (n3 + 1) * 256
                If n4 + 256 > KSFv2.Length Then Exit For
                Dim n5 As Integer = CInt(GetBigEndian32(KSFv2, n4 + 0))
                Dim n6 As Integer = CInt(GetBigEndian32(KSFv2, n4 + 4))
                Dim MasterKey_EncryptedLength As Integer = CInt(GetBigEndian32(KSFv2, n4 + 88))
                Dim MasterKey_Length As Integer = CInt(GetBigEndian32(KSFv2, n4 + 92))
                Dim MasterKey_EncryptionKey(31) As Byte
                Array.Copy(KSFv2_RootMasterKey, 0, MasterKey_EncryptionKey, 0, 32)
                Dim MasterKey_EncryptionIV(15) As Byte
                Array.Copy(KSFv2, n4 + 72, MasterKey_EncryptionIV, 0, 16)
                Dim MasterKey_Encrypted(MasterKey_EncryptedLength - 1) As Byte
                Array.Copy(KSFv2, n4 + 96, MasterKey_Encrypted, 0, MasterKey_EncryptedLength)
                Dim buf1 As Byte() = DecryptAES(MasterKey_Encrypted, MasterKey_EncryptionKey, MasterKey_EncryptionIV)
                Dim MasterKey(MasterKey_Length - 1) As Byte
                Array.Copy(buf1, 0, MasterKey, 0, MasterKey_Length)
                s2 &= n5.ToString() & vbTab & "| " & n6.ToString() & vbTab & "| " & BytesToHex(MasterKey) & vbCrLf
            Next
            Return s2
        Catch ex As Exception
            Return s1
        End Try
    End Function

    Public Function KMCv2_GetMasterKey(KSFv2 As Byte(), DomainID As Integer, KeyID As Integer) As Byte()
        Try
            If KSFv2.Length < 32 Then Return New Byte() {}
            For n1 As Integer = 0 To 33
                If KSFv2(n1) <> KSFv2_Magic(n1) Then Return New Byte() {}
            Next
            Dim KSFv2_IterationCount As Integer = CInt(GetBigEndian32(KSFv2, 52))
            Dim KSFv2_RecordCount As Integer = CInt(GetBigEndian32(KSFv2, 184))
            Dim b1(31) As Byte
            Dim b2(31) As Byte
            Array.Copy(KSFv2, 56, b1, 0, 32)
            For n2 As Integer = 0 To 31
                b1(n2) = KSFv2(56 + n2) Xor KSFv2(88 + n2) Xor KSFv2_Mask(n2)
            Next
            Array.Copy(KSFv2, 152, b2, 0, 32)
            Dim KSFv2_RootMasterKey As Byte() = PKCS5_PBKDF2_HMAC(b1, b2, KSFv2_IterationCount, 32)
            For n3 As Integer = 0 To KSFv2_RecordCount - 1
                Dim n4 As Integer = (n3 + 1) * 256
                If n4 + 256 > KSFv2.Length Then Exit For
                Dim n5 As Integer = CInt(GetBigEndian32(KSFv2, n4 + 0))
                Dim n6 As Integer = CInt(GetBigEndian32(KSFv2, n4 + 4))
                If DomainID = n5 AndAlso KeyID = n6 Then
                    Dim MasterKey_EncryptedLength As Integer = CInt(GetBigEndian32(KSFv2, n4 + 88))
                    Dim MasterKey_Length As Integer = CInt(GetBigEndian32(KSFv2, n4 + 92))
                    Dim MasterKey_EncryptionKey(31) As Byte
                    Array.Copy(KSFv2_RootMasterKey, 0, MasterKey_EncryptionKey, 0, 32)
                    Dim MasterKey_EncryptionIV(15) As Byte
                    Array.Copy(KSFv2, n4 + 72, MasterKey_EncryptionIV, 0, 16)
                    Dim MasterKey_Encrypted(MasterKey_EncryptedLength - 1) As Byte
                    Array.Copy(KSFv2, n4 + 96, MasterKey_Encrypted, 0, MasterKey_EncryptedLength)
                    Dim buf1 As Byte() = DecryptAES(MasterKey_Encrypted, MasterKey_EncryptionKey, MasterKey_EncryptionIV)
                    Dim MasterKey(MasterKey_Length - 1) As Byte
                    Array.Copy(buf1, 0, MasterKey, 0, MasterKey_Length)
                    Return MasterKey
                End If
            Next
            Return New Byte() {}
        Catch ex As Exception
            Return New Byte() {}
        End Try
    End Function

    Private Function PKCS5_PBKDF2_HMAC(password As Byte(), salt As Byte(), iteration_count As Integer, key_length As Integer) As Byte()
        Using item1 As New Rfc2898DeriveBytes(password, salt, iteration_count, HashAlgorithmName.SHA256)
            Return item1.GetBytes(key_length)
        End Using
    End Function

    Public Function UnGZip(data As Byte(), Optional allowTrailing As Boolean = True) As Byte()
        Dim buf1 As Byte() = data
        Using item1 As New MemoryStream()
            Dim n1 As Integer = 0
            If allowTrailing Then
                n1 = BitConverter.ToInt32(buf1, buf1.Length - 4)
                item1.Write(buf1, 0, buf1.Length - 4)
            Else
                n1 = BitConverter.ToInt32(buf1, 0)
                item1.Write(buf1, 4, buf1.Length - 4)
            End If
            Dim buf2 As Byte() = New Byte(n1 - 1) {}
            item1.Position = 0
            Using item2 As New Compression.GZipStream(item1, CompressionMode.Decompress)
                item2.Read(buf2, 0, buf2.Length)
            End Using
            Return buf2
        End Using
    End Function

End Class
