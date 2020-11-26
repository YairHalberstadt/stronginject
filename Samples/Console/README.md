# StrongInject.Samples.ConsoleApp

## Overview

This sample demonstrates a console application that might be part of a messaging application.

It reads messages from a kafka topic ("all_messages") then forwards the message to the recipients inbox (the "inbox_[user-id]" kafka topic).

## Requirements

Docker

## Learning Points

This app demonstrates a number of key features and techniques using StrongInject:

1. Factories are used extensively, to facilitate passing a config around, and to enable registering a lot of generic types.
2. Loading a config at runtime, in a way that makes it easy to swap out how the config is loaded.
3. Using async resolution (in this case to asyncronously load a config file from disk).
4. Usage of a module to separate out registrations that conceptually belong together.

## Notes

Program.cs has a lot of code to start some docker containers, create kafka topics, and produce messages to those topics. You can ignore most of this code. 
