# RegexMaker

## What does it do?
Not much at the moment - a work in progress.  The plan is to create a sort of visual programming drag/drop
screen which will allow setting up nodes which take in patterns and produce patterns.  For instance a Repeat node
will take a pattern and output a pattern which looks for repeats of that pattern.  Each node will have a parameters
screen which will allow setting parameters for that node.  For instance the Repeat node will have a parameter for
min and max times to repeat the pattern (negative value on max will mean infinity - the default).  The output pattern of
one node can be connected to the input of another node, allowing for complex patterns to be built up visually.  
The final output will be a regex pattern which can be used in code or elsewhere.  I only started on this last night
so quite a ways to go.  Right now there is a node class and some example nodes for things like repeat, string,
concatenate, etc..  The drag canvas is a custom control which allows for dragging the nodes around on the screen. 
The nodes are just miscellaneous UIElements.  It's adapted for Avalonia from Josh Smith's DragCanvas control.  Whether
I'll even need it or not is TBD.

It also has a way to produce random strings based on the nodes.  Not precisely sure how this might be useful but it seemed
easy enough to do that I wanted to get it in early so it will be easy to implement in each node type.  I suppose it might
be handy to generate test cases ala Bogus.  Perhaps in conjunction with bogus.  It will require some sort of serializer/deserializer
so people can use it in their code.  Might be fun for producing random nonsense also.

## Tools and Products Used

* [NuGet](https://www.nuget.org/)
* [GitHub](https://github.com/)
* [Avalonia](https://avaloniaui.net/)


## Versions & Release Notes

version 0.1.0:
 * Initial Checkin
