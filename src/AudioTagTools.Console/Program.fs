let commandMap =
    [ "cache-tags", AudioTagTools.Cacher.start
      "find-duplicates", AudioTagTools.DuplicateFinder.start
      "export-genres", AudioTagTools.GenreExtractor.start ]
    |> Map.ofList

let commandInstructions =
    commandMap
    |> Map.keys
    |> String.concat "\" or \""
    |> sprintf "Supply a supported command: \"%s\"."

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
        printfn $"{commandInstructions}"
        ExitCode.InvalidArgumentCount
    | _ ->
        printfn "Starting..."

        let command = Array.head allArgs
        let operationArgs = Array.tail allArgs

        Map.tryFind command commandMap
        |> function
        | Some requestedOperation ->
            match requestedOperation operationArgs with
            | Ok msg ->
                printfn $"{msg}"
                printfn $"Done in {watch.ElapsedFriendly}."
                ExitCode.Success
            | Error msg ->
                printfn $"{msg}"
                printfn $"Failed after {watch.ElapsedFriendly}."
                ExitCode.OperationFailure
        | None ->
            printfn $"Invalid command \"{command}\". {commandInstructions}"
            ExitCode.InvalidCommand
    |> int
