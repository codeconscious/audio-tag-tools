module Commands

let commandMap =
    [ "cache-tags", AudioTagTools.Cacher.start
      "find-duplicates", AudioTagTools.DuplicateFinder.start
      "export-genres", AudioTagTools.GenreExtractor.start ]
    |> Map.ofList

let (|ValidCommand|InvalidCommand|) requestedCommand =
    commandMap
    |> Map.tryFind requestedCommand
    |> function
    | Some command -> ValidCommand command
    | None -> InvalidCommand

let instructions =
    commandMap
    |> Map.keys
    |> String.concat "\" or \""
    |> sprintf "Supply a supported command: \"%s\"."

