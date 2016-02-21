Imports LightweightProcesses

Public Class Connector(Of M As Class, R As Class)
    Implements ICanBeCalled(Of M, R)

    Private producer As New Connector(Of Tuple(Of IConsumeMessages(Of R), M))

    Public Async Function [Call](ret As IConsumeMessages(Of R), msg As M) As Task Implements ICanBeCalled(Of M, R).Call
        Dim post = await producer.Post
        If post Is Nothing Then Return
        post(Tuple.Create(ret, msg))
    End Function

    Public Function Receive()  As Task(Of Tuple(Of IConsumeMessages(Of R), M)) Implements IProduceMessages(Of Tuple(Of IConsumeMessages(Of R), M)).Receive
        Return producer.Receive
    End Function

    Public Sub Close() Implements IDisposable.Dispose
        producer.Close
    End Sub

    <Conditional("DEBUG")>
    Public Sub CheckSaldo
        producer.CheckSaldo
    End Sub

End Class
