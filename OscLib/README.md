# OscLib
An attempt to create a full implementation of Open Sound Control protocol in C#/.Net/Mono/whatever, with an aim towards flexibility, efficiency, ease of use and as little GC pressure as feasibly possible.

Follows the [Open Sound Control 1.0 Specification](http://cnmat.org/OpenSoundControl/OSC-spec.html), for the most part.

Very much **WORK IN PROGRESS** - not suitable for anything nearing actual use just yet, and will most likely get broken and rearranged in randomest ways. Also in dire need of proper documentation.

What follows is a quick'n'dirty description of the architecture and functionality of this library - just to give a general idea how it works.

## How it works
The basic idea behind this library is to provide a series of components that implement various elements specified in OSC Protocol - allowing to send, receive and process packets of OSC data in a sort of 
conveyor-belt-like fashion. These components are more or less independent from each other; and can be linked, de-linked, overwritten and swapped for new, bespoke components as and when required by the task at hand.

### Packets, Messages, Bundles
Let's start from the most important bit - the OSC packets themselves. Depending on the context, this library will represent an OSC packet as one of no less than three types of structs: 

* OscPacket (naturally)
* OscMessage
* OscBundle

**OscPacket** represents an OSC packet in its purest form - as an OSC Protocol-valid sequence of bytes, contained inside a byte array. No more, no less.

**OscMessage** represents a deserialized OSC packet that contains just one OSC message: an address pattern (represented as an OscString struct) and a sequence of arguments (represented as an array of objects).

**OscBundle** represents a deserialized OSC packet that contains multiple elements - OSC messages and other bundles - all united under a single timetag (represented as an OscTimetag struct).

The (possibly somewhat naive) idea is, a packet of OSC data might require a variety of approaches, depending on whether it is outbound or inbound, whether it carries a message or a bundle, whether it is aimed at one
address or it requires pattern-matching to several, and so on. All this makes designing a single universal, sufficiently-performant, sufficiently-flexible data structure to represent an OSC packet quite challenging. 
Which is why there are three of them. 

### Converters
Instead, there is a class of objects explicitly concerned with converting OscMessage and OscBundle structs into OscPacket structs and vice versa - the (hopefully) aptly-named OscConverter class. The base class 
is abstract, specifying the methods for all conversion pathways; the implementations provide "rule sets" for data type serialization/deserialization, particular OSC type tags in use and so on. This is to allow  
for all the slightly-different versions of OSC Protocol currently in use by various applications and platforms, each with its own set of caveats and peculiarities. 

The idea is that deserialized structs - OscMessage and OscBundle - are OSC version-agnostic. They can be serialized into binary data-containing OscPacket by the specific implementation of an OscConverter as needed, and then 
that OscPacket can be sent out to its intended target. Furthermore, that same implementation of an OscConverter can be used to deserialize the OscPackets received from that same target - converting them into OSC version-agnostic
OscMessage and OscBundle structs.

### Links

