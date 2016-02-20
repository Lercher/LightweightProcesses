Public Interface IProduceMessages(Of M As Class)
    Inherits IDisposable

    Function Receive() As Task(Of M)
End Interface
