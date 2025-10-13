open Commands

type ExitCode =
    | Success = 0
    | InvalidArgumentCount = 1
    | InvalidCommand = 2
    | OperationFailure = 3

let runCommand (watch: Startwatch.Library.Watch) validCommandFn args =
    match validCommandFn args with
    | Ok msg ->
        printfn $"{msg}"
        printfn $"Done in {watch.ElapsedFriendly}."
        ExitCode.Success |> int
    | Error msg ->
        printfn $"{msg}"
        printfn $"Failed after {watch.ElapsedFriendly}."
        ExitCode.OperationFailure |> int

let invalidArgCountExit () =
    printfn $"{instructions}"
    ExitCode.InvalidArgumentCount |> int

let invalidCommandExit command =
    printfn $"Invalid command \"{command}\". {instructions}"
    ExitCode.InvalidCommand |> int

[<EntryPoint>]
let main allArgs : int =
    let watch = Startwatch.Library.Watch()

    match allArgs with
    | [| |] -> invalidArgCountExit ()
    | _ ->
        printfn "Starting..."

        let requestedCommand = Array.head allArgs
        let args = Array.tail allArgs

        match requestedCommand with
        | InvalidCommand -> invalidCommandExit requestedCommand
        | ValidCommand validCommandFn -> runCommand watch validCommandFn args

