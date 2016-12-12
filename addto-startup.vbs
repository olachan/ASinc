Sub CreateShortcut(strTargetPath)
    Set WshShell = WScript.CreateObject("WScript.Shell")
    Set oShellLink = WshShell.CreateShortcut(WshShell.SpecialFolders("Startup") & "\ASinc.lnk")
    oShellLink.TargetPath = strTargetPath
    oShellLink.Arguments = """" & WshShell.CurrentDirectory & "\ASinc.exe"""
    oShellLink.WindowStyle = 7
    oShellLink.Description = "ASinc"
    oShellLink.WorkingDirectory = WshShell.CurrentDirectory
    oShellLink.Save
End Sub

Sub Main()
    Set WshShell = WScript.CreateObject("WScript.Shell")
    Set fso = CreateObject("Scripting.FileSystemObject")

    ' If fso.FileExists(WshShell.SpecialFolders("Startup") & "\ASinc.lnk") Then
    '     CreateShortcut("""" & WshShell.CurrentDirectory & "\ASinc.exe""")
    '     If WshShell.Popup("ASinc.exe已经加入到启动项，是否删除？(本对话框6秒后消失)", 6, "ASinc 对话框", vbOKCancel+vbQuestion) = 1 Then
    '         fso.DeleteFile(WshShell.SpecialFolders("Startup") & "\ASinc.lnk")
    '         WshShell.Popup "删除成功", 5, "ASinc 对话框", vbInformation
    '     End If
    '     WScript.Quit
    ' End If

    If WshShell.Popup("是否将ASinc.exe加入到启动项？(本对话框6秒后消失)", 6, "ASinc 对话框", vbOKCancel+vbQuestion) = 1 Then
            strTargetPath = """" & WshShell.CurrentDirectory & "\ASinc.exe"""
        CreateShortcut(strTargetPath)

        WshShell.Popup "成功加入ASinc到启动项", 5, "ASinc 对话框", vbInformation
    End If
End Sub

Main
