
# Magic Signals ASP.NET Core

Magic signals is a _"Super Signals"_ implementation for .Net Core, allowing you to invoke functions from one assembly
in another assembly without having any direct references between the projects.

This is made possible by having a YALOA, allowing us to invoke methods in other classes, through a _"magic string"_,
which references a type, in a dictionary, where the string is its key. Imagine the following code.

```csharp
[Slot(Name = "foo.bar")]
public class FooBar : ISlot
{
    public void Signal(ISignaler signaler, Node input)
    {
        input.Value = 42;
    }
}
```

The above declares a _"slot"_ for the signal **[foo.bar]**. In any other place in our AppDomain we can use an `ISignaler`
instance to Signal the above slot by using something such as the following.

```csharp
signaler.Signal("foo.bar", input);
```

After the invocation to the above `Signal`, the value of our node will be 42.

Notice that there are no shared types between the invoker and the handler, and there are no references necessary to
be shared between these two assemblies. This results in an extremely loosely coupled plugin architecture, where you can
dynamically add any plugin you wish into your AppDomain, by simply referencing whatever plugin assembly you
wish to bring into your AppDomain, and immediately start consuming your plugin functionality.

Tha Magic Signals implementation uses `IServiceProvider` to instantiate your above `FooBar` class when it
wants to evaluate your slot. This makes it behave as a good IoC citizen, allowing you to pass in for instance
interfaces into your constructor, and have the .Net Core dependency injection automatically create objects
of whatever interface your slot implementation requires.

## Passing arguments to your slots

The Node class provides a graph object for you, allowing you to automagically pass in any arguments you wish.
Notice, the whole idea is to de-couple your assemblies, hence you shouldn't really pass in anything but _"native types"_,
such as for instance `System.String`, `System.DateTime`, integers, etc. However, most complex POD structures, can also
easily be represented using this `Node` class. The Node class is basically a name/value/children graph object, where
the value can be any object, the name a string, and children is a list of children Nodes. In such a way, it provides
a more C# friendly graph object, kind of resembling JSON, allowing you to internally within your assemblies, pass
in a Node object as your parameters form the point you signal, to the slot where you handle the signal.
