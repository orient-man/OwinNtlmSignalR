open System
open System.Net
open FSharp.Interop.Dynamic
open Owin
open Microsoft.AspNet.SignalR
open Microsoft.AspNet.SignalR.Hubs
open Microsoft.Owin.Hosting

[<AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)>]
type FxPosAuthorize () =
    inherit AuthorizeAttribute ()
    override __.UserAuthorized(user) =
        if isNull user then new ArgumentNullException("user") |> raise
        user.Identity.Name = "EBRE\mm21876"

[<FxPosAuthorize()>]
[<HubName("FxPosHub")>]
type FxPosHub () = inherit Hub ()

type Owin.IAppBuilder with
    member app.UseIntegratedWindowsAuthentication() =
        let listener = app.Properties.["System.Net.HttpListener"] :?> HttpListener
        listener.AuthenticationSchemes <- AuthenticationSchemes.IntegratedWindowsAuthentication
        app

type Server (host: string) =
    let startup (app: IAppBuilder) =
        app
            .UseIntegratedWindowsAuthentication()
            .UseFileServer(enableDirectoryBrowsing = true)
            .MapSignalR()
            |> ignore

    let clients = GlobalHost.ConnectionManager.GetHubContext<FxPosHub>().Clients

    do
        WebApp.Start(host, startup) |> ignore
        printfn "Static server running on %s" host

    member __.Send message = clients.All?messageToTheClient message

type FxPosition = FxPosition of int

[<EntryPoint>]
let main _ =
    let server = Server "http://localhost:8781"

    let rec loop value =
        async {
            let msg = value |> FxPosition
            server.Send msg
            do! Async.Sleep 1000
            return! loop (value + 1) }

    1 |> loop |> Async.RunSynchronously
    0