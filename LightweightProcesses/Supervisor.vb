''' <summary>
''' A container for lightweight processes. 
''' Gets notified, when such a process fails. 
''' It is common to simply restart a failed process.
''' Processes should communicate only via <see cref="Channel(Of M)"/> instances.
''' </summary>
Public Class Supervisor
    Private processes As New HashSet(Of Lightweight.Process)

    Public Sub Spawn(of M As Class)(Producer As IProduceMessages(Of M), Processor As Action(Of M))
        Dim proc As Lightweight.Process = New Lightweight.ListenerProcess(Of M) With {.Processor = Processor, .Producer = Producer}
        SyncLock processes
            processes.Add(proc)
        End SyncLock
        Dim handler =
            Async Function()
                Do
                    Dim msg = Await Producer.Receive()
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
End Class

