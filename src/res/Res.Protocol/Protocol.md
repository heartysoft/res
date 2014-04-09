            
##1. Append

Comes from client to router socket.

###Message Format
1. [Return Address] : 
    - Routing frames. 
    - An ordered array of frames, as requests may be brokered.
2. Empty
    - Denotes end of routing frames.
3. Protocol 
    - "Res01"
    - Request protocol. Currently, onlt Res01 is supported.
4. Command 
    - AppendCommit ["AC"]
5. RequestId
    - Request Identifier. This will be passed back as part of response enabling client to correlate.
6. Context
    - Target context (as the event storage is multi-tennant).
7. Stream
    - Target stream of context
8. ExpectedVersion
    - Expected initial version for the stream in the context.
    - If positive, a mismatch will cause a concurrency violation.
    - If 1, must be a new stream.
    - If -1, concurrency check will be bypassed. [Currently not implemented].
9. Event Count
    - Number of events.
10. One per event:
    1. EventId
        - Event Identifier. Guid as byte[].
    2. Timestamp
        - Time of the event.
    3. TypeKey
        - String representing the type of the event.
    4. Headers
        - String representing headers. Upto client to decide how to use this.
    5. Body
        - String representing body. Upto client to decide how to use this.


#Result Format In General
1. [Routing Frames]
2. Empty
	- Signals end of routing frame
3. <code>**Protocol** string </code>
    - Client protocol.
    - Currently, only "Res01"
4. <code>**RequestId** string </code>
    - Request correlation id.
5. <code>**Command** string</code>
	- Message type from ResCommands representing the response.
6. [Details]

	- In case of any error, Command will be ResCommands.Error ["ER"], and Details will be two frames:
		* Error code. An integer code representing the error.
		* Serialised error message
	- In case of success, Command will represent the response, and Details will be zero or more frames depending on the command.
	- The description of each type of response contains only the Command and Details in the following sections, as the header and error formats are the same for all responses.

###Error Codes
- <code>Empty:</code> Success
- <code>-1:</code> Unexpected error
- <code>1:</code> Malformed message
- <code>2:</code> Unsupported protocol
- <code>3:</code> Unsupported Command
- <code>4:</code> Storage Writer Busy
- <code>5:</code> Storage Writer Timeout
- <code>6:</code> Concurrency Exception
- <code>7:</code> Event Storage Exception
- <code>8:</code> Event Not Found
- <code>9:</code> Storage Reader Timeout
- <code>10:</code> Storage Reader Busy


##CommitResult
Sent back to client after a commit

1. <code>**Command** string</code>
    - CommitResult ["CR"]
2. <code>**CommitId** Guid</code>
    - The commit id, if commit is successful.


#Internal events. May remove if we choose to go in mem queue + polling on socket thread, like we are doing for the client. NOT for external consumption.

##Response Ready

Internal event for queuing results. Comes from Sink to router socket.

1. [Address] 
    - single frame...comes from internal sink
2. Empty
	- Denote end of routing frames
3. Protocol
    - "Res01"
4. Command 
    - ResponseReady ["RR"]
5. [Routing frames]
    - For sending back to client.
6. Empty
   - Denotes end of routing frames.
7. RequestId
    - Request correlation id.
8. Result 
    - Empty if successful. Error code otherwise. [TODO: Description of error codes.]
9. Error  
    - Serialised error details | Empty in case of success.
10. CommitId
    - The commit id, if commit is successful.




