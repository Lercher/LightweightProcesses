Imports System.Collections.Concurrent
Imports LightweightProcesses
''' <summary>
''' Lightweight one-way async pipe.
''' </summary>
''' <typeparam name="M">Message Type</typeparam>
''' <remarks>
''' It is forbidden to have more than one listener <see cref="Channel(Of M).Receive()"/> on the same channel. The class doesn't recognize this situation and thus can't enforce this constraint.
''' See https://github.com/dotnet/corefx/blob/master/src/System.Threading.Tasks.Dataflow/src/Blocks/BufferBlock.cs for an alternative
''' </remarks>
Public Class Channel(Of M As Class)
    Implements IProduceMessages(Of M)
    Implements IConsumeMessages(Of M)
    Implements IDisposable
    Private q As New Queue(Of M)(capacity:=0)
    Private receivedHead As TaskCompletionSource(Of M)
    Private closing As Boolean = False
    Private n As Integer = 0

    ''' <summary>
    ''' await a result of <typeparamref name="M"/>
    ''' </summary>
    ''' <returns>a message or null if the channel was explicitly closed with <see cref="Close()"/></returns>
    Public Function Receive() As Task(Of M) Implements IProduceMessages(Of M).Receive
        Threading.Interlocked.Decrement(n)
        SyncLock q
            If receivedHead Is Nothing OrElse receivedHead.Task.IsCompleted Then
                If q.Count = 0 Then
                    receivedHead = New TaskCompletionSource(Of M)
                    Return receivedHead.Task
                Else
                    Return Task.FromResult(q.Dequeue)
                End If
            Else
                return receivedHead.Task
            End If
        End SyncLock
    End Function

    Private Sub InternalPost(ByVal msg As M)
        SyncLock q
            If closing AndAlso msg Is Nothing Then Return 'double close is ok
            If closing Then Throw New ArgumentException("cannot post to closed " & NameOf(Channel(Of M)), NameOf(msg))
            If msg Is Nothing Then closing = True

            If receivedHead Is Nothing OrElse receivedHead.Task.IsCompleted Then
                q.Enqueue(msg)
            Else
                receivedHead.SetResult(msg)
            End If
        End SyncLock
    End Sub


    ''' <summary>
    ''' post a message of type <typeparamref name="M"/> in a non blocking way
    ''' </summary>
    ''' <param name="msg">the message which can be async <see cref="Receive()"/>ed. Must not be null.</param>
    Public Sub Post(msg As M)
        If msg Is Nothing Then Throw New ArgumentNullException("the posted message cannot be null", NameOf(msg))
        Threading.Interlocked.Increment(n)
        InternalPost(msg)
    End Sub

    Public Function Post() As Task(Of Action(Of M)) Implements IConsumeMessages(Of M).Post
        Return Task.FromResult(Of Action(Of M))(AddressOf Post)
    End Function

    ''' <summary>
    ''' close the chanel so that the receiver process <see cref="Supervisor"/> terminates
    ''' </summary>
    Public Sub Close() Implements IDisposable.Dispose
        InternalPost(Nothing)
    End Sub

    <Conditional("DEBUG")>
    Public Sub CheckSaldo
        Dim nn = Threading.Interlocked.Increment(n)
        If nn = 0 Then Return
        Console.WriteLine("Saldo {0:n0}", nn)
    End Sub

End Class
