module DuplicateFinder.Library

open Errors
open ArgValidation
open IO
open Tags
open Settings
open CCFSharpUtils.Library
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result

let private run (args: string array) : Result<unit, Error> =
    result {
        let! settingsFile, tagLibraryFile = validate args

        let! settings = readFile settingsFile >>= parseToSettings
        let! tags = readFile tagLibraryFile >>= parseToTags

        printSummary settings

        let duplicates =
           tags
           |> tee (printCount "Total file count:    ")
           |> discardExcluded settings
           |> tee (printCount "Filtered file count: ")
           |> findDuplicates settings
           |> tee printDuplicates

        return!
            duplicates |> savePlaylist settings
    }

let start args : Result<string, string> =
    match run args with
    | Ok _ -> Ok "Finished searching successfully."
    | Error e -> Error (message e)
