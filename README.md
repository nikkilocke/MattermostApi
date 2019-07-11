# MattermostApi

This is a C# wrapper to (the most important bits of) the [Mattermost API](https://api.mattermost.com).

It is provided with a Visual Studio 2019 build solution for .NET Standard, so can be used with any version of .NET.

There is a test project (for net core only) which also demonstrates usage.

## Setup before using the API

In order to use the Mattermost API you need to register your application with the relevant Mattermost server - [see instructions](https://docs.mattermost.com/developer/oauth-2-0-applications.html). This returns a Client Id and Client Secret. When registering, you have to provide a redirect uri for the OAuth2 authorisation process. For simple use, provide something like http://localhost:9999 (choose another port number if you like).

This information has to be provided in an object that implements the [ISettings](../master/MattermostApi/Settings.cs) interface, which is then used to create a MattermostApi instance. A Settings class which imnplements this interface is provided, to save you work. This provides a static Load method, reads the settings from *LocalApplicationData*/MattermostApi/Settings.json. On a Windows 10 machine, *LocalApplicationData* is `C:\Users\<USER>\AppData\Local`, on Linux it is `~user/.local/share`.

## Testing

In order to run the Unit Tests provided, you must provide additional data in your ISettings object - see the Settings object in [UnitTest1.cs](../master/Tests/UnitTest1.cs).

## Hooks for more complex uses

You do not have to use the provided Settings class, provided you have a class that implements ISettings.

As part of the OAuth2 process, the default implementation starts a browser to obtain authorisation. This is done by calling OpenBrowser. You can provide an alternative action to open a browser, or otherwise call the Mattermost OAuth page to ask for authorisation.

Once authorisation is complete, the OAuth2 process will redirect the browser to the redirect url you provide in the settings. The default implementation provides an extremely dumb web server to listed on the redirect url port, and collect the `code=` parameter from the request. You can provide an alternative by providing a `WaitForRedirect` async function.

These options would be useful if you were using the Api in your own Web Server, for instance.

## License

This wrapper is licensed under creative commons share-alike, see [license.txt](../master/license.txt).

## Using the Api

The Unit Tests should give you sufficient examples on using the Api.

An Api instance is created by passing it an object that implements ISettings (a default class is provided which will read the settings from a json file). The Api instance is IDisposable, so should be Disposed when no longer needed (this is because it contains an HttpClient).

C# classes are provided for the objects you can send to or receive from the Mattermost api. For instance the Channel object represents channels. These main objects have methods which call the Mattermost api - such as Channel.Create to create a new channel, Channel.GetById to get channel details, etc.

Some Api calls return a list of items (such as Team.GetChannelsForUser). These are returned as an ApiList<Channel>. The Mattermost api itself usually only returns the first few items in the list, and needs to be called again to return the next chunk of items. This is all done for you by ApiList - it has a method called All(Api) which will return an IEnumerable of the appropriate listed object. Enumerating the enumerable will return all the items in the first chunk, then call the Mattermost api to get the next chunk, return them and so on. It hides all that work from the caller, while remaining as efficient as possible by only getting data when needed - for instance, using Linq calls like Any or First will stop getting data when the first item that matches the selection function is found.


