
Public Module Extensions
    <Runtime.CompilerServices.Extension>
    Public Async Function Invoke(Of M As Class, R As Class)(this As ICanBeCalled(Of M, R), msg As M) As Task(Of R)
        Dim retCon = New Connector(Of R)
        Await this.Call(retCon, msg)
        Dim ret = Await retCon.Receive()
        Return ret
    End Function
End Module