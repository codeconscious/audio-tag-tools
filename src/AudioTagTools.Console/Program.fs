open Commands

type ExitCode =
    | Success = 0
    | InvalidArgumentCount = 1
    | InvalidCommand = 2
    | OperationFailure = 3

[<EntryPoint>]
let main allArgs : int =
    let watch = Startwatch.Library.Watch()

    match allArgs with
    | [| |] ->
        printfn $"{instructions}"
        ExitCode.InvalidArgumentCount
    | _ ->
        printfn "Starting..."

        let requestedCommand = Array.head allArgs
        let args = Array.tail allArgs

        match requestedCommand with
        | ValidCommand command ->
            match command args with
            | Ok msg ->
                printfn $"{msg}"
                printfn $"Done in {watch.ElapsedFriendly}."
                ExitCode.Success
            | Error msg ->
                printfn $"{msg}"
                printfn $"Failed after {watch.ElapsedFriendly}."
                ExitCode.OperationFailure
        | _ ->
            printfn $"Invalid command \"{requestedCommand}\". {instructions}"
            ExitCode.InvalidCommand
    |> int
