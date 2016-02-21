Namespace Lightweight
    Friend Class ListenerProcess(Of M As Class)
        Inherits Process
        Public Property Producer As IProduceMessages(Of M)
        Public Property Processor As Func(Of M, Task)
    End Class
End Namespace
