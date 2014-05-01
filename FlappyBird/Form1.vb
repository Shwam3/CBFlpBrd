﻿Public Class frmFlappyBird
    Private Const localVersion As String = "2.16.7"
    Private Const remoteVersionFile As String = "https://raw.githubusercontent.com/Ombeacrrind/CBFlpBrd/master/version.txt"
    Private Const fileDownloadLocation As String = "https://dl.dropboxusercontent.com/s/ekhvbvdivqeuxd9/FlappyBird.exe?dl=1"
    '"http://download1785.mediafire.com/zvc7b0r2m2cg/gvgis9qguft7od2/FlappyBird.exe"
    '"https://copy.com/ViLgHdgBtflJ/FlappyBird.exe?download=1"

    Private noclip As Boolean = False

    Private RED As Color = Color.FromArgb(255, 0, 0)
    Private YELLOW As Color = Color.FromArgb(230, 230, 0)
    Private GREEN As Color = Color.FromArgb(0, 155, 0)

    Private PIPE_HEIGHT As Integer
    Private PIPE_TOP_OFFSET As Integer = 175
    Private PIPE_BOTTOM_OFFSET As Integer = 305
    Private PIPE_STEP As Integer = 4

    Private birdYVelocity As Integer = 0

    Private birdFrame As Integer = 3
    Private pipeFrame As Integer = 0

    Private lastPipe As Integer = 4
    Private updatePipe1 As Boolean = False
    Private updatePipe2 As Boolean = False
    Private updatePipe3 As Boolean = False
    Private updatePipe4 As Boolean = False
    Private pipe1Scored As Boolean = False
    Private pipe2Scored As Boolean = False
    Private pipe3Scored As Boolean = False
    Private pipe4Scored As Boolean = False

    Dim currImg As String = "Bird"
    Dim lastImg As String = "Bird2"

    Private gameOver As Boolean = True
    Private canStartGame As Boolean = True

    Protected Friend rnd As New Random

    Private Sub frmFlappyBird_Load() Handles MyBase.Load
        ' Version control
        Try
            Dim remoteVersion As String = New System.Net.WebClient().DownloadString(remoteVersionFile)
            Dim remoteBits() As String = remoteVersion.Split(CChar("."))
            Dim localBits() As String = localVersion.Split(CChar("."))
            DebugMsg("Local: " & localVersion & Environment.NewLine & "Remote: " & remoteVersion)

            If remoteBits(0) > localBits(0) Or remoteBits(1) > localBits(1) Or remoteBits(2) > localBits(2) Then
                If MsgBox("New version available - v" & remoteVersion & " (Current: v" & localVersion & ")" & Environment.NewLine & "Download now?" & _
                          Environment.NewLine & "PS: Your high score might not survive", MsgBoxStyle.YesNo, "Version check") = MsgBoxResult.Yes Then
                    Try
                        Me.Cursor = Cursors.AppStarting
                        My.Computer.Network.DownloadFile(New Uri(fileDownloadLocation), My.Computer.FileSystem.CurrentDirectory & "\Flappy Bird - new.exe", _
                                   Nothing, True, 2000, True, FileIO.UICancelOption.ThrowException)
                        'MsgBox("Delete this file and rename the new version the same as the old. Your high score should be fine")
                        Process.Start("explorer.exe", "/select, " & My.Computer.FileSystem.CurrentDirectory & "\Flappy Bird - new.exe")
                    Catch ex As Exception
                        My.Computer.Clipboard.SetText(fileDownloadLocation)
                        MsgBox("Unable to download, try navigating to the link placed on your clipboard", Nothing, "Version check")
                    Finally
                        Me.Cursor = Nothing
                        Application.Exit()
                    End Try

                    'Try
                    '    
                    '    Process.Start(fileDownloadLocation)
                    'Catch ex As Exception
                    '    My.Computer.Clipboard.SetText(fileDownloadLocation)
                    '    MsgBox("Aparently I cant code properly, navigate to " & fileDownloadLocation & Environment.NewLine & "(link placed on clipboard)", MsgBoxStyle.Exclamation, "Version check")
                    'Finally
                    '   Me.Cursor = Nothing
                    '   Application.Exit()
                    'End Try
                    Return
                End If
            End If
        Catch ex As Exception
            DebugMsg("Unable to fetch remote version")
        End Try

        PIPE_HEIGHT = imgPipe1Top.Height

        Try
            lblHighScore.Text = CStr(My.Settings.highScore)
        Catch ex As Exception
            lblHighScore.Text = "0"
            MsgBox("High score cannot be saved:" & Environment.NewLine & ex.Message, MsgBoxStyle.Exclamation)
            My.Computer.Clipboard.SetText(ex.Message & Environment.NewLine & ex.InnerException.ToString)
        End Try
        centreLabels()

        imgDeath.BringToFront()
        imgDeath.Size = MyBase.Size
        lblScore.BringToFront()
        lblHighScore.BringToFront()

        If My.Application.CommandLineArgs().Contains("-cheat") Then
            setCheat(True)
        Else
            MyBase.Text = "Flappy Bird - v" & localVersion
        End If

        resetForm()
    End Sub

    Private Sub resetForm()
        imgBird.Location = New System.Drawing.Point(100, 133)
        imgGround.Left = 0

        imgPipe1Top.Location = New System.Drawing.Point(500, rnd.Next(140) - 175)
        imgPipe2Top.Location = New System.Drawing.Point(500, rnd.Next(140) - 175)
        imgPipe3Top.Location = New System.Drawing.Point(500, rnd.Next(140) - 175)
        imgPipe4Top.Location = New System.Drawing.Point(500, rnd.Next(140) - 175)

        imgPipe1Bottom.Location = New System.Drawing.Point(500, imgPipe1Top.Location.Y + 303)
        imgPipe2Bottom.Location = New System.Drawing.Point(500, imgPipe2Top.Location.Y + 303)
        imgPipe3Bottom.Location = New System.Drawing.Point(500, imgPipe3Top.Location.Y + 303)
        imgPipe4Bottom.Location = New System.Drawing.Point(500, imgPipe4Top.Location.Y + 303)

        updatePipe1 = False
        updatePipe2 = False
        updatePipe3 = False
        updatePipe4 = False
        pipe1Scored = False
        pipe2Scored = False
        pipe3Scored = False
        pipe4Scored = False
    End Sub

    Private Sub UpdateTimer_Tick() Handles UpdateTimer.Tick
        If gameOver Then
            UpdateTimer.Enabled = False

            imgDeath.Visible = False
            imgPause.Image = My.Resources.Paused
            imgBird.Image = My.Resources.Bird
            imgBird.Size = New Size(40, 26)
            imgBird.Location = New Point(100, 132)

            lblInstructions.Visible = True
            resetForm()
            UpdateTimer.Interval = 25

            canStartGame = True
            Return
        End If

        movementBird()
        movementPipes()
        movementGround()
        updateLabels()
    End Sub

    Private Sub movementBird()
        Dim newY As Integer = CInt(imgBird.Location.Y - Math.Round(birdYVelocity))

        If newY < 1 Then
            birdYVelocity = 0
            imgBird.Location = New System.Drawing.Point(imgBird.Location.X, 1)
        ElseIf newY + imgBird.Height >= imgGround.Location.Y Then
            imgBird.Location = New System.Drawing.Point(imgBird.Location.X, imgGround.Location.Y - imgBird.Height)
            endGame("floor")
            Return
        Else
            imgBird.Location = New System.Drawing.Point(imgBird.Location.X, newY)
            If birdYVelocity <= -4 Then
                birdYVelocity = CInt(Math.Max(-9, Math.Round(birdYVelocity) - 2))
            Else
                birdYVelocity = CInt(Math.Max(-9, Math.Round(birdYVelocity) - 1))
            End If
        End If

        If birdYVelocity > 3 Then
            If Not currImg <> "BirdUp" Or currImg <> "BirdUp2" Then
                If currImg.EndsWith("2") Then
                    imgBird.Image = My.Resources.BirdUp2
                    lastImg = currImg
                    currImg = "BirdUp2"
                Else
                    imgBird.Image = My.Resources.BirdUp
                    lastImg = currImg
                    currImg = "BirdUp"
                End If
            End If
        ElseIf birdYVelocity <= -9 Then
            imgBird.Image = My.Resources.BirdDive
            currImg = "BirdDive"
        ElseIf birdYVelocity < -4 Then
            If Not currImg <> "BirdDown" Or currImg <> "BirdDown2" Then
                If currImg.EndsWith("2") Then
                    imgBird.Image = My.Resources.BirdDown2
                    lastImg = currImg
                    currImg = "BirdDown2"
                Else
                    imgBird.Image = My.Resources.BirdDown
                    lastImg = currImg
                    currImg = "BirdDown"
                End If
            End If
        Else
            If Not currImg <> "Bird" Or currImg <> "Bird2" Then
                If currImg.EndsWith("2") Then
                    imgBird.Image = My.Resources.Bird2
                    lastImg = currImg
                    currImg = "Bird2"
                Else
                    imgBird.Image = My.Resources.Bird
                    lastImg = currImg
                    currImg = "Bird"
                End If
            End If
        End If

        If birdFrame = 3 Then
            Select Case currImg
                Case "Bird"
                    imgBird.Image = My.Resources.Bird2
                    currImg = "Bird2"
                Case "Bird2"
                    imgBird.Image = My.Resources.Bird
                    currImg = "Bird"
                Case "BirdUp"
                    imgBird.Image = My.Resources.BirdUp2
                    currImg = "BirdUp2"
                Case "BirdUp2"
                    imgBird.Image = My.Resources.BirdUp
                    currImg = "BirdUp"
                Case "BirdDown"
                    imgBird.Image = My.Resources.BirdDown2
                    currImg = "BirdDown2"
                Case "BirdDown2"
                    imgBird.Image = My.Resources.BirdDown
                    currImg = "BirdDown"
            End Select
            birdFrame = 0
        Else
            birdFrame = birdFrame + 1
        End If

        If currImg.StartsWith("BirdUp") Or currImg.StartsWith("BirdDown") Then
            imgBird.Size = New Size(35, 35)
            If lastImg = "BirdDive" Then
                imgBird.Location = New Point(102, imgBird.Location.Y - 9)
            ElseIf lastImg.StartsWith("Bird") Then
                imgBird.Location = New Point(102, imgBird.Location.Y + 2)
            End If
        ElseIf currImg = "BirdDive" Then
            imgBird.Size = New Size(26, 40)
            If lastImg.StartsWith("BirdUp") Or lastImg.StartsWith("BirdDown") Then
                imgBird.Location = New Point(107, imgBird.Location.Y - 2)
            End If
        ElseIf currImg.StartsWith("Bird") Then
            imgBird.Size = New Size(40, 26)
            If lastImg.StartsWith("BirdUp") Or lastImg.StartsWith("BirdDown") Then
                imgBird.Location = New Point(100, imgBird.Location.Y + 9)
            End If
        End If

        lastImg = currImg
    End Sub

    Private Sub movementGround()
        Dim newX As Integer = imgGround.Location.X - 4

        If newX <= -499 Then
            imgGround.Left = 0
        Else
            imgGround.Left = newX
        End If
    End Sub

    Private Sub movementPipes()
        Dim newX As Integer

        If pipeFrame >= 40 Then
            pipeFrame = 0

            Select Case lastPipe
                Case 1
                    updatePipe2 = True
                    lastPipe = 2
                    pipe2Scored = False
                    Exit Select
                Case 2
                    updatePipe3 = True
                    lastPipe = 3
                    pipe3Scored = False
                    Exit Select
                Case 3
                    updatePipe4 = True
                    lastPipe = 4
                    pipe4Scored = False
                    Exit Select
                Case 4
                    updatePipe1 = True
                    lastPipe = 1
                    pipe1Scored = False
                    Exit Select
            End Select
        Else
            pipeFrame = pipeFrame + CInt(Math.Ceiling(PIPE_STEP / 4))
        End If

        ' Pipe 1
        If updatePipe1 Then
            newX = imgPipe1Top.Location.X - PIPE_STEP
            If newX <= -32 Then
                imgPipe1Top.Location = New System.Drawing.Point(500, rnd.Next(140) - PIPE_TOP_OFFSET)
                imgPipe1Bottom.Location = New System.Drawing.Point(500, imgPipe1Top.Location.Y + PIPE_BOTTOM_OFFSET)
                updatePipe1 = False
            Else
                imgPipe1Top.Location = New System.Drawing.Point(newX, imgPipe1Top.Location.Y)
                imgPipe1Bottom.Location = New System.Drawing.Point(newX, imgPipe1Bottom.Location.Y)
            End If

            If imgPipe1Top.Location.X <= 118 And Not pipe1Scored Then
                lblScore.Text = CStr(CInt(lblScore.Text) + 1)
                My.Computer.Audio.PlaySystemSound(Media.SystemSounds.Asterisk)
                centreLabels()
                pipe1Scored = True
            End If

            If Not noclip Then
                If imgPipe1Top.Location.X + 1 <= imgBird.Location.X + imgBird.Width And imgPipe1Top.Location.X + imgPipe1Top.Width - 1 >= imgBird.Location.X Then
                    If imgBird.Location.Y <= imgPipe1Top.Location.Y + PIPE_HEIGHT - 2 Or imgBird.Location.Y + imgBird.Height >= imgPipe1Bottom.Location.Y + 2 Then
                        endGame("pipe")
                        Return
                    End If
                End If
            End If
        End If

        ' Pipe 2
        If updatePipe2 Then
            newX = imgPipe2Top.Location.X - PIPE_STEP
            If newX <= -32 Then
                imgPipe2Top.Location = New System.Drawing.Point(500, rnd.Next(140) - PIPE_TOP_OFFSET)
                imgPipe2Bottom.Location = New System.Drawing.Point(500, imgPipe2Top.Location.Y + PIPE_BOTTOM_OFFSET)
                updatePipe2 = False
            Else
                imgPipe2Top.Location = New System.Drawing.Point(newX, imgPipe2Top.Location.Y)
                imgPipe2Bottom.Location = New System.Drawing.Point(newX, imgPipe2Bottom.Location.Y)
            End If

            If imgPipe2Top.Location.X <= 118 And Not pipe2Scored Then
                lblScore.Text = CStr(CDbl(lblScore.Text) + 1)
                My.Computer.Audio.PlaySystemSound(Media.SystemSounds.Asterisk)
                centreLabels()
                pipe2Scored = True
            End If

            If Not noclip Then
                If imgPipe2Top.Location.X + 1 <= imgBird.Location.X + imgBird.Width And imgPipe2Top.Location.X + imgPipe2Top.Width - 1 >= imgBird.Location.X Then
                    If imgBird.Location.Y <= imgPipe2Top.Location.Y + PIPE_HEIGHT - 2 Or imgBird.Location.Y + imgBird.Height >= imgPipe2Bottom.Location.Y + 2 Then
                        endGame("pipe")
                        Return
                    End If
                End If
            End If
        End If

        ' Pipe 3
        If updatePipe3 Then
            newX = imgPipe3Top.Location.X - PIPE_STEP
            If newX <= -32 Then
                imgPipe3Top.Location = New System.Drawing.Point(500, rnd.Next(140) - PIPE_TOP_OFFSET)
                imgPipe3Bottom.Location = New System.Drawing.Point(500, imgPipe3Top.Location.Y + PIPE_BOTTOM_OFFSET)
                updatePipe3 = False
            Else
                imgPipe3Top.Location = New System.Drawing.Point(newX, imgPipe3Top.Location.Y)
                imgPipe3Bottom.Location = New System.Drawing.Point(newX, imgPipe3Bottom.Location.Y)
            End If

            If imgPipe3Top.Location.X <= 118 And Not pipe3Scored Then
                lblScore.Text = CStr(CDbl(lblScore.Text) + 1)
                My.Computer.Audio.PlaySystemSound(Media.SystemSounds.Asterisk)
                centreLabels()
                pipe3Scored = True
            End If

            If Not noclip Then
                If imgPipe3Top.Location.X + 1 <= imgBird.Location.X + imgBird.Width And imgPipe3Top.Location.X + imgPipe3Top.Width - 1 >= imgBird.Location.X Then
                    If imgBird.Location.Y <= imgPipe3Top.Location.Y + PIPE_HEIGHT - 2 Or imgBird.Location.Y + imgBird.Height >= imgPipe3Bottom.Location.Y + 2 Then
                        endGame("pipe")
                        Return
                    End If
                End If
            End If
        End If

        ' Pipe 4
        If updatePipe4 Then
            newX = imgPipe4Top.Location.X - PIPE_STEP
            If newX <= -31 Then
                imgPipe4Top.Location = New System.Drawing.Point(500, rnd.Next(140) - PIPE_TOP_OFFSET)
                imgPipe4Bottom.Location = New System.Drawing.Point(500, imgPipe4Top.Location.Y + PIPE_BOTTOM_OFFSET)
                updatePipe4 = False
            Else
                imgPipe4Top.Location = New System.Drawing.Point(newX, imgPipe4Top.Location.Y)
                imgPipe4Bottom.Location = New System.Drawing.Point(newX, imgPipe4Bottom.Location.Y)
            End If

            If imgPipe4Top.Location.X <= 118 And Not pipe4Scored Then
                lblScore.Text = CStr(CDbl(lblScore.Text) + 1)
                My.Computer.Audio.PlaySystemSound(Media.SystemSounds.Asterisk)
                centreLabels()
                pipe4Scored = True
            End If

            If Not noclip Then
                If imgPipe4Top.Location.X + 1 <= imgBird.Location.X + imgBird.Width And imgPipe4Top.Location.X + imgPipe4Top.Width - 1 >= imgBird.Location.X Then
                    If imgBird.Location.Y <= imgPipe4Top.Location.Y + PIPE_HEIGHT - 2 Or imgBird.Location.Y + imgBird.Height >= imgPipe4Bottom.Location.Y + 2 Then
                        endGame("pipe")
                        Return
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub updateLabels()
        If CInt(lblScore.Text) > CInt(lblHighScore.Text) Then
            lblHighScore.Text = lblScore.Text
            centreLabels()
            Try
                My.Settings.highScore = CInt(lblHighScore.Text)
            Catch ex As Exception
            End Try
        End If

        If CInt(lblScore.Text) < Math.Ceiling(CInt(lblHighScore.Text) / 2) Then
            lblScore.BackColor = RED
        ElseIf CInt(lblScore.Text) >= Math.Ceiling(CInt(lblHighScore.Text) / 2) And CInt(lblScore.Text) < CInt(lblHighScore.Text) Then
            lblScore.BackColor = YELLOW
        ElseIf CInt(lblScore.Text) >= CInt(lblHighScore.Text) Then
            lblScore.BackColor = GREEN
        End If

        'If lblHighScore.Text < 10 Then
        '    lblHighScore.BackColor = RED
        'ElseIf lblHighScore.Text >= 10 And lblHighScore.Text < 20 Then
        '    lblHighScore.BackColor = YELLOW
        'Else
        '    lblHighScore.BackColor = GREEN
        'End If
    End Sub

    Private Sub frmFlappyBird_Click() Handles MyBase.Click, imgPipe1Top.Click, imgPipe1Bottom.Click, imgPipe2Top.Click, imgPipe2Bottom.Click, imgPipe3Top.Click, imgPipe3Bottom.Click, imgPipe4Top.Click, imgPipe3Bottom.Click, imgBird.Click, lblScore.Click
        If canStartGame Then
            canStartGame = False
            gameOver = False

            UpdateTimer.Enabled = True

            imgBird.Image = My.Resources.Bird
            currImg = "Bird"

            imgPause.Image = My.Resources.Unpaused
            lblInstructions.Visible = False

            lblScore.BackColor = RED

            birdFrame = 3
            birdYVelocity = 13

            pipeFrame = 0

            lblScore.Text = "0"
            centreLabels()
        Else
            birdYVelocity = 9
        End If
    End Sub

    Private Sub frmFlappyBird_KeyDown(sender As System.Object, e As System.Windows.Forms.KeyEventArgs) Handles MyBase.KeyDown
        If e.KeyValue = 32 Then
            If Not UpdateTimer.Enabled Then
                imgPause_Click()
            End If

            frmFlappyBird_Click()
            e.Handled = True
        ElseIf e.KeyValue = 80 Or e.KeyValue = 112 Then
            imgPause_Click()
            e.Handled = True
        ElseIf e.KeyValue = 223 Then
            setCheat(Not noclip)
        End If
    End Sub

    Private Sub imgPause_Click() Handles imgPause.Click, MyBase.Deactivate
        If Not gameOver And UpdateTimer.Enabled Then
            UpdateTimer.Enabled = False
            imgPause.Image = My.Resources.Paused
        Else
            UpdateTimer.Enabled = True
            imgPause.Image = My.Resources.Unpaused
        End If
    End Sub

    Private Sub endGame(Optional ByVal type As String = "")
        UpdateTimer.Interval = 1000
        UpdateTimer.Enabled = False

        If lblScore.BackColor = GREEN Then
            Try
                Dim bmp As New Bitmap(MyBase.Width + 10, MyBase.Height + 10)
                Dim graph As Graphics = Graphics.FromImage(bmp)

                graph.CopyFromScreen(Me.Location.X - 5, Me.Location.Y - 5, 0, 0, bmp.Size)

                ' Save a Screenshot to file
                Dim filePath As String = My.Computer.FileSystem.CurrentDirectory & "\HighScore.png"
                bmp.Save(filePath)

                DebugMsg("Screensot saved to """ & filePath & """")

                bmp.Dispose()
                graph.Dispose()
            Catch ex As Exception
                DebugMsg(ex.Message)
            End Try
        End If
        UpdateTimer.Enabled = True
        Select Case type
            Case "floor"
                imgDeath.Image = My.Resources.floorDeath
                imgDeath.Visible = True
            Case "pipe"
                imgDeath.Image = My.Resources.pipeDeath
                imgDeath.Visible = True
        End Select
        gameOver = True
        canStartGame = False
        centreLabels()
    End Sub

    Private Sub frmFlappyBird_FormClosing() Handles MyBase.FormClosing
        Try
            My.Settings.highScore = CInt(lblHighScore.Text)
            My.Settings.Save()
        Catch ex As Exception
            MsgBox("High score not saved:" & Environment.NewLine & ex.Message, MsgBoxStyle.Exclamation)
        End Try
    End Sub

    Private Sub lblHighScore_Click() Handles lblHighScore.Click
        If MsgBox("Reset high score?", MsgBoxStyle.YesNo) = 6 Then
            lblHighScore.Text = "0"
            centreLabels()
            My.Settings.highScore = 0
        End If
    End Sub

    Private Sub centreLabels() Handles lblHighScore.TextChanged, lblScore.TextChanged
        lblScore.Location = New Point(CInt((MyBase.ClientSize.Width - lblScore.Width) / 2), lblScore.Location.Y)
        lblHighScore.Location = New Point(CInt((MyBase.ClientSize.Width - lblHighScore.Width) / 2), lblHighScore.Location.Y)
    End Sub

    Private Sub DebugMsg(ByVal msg As String)
        If My.Application.CommandLineArgs().Contains("-debug") Then
            MsgBox(msg)
        End If
    End Sub

    Private Sub setCheat(ByVal cheat As Boolean)
        noclip = cheat
        If cheat Then
            PIPE_STEP = 10
            MyBase.Text = "Flappy Bird - v" & localVersion & " - Cheating"
        Else
            PIPE_STEP = 4
            MyBase.Text = "Flappy Bird - v" & localVersion
        End If
    End Sub
End Class
