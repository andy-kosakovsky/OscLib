# OscLib
An attempt to create a full implementation of Open Sound Control protocol in C#/.Net/Mono/whatever, with an aim towards flexibility, efficiency, ease of use and as little GC pressure as feasibly possible.

Follows the [Open Sound Control 1.0 Specification](http://cnmat.org/OpenSoundControl/OSC-spec.html), for the most part.

Very much **WORK IN PROGRESS** - not suitable for anything nearing actual use just yet, and will most likely get broken and rearranged in randomest ways. Also in dire need of proper documentation.

What follows is a quick'n'dirty description of the architecture and functionality of this library - just to give a general idea how it works.

## How it works
The basic idea behind this library is to provide a series of components for processing OSC packets in a sort of conveyor-belt-like fashion. These components can be linked, de-linked, overwritten and swapped for
new, bespoke components as and when required by the task at hand.

### Packets, Messages, Bundles
Let's start from the most important bit - the OSC packets themselves. Depending on the context, this library will represent an OSC packet as one of no less than three types of structs: 

* OscPacket (naturally)
* OscMessage
* OscBundle

**OscPacket** represents an OSC packet in its purest form - as an OSC Protocol-valid sequence of bytes, contained inside a byte array. No more, no less.

**OscMessage** represents a deserialized OSC packet that contains just one OSC message: an address pattern (represented as an OscString struct) and a sequence of arguments (represented as an array of objects).

**OscBundle** represents a deserialized OSC packet that contains multiple elements - OSC messages and other bundles - all united under a single timetag (represented as an OscTimetag struct).

The (possibly somewhat naive) idea is, all these representations require quite different approaches, which makes uniting them all into one universal data structure quite challenging. Which is why they aren't. 

### Converters and Links
Instead, there is a class of objects explicitly concerned with converting OscMessage and OscBundle structs into OscPacket structs and vice versa - the (hopefully) aptly-named OscConverter class.
