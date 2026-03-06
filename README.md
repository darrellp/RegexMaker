# RegexMaker
![Screenshot](Images/RegexMaker%20Screenshot.png)

## What does it do?
RegexMaker produces a visual programming drag/drop screen which allows setting up nodes which take in patterns and produce patterns.  
For instance a Repeat node
will take a pattern and output a pattern which matches repeats of that pattern.  Each node has a parameters
screen which allows setting parameters for that node.  For instance the Repeat node has a parameter for
min and max times to repeat the pattern.  There's also a checkbox for max to be infinity and a checkbox to make it a lazy search.  The output pattern of
one node can be connected to the input of another node, allowing for complex patterns to be built up visually.  
The final output will be a regex pattern which can be used in code or elsewhere.  Whenever a node is selected it's current output regex is
displayed at the bottom.

It also allows you to enter text to test the pattern on and highlights any matches.  Clicking on any match shows the position of the match and any
captures to the right of the text.

It now allows you to press a "Code" button and will produce code which will produce the regular expression using the Stex string
extensions.  Stex is a way to make putting together regular expressions in code MUCH easier.  It's a set of string extension methods
and can be just downloaded from the StringExtensions.cs file in RegexMaker project.  An example produced from Regexmaker for
a somewhat complicated phone number pattern (shown in the screenshot above) is
```csharp
var __Repeat__0 = "1-".Rep(0, 1);
var __CharClass__1 = Stex.Digit;
var __Repeat__2 = __CharClass__1.Rep(3, 3);
var AreaCode = __Repeat__2.Named("AreaCode");
var __Concatenate__3 = Stex.Cat(__Repeat__0, AreaCode);
var __Repeat__4 = Stex.Cat(__Concatenate__3, "-").Rep(0, 1);
var Prefix = __Repeat__2.Named("Prefix");
var __Concatenate__5 = Stex.Cat(Prefix, "-");
var Xchg = __CharClass__1.Rep(4, 4).Named("Xchg");
var __Repeat__6 = Stex.White.Rep(1, -1);
var __AnyWordFrom__7 = Stex.AnyOf("Ext", "X", "x", "ext");
var Ext = __CharClass__1.Rep(1, -1).Named("Ext");
var __Repeat__8 = Stex.Cat(__Repeat__6, __AnyWordFrom__7, Ext).Rep(0, 1);
var result = Stex.Cat(__Repeat__4, __Concatenate__5, Xchg, __Repeat__8);
```
This can be easily modified in a few minutes using an editor to look like
```csharp
var optOnePrefix = "1-".Rep(0, 1);
var digit = Stex.Digit;
var digitTriplet = digit.Rep(3, 3);
var AreaCode = digitTriplet.Named("AreaCode");
var extAreaCode = Stex.Cat(optOnePrefix, AreaCode);
var optAreaCode = Stex.Cat(extAreaCode, "-").Rep(0, 1);
var Prefix = digitTriplet.Named("Prefix");
var prefixDash = Stex.Cat(Prefix, "-");
var Xchg = digit.Rep(4, 4).Named("Xchg");
var extSpaces = Stex.White.Rep(1, -1);
var extPrefix = Stex.AnyOf("Ext", "X", "x", "ext");
var Ext = digit.Rep(1, -1).Named("Ext");
var optExt = Stex.Cat(extSpaces, extPrefix, Ext).Rep(0, 1);
var result = Stex.Cat(optAreaCode, prefixDash, Xchg, optExt);
```
Personally, I think this is about as readable as you're going to find such a complex Regex whose actual regex string is

(?:(?:1-)?(?<AreaCode>\d{3})-)?(?<Prefix>\d{3})-(?<Xchg>\d{4})(?:\s+(?:(?:Ext)|(?:X)|(?:x)|(?:ext))(?<Ext>\d+))?

I find the code far easier to deal with.

Currently it still has some aesthetic rough edges. Nodes are dragged from a toolbox on the left.
Dragging from port to port connects nodes.  Clicking on a node selects it, shows the parameters for that node in the right pane and it's current output 
regex at the bottom. 
The parameters can be edited and the output regex will update as you edit them.  You can increase the number of ports for the 
Concatenate and AnyOf node in their parameter pane.


It also has a way to produce random strings based on the nodes.  Not precisely sure how this might be useful but it seemed
easy enough to do that I wanted to get it in early so it will be easy to implement in each node type.  I suppose it might
be handy to generate test cases ala Bogus.  Perhaps in conjunction with bogus.  It will require some sort of serializer/deserializer
so people can use it in their code.  Might be fun for producing random nonsense also. Save/Load will save and load the network to a file.
The CR/LF button is a toggle which, when pressed, causes line breaks to be represented by "CR/LF".  Otherwise they are represented by
a single '\n'.  The WS key will display white space in the sample text window.

I still have to implement a lot of nodes, serialization, a copy button to copy the regex to the clipboard and a way to test the regex against
arbitrary input as well as a way to put out random matching strings.

Still to come: 
* Search and replace
* Random strings which match the pattern
* More Regex nodes
* Variable naming for the code output only\
Currently named captures will produce a variable name with the same name as the capture but if you want something to have a name in the code output but don't necessarily
want a capture - well, that's what this is about.
* Arbitrary unset variable names in the output code\
Idea here is to allow for variables used in the pattern which can be set to values outside the pattern in code for customized patterns.  They
will have a default value which will be used in the regex pattern produced.
* A few more aesthetic tweaks

The project currently builds fine for a desktop windows app and Web AL.  I don't have a mac which is needed to build a mac version but I
assume that with the excellent cross platform Avalonia it will work fine.  Also on Linux.  Doesn't really seem like a great fit for any
mobile apps.

## Tools and Products Used

* [NuGet](https://www.nuget.org/)
* [GitHub](https://github.com/)
* [Avalonia](https://avaloniaui.net/)


## Versions & Release Notes

version 0.1.0:
 * Initial Checkin

version 0.2.0:
 * Basic functionality with some nodes implemented.
