# StrongInject.Samples.XamarinApp

## Overview

This sample modifies the default Xamarin.Forms sample app to use DI via StrongInject. No functionality has been added or removed, but the code style has been changed to be consistent with StrongInject.

It consists of an "About" tab, an "Items" tab which lets you navigate to each item and add new items, and a "Login" page.

The implementation presents just one way to do navigation whilst injecting pages and view models. Other techniques are possible.

## Debugging

The Android app is not built by the Debug/Release configurations as the dotnet CLI doesn't support xamarin. To debug the app change the configuration to AndroidApp, and then run/debug as normal.

## Navigation

In this sample all pages and view models are created by the StrongInject container.

When a page/ViewModel wants to navigate to a different page it takes a dependency on `INavigationService`, `INavigationService<T>` or `IParameterizedNavigationService<T>` depending on its exact needs.

To avoid ViewModels controlling which page is used for which ViewModel, `INavigationService<T>` and `IParameterizedNavigationService<T>` navigate to a specific ViewModel rather than a specific page. By registering your page as `IViewOf<T>` in the container, you can configure which pages are linked to which ViewModel. This means you could theoretically have two containers constructing the exact same ViewModels but assigning them to different pages.

AppShell.xaml delares a number of routes, and binds them to a DataTemplate returning an injected `Func<Page>`. This shows how you can use routes, and still make sure that you are injecting all your pages.

## Learning Points

This app demonstrates a number of key features and techniques using StrongInject:

1. The Shell is set only after the container is run, but many services in the container depend on the Shell. We use `Lazy<T>`, provided by the builtin `LazyModule` to allow this to work.
2. We use factories to register generic types, as well as to register a static field.
3. A marker interface `IViewOf<T>` is used to help StrongInject link up the correct Page with the correct ViewModel.
4. Funcs are used to lazily create pages and ViewModels, rather than creating them all at startup.
5. Funcs are used to parameterize a ViewModel, so that the `ItemsViewModel` can create a different `ItemDetailViewModel` for each `Item`.
