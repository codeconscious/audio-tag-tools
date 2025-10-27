module Console.Commands

type Command = string array -> Result<string, string>

let commandMap =
    [ "cache-tags", Cacher.Library.start
      "analyze-library", LibraryAnalysis.Library.start
      "find-duplicates", DuplicateFinder.Library.start
      "export-genres", GenreExtractor.Library.start ]
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

