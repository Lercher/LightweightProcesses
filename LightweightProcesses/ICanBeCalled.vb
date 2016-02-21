Public Interface ICanBeCalled(Of M As Class, R As Class)
    Inherits IProduceMessages(Of Tuple(Of IConsumeMessages(Of R), M))

    Function [Call](ret As IConsumeMessages(Of R), msg As M) As Task
    'ReadOnly Property [return] As 
End Interface
