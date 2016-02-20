Public Interface IProduceMessages(Of M As Class)
    Function Receive() As Task(Of M)
End Interface
'Public Interface I