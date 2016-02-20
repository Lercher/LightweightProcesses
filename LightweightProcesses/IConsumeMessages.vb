Public Interface IConsumeMessages(Of M As Class)
    Function Post() As Task(Of Action(Of M))
End Interface