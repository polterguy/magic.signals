
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
