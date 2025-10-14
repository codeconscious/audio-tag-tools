open Commands

type ExitCode =
    | Success = 0
    | InvalidArgumentCount = 1
    | InvalidCommand = 2
    | OperationFailure = 3

[<EntryPoint>]
let main allArgs =
    let watch = Startwatch.Library.Watch()

    let execute (commandFn: Command) args =
        match commandFn args with
        | Ok msg ->
            printfn $"%s{msg}"
            printfn $"Done in %s{watch.ElapsedFriendly}."
            ExitCode.Success
        | Error msg ->
            printfn $"%s{msg}"
            printfn $"Failed after %s{watch.ElapsedFriendly}."
            ExitCode.OperationFailure

    let invalidArgCountExit () =
        printfn $"%s{instructions}"
        ExitCode.InvalidArgumentCount

    let invalidCommandExit command =
        printfn $"Invalid command \"%s{command}\". %s{instructions}"
        ExitCode.InvalidCommand

    match allArgs with
    | [| |] ->
        invalidArgCountExit ()
    | _ ->
        printfn "Starting..."

        let requestedCommand = allArgs[0]
        let args = allArgs[1..]

        match requestedCommand with
        | InvalidCommand -> invalidCommandExit requestedCommand
        | ValidCommand commandFn -> execute commandFn args
    |> int

