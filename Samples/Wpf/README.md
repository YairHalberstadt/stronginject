# StrongInject.Samples.Wpf

## Overview

This sample uses StrongInject to instantiate a simple WPF application.

It consists of a window showing a list of users which can be added to, edited, or deleted.

## Debugging

The WPF app is not built by the Debug/Release configurations as it can only be built on windows. To debug the app change the configuration to WPF, and then run/debug as normal.

## Dependency Injection in WPF

WPF classically expects user controls and view models to have a parameterless constructor, and then use a service locator instead of DI to access needed services. This is rightly [considered an anti pattern](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/).

Instead we instantiate a tree of view models using StrongInject. Each ViewModel accepts any child ViewModels directly in its constructor, or via a `Func`. Every `ViewModel` exposes the child ViewModels as properties.

We resolve the `MainWindow` directly, and the `MainWindow` sets the `MainWindowViewModel` as its `DataContext` in its constructor.

All UserControls use Data Binding to bind the `DataContext` of any child User Controls to the relevant ChildViewModel property on the ViewModel.

## Learning Points

This app uses a relatively simple and straightforward StrongInject container. It's main learning points are how to use Dependency Injection in WPF to create all the ViewModels and services, and then bind them to the UserControls.

Take a look at all the ViewModels and their constructors, and then take a look at how the DataContext is set for each Window/UserControl.
