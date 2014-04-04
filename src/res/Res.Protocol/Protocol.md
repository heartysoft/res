            
*Append*
Append: Comes from client to router socket.
[Return Address]
Empty
Protocol - "Res01"
Command - "AppendCommit" - "AC"
RequestId
Context
Stream
ExpectedVersion
====================
One per event:
[
EventId
Timestamp
TypeKey
Headers
Body
]
====================

*Response Ready*
ResponseReady: Comes from Sink to router socket.
[Address] - single frame...comes from internal sink
Protocol
Command - "ResponseReady" - "RR"
[Sender]
Empty
RequestId
Result -> Success [Empty frame denotes success. Error will have serialized error details as the frame.]
       -> Error 
CommitId


*CommitResult*
CommitResult: Sent back to client after a commit
[Sender]
Empty
Protocol
Command - "CommitResult" - "CR"
RequestId
Result -> Success [Empty frame denotes success. Error will have serialized error details as the frame.]
       -> Error 
CommitId


