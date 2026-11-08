module Console.Commands

open FSharpPlus

type Command = string array -> Result<string, string>

let commandMap =
    Map.ofList
        [ "cache-tags", Cacher.Library.start
          "analyze-library", LibraryAnalysis.Library.start
          "find-duplicates", DuplicateFinder.Library.start
          "export-genres", GenreExtractor.Library.start ]

let (|ValidCommand|InvalidCommand|) requestedCommand =
    commandMap
    |> Map.tryFind requestedCommand
    |> option (fun command -> ValidCommand command) InvalidCommand

let instructions =
    commandMap
    |> Map.keys
    |> String.concat "\" or \""
    |> sprintf "Supply a supported command: \"%s\"."
