# Lightweight Processes

An Experimental lib to check how Lightweight processes like Erlang 
could be done using async/await and the TPL.

And the answer ist yes. I ran about 100k processes in parallel on my box, 
consuming about 10 or so TPL threads communicating with each other.

Using the Connector class (something like a channel of length 0..1)
I can have a infinte source of data on the producer side, which is
only asked to produce it, when the consumer on the other side
of the Connector is ready to process this data.
