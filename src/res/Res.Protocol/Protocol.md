            
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

##CommitResult
Sent back to client after a commit

1. [Routing Frames]
    - Used for routing the message back.
2. Empty
    - Denotes end of routing frame.
3. <code>**Protocol** string </code>
    - Client protocol.
    - Currently, only "Res01"
4. <code>**RequestId** string </code>
    - Request correlation id.
5. <code>**Command** string</code>
    - CommitResult ["CR"]
6. <code>**Result** string or empty </code>
    - Empty if successful. Error code otherwise. [TODO: Description of error codes.]
7. <code>**Error** string</code>
    - Serialised error details | Empty in case of success.
8. <code>**CommitId** Guid</code>
    - The commit id, if commit is successful.

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




