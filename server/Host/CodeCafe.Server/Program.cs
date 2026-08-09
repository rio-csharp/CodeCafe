// This shim exists so CodeCafe.Server.csproj remains buildable as the container image
// target while the actual implementation lives in CodeCafe.Host. The host project cannot
// be the container target because it is referenced by test projects, and making it
// <OutputType>Exe</OutputType> would turn test projects into console apps.

using CodeCafe.Host.Common;
await WebApplicationRunner.RunAsync(args);
