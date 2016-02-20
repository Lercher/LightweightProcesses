Namespace Lightweight
    Public Class TalkerProcess(Of M As Class, S)
        Inherits Process

        Public Property Consumer As IConsumeMessages(Of M)
        Public Property Generator As Func(Of S, Action(Of M), Task(Of S))
        Public Property InitialState As S
    End Class
End Namespace
