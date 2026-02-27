# RegexMaker

## What does it do?
RegexMaker produces a visual programming drag/drop screen which allows setting up nodes which take in patterns and produce patterns.  
For instance a Repeat node
will take a pattern and output a pattern which matches repeats of that pattern.  Each node has a parameters
screen which allows setting parameters for that node.  For instance the Repeat node has a parameter for
min and max times to repeat the pattern.  There's also a checkbox for max to be infinity and a checkbox to make it a lazy search.  The output pattern of
one node can be connected to the input of another node, allowing for complex patterns to be built up visually.  
The final output will be a regex pattern which can be used in code or elsewhere.  Whenever a node is selected it's current output regex is
displayed at the bottom.

Currently it's aesthetically a bit rough but has most of the elements in place to actually be functional. Nodes are dragged from a toolbox on the left.
Dragging from port to port connects nodes.  Clicking on a node selects it, shows the parameters for that node in the right pane and it's current output 
regex at the bottom.  The parameters can be edited and the output regex will update as you edit them.  The nodes can be dragged and the connections 
will update accordingly.  You can increase the number of ports for the Concatenate and AnyOf node in their paramaeter pane.

It also has a way to produce random strings based on the nodes.  Not precisely sure how this might be useful but it seemed
easy enough to do that I wanted to get it in early so it will be easy to implement in each node type.  I suppose it might
be handy to generate test cases ala Bogus.  Perhaps in conjunction with bogus.  It will require some sort of serializer/deserializer
so people can use it in their code.  Might be fun for producing random nonsense also.

I still have to implement a lot of nodes, serialization, a copy button to copy the regex to the clipboard and a way to test the regex against
arbitrary input as well as a way to put out random matching strings.

## Tools and Products Used

* [NuGet](https://www.nuget.org/)
* [GitHub](https://github.com/)
* [Avalonia](https://avaloniaui.net/)


## Versions & Release Notes

version 0.1.0:
 * Initial Checkin

version 0.2.0:
 * Basic functionality with some nodes implemented.
