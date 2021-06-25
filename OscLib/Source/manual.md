# (Future) OscLib manual
What follows is a quick'n'dirty description of aims, architecture and functionality of this library - just to give a general idea about how it works, really

## Disclaimer
This library is in mid- to late-alpha stages - while most of functionality is more or less complete, it's all still a tad undercooked; parts of it are still held together by tape, and not all of it is tested thoroughly enough. There's also probably better ways to do at least some of the things it does. It's *functional*, but results may vary. Also, there will probably be changes that break everything.

## What's it for
This library kind of accidentally grew from a personal project that involved trying to make Unity3D and SuperCollider talk to each other. 

The aim is to build a reliable, flexible tool that is capable of sending, receiving and reacting to loads and loads of OSC packets quickly enough - preferably without causing too much strain on the garbage collector, and without eating too much processing power. The plan is for it to be usable with anything that supports Mono/.Net - Unity3D and Godot first and foremost, but any other Mono application should be fair game too.   

## How it works
The basic idea behind this library is to provide a series of components, each implementing a part of OSC Protocol - allowing to send, receive and process packets of OSC data in a sort of conveyor-belt-like fashion. These components are more or less independent from each other; they can be linked, de-linked, overwritten and swapped for new, bespoke components as and when required by the task at hand.

### Packets, Messages, Bundles
Let's start from the most important bit - the OSC packets themselves. Depending on the context, this library will represent an OSC packet as one of no less than three types of structs: 

* OscPacket (naturally)
* OscMessage
* OscBundle

**OscPacket** represents an OSC packet in its purest form - as an OSC Protocol-valid sequence of bytes, contained inside a byte array. No more, no less.

**OscMessage** represents a deserialized OSC packet that contains just one OSC message: an address pattern (represented as an OscString struct) and a sequence of arguments (represented as an array of objects).

**OscBundle** represents a deserialized OSC packet that contains multiple elements - OSC messages and other bundles - all united under a single timetag (represented as an OscTimetag struct).

The (possibly somewhat naive) idea is, processing a packet of OSC data might require a variety of approaches, depending on multiple factors: whether it is outbound or inbound, whether it carries a single message or a bundle, whether it needs to be sent out to just one end point or several disparate targets that might not implement OSC in exactly the same way, whether it is aimed at one OSC Method within an address space or it requires pattern-matching to several, and so on and so forth. All this makes designing a single universal, sufficiently-performant, sufficiently-flexible data structure to represent an OSC packet quite a challenging endeavour. Which is why there are three of them in this library. 

### Converters
Instead, there's a component that is explicitly concerned with converting OscMessage and OscBundle structs into OscPacket structs and vice versa - the (hopefully) aptly-named **OscConverter**. The base class is abstract, specifying the methods for all conversion pathways; the implementations provide the "rule sets" - as in, how the various data types are handled, the particular OSC type tags in use by the particular protocol implementations, and so on. This is to allow for all the slightly-different versions of OSC Protocol currently in use by various applications and platforms, each with its own set of caveats and peculiarities. It also allows using several different versions of OSC Protocol at once, if needed for whatever reason. 

The idea is that two structs representing deserialized OSC packets - OscMessages and OscBundles - are *OSC Protocol version-agnostic*. They can be serialized into binary data-containing OscPackets by a specific implementation of OscConverter, and then these OscPackets can be sent out to their intended target. Furthermore, the same OscConverter implementation can be used to deserialize the packets received from that same target - converting them into version-agnostic OscMessage and OscBundle structs.

### Links
The **OscLink** component is the one actually concerned with both sending and receiving OSC packets, utilising the internal UdpClient object. It can be set up in two ways: either trading OscPackets with only one specified end point, or being open to communications with any and all end points. Upon receiving OSC data, the OscLink instance puts it inside an OscPacket struct and passes it along by invoking the corresponding event.

### Receivers
The **OscReceiver** component connects to a single OscLink and does two things. One: it deserializes the OscPackets passed to it by the connected OscLink into either OscMessages or OscBundles (utilising the specified OscConverter) and passes them further down the line by invoking corresponding events. Two: if configured to do so, OscReceiver will delay the incoming OscBundles, holding off passing them down the line until the moment in time specified by their timetags.

### Address Spaces
The **OscAddressSpace component** implements OSC Address Space as a tree of containers and methods, represented by instances of **OscContainer** and **OscMethod** classes. One OscAddressSpace can be connected to one or multiple OscReceivers, and it will dispatch all OSC messages and bundles passed to it to corresponding OscMethods, according to their address patterns. Currently, OSC Protocol V. 1.0 pattern-matching rules are fully implemented. 

When the encompassing OscAddressSpace dispatches a message to an instance of OscMethod, that instance will invoke the event attached to it, passing the message arguments to all subscribed event handlers. This allows subscribing multiple event handlers to one OscMethod, should it be needed.

## Examples

### Two OscLinks, one localhost
later

### Talking to SuperCollider
later

### Talking to PureData
later

### Across the ~~Universe~~ World Wide Web  
later
