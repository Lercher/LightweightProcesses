''' <summary>
''' A container for lightweight processes. 
''' Gets notified, when such a process fails. 
''' It is common to simply restart a failed process.
''' Processes should communicate only via <see cref="Channel(Of M)"/> instances.
''' </summary>
Public Class Supervisor
    Private processes As New HashSet(Of Lightweight.Process)

    Public Overloads Sub Spawn(of M As Class)(producer As IProduceMessages(Of M), processor As Action(Of M))
        Dim proc = New Lightweight.ListenerProcess(Of M) With {.Processor = processor, .Producer = producer}
        SyncLock processes
            processes.Add(proc)
        End SyncLock
        Dim handler =
            Async Function()
                Do
                    Dim msg = Await producer.Receive()
                    If msg Is Nothing Then Exit do
                    processor(msg)
                Loop
                SyncLock processes
                    processes.Remove(proc)
                End SyncLock
            End Function
        proc.t = Task.Run(handler)
    End Sub

    Public Overloads Sub Spawn(of M As Class, S)(consumer As IConsumeMessages(Of M), initialstate As S, generator As Func(Of S, Action(Of M), Task(of S)))
        Dim proc = New Lightweight.TalkerProcess(Of M, S) With { .InitialState = initialstate, .Generator = generator, .Consumer = consumer }
        SyncLock processes
            processes.Add(proc)
        End SyncLock
        Dim handler = 
            Async Function()
                Dim current = initialstate
                Do
                    Dim ch = Await consumer.Post()
                    If ch Is Nothing Then Exit Do
                    current = Await generator(current, ch)
                Loop
                SyncLock processes
                    processes.Remove(proc)
                End SyncLock
            End Function
        proc.t = Task.Run(handler)
    End Sub

    Public Overloads Sub Spawn(of M As Class, S)(consumer As IConsumeMessages(Of M), initialstate As S, syncgenerator As Func(Of S, Action(Of M), S))
        Spawn(consumer, initialstate, Function(state, post) Task.FromResult(syncgenerator(state, post)))
    End Sub

    Public Sub Join
        Dim ar As Task() = Nothing
        SyncLock processes
            Dim qy = From p In processes Select p.t
            ar = qy.ToArray
        End SyncLock
        Console.WriteLine("Joining {0:n0} processes ...", ar.Length)
        Task.WaitAll(ar)
        Console.WriteLine("Joined.")
    End Sub
End Class

