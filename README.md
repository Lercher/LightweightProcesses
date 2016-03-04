# Lightweight Processes


## Introduction

An Experimental lib to check how Lightweight processes like in Erlang 
could be done using async/await and the TPL.

And the answer ist yes. I ran about 100k processes in parallel on my box, 
consuming about 10 or so TPL threads communicating with each other and some 20 bytes of RAM per process.

Using the Connector class (something like a channel of length 0..1)
I can have a infinte source of data on the producer side, which is
only asked to produce it, when the consumer on the other side
of the Connector is ready to process this data.


## Entry points

* Supervisor - to spawn new and join lightweight processes. Todo: Do the supervision.
* Channel(M) - awaitable communication channel with infinite length.
* Connector(M) - awaitable communication channel with length one.
* Connector(M,R) - awaitable bidirectional communication channel of length one with awaitable return of type R

Note: The difference of an Connector<M,R> "call" compared to a standard await/async call is, that you have to await the ability to post the request message data to the Connector which is not resolved unless someone starts to await the return message.

Note 2: a second difference is, that it is usually no problem to spawn consumer and producer processes in any desired order and (this is currently a TODO) to respawn failed consumer/producer process at the cost of a lost message with a standard supervision strategy.


## Documentation

Currently none, however ...

Please have a look at the Supervisor.vb source file: There is some xml documentation
for .Net intellisense. 

In Module1.vb there are some sample use cases.
