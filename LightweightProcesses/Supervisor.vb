''' <summary>
''' A container for lightweight processes. 
''' Gets notified, when such a process fails. 
''' It is common to simply restart a failed process.
''' Processes should communicate only via <see cref="Channel(Of M)"/> instances.
''' </summary>
Public Class Supervisor
    Private processes As New HashSet(Of LightweightProcess)

    Public Sub Spawn(of M As Class)(ch As IReceiveMessages(Of M), Processor As Action(Of M))
        Dim proc As LightweightProcess = New LightweightProcess(Of M) With {.Processor = Processor, .Channel = ch}
        SyncLock processes
            processes.Add(proc)
        End SyncLock
        Dim handler =
            Async Function()
                Do
                    Dim msg = Await ch.Receive()
                    If msg Is Nothing Then Exit do
                    Processor(msg)
                Loop
                SyncLock processes
                    processes.Remove(proc)
                End SyncLock
            End Function
        proc.t = Task.Run(handler)
    End Sub

    Public Sub Join
        Dim ar As Task() = Nothing
        SyncLock processes
            Dim qy = From p In processes Select p.t
            ar = qy.ToArray
        End SyncLock
        Console.WriteLine("Joining {0:n0} processes", ar.Length)
        Task.WaitAll(ar)
    End Sub

    Public MustInherit Class LightweightProcess
        Public t As Task
    End Class

    Public Class LightweightProcess(Of M As Class)
        Inherits LightweightProcess
        Public Property Processor As Action(Of M)
        Public Property Channel As IReceiveMessages(Of M)
    End Class
End Class