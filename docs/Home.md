# StrongInject

StrongInject is a compile time IOC framework for C#, utilizing the new roslyn [Source Generators](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/) feature.

## Concepts

### Containers

A container is esentially a factory that knows how to provide an instance of a type on demand, and then dispose of it once it's no longer needed.

### [[Registration]]

Registration is how you let your container know what it can use, and how, to try and create that instance.


