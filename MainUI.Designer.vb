<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MainUI
    Inherits System.Windows.Forms.Form

    'Form 重写 Dispose，以清理组件列表。
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Windows 窗体设计器所必需的
    Private components As System.ComponentModel.IContainer

    '注意: 以下过程是 Windows 窗体设计器所必需的
    '可以使用 Windows 窗体设计器修改它。  
    '不要使用代码编辑器修改它。
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MainUI))
        Me.LblVersion = New System.Windows.Forms.Label()
        Me.BtnCopy = New System.Windows.Forms.Button()
        Me.BtnSave = New System.Windows.Forms.Button()
        Me.BtnOpen = New System.Windows.Forms.Button()
        Me.TxtMain = New System.Windows.Forms.TextBox()
        Me.OfdBoardinfo = New System.Windows.Forms.OpenFileDialog()
        Me.BtnParse = New System.Windows.Forms.Button()
        Me.SFDBoardinfo = New System.Windows.Forms.SaveFileDialog()
        Me.RadD3 = New System.Windows.Forms.RadioButton()
        Me.RadD4 = New System.Windows.Forms.RadioButton()
        Me.CmbMode = New System.Windows.Forms.ComboBox()
        Me.RadD1 = New System.Windows.Forms.RadioButton()
        Me.RadVx = New System.Windows.Forms.RadioButton()
        Me.RadD2 = New System.Windows.Forms.RadioButton()
        Me.ChkPolicy = New System.Windows.Forms.CheckBox()
        Me.ChkDynKey = New System.Windows.Forms.CheckBox()
        Me.ChkZip = New System.Windows.Forms.CheckBox()
        Me.SuspendLayout()
        '
        'LblVersion
        '
        Me.LblVersion.AutoSize = False
        Me.LblVersion.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.LblVersion.ForeColor = System.Drawing.Color.LightCyan
        Me.LblVersion.Location = New System.Drawing.Point(244, 9)
        Me.LblVersion.Name = "LblVersion"
        Me.LblVersion.Size = New System.Drawing.Size(297, 31)
        Me.LblVersion.TabIndex = 200
        Me.LblVersion.Text = "HW Dollar4 版本: 1.0.0 (2020.01)"
        Me.LblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'BtnCopy
        '
        Me.BtnCopy.BackColor = System.Drawing.Color.Violet
        Me.BtnCopy.Font = New System.Drawing.Font("微软雅黑", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnCopy.ForeColor = System.Drawing.Color.White
        Me.BtnCopy.Location = New System.Drawing.Point(547, 8)
        Me.BtnCopy.Name = "BtnCopy"
        Me.BtnCopy.Size = New System.Drawing.Size(110, 32)
        Me.BtnCopy.TabIndex = 201
        Me.BtnCopy.Text = "复制 (&C)"
        Me.BtnCopy.UseVisualStyleBackColor = False
        '
        'BtnSave
        '
        Me.BtnSave.BackColor = System.Drawing.Color.MediumBlue
        Me.BtnSave.Font = New System.Drawing.Font("微软雅黑", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnSave.ForeColor = System.Drawing.Color.White
        Me.BtnSave.Location = New System.Drawing.Point(12, 8)
        Me.BtnSave.Name = "BtnSave"
        Me.BtnSave.Size = New System.Drawing.Size(110, 32)
        Me.BtnSave.TabIndex = 101
        Me.BtnSave.Text = "保存 (&S)"
        Me.BtnSave.UseVisualStyleBackColor = False
        '
        'BtnOpen
        '
        Me.BtnOpen.BackColor = System.Drawing.Color.MediumSlateBlue
        Me.BtnOpen.Font = New System.Drawing.Font("微软雅黑", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnOpen.ForeColor = System.Drawing.Color.White
        Me.BtnOpen.Location = New System.Drawing.Point(128, 8)
        Me.BtnOpen.Name = "BtnOpen"
        Me.BtnOpen.Size = New System.Drawing.Size(110, 32)
        Me.BtnOpen.TabIndex = 111
        Me.BtnOpen.Text = "打开 (&O)"
        Me.BtnOpen.UseVisualStyleBackColor = False
        '
        'TxtMain
        '
        Me.TxtMain.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.TxtMain.Location = New System.Drawing.Point(12, 46)
        Me.TxtMain.MaxLength = 2147483647
        Me.TxtMain.Multiline = True
        Me.TxtMain.Name = "TxtMain"
        Me.TxtMain.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.TxtMain.Size = New System.Drawing.Size(760, 471)
        Me.TxtMain.TabIndex = 801
        '
        'OfdBoardinfo
        '
        Me.OfdBoardinfo.Filter = "所有文件|*.*"
        '
        'BtnParse
        '
        Me.BtnParse.BackColor = System.Drawing.Color.Orchid
        Me.BtnParse.Font = New System.Drawing.Font("微软雅黑", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnParse.ForeColor = System.Drawing.Color.White
        Me.BtnParse.Location = New System.Drawing.Point(662, 8)
        Me.BtnParse.Name = "BtnParse"
        Me.BtnParse.Size = New System.Drawing.Size(110, 32)
        Me.BtnParse.TabIndex = 211
        Me.BtnParse.Text = "计算 (&P)"
        Me.BtnParse.UseVisualStyleBackColor = False
        '
        'SFDBoardinfo
        '
        Me.SFDBoardinfo.Filter = "所有文件|*.*"
        '
        'RadD2
        '
        Me.RadD2.AutoSize = True
        Me.RadD2.Font = New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.RadD2.ForeColor = System.Drawing.Color.White
        Me.RadD2.Location = New System.Drawing.Point(290, 525)
        Me.RadD2.Name = "RadD2"
        Me.RadD2.Size = New System.Drawing.Size(74, 24)
        Me.RadD2.TabIndex = 916
        Me.RadD2.TabStop = True
        Me.RadD2.Text = "$&2模式"
        Me.RadD2.UseVisualStyleBackColor = True
        '
        'RadD3
        '
        Me.RadD3.AutoSize = True
        Me.RadD3.Font = New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.RadD3.ForeColor = System.Drawing.Color.White
        Me.RadD3.Location = New System.Drawing.Point(374, 525)
        Me.RadD3.Name = "RadD3"
        Me.RadD3.Size = New System.Drawing.Size(74, 24)
        Me.RadD3.TabIndex = 936
        Me.RadD3.TabStop = True
        Me.RadD3.Text = "$&3模式"
        Me.RadD3.UseVisualStyleBackColor = True
        '
        'RadD4
        '
        Me.RadD4.AutoSize = True
        Me.RadD4.Font = New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.RadD4.ForeColor = System.Drawing.Color.White
        Me.RadD4.Location = New System.Drawing.Point(458, 525)
        Me.RadD4.Name = "RadD4"
        Me.RadD4.Size = New System.Drawing.Size(80, 26)
        Me.RadD4.TabIndex = 951
        Me.RadD4.Text = "$&4模式"
        Me.RadD4.UseVisualStyleBackColor = True
        '
        'RadVx
        '
        Me.RadVx.AutoSize = True
        Me.RadVx.Checked = True
        Me.RadVx.Font = New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.RadVx.ForeColor = System.Drawing.Color.White
        Me.RadVx.Location = New System.Drawing.Point(122, 525)
        Me.RadVx.Name = "RadVx"
        Me.RadVx.Size = New System.Drawing.Size(74, 24)
        Me.RadVx.TabIndex = 921
        Me.RadVx.TabStop = True
        Me.RadVx.Text = "V&x模式"
        Me.RadVx.UseVisualStyleBackColor = True
        'CmbMode
        '
        Me.CmbMode.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.CmbMode.FormattingEnabled = True
        Me.CmbMode.Location = New System.Drawing.Point(12, 523)
        Me.CmbMode.Name = "CmbMode"
        Me.CmbMode.Size = New System.Drawing.Size(96, 28)
        Me.CmbMode.TabIndex = 901
        '
        'RadD1
        '
        Me.RadD1.AutoSize = True
        Me.RadD1.Font = New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.RadD1.ForeColor = System.Drawing.Color.White
        Me.RadD1.Location = New System.Drawing.Point(206, 525)
        Me.RadD1.Name = "RadD1"
        Me.RadD1.Size = New System.Drawing.Size(80, 26)
        Me.RadD1.TabIndex = 911
        Me.RadD1.Text = "$&1模式"
        Me.RadD1.UseVisualStyleBackColor = True
        '
        'ChkDynKey
        '
        Me.ChkDynKey.AutoSize = True
        Me.ChkDynKey.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.ChkDynKey.ForeColor = System.Drawing.Color.LightCyan
        Me.ChkDynKey.Location = New System.Drawing.Point(500, 527)
        Me.ChkDynKey.Name = "ChkDynKey"
        Me.ChkDynKey.Size = New System.Drawing.Size(56, 23)
        Me.ChkDynKey.TabIndex = 985
        Me.ChkDynKey.Text = "动态"
        Me.ChkDynKey.UseVisualStyleBackColor = True
        Me.ChkDynKey.Visible = False
        '
        'ChkZip
        '
        Me.ChkZip.AutoSize = True
        Me.ChkZip.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.ChkZip.ForeColor = System.Drawing.Color.LightCyan
        Me.ChkZip.Location = New System.Drawing.Point(546, 527)
        Me.ChkZip.Name = "ChkZip"
        Me.ChkZip.Size = New System.Drawing.Size(56, 23)
        Me.ChkZip.TabIndex = 986
        Me.ChkZip.Text = "压缩"
        Me.ChkZip.UseVisualStyleBackColor = True
        Me.ChkZip.Visible = False
        '
        'ChkPolicy
        '
        Me.ChkPolicy.AutoSize = True
        Me.ChkPolicy.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.ChkPolicy.ForeColor = System.Drawing.Color.LightCyan
        Me.ChkPolicy.Location = New System.Drawing.Point(620, 527)
        Me.ChkPolicy.Name = "ChkPolicy"
        Me.ChkPolicy.Size = New System.Drawing.Size(56, 23)
        Me.ChkPolicy.TabIndex = 991
        Me.ChkPolicy.Text = "下载"
        Me.ChkPolicy.UseVisualStyleBackColor = True
        '
        'MainUI
        '
        Me.AllowDrop = True
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.BackColor = System.Drawing.Color.MediumVioletRed
        Me.ClientSize = New System.Drawing.Size(784, 561)
        Me.Controls.Add(Me.ChkPolicy)
        Me.Controls.Add(Me.ChkDynKey)
        Me.Controls.Add(Me.ChkZip)
        Me.Controls.Add(Me.RadD1)
        Me.Controls.Add(Me.RadVx)
        Me.Controls.Add(Me.RadD2)
        Me.Controls.Add(Me.CmbMode)
        Me.Controls.Add(Me.RadD3)
        Me.Controls.Add(Me.RadD4)
        Me.Controls.Add(Me.BtnParse)
        Me.Controls.Add(Me.LblVersion)
        Me.Controls.Add(Me.BtnCopy)
        Me.Controls.Add(Me.BtnSave)
        Me.Controls.Add(Me.BtnOpen)
        Me.Controls.Add(Me.TxtMain)
        Me.Font = New System.Drawing.Font("微软雅黑", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Margin = New System.Windows.Forms.Padding(5)
        Me.MaximizeBox = False
        Me.Name = "MainUI"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "HW Dollar4"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LblVersion As Label
    Friend WithEvents BtnCopy As Button
    Friend WithEvents BtnSave As Button
    Friend WithEvents BtnOpen As Button
    Friend WithEvents TxtMain As TextBox
    Friend WithEvents OfdBoardinfo As OpenFileDialog
    Friend WithEvents BtnParse As Button
    Friend WithEvents SFDBoardinfo As SaveFileDialog
    Friend WithEvents RadD3 As RadioButton
    Friend WithEvents RadD4 As RadioButton
    Friend WithEvents CmbMode As ComboBox
    Friend WithEvents RadD1 As RadioButton
    Friend WithEvents RadVx As RadioButton
    Friend WithEvents RadD2 As RadioButton
    Friend WithEvents ChkPolicy As CheckBox
    Friend WithEvents ChkDynKey As CheckBox
    Friend WithEvents ChkZip As CheckBox
End Class
