Imports System.Threading

Module Module1
    Private bag as New Concurrent.ConcurrentBag(Of Integer)
    Private n As Integer = 0
    Private nn As Integer = 0

    Sub Main
        MainCallable
        MainConnector
        MainBasicConnector
        MainChannel
    End Sub

    Sub MainCallable
        Dim sv = New Supervisor
        Dim con = New Connector(Of String, String)

        ' Caller
        Dim t = Task.Run(
            Async Function()
                For i = 0 To 9
                    Dim retCon = New Connector(Of String)
                    Console.WriteLine("await Call {0}", i)
                    Await con.Call(retCon, i.ToString)
                    Console.WriteLine("Await Receive {0}", i)
                    Dim ret = Await retCon.Receive()
                    Console.WriteLine("Result={0}", ret)
                Next
                ' shorter with extension method:
                For i = 100 To 109
                    Dim ret = Await con.Invoke(i.ToString)
                    Console.WriteLine("Result={0}", ret)
                next
            End function)

        Thread.Sleep(250)

        ' Worker
        sv.Spawn(con,
            Function(s)
                ' Note that this function gets called *after* the call to Receive() on the return channel.
                Dim ret = s & "+" & s
                Console.WriteLine("Converting {0} to {1}.", s, ret)
                Return ret
            End Function)

        Task.WaitAll(t)
        con.Close
        sv.Join
        con.CheckSaldo
        Console.ReadLine
    End Sub

    Sub MainConnector
        Dim sv = New Supervisor
        Dim con1 = New Connector(Of String)
        Dim con2 = New Connector(Of String)

        ' Consumer
        sv.Spawn(con2,
            Sub(s)
                Console.WriteLine("Received {0}", s)
                Thread.Sleep(250)
            End Sub)

        ' Transformer
        sv.Spawn(con1, con2,
            Function(s1)
                Return s1 & "+" & s1
            End Function)

        ' Producer
        sv.Spawn(con1, 0,
            Function(i, post)
                post(i.ToString)
                If i > 10 Then con1.Close
                Return i + 1
            End Function)

        sv.Join
        con1.CheckSaldo
        con2.CheckSaldo
        Console.ReadLine
    End Sub

    Sub MainBasicConnector()
        Dim con = New Connector(Of String)

        Dim producerwait = 1000
        Dim consumerwait = 100

        dim consumer = Task.Run(
            Async Function()
                For i = 0 To 400
                    Thread.Sleep(consumerwait)
                    Console.WriteLine("{0:n0}. Await Receive", i)
                    Dim s = Await con.Receive()
                    If s Is Nothing Then
                        Console.WriteLine("Receive returned null.")
                        Exit for
                    End If
                    Console.WriteLine("{0:n0}. Received {0}", i, s)
                Next
                Console.WriteLine("Receive loop ended. Closing Connector.")
                con.Close()
            End Function)


        dim producer = Task.Run(
            Async Function()
                For i = 0 To 4
                    Console.WriteLine("{0:n0}. Await Post", i)
                    Dim pst = Await con.Post()
                    If pst Is Nothing Then
                        Console.WriteLine("Post returned null.")
                        Exit For
                    End If
                    Thread.Sleep(producerwait)
                    Console.WriteLine("{0:n0}. Posting {0}", i)
                    pst(i.ToString)
                Next
                Console.WriteLine("Post loop ended. Closing Connector.")
                con.Close
            End Function)



        Task.WaitAll(consumer, producer)
        Console.WriteLine("Both tasks ended. Checking saldo ...")
        con.CheckSaldo()

        Console.Write("Press Return ... ")
        Console.ReadLine
    End Sub

    Sub MainChannel()
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
