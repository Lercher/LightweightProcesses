Imports LightweightProcesses

Public Class Connector(Of M As Class)
    Implements IProduceMessages(Of M)
    Implements IConsumeMessages(Of M)

    Private con As New Connection
    Private closing As Boolean = False
    Private n As Integer = 0

    Public Function Receive() As Task(Of M) Implements IProduceMessages(Of M).Receive
        Threading.Interlocked.Decrement(n)
        SyncLock Me
            If closing Then Return Task.FromResult(Of M)(Nothing)
            If con.rcv.Task.IsCompleted Then con = New Connection
            Dim thiscon = con
            thiscon.pst.SetResult(
                Sub(msg)
                    If msg Is Nothing Then Throw New ArgumentNullException("the posted message cannot be null", NameOf(msg))
                    Threading.Interlocked.Increment(n)
                    SyncLock Me
                        con = New Connection
                    End SyncLock
                    Console.WriteLine("Setresult Task ID {0}", thiscon.rcv.Task.Id)
                    thiscon.rcv.SetResult(msg)
                End Sub
            )
            Console.WriteLine("Receive await Task ID {0}", thiscon.rcv.Task.Id)
            Return thiscon.rcv.Task
        End SyncLock
    End Function

    Public Function Post() As Task(Of Action(Of M)) Implements IConsumeMessages(Of M).Post
        SyncLock Me
            If closing Then Return Task.FromResult(Of Action(Of M))(Nothing)
            Return con.pst.Task
        End SyncLock
    End Function

    Public Sub Close()
        SyncLock Me
            closing = True
            If Not con.rcv.Task.IsCompleted Then con.rcv.SetResult(Nothing)
            If not con.pst.Task.IsCompleted Then con.pst.SetResult(Nothing)
        End SyncLock
    End Sub

    <Conditional("DEBUG")>
    Public Sub CheckSaldo
        Dim nn = Threading.Interlocked.Add(n, 0)
        If nn = 0 Then Return
        Console.WriteLine("Saldo {0:n0}", nn)
    End Sub


    Private Class Connection
        Public rcv As New TaskCompletionSource(Of M)
        Public pst As New TaskCompletionSource(Of Action(Of M))
    End Class
End Class
