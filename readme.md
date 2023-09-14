# F# / Bolero / WASM Sample for AD Authentication and MS Graph API Usage

#### Shows how to:
- Authenticate a user with Active Directory (from WASM)
- On behalf of an application also registered in Active Directory
- And then invoke the MS Graph API with the access token obtained from AD

The AD setup required to run this sample is documented [here](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/hosted-with-azure-active-directory-b2c?view=aspnetcore-7.0).

#### The high level steps are:

1. Create an application in Azure Active Directory (AD-app)
2. Configure at least the MS Graph / User.Read permission for the AD-app
3. Set up the appropriate authentication callback URL(s) in the AD-app 
3. Note the tenant id and client id of the AD-app. These will be needed in the WASM app.

The tenant id and client id are used in the [wwwroot/appsettings.json](/src/BoleroWasm.Client/wwwroot/appsettings.json) file. See [Startup.fs](/src/BoleroWasm.Client/Startup.fs) for when these settings are referenced.

**The code is commented where needed to aid in understanding of the underlying process.**

The underlying library (Microsoft.Authentication.WebAssembly.Msal) and related components  were built for Blazor. Their use in Bolero requires a particular adaptation. The Blazor documentation/samples are only partially helpful in understanding the implementation here.

## WASM Debugging
There is debugger support now available for WASM / webassembly code. The configurations in [launch.json](/.vscode/launch.json) and [launchSettings.json](/src/BoleroWasm.Client/Properties/launchSettings.json) allow for standalone WASM application debugging. 

F# code breakpoints are honored and the debugging experience is decent enough for F#. Computation expressions are presented as nested lambdas so have to traverse the call stack to get the information needed.

**Note**: The application should be built/compiled before the debugger is launched after code changes, otherwise the app can hang. A pre-launch build task was added to launch.json to ensure this. Note that the WASM configuration is different from normal dotnet core applications to configure the pre-launch task.








