module BoleroWasm.Client.Main

open System
open System.Net.Http
open System.Net.Http.Json
open Microsoft.AspNetCore.Components
open Elmish
open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Components.Authorization
open Microsoft.AspNetCore.Components.WebAssembly.Authentication
open System.Security.Claims


/// Routing endpoints definition.
type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/counter">] Counter
    | [<EndPoint "/data">] Data
    | [<EndPoint "/authentication/{action}">] Authentication of action:string //need a separate route for authentication

/// The Elmish application's model.
type Model =
    {
        page: Page
        counter: int
        books: Book[] option
        error: string option
        user : ClaimsPrincipal option
        photo : string option
    }

and Book =
    {
        title: string
        author: string
        publishDate: DateTime
        isbn: string
    }

let initModel =
    {
        page = Home
        counter = 0
        books = None
        error = None
        user = None
        photo = None
    }

/// The Elmish application's update messages.
type Message =
    | SetPage of Page
    | Increment
    | Decrement
    | SetCounter of int
    | GetBooks
    | GotBooks of Book[]
    | Error of exn
    | ClearError
    | Authenticated of ClaimsPrincipal option
    | LoginLogout
    | GetUserDetails 
    | GotUserDetails of string option

let isAuthenticated model = 
    match model.user with 
    | Some c when c.Identity.IsAuthenticated -> true 
    | _ -> false

//Ultimately takes the user to the login/logout page of AD 
let loginLogout (navMgr:NavigationManager) model =
    if isAuthenticated model then
        navMgr.NavigateToLogout("authentication/logout")
    else
        navMgr.NavigateToLogin("authentication/login")

let postAuth model user =
    let model = {model with user=user}
    let cmd = if isAuthenticated model then Cmd.ofMsg GetUserDetails else Cmd.none
    model,cmd

let update (navMgr:NavigationManager) (http: HttpClient) (httpFac:IHttpClientFactory) message model =
    match message with

    //auth related messages
    | LoginLogout -> loginLogout navMgr model;model,Cmd.none
    | Authenticated user -> postAuth model user
    | GetUserDetails -> model, Cmd.OfTask.either Graph.Api.getDetails (model.user,httpFac) GotUserDetails Error
    | GotUserDetails data -> {model with photo=data},Cmd.none

    | SetPage page ->
        { model with page = page }, Cmd.none

    | Increment ->
        { model with counter = model.counter + 1 }, Cmd.none
    | Decrement ->
        { model with counter = model.counter - 1 }, Cmd.none
    | SetCounter value ->
        { model with counter = value }, Cmd.none

    | GetBooks ->
        let getBooks() = http.GetFromJsonAsync<Book[]>("/books.json")
        let cmd = Cmd.OfTask.either getBooks () GotBooks Error
        { model with books = None }, cmd
    | GotBooks books ->
        { model with books = Some books }, Cmd.none

    | Error exn ->
        { model with error = Some exn.Message }, Cmd.none
    | ClearError ->
        { model with error = None }, Cmd.none


/// Connects the routing system to the Elmish application.
let router = Router.infer SetPage (fun model -> model.page)

type Main = Template<"wwwroot/main.html">

let homePage model dispatch =
    Main.Home().Elt()

let counterPage model dispatch =
    Main.Counter()
        .Decrement(fun _ -> dispatch Decrement)
        .Increment(fun _ -> dispatch Increment)
        .Value(model.counter, fun v -> dispatch (SetCounter v))
        .Elt()

let dataPage model dispatch =
    Main.Data()
        .Reload(fun _ -> dispatch GetBooks)
        .Rows(cond model.books <| function
            | None ->
                Main.EmptyData().Elt()
            | Some books ->
                forEach books <| fun book ->
                    tr {
                        td { book.title }
                        td { book.author }
                        td { book.publishDate.ToString("yyyy-MM-dd") }
                        td { book.isbn }
                    })
        .Elt()

let authenticatePage action model dispatch =
    Main.Authentication()
        .AuthComponent(
            concat{
                //Blazor component that redirects to AD login/logout pages 
                //It uses the information from services.AddMsalAuthentication in Startup.fs
                //to construct the redirect URL
                comp<RemoteAuthenticatorView> {
                    "Action" => action
                }
            })
        .Elt()

let menuItem (model: Model) (page: Page) (text: string) =
    Main.MenuItem()
        .Active(if model.page = page then "is-active" else "")
        .Url(router.Link page)
        .Text(text)
        .Elt()

let view model dispatch =
    Main()
        .Menu(concat {
            menuItem model Home "Home"
            menuItem model Counter "Counter"
            menuItem model Data "Download data"
        }).AuthenticateLink(
                div {
                    a {                
                        attr.href "#"
                        on.click (fun _ -> dispatch LoginLogout)               
                        match model.user with 
                        | Some c when c.Identity.IsAuthenticated -> text "Logout"
                        | _                                      -> text "Login"
                    }      
                    section{
                        attr.style "width:200px; border: 1px solid #2d2d2d; display: flex; justify-content: center; align-items: center;"
                        let imgStyle = "max-width:30px; height:auto; margin:2px;"
                        div {
                            match model.photo with 
                            | Some str -> img {attr.src $"data:png,base64, {str}"; attr.style imgStyle }
                            | None -> img {attr.src "img/person.png"; attr.style imgStyle }
                        }
                        div {
                            match model.user with
                            | Some c when c.Identity.IsAuthenticated -> text (c.Identity.Name)                                                                
                            | _ -> text ""                        
                        }
                    }              
                }
        )
        .Body(
            cond model.page <| function
            | Home -> homePage model dispatch
            | Counter -> counterPage model dispatch
            | Data -> dataPage model dispatch
            | Authentication action -> authenticatePage action model dispatch
        )
        .Error(
            cond model.error <| function
            | None -> empty()
            | Some err ->
                Main.ErrorNotification()
                    .Text(err)
                    .Hide(fun _ -> dispatch ClearError)
                    .Elt()
        )
        .Elt()

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    [<Inject>]
    member val HttpClient = Unchecked.defaultof<HttpClient> with get, set

    [<Inject>]
    member val Auth : AuthenticationStateProvider = Unchecked.defaultof<_> with get, set

    [<Inject>]
    member val HttpFac : IHttpClientFactory = Unchecked.defaultof<_> with get, set

    override this.Program =

        //When authentication state changes (i.e. user is authenticated, etc.),
        //this handler is invoked. It dispatches a message to the app
        //with the authentication state
        let handler = new AuthenticationStateChangedHandler(fun t -> 
            task {
                let! s = t              
                this.Dispatch (Authenticated (Some s.User)) 
            } |> ignore)
        this.Auth.add_AuthenticationStateChanged(handler)

        let update = update this.NavigationManager this.HttpClient this.HttpFac
        Program.mkProgram (fun _ -> initModel, Cmd.ofMsg GetBooks) update view
        |> Program.withRouter router