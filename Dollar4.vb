Imports System.Text.RegularExpressions

Public Class Dollar4

    Private Sub BtnD4_Click(sender As Object, e As EventArgs) Handles BtnD4.Click
        Try
            Dim rawKey As String = TxtD4.Text.Trim().Replace("[", "").Replace("]", "").Replace(",", "")
            Dim compactKey As String = rawKey
            If rawKey.Contains(" ") Then
                compactKey = ""
                For Each chunk In rawKey.Split(" "c)
                    If chunk.Length < 5 Then
                        compactKey &= chunk.Trim()
                    End If
                Next
            End If

            Dim hexOnly As String = ""
            For Each ch As Match In Regex.Matches(compactKey, "[0-9a-fA-F]")
                hexOnly &= ch.Value
            Next

            MainUI.D4Password = MainUI.HexToBytes(hexOnly)
            MainUI.D4PasswordLast = MainUI.BytesToHex(MainUI.D4Password)
            Dispose()
        Catch ex As Exception
            Dispose()
        End Try
    End Sub

    Private Sub Dollar4_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Try
            Dispose()
        Catch ex As Exception
            Dispose()
        End Try
    End Sub

    Private Sub Dollar4_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            AcceptButton = BtnD4
            If MainUI.D4PasswordLast.Length > 0 Then TxtD4.Text = MainUI.D4PasswordLast
        Catch ex As Exception
            Debug.WriteLine("[忽略] " & ex.Message)
        End Try
    End Sub

End Class