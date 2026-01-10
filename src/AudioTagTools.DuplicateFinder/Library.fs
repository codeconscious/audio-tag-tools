module DuplicateFinder.Library

open Errors
open ArgValidation
open IO
open Tags
open Settings
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result
open Shared.Common

let private run (args: string array) : Result<unit, Error> =
    result {
        let! settingsFile, tagLibraryFile = validate args

        let! settings = settingsFile |> readFile >>= parseToSettings |> Result.tee printSummary

        let! tags = tagLibraryFile |> readFile >>= parseToTags

        return!
           tags
           |> tee (printCount "Total file count:    ")
           |> filter settings
           |> tee (printCount "Filtered file count: ")
           |> findDuplicates settings
           |> tee printDuplicates
           |> savePlaylist settings
    }

let start args : Result<string, string> =
    match run args with
    | Ok _ -> Ok "Finished searching successfully!"
    | Error e -> Error (message e)
