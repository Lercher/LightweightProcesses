Namespace Lightweight
    Public Class CallableProcess(of M as Class, R As Class)
        Inherits Process

        Public Property Callable As ICanBeCalled(Of M, R)
        Public Property Worker As Func(Of M, Task(Of R))
    End Class
End Namespace
