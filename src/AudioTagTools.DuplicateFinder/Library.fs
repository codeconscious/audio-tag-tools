module DuplicateFinder.Library

open Errors
open ArgValidation
open IO
open Tags
open Settings
open Shared.Types
open CCFSharpUtils
open CCFSharpUtils.Operators
open FSharpPlus
open FsToolkit.ErrorHandling.Operator.Result

let private run args : Result<unit, CommandError> =
    monad' {
        let! settingsFile, tagLibFile = validate args

        let! settings = settingsFile |> File.readText' |!! FileReadError >>= (Json >> parseToSettings)
        let! tags = tagLibFile |> File.readText' |!! FileReadError >>= (Json >> parseToTags)

        printSummary settings

        let! duplicates =
           tags
           |- printCount "Total file count:    "
           |> discardExcluded settings
           |-- printCount "Filtered file count: "
           |>> findDuplicates settings
           |-- printDuplicates

        return!
            duplicates |> savePlaylist settings
    }

let start args : Result<string, string> =
    match run args with
    | Ok ()   -> Ok "Finished searching successfully."
    | Error e -> Error (message e)
