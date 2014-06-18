# Message Formats
Request and Response message formats follow the same preambles as Protocol.md.

Notably, requests start with:

1. [Return Address]
2. Empty Frame
3. Protocol
4. Command
5. RequestId

Responses are of the form:

1. [Routing Frames]
2. Empty Frame
3. Protocol
4. Request
5. Command
6. [Payload]

-------------------------------

# Outline
Clients are expected to make a Subscribe request when starting up.
Subscribe will declare a new queue with start time if it does not
already exist. The response will be the first batch of events for
the subscriber.

Once a batch of events are processed, the client will send a
Proceed request. This will progress the subscription and return
the next batch of events, if any.

In case there are no new events, the client will wait for a specified
timeout and reissue the Proceed request.

At this time, subscriptions are poll based. This may change in the
future. (Durable subscription are likely to be maintained even if
real time push based subscriptions are introduced.)

# 1. Subscribe

###Message Format:
<code>**Protocol:**</code> "Res01"
<code>**Command:**</code>  SubscribeQueue [SQ]
<code>**UtcNow** datetime</code>
<code>**Payload:**</code>

1. <code>**QueueId** -string</code>
1. <code>**SubscriberId** -string</code>
1. <code>**Context** -string</code>
1. <code>**Filter** -string</code>
1. <code>**StartTime** -datetime</code>
1. <code>**Count** -int</code>
    Batch size.
1. <code>**AllocationTimeInMilliseconds** -int</code>
    TTL for reservation. Reservation is freed up after this time.

# 2. Subscribe Response
<code>**Protocol:**</code> "Res01"
<code>**Command:**</code>  QueuedEvents [QE]
<code>**Payload:**</code>

1. <code>**QueueId**</code>
1. <code>**SubscriberId**</code>
1. <code>**UtcNow** datetime</code>
1. <code>**StartMarker** int</code> ? Maybe string is better?
1. <code>**EndMarker** int</code> ? Maybe string is better?
1. <code>**Count** int </code>
	- Number of events.
1. One per event:
	1. <code>**EventId** guid</code>
	2. <code>**StreamId** string</code>
	3. <code>**Context** string</code>
	4. <code>**Sequence** long</code>
	5. <code>**Timestamp** datetime</code>
	6. <code>**Type tag** string</code>
	7. <code>**Headers** string</code>
	8. <code>**Body** string</code>

# 3. Acknowledge Queue
<code>**Protocol:**</code> "Res01"
<code>**Command:**</code>  QueuedEvents [AQ]
<code>**Payload:**</code>

1. <code>**QueueId**</code>
1. <code>**SubscriberId**</code>
1. <code>**StartMarker**</code>
1. <code>**EndMarker**</code>
1. <code>**Count** </code>
1. <code>**AllocationTimeInMilliseconds** int</code>
	-  -1 to not allocate events.

# 4. Acknowledge Queue response:
Same as Subscribe Response.

# 5. Reset Queue:
1. <code>**QueueId**</code>
1. <code>**Reset To** datetime</code>
1. <code>**UtcNow** datetime</code>

