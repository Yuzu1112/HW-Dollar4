<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Search
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Search))
        Me.PnlBody = New System.Windows.Forms.Panel()
        Me.ShFind = New System.Windows.Forms.Panel()
        Me.TxtFind = New System.Windows.Forms.TextBox()
        Me.BtnPrev = New System.Windows.Forms.Button()
        Me.BtnNext = New System.Windows.Forms.Button()
        Me.ShReplace = New System.Windows.Forms.Panel()
        Me.TxtReplace = New System.Windows.Forms.TextBox()
        Me.BtnReplace = New System.Windows.Forms.Button()
        Me.BtnReplaceAll = New System.Windows.Forms.Button()
        Me.PnlBody.SuspendLayout()
        Me.ShFind.SuspendLayout()
        Me.ShReplace.SuspendLayout()
        Me.SuspendLayout()
        '
        'PnlBody
        '
        Me.PnlBody.BackColor = System.Drawing.Color.FromArgb(CType(CType(250, Byte), Integer), CType(CType(228, Byte), Integer), CType(CType(240, Byte), Integer))
        Me.PnlBody.Location = New System.Drawing.Point(10, 10)
        Me.PnlBody.Name = "PnlBody"
        Me.PnlBody.Size = New System.Drawing.Size(368, 172)
        Me.PnlBody.TabIndex = 100
        '
        'ShFind
        '
        Me.ShFind.BackColor = System.Drawing.Color.White
        Me.ShFind.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.ShFind.Location = New System.Drawing.Point(12, 12)
        Me.ShFind.Name = "ShFind"
        Me.ShFind.Size = New System.Drawing.Size(344, 30)
        Me.ShFind.TabIndex = 101
        '
        'TxtFind
        '
        Me.TxtFind.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.TxtFind.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.TxtFind.Location = New System.Drawing.Point(9, 5)
        Me.TxtFind.MaxLength = 65535
        Me.TxtFind.Name = "TxtFind"
        Me.TxtFind.Size = New System.Drawing.Size(324, 22)
        Me.TxtFind.TabIndex = 102
        '
        'BtnPrev
        '
        Me.BtnPrev.BackColor = System.Drawing.Color.MediumSlateBlue
        Me.BtnPrev.Font = New System.Drawing.Font("微软雅黑", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnPrev.ForeColor = System.Drawing.Color.White
        Me.BtnPrev.Location = New System.Drawing.Point(12, 50)
        Me.BtnPrev.Name = "BtnPrev"
        Me.BtnPrev.Size = New System.Drawing.Size(92, 32)
        Me.BtnPrev.TabIndex = 103
        Me.BtnPrev.Text = "上一个"
        Me.BtnPrev.UseVisualStyleBackColor = False
        '
        'BtnNext
        '
        Me.BtnNext.BackColor = System.Drawing.Color.MediumSlateBlue
        Me.BtnNext.Font = New System.Drawing.Font("微软雅黑", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnNext.ForeColor = System.Drawing.Color.White
        Me.BtnNext.Location = New System.Drawing.Point(116, 50)
        Me.BtnNext.Name = "BtnNext"
        Me.BtnNext.Size = New System.Drawing.Size(92, 32)
        Me.BtnNext.TabIndex = 104
        Me.BtnNext.Text = "下一个"
        Me.BtnNext.UseVisualStyleBackColor = False
        '
        'ShReplace
        '
        Me.ShReplace.BackColor = System.Drawing.Color.White
        Me.ShReplace.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.ShReplace.Location = New System.Drawing.Point(12, 90)
        Me.ShReplace.Name = "ShReplace"
        Me.ShReplace.Size = New System.Drawing.Size(344, 30)
        Me.ShReplace.TabIndex = 105
        '
        'TxtReplace
        '
        Me.TxtReplace.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.TxtReplace.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.TxtReplace.Location = New System.Drawing.Point(9, 5)
        Me.TxtReplace.MaxLength = 65535
        Me.TxtReplace.Name = "TxtReplace"
        Me.TxtReplace.Size = New System.Drawing.Size(324, 22)
        Me.TxtReplace.TabIndex = 106
        '
        'BtnReplace
        '
        Me.BtnReplace.BackColor = System.Drawing.Color.Violet
        Me.BtnReplace.Font = New System.Drawing.Font("微软雅黑", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnReplace.ForeColor = System.Drawing.Color.White
        Me.BtnReplace.Location = New System.Drawing.Point(12, 128)
        Me.BtnReplace.Name = "BtnReplace"
        Me.BtnReplace.Size = New System.Drawing.Size(92, 32)
        Me.BtnReplace.TabIndex = 107
        Me.BtnReplace.Text = "替换"
        Me.BtnReplace.UseVisualStyleBackColor = False
        '
        'BtnReplaceAll
        '
        Me.BtnReplaceAll.BackColor = System.Drawing.Color.Violet
        Me.BtnReplaceAll.Font = New System.Drawing.Font("微软雅黑", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnReplaceAll.ForeColor = System.Drawing.Color.White
        Me.BtnReplaceAll.Location = New System.Drawing.Point(116, 128)
        Me.BtnReplaceAll.Name = "BtnReplaceAll"
        Me.BtnReplaceAll.Size = New System.Drawing.Size(92, 32)
        Me.BtnReplaceAll.TabIndex = 108
        Me.BtnReplaceAll.Text = "全部替换"
        Me.BtnReplaceAll.UseVisualStyleBackColor = False
        '
        'Search
        '
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.BackColor = System.Drawing.Color.MediumVioletRed
        Me.ClientSize = New System.Drawing.Size(388, 192)
        Me.Controls.Add(Me.PnlBody)
        Me.Font = New System.Drawing.Font("微软雅黑", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "Search"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "查找和替换"
        Me.PnlBody.Controls.Add(Me.BtnReplaceAll)
        Me.PnlBody.Controls.Add(Me.BtnReplace)
        Me.PnlBody.Controls.Add(Me.ShReplace)
        Me.PnlBody.Controls.Add(Me.BtnNext)
        Me.PnlBody.Controls.Add(Me.BtnPrev)
        Me.PnlBody.Controls.Add(Me.ShFind)
        Me.ShFind.Controls.Add(Me.TxtFind)
        Me.ShReplace.Controls.Add(Me.TxtReplace)
        Me.PnlBody.ResumeLayout(False)
        Me.ShFind.ResumeLayout(False)
        Me.ShReplace.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents PnlBody As Panel
    Friend WithEvents ShFind As Panel
    Friend WithEvents TxtFind As TextBox
    Friend WithEvents BtnPrev As Button
    Friend WithEvents BtnNext As Button
    Friend WithEvents ShReplace As Panel
    Friend WithEvents TxtReplace As TextBox
    Friend WithEvents BtnReplace As Button
    Friend WithEvents BtnReplaceAll As Button
End Class
