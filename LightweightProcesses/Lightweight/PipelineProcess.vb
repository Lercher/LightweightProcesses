Namespace Lightweight
    Friend Class PipelineProcess(Of M1 As Class, M2 As Class)
        Inherits Process

        Public Property Producer As IProduceMessages(Of M1)
        Public Property Consumer As IConsumeMessages(Of M2)
        Public Property Transform As Func(Of M1, Task(of M2))
    End Class
End Namespace
