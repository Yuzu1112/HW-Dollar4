Imports Microsoft.Win32
Imports System.IO
Imports System.IO.Compression
Imports System.Media
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Text

Partial Public Class MainUI

    ' libmbedtls.dll 的 5 个导出函数已全部改为纯托管实现，不再引用该原生库：
    '   libmbedtls_gcm_auth_decrypt_pbkdf2  ->  HwGcm.DecryptPbkdf2
    '   libmbedtls_gcm_crypt_and_tag_pbkdf2 ->  HwGcm.EncryptPbkdf2
    '   libmbedtls_md_hmac_sha256           ->  HMACSHA256
    '   libmbedtls_test_add / test_subtract ->  从未被调用的联调测试桩，直接移除

    ' user32 的导出函数全部是 WINAPI 约定（x86 下为 __stdcall），不能声明成 Cdecl：
    ' 调用方与目标方对"谁负责清理栈参数"的约定不一致，VS 调试时会触发
    ' PInvokeStackImbalance 警告。默认的 Winapi 才是正确约定，这里显式写明。
    '
    ' 签名对照 WinUser.h：
    '   BOOL  AppendMenuW(HMENU hMenu, UINT uFlags, UINT_PTR uIDNewItem, LPCWSTR lpNewItem);
    '   HMENU GetSystemMenu(HWND hWnd, BOOL bRevert);
    ' uIDNewItem 是 UINT_PTR（指针宽度），x64 下不能用 Integer。

    <Flags()>
    Public Enum MenuFlags As Integer
        MF_BYPOSITION = 1024
        MF_REMOVE = 4096
        MF_SEPARATOR = 2048
        MF_STRING = 0
    End Enum

    Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
        If m.Msg = 274 Then
            If m.WParam.ToInt32 = &H1FFE Then
                MsgBox("此软件包含的开源软件由权利持有人发放许可证。" & vbCrLf & vbCrLf &
                       "软件： aescrypt2 1.0" & vbCrLf &
                       "版权声明： Copyright (C) 2004,2005  Christophe Devine" & vbCrLf &
                       "许可证： GPL v2 License" & vbCrLf &
                       "许可证文本： https://www.gnu.org/licenses/gpl-2.0.txt" & vbCrLf & vbCrLf &
                       "软件： Mbed TLS 3.6.0 LTS" & vbCrLf &
                       "版权声明： (C) Copyright The Mbed TLS Contributors" & vbCrLf &
                       "许可证： GPL v2 License" & vbCrLf &
                       "许可证文本： https://www.gnu.org/licenses/gpl-2.0.txt" & vbCrLf & vbCrLf &
                       "软件： HW-Patrina" & vbCrLf &
                       "项目地址： https://github.com/ipatrina/HW-Patrina" & vbCrLf &
                       "许可证： GPL-2.0 License" & vbCrLf &
                       "许可证文本： https://www.gnu.org/licenses/gpl-2.0.txt", vbInformation, "开源软件使用声明")
            End If

            If m.WParam.ToInt32 = &H1FFF Then
                Dim VersionStrings As String() = Application.ProductVersion.ToString.Split("."c)
                MsgBox("HW Dollar4" & vbCrLf & vbCrLf & "华为ONT配置文件实用工具" & vbCrLf & vbCrLf & "软件版本：" & VersionStrings(0) & "." & VersionStrings(1) & "." & VersionStrings(2) & vbCrLf & "更新时间：20" & VersionStrings(3).Substring(0, 2) & "年" & CInt(VersionStrings(3).Substring(2, 2)) & "月" & vbCrLf & vbCrLf & "Copyright © 2020-20" & VersionStrings(3).Substring(0, 2) & " 版权所有", vbInformation, "关于")
            End If
        End If
        MyBase.WndProc(m)
    End Sub

    Public Shared BoardInfoVersion As Integer = 3
    ''' <summary>$2 字段的算法上下文（OS / BOARDINFO / OMCI），来自模式下拉。</summary>
    Public Shared D2KeyType As String = "OS"
    ''' <summary>重建下拉项时压住 CmbMode.SelectedIndexChanged，避免自己触发自己。</summary>
    Private Shared _modeLock As Boolean = False
    ''' <summary>
    ''' 置 True 时 <see cref="HW_D4Encrypt"/> 不再把密钥槽归位到默认 (1,2)。
    ''' 目前只有 0712 下载包的内层 03 需要（它要写 (0,0) + 默认派生口令）。
    ''' </summary>
    Private Shared _keepKeySlot As Boolean = False
    Public Shared D4KeyStore As Byte() = New Byte() {}
    Public Shared D4KeyStoreFile As Byte() = New Byte() {}
    Public Shared D4KeyStoreLast As String = Application.StartupPath
    Public Shared D4Password As Byte() = New Byte() {}
    Public Shared D4PasswordLast As String = ""
    Public ReadOnly HW_CTREE_Key As String = "hex:13395537D2730554A176799F6D56A239" 'Keystore D4K1
    Public ReadOnly HW_D1_Key As Byte() = HexToBytes("B8363C9B77DAED4B9ABB9F2F6DF5F1D5CB64975D5D3BCEE8827F2F42235F9229") 'Keystore D1K1
    Public ReadOnly HW_D2_Key As Byte() = Encoding.UTF8.GetBytes("9jK0lk5kLmxn8sjojW962llHY76xAc2z") 'Keystore D1K2

    ''' <summary>
    ''' $3 内置主密钥（32 字节 ASCII 文本，作为 PBKDF2 口令原样使用，不做 hex 解码）。
    ''' 作用：允许**不导入 kmc_store 也能解 $3**——密钥库里取到该 (Domain, Key) 就用库里的，
    ''' 取不到才回退本密钥。来源为 HW FW Toolkit 的 <c>XmlKeys.D3BuiltinMasterKey</c>；
    ''' 上游 HW Dollar4 没有这一项（它取不到库就弹窗口手输），因此这是有意增加的能力。
    ''' </summary>
    Public ReadOnly D3_Builtin_MasterKey As Byte() = Encoding.UTF8.GetBytes("d19bb451dd33b3b3cf4b60525a39fc1f")
    Public Shared PolicyPassword As String = ""

    ' 查找/替换窗口常驻：只维护一个实例，再次 Ctrl+F 只负责激活。
    ' 窗口不阻塞主界面（Show 而非 ShowDialog），关掉后下次重建。
    Private Shared _searchForm As Search = Nothing

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = (Keys.Control Or Keys.F) Then
            ShowSearchWindow(False)
            Return True
        End If
        If keyData = (Keys.Control Or Keys.H) Then
            ShowSearchWindow(True)
            Return True
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    ''' <summary>
    ''' 当前状态有没有「能存出来的东西」。没有对应落盘形态的组合直接把「保存」置灰，
    ''' 免得点下去写出明文（$4 + 非 XML）或者半截东西（$3 只解不产、下载包必须是 XML）。
    ''' </summary>
    ''' <summary>当前状态存不出东西时的原因；空串表示可以保存。</summary>
    Private _saveBlockReason As String = ""

    Private Shared ReadOnly HW_CTREE_CRC_Table As UInteger() = {&H0UI, &H4C11DB7UI, &H9823B6EUI, &HD4326D9UI, &H130476DCUI, &H17C56B6BUI, &H1A864DB2UI, &H1E475005UI, &H2608EDB8UI, &H22C9F00FUI, &H2F8AD6D6UI, &H2B4BCB61UI, &H350C9B64UI, &H31CD86D3UI, &H3C8EA00AUI, &H384FBDBDUI, &H4C11DB70UI, &H48D0C6C7UI, &H4593E01EUI, &H4152FDA9UI, &H5F15ADACUI, &H5BD4B01BUI, &H569796C2UI, &H52568B75UI, &H6A1936C8UI, &H6ED82B7FUI, &H639B0DA6UI, &H675A1011UI, &H791D4014UI, &H7DDC5DA3UI, &H709F7B7AUI, &H745E66CDUI, &H9823B6E0UI, &H9CE2AB57UI, &H91A18D8EUI, &H95609039UI, &H8B27C03CUI, &H8FE6DD8BUI, &H82A5FB52UI, &H8664E6E5UI, &HBE2B5B58UI, &HBAEA46EFUI, &HB7A96036UI, &HB3687D81UI, &HAD2F2D84UI, &HA9EE3033UI, &HA4AD16EAUI, &HA06C0B5DUI, &HD4326D90UI, &HD0F37027UI, &HDDB056FEUI, &HD9714B49UI, &HC7361B4CUI, &HC3F706FBUI, &HCEB42022UI, &HCA753D95UI, &HF23A8028UI, &HF6FB9D9FUI, &HFBB8BB46UI, &HFF79A6F1UI, &HE13EF6F4UI, &HE5FFEB43UI, &HE8BCCD9AUI, &HEC7DD02DUI, &H34867077UI, &H30476DC0UI, &H3D044B19UI, &H39C556AEUI, &H278206ABUI, &H23431B1CUI, &H2E003DC5UI, &H2AC12072UI, &H128E9DCFUI, &H164F8078UI, &H1B0CA6A1UI, &H1FCDBB16UI, &H18AEB13UI, &H54BF6A4UI, &H808D07DUI, &HCC9CDCAUI, &H7897AB07UI, &H7C56B6B0UI, &H71159069UI, &H75D48DDEUI, &H6B93DDDBUI, &H6F52C06CUI, &H6211E6B5UI, &H66D0FB02UI, &H5E9F46BFUI, &H5A5E5B08UI, &H571D7DD1UI, &H53DC6066UI, &H4D9B3063UI, &H495A2DD4UI, &H44190B0DUI, &H40D816BAUI, &HACA5C697UI, &HA864DB20UI, &HA527FDF9UI, &HA1E6E04EUI, &HBFA1B04BUI, &HBB60ADFCUI, &HB6238B25UI, &HB2E29692UI, &H8AAD2B2FUI, &H8E6C3698UI, &H832F1041UI, &H87EE0DF6UI, &H99A95DF3UI, &H9D684044UI, &H902B669DUI, &H94EA7B2AUI, &HE0B41DE7UI, &HE4750050UI, &HE9362689UI, &HEDF73B3EUI, &HF3B06B3BUI, &HF771768CUI, &HFA325055UI, &HFEF34DE2UI, &HC6BCF05FUI, &HC27DEDE8UI, &HCF3ECB31UI, &HCBFFD686UI, &HD5B88683UI, &HD1799B34UI, &HDC3ABDEDUI, &HD8FBA05AUI, &H690CE0EEUI, &H6DCDFD59UI, &H608EDB80UI, &H644FC637UI, &H7A089632UI, &H7EC98B85UI, &H738AAD5CUI, &H774BB0EBUI, &H4F040D56UI, &H4BC510E1UI, &H46863638UI, &H42472B8FUI, &H5C007B8AUI, &H58C1663DUI, &H558240E4UI, &H51435D53UI, &H251D3B9EUI, &H21DC2629UI, &H2C9F00F0UI, &H285E1D47UI, &H36194D42UI, &H32D850F5UI, &H3F9B762CUI, &H3B5A6B9BUI, &H315D626UI, &H7D4CB91UI, &HA97ED48UI, &HE56F0FFUI, &H1011A0FAUI, &H14D0BD4DUI, &H19939B94UI, &H1D528623UI, &HF12F560EUI, &HF5EE4BB9UI, &HF8AD6D60UI, &HFC6C70D7UI, &HE22B20D2UI, &HE6EA3D65UI, &HEBA91BBCUI, &HEF68060BUI, &HD727BBB6UI, &HD3E6A601UI, &HDEA580D8UI, &HDA649D6FUI, &HC423CD6AUI, &HC0E2D0DDUI, &HCDA1F604UI, &HC960EBB3UI, &HBD3E8D7EUI, &HB9FF90C9UI, &HB4BCB610UI, &HB07DABA7UI, &HAE3AFBA2UI, &HAAFBE615UI, &HA7B8C0CCUI, &HA379DD7BUI, &H9B3660C6UI, &H9FF77D71UI, &H92B45BA8UI, &H9675461FUI, &H8832161AUI, &H8CF30BADUI, &H81B02D74UI, &H857130C3UI, &H5D8A9099UI, &H594B8D2EUI, &H5408ABF7UI, &H50C9B640UI, &H4E8EE645UI, &H4A4FFBF2UI, &H470CDD2BUI, &H43CDC09CUI, &H7B827D21UI, &H7F436096UI, &H7200464FUI, &H76C15BF8UI, &H68860BFDUI, &H6C47164AUI, &H61043093UI, &H65C52D24UI, &H119B4BE9UI, &H155A565EUI, &H18197087UI, &H1CD86D30UI, &H29F3D35UI, &H65E2082UI, &HB1D065BUI, &HFDC1BECUI, &H3793A651UI, &H3352BBE6UI, &H3E119D3FUI, &H3AD08088UI, &H2497D08DUI, &H2056CD3AUI, &H2D15EBE3UI, &H29D4F654UI, &HC5A92679UI, &HC1683BCEUI, &HCC2B1D17UI, &HC8EA00A0UI, &HD6AD50A5UI, &HD26C4D12UI, &HDF2F6BCBUI, &HDBEE767CUI, &HE3A1CBC1UI, &HE760D676UI, &HEA23F0AFUI, &HEEE2ED18UI, &HF0A5BD1DUI, &HF464A0AAUI, &HF9278673UI, &HFDE69BC4UI, &H89B8FD09UI, &H8D79E0BEUI, &H803AC667UI, &H84FBDBD0UI, &H9ABC8BD5UI, &H9E7D9662UI, &H933EB0BBUI, &H97FFAD0CUI, &HAFB010B1UI, &HAB710D06UI, &HA6322BDFUI, &HA2F33668UI, &HBCB4666DUI, &HB8757BDAUI, &HB5365D03UI, &HB1F740B4UI}

    ReadOnly KSFv2_Magic As Byte() = New Byte() {&H5F, &H64, &H97, &H8D, &H19, &H4F, &H89, &HCF, &HA8, &H3F, &H8E, &HE1, &HDB, &H1, &H3C, &HC, &H88, &H42, &H4A, &H1C, &HB7, &HFC, &HAD, &H70, &H4E, &H45, &H13, &HA5, &H14, &H46, &H71, &H6C, &H0, &H2}
    ReadOnly KSFv2_Mask As Byte() = New Byte() {&HB2, &HA1, &HC, &H73, &H52, &H73, &H76, &HA1, &H60, &H62, &H2E, &H8, &H52, &H8, &H2E, &HA9, &H60, &HBC, &H2E, &H73, &H52, &HB, &HC, &HBC, &HEE, &HA, &H2E, &H8, &H52, &H9C, &H76, &HA9}

End Class
