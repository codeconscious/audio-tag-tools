module DuplicateFinder.Library

open Errors
open ArgValidation
open IO
open Tags
open Settings
open CCFSharpUtils
open FSharpPlus.Operators
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result

let private run (args: string array) : Result<unit, DupeFinderError> =
    result {
        let! settingsFile, tagLibraryFile = validate args

        let! settings    = settingsFile   |> File.readText' |! FileReadError >>= parseToSettings
        let! libraryTags = tagLibraryFile |> File.readText' |! FileReadError >>= parseToTags

        printSummary settings

        let duplicates =
           libraryTags
           |> tap (printCount "Total file count:    ")
           |> discardExcluded settings
           |> tap (printCount "Filtered file count: ")
           |> findDuplicates settings
           |> tap printDuplicates

        return!
            duplicates |> savePlaylist settings
    }

let start args : Result<string, string> =
    match run args with
    | Ok ()   -> Ok "Finished searching successfully."
    | Error e -> Error (message e)
