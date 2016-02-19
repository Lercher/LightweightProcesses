Imports System.Threading

Module Module1
    Private bag as New Concurrent.ConcurrentBag(Of Integer)
    Private n As Integer = 0
    Private nn As Integer = 0

    Sub Main()
        Dim sv = New Supervisor
        Dim ch(0 To 9999) as Channel(Of String)

        For c = 0 To ch.Length - 1
            ch(c) = New Channel(Of String)
            ch(c).Post("init") : nn += 1
        Next
        Const MSGCOUNT As Integer = 8
        For c = 0 To ch.Length - 1
            For i = 0 To (c Mod MSGCOUNT)
                ch(c).Post(i.ToString) : nn += 1
            Next
        Next
        For c = 0 To ch.Length - 1
            sv.Spawn(ch(c), AddressOf Record)
        Next
        For c = 0 To ch.Length - 1
            For i = (c Mod MSGCOUNT) To MSGCOUNT
                ch(c).Post((i + 1000).ToString) : nn += 1
            Next
        Next
        For c = 0 To ch.Length - 1
            ch(c).Close()
        Next
        sv.Join
        For c = 0 To ch.Length - 1
            ch(c).CheckSaldo
        Next
        Console.WriteLine : Console.WriteLine
        Console.WriteLine("bag size: {0:n0} Threads, {1:n0} calls, should be {2:n0}, {3:n0} processes", bag.Count, n, nn, ch.Length)
        For each i In From ii In bag Order by ii Distinct
            Console.Write("{0} ", i)
        Next
        Console.ReadLine
    End Sub

    Sub Record(s As string)
        Interlocked.Increment(n)
        Dim tid = Thread.CurrentThread.ManagedThreadId
        if not bag.Contains(tid) then bag.Add(tid)
        'Console.Write("M{0}-T{1}-ch{2} ", s, tid, cc)
    End Sub

End Module
