Namespace Lightweight
    Public Class ListenerProcess(Of M As Class)
        Inherits Process
        Public Property Producer As IProduceMessages(Of M)
        Public Property Processor As Action(Of M)
    End Class
End Namespace
