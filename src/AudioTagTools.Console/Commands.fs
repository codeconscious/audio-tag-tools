module Commands

let commandMap =
    [ "cache-tags", AudioTagTools.Cacher.start
      "find-duplicates", AudioTagTools.DuplicateFinder.start
      "export-genres", AudioTagTools.GenreExtractor.start ]
    |> Map.ofList

let (|ValidCommand|_|) requestedCommand =
    commandMap
    |> Map.tryFind requestedCommand

let instructions =
    commandMap
    |> Map.keys
    |> String.concat "\" or \""
    |> sprintf "Supply a supported command: \"%s\"."

