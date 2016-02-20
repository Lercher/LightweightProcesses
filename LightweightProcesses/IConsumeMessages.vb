Public Interface IConsumeMessages(Of M As Class)
    Inherits IDisposable

    Function Post() As Task(Of Action(Of M))
End Interface