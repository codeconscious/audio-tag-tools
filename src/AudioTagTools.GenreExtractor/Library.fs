module GenreExtractor.Library

open ArgValidation
open Errors
open Exporting
open IO
open Shared.TagLibrary
open Shared.Constants
open CCFSharpUtils.Library
open FsToolkit.ErrorHandling

// The separator character should be rare and highly unlikely to appear in files' tags.
let private separator = "＼"

let private run args : Result<unit, Error> =
    result {
        let! tagLibraryFile, genreFile = validate args

        let! oldGenres = genreFile |> readGenres |. printOldSummary

        let! tags =
            tagLibraryFile
            |> readThenParseToJson
            |. printTagCount
            |! TagParseError

        let newGenres = tags |> generateGenreData separator

        printChanges oldGenres newGenres

        do!
            genreFile
            |> File.copyToBackupFile timeStampFormat
            |* printfn "Created backup file \"%O\"." // %O formats any object via ToString().
            |! FileWriteError

        return!
            newGenres
            |> writeLines genreFile.FullName
    }

let start args : Result<string, string> =
    match run args with
    | Ok () -> Ok "Finished exporting genres successfully!"
    | Error e -> Error (message e)
