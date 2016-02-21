''' <summary>
''' A container for lightweight processes. 
''' Gets notified, when such a process fails. 
''' It is common to simply restart a failed process.
''' Processes should communicate only via <see cref="Channel(Of M)"/> instances.
''' </summary>
Public Class Supervisor
    Private list As New HashSet(Of Lightweight.Process)

    Public Overloads Sub Spawn(of M As Class)(
            producer As IProduceMessages(Of M),
            processor As Action(Of M)
        )
        Dim proc = New Lightweight.ListenerProcess(Of M) With {.Processor = processor, .Producer = producer}
        SyncLock list
            list.Add(proc)
            Dim handler =
                Async Function()
                    Do
                        Dim msg = Await producer.Receive()
                        If msg Is Nothing Then Exit do
                        processor(msg)
                    Loop
                    SyncLock list
                        list.Remove(proc)
                    End SyncLock
                End Function
            proc.t = Task.Run(handler)
        End SyncLock
    End Sub

    Public Overloads Sub Spawn(of M As Class, S)(
            consumer As IConsumeMessages(Of M),
            initialstate As S,
            generator As Func(Of S, Action(Of M), Task(of S))
        )
        Dim proc = New Lightweight.TalkerProcess(Of M, S) With {.InitialState = initialstate, .Generator = generator, .Consumer = consumer}
        SyncLock list
            list.Add(proc)
            Dim handler =
                Async Function()
                    Dim state = initialstate
                    Do
                        Dim post = Await consumer.Post()
                        If post Is Nothing Then Exit Do
                        state = Await generator(state, post)
                    Loop
                    SyncLock list
                        list.Remove(proc)
                    End SyncLock
                End Function
            proc.t = Task.Run(handler)
        End SyncLock
    End Sub

    Public Overloads Sub Spawn(of M As Class, S)(
            consumer As IConsumeMessages(Of M),
            initialstate As S,
            syncgenerator As Func(Of S, Action(Of M), S)
        )
        Spawn(consumer, initialstate, Function(state, post) Task.FromResult(syncgenerator(state, post)))
    End Sub

    Public Overloads Sub Spawn(of M1 As Class, M2 As Class)(
            producer As IProduceMessages(Of M1),
            consumer As IConsumeMessages(Of M2),
            asynctransform As Func(Of M1, Task(Of M2))
        )
        Dim proc = New Lightweight.PipelineProcess(Of M1, M2) With {.Producer = producer, .Transform = asynctransform, .Consumer = consumer}
        SyncLock list
            list.Add(proc)
            Dim handler =
                Async Function()
                    Do
                        Dim post = Await consumer.Post()
                        If post Is Nothing Then
                            producer.Dispose
                            Exit Do
                        End If
                        Dim msg1 = Await producer.Receive()
                        If msg1 Is Nothing Then
                            consumer.Dispose
                            Exit Do
                        End If
                        Dim msg2 = Await asynctransform(msg1)
                        If msg2 Is Nothing Then
                            producer.Dispose
                            consumer.Dispose
                            Exit do
                        End If
                        post(msg2)
                    Loop
                    SyncLock list
                        list.Remove(proc)
                    End SyncLock
                End Function
            proc.t = Task.Run(handler)
        End SyncLock
    End sub

    Public Overloads Sub Spawn(of M1 As Class, M2 As Class)(
            producer As IProduceMessages(Of M1),
            consumer As IConsumeMessages(Of M2),
            synctransform As Func(Of M1, M2)
        )
        Spawn(producer, consumer, Function(msg1) Task.FromResult(synctransform(msg1)))
    End Sub


    Public Overloads Sub Spawn(of M As Class, R As Class)(
        callable As ICanBeCalled(Of M, R),
        asyncworker As Func(Of M, Task(Of R))
    )
        Dim proc = New Lightweight.CallableProcess(Of M, R) With {.Callable = callable, .Worker = asyncworker}
        SyncLock list
            list.Add(proc)
            Dim handler =
                Async Function()
                    Do
                        Dim returnPlusMsg = Await callable.Receive()
                        If returnPlusMsg Is Nothing Then Exit Do ' the Connector<M,R> was closed
                        Dim post = Await returnPlusMsg.Item1.Post()
                        If post Is Nothing Then
                            ' We do not close the caller because one of the possibly multiple return channels was closed.
                            ' We simply drop the message returnPlusMsg.Item2
                            Continue Do
                        End If
                        Dim ret = Await asyncworker(returnPlusMsg.Item2)
                        If ret Is Nothing Then
                            returnPlusMsg.Item1.Dispose
                            callable.Dispose
                            Exit Do
                        End If
                        post(ret)
                    Loop
                    SyncLock list
                        list.Remove(proc)
                    End SyncLock
                End Function
            proc.t = Task.Run(handler)
        End SyncLock
    End Sub

    Public Overloads Sub Spawn(of M As Class, R As Class)(
        caller As ICanBeCalled(Of M, R),
        syncworker As Func(Of M, R)
    )
        Spawn(caller, Function(msg) Task.FromResult(syncworker(msg)))
    End Sub


    Public Sub Join
        Dim ar As Task() = Nothing
        SyncLock list
            Dim qy = From p In list Select p.t
            ar = qy.ToArray
        End SyncLock
        Console.WriteLine("Joining {0:n0} processes ...", ar.Length)
        Task.WaitAll(ar)
        Console.WriteLine("Joined.")
    End Sub
End Class

