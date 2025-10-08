module AudioTagTools.DuplicateFinder

open Operators
open Errors
open ArgValidation
open IO
open Tags
open Settings
open FsToolkit.ErrorHandling

let private run (args: string array) : Result<unit, Error> =
    result {
        let! settingsFile, tagLibraryFile = validate args

        let! settings =
            settingsFile
            |> readFile
            >>= parseToSettings
            <.> printSummary

        return!
            tagLibraryFile
            |> readFile
            >>= parseToTags
            <.> printCount "Total file count:    "
            <!> filter settings
            <.> printCount "Filtered file count: "
            <!> findDuplicates settings
            <.> printDuplicates
            >>= savePlaylist settings
    }

let start args : Result<string, string> =
    match run args with
    | Ok _ -> Ok "Finished searching successfully!"
    | Error e -> Error (message e)
