''' <summary>
''' A container for lightweight processes (async/Task based) that can be spawned and joined as a group. 
''' Gets notified, when a process fails. (TODO)
''' It is common to simply restart a failed process. (TODO)
''' Processes should communicate only via <see cref="IProduceMessages(Of M)"/> or <see cref="IConsumeMessages(Of M)"/> instances.
''' </summary>
''' <remarks>
''' The control flow when using <see cref="Connector(Of M)"/>s is usually inverted in comparism to standard async/await calls: only
''' when the messages sink is ready to process a message, the source is triggered to produce the next message.
''' With one-way <see cref="Channel(Of M)"/>s all the messages get buffered in the channel's unlimited queue, so the control flow is decoupled.
''' 
''' Processes can be spawned in arbitrary order: producers wait for their consumers to be ready which includes creation. But be aware of the evil 
''' <see cref="Channel(Of M)"/>, which buffers all producer's posts even if there is no consumer yet.
''' 
''' Producers <see cref="IProduceMessages(Of M)"/> are commonly instances of <see cref="Connector(Of M)"/> or <see cref="Channel(Of M)"/> (Source).
''' Consumers <see cref="IConsumeMessages(Of M)"/> are commonly instances of <see cref="Connector(Of M)"/> or <see cref="Channel(Of M)"/> (Sink).
''' Callables <see cref="ICanBeCalled(Of M, R)"/> are commonly instances of <see cref="Connector(Of M, R)"/>.
''' </remarks>
Public Class Supervisor
    Private list As New HashSet(Of Lightweight.Process)

    ''' <summary>
    ''' Processes messages with side effects only (Sink)
    ''' </summary>
    ''' <typeparam name="M">Message Type</typeparam>
    ''' <param name="producer">source of a stream of messages</param>
    ''' <param name="asyncprocessor">async processing of messages</param>
    Public Overloads Sub Spawn(of M As Class)(
            producer As IProduceMessages(Of M),
            asyncprocessor As Func(Of M, Task)
        )
        Dim proc = New Lightweight.ListenerProcess(Of M) With {.Processor = asyncprocessor, .Producer = producer}
        SyncLock list
            list.Add(proc)
            Dim handler =
                Async Function()
                    Do
                        Dim msg = Await producer.Receive()
                        If msg Is Nothing Then Exit do
                        Await asyncprocessor(msg)
                    Loop
                    SyncLock list
                        list.Remove(proc)
                    End SyncLock
                End Function
            proc.t = Task.Run(handler)
        End SyncLock
    End Sub

    ''' <summary>
    ''' Processes messages with side effects only (Sink)
    ''' </summary>
    ''' <typeparam name="M">Message Type</typeparam>
    ''' <param name="producer">source of a stream of messages</param>
    ''' <param name="syncprocessor">sync processing of messages</param>
    Public Overloads Sub Spawn(of M As Class)(
        producer As IProduceMessages(Of M),
        syncprocessor As Action(Of M)
    )
        Spawn(producer,
            Function(msg)
                syncprocessor(msg)
                Return Task.CompletedTask
            End Function)
    End Sub

    ''' <summary>
    ''' Generates messages on demand of a consumer (Source)
    ''' </summary>
    ''' <typeparam name="M">Message Type</typeparam>
    ''' <typeparam name="S">State Type for the generator function</typeparam>
    ''' <param name="consumer">the generated messages are posted to this consumer</param>
    ''' <param name="initialstate">Initial state for the generator function</param>
    ''' <param name="asyncgenerator">async generator function. Gets it's current state as 1st parameter and returns a Task for it's new state, second parameter is the sink for the generated messages</param>
    Public Overloads Sub Spawn(of M As Class, S)(
            consumer As IConsumeMessages(Of M),
            initialstate As S,
            asyncgenerator As Func(Of S, Action(Of M), Task(of S))
        )
        Dim proc = New Lightweight.TalkerProcess(Of M, S) With {.InitialState = initialstate, .Generator = asyncgenerator, .Consumer = consumer}
        SyncLock list
            list.Add(proc)
            Dim handler =
                Async Function()
                    Dim state = initialstate
                    Do
                        Dim post = Await consumer.Post()
                        If post Is Nothing Then Exit Do
                        state = Await asyncgenerator(state, post)
                    Loop
                    SyncLock list
                        list.Remove(proc)
                    End SyncLock
                End Function
            proc.t = Task.Run(handler)
        End SyncLock
    End Sub

    ''' <summary>
    ''' Generates messages on demand of a consumer (Source)
    ''' </summary>
    ''' <typeparam name="M">Message Type</typeparam>
    ''' <typeparam name="S">State Type for the generator function</typeparam>
    ''' <param name="consumer">the generated messages are posted to this consumer</param>
    ''' <param name="initialstate">Initial state for the generator function</param>
    ''' <param name="syncgenerator">sync generator function. Gets it's current state as 1st parameter and returns it's new state, second parameter is the sink for the generated messages</param>
    Public Overloads Sub Spawn(of M As Class, S)(
            consumer As IConsumeMessages(Of M),
            initialstate As S,
            syncgenerator As Func(Of S, Action(Of M), S)
        )
        Spawn(consumer, initialstate, Function(state, post) Task.FromResult(syncgenerator(state, post)))
    End Sub

    ''' <summary>
    ''' Transforms messages with a function (Pipeline)
    ''' </summary>
    ''' <typeparam name="M1">From Message Type</typeparam>
    ''' <typeparam name="M2">To Message Type</typeparam>
    ''' <param name="producer">source of a stream of messages</param>
    ''' <param name="consumer">the transformed messages will be posted to this consumer</param>
    ''' <param name="asynctransform">async transformation</param>
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

    ''' <summary>
    ''' Transforms messages with a function (Pipeline)
    ''' </summary>
    ''' <typeparam name="M1">From Message Type</typeparam>
    ''' <typeparam name="M2">To Message Type</typeparam>
    ''' <param name="producer">source of a stream of messages</param>
    ''' <param name="consumer">the transformed messages will be posted to this consumer</param>
    ''' <param name="synctransform">sync transformation</param>
    Public Overloads Sub Spawn(of M1 As Class, M2 As Class)(
            producer As IProduceMessages(Of M1),
            consumer As IConsumeMessages(Of M2),
            synctransform As Func(Of M1, M2)
        )
        Spawn(producer, consumer, Function(msg1) Task.FromResult(synctransform(msg1)))
    End Sub

    ''' <summary>
    ''' Transformation of a fixed source of messages to varying sinks (Call)
    ''' </summary>
    ''' <typeparam name="M">Source Message Type</typeparam>
    ''' <typeparam name="R">Result Message Type</typeparam>
    ''' <param name="callable">A producer which streams pairs of a message and a return sink. Usually a <see cref="Connector(Of M, R)"/>.</param>
    ''' <param name="asyncworker">async function which transforms an <typeparamref name="M"/> instance to an <typeparamref name="R"/> instance.</param>
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

    ''' <summary>
    ''' Transformation of a fixed source of messages to varying sinks (Call)
    ''' </summary>
    ''' <typeparam name="M">Source Message Type</typeparam>
    ''' <typeparam name="R">Result Message Type</typeparam>
    ''' <param name="callable">A producer which streams pairs of a message and a return sink. Usually a <see cref="Connector(Of M, R)"/>.</param>
    ''' <param name="syncworker">sync function which transforms an <typeparamref name="M"/> instance to an <typeparamref name="R"/> instance.</param>
    Public Overloads Sub Spawn(of M As Class, R As Class)(
        callable As ICanBeCalled(Of M, R),
        syncworker As Func(Of M, R)
    )
        Spawn(callable, Function(msg) Task.FromResult(syncworker(msg)))
    End Sub


    ''' <summary>
    ''' Waits for all processes in this supervisor to complete
    ''' </summary>
    Public Sub Join
        Dim ar As Task() = Nothing
        SyncLock list
            Dim qy = From p In list Select p.t
            ar = qy.ToArray
        End SyncLock
        #If DEBUG Then
        Console.WriteLine("Joining {0:n0} processes ...", ar.Length)
        #End If
        Task.WaitAll(ar)
        #If DEBUG Then
        Console.WriteLine("Joined.")
        #End If
    End Sub
End Class

