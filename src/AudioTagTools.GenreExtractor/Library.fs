module GenreExtractor.Library

open ArgValidation
open Errors
open Exporting
open IO
open Shared
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result
open CCFSharpUtils.Library

let private run args : Result<unit, Error> =
    result {
        let! tagLibraryFile, genreFile =
            validate args

        let! oldGenres =
            genreFile
            |> readLines
            |> Result.tee printOldSummary

        let! tags =
            tagLibraryFile
            |> readThenParseToJson
            |> Result.mapError TagParseError
            |> Result.tee printTagSummary

        // The separator character should be rare and highly unlikely to appear in files' tags.
        let newGenres = tags |> generateNewGenreData "＼"

        printChanges oldGenres newGenres

        do!
            genreFile
            |> copyToBackupFile
            |> Result.map ignore

        return!
            newGenres
            |> writeLines genreFile.FullName
    }

let start args : Result<string, string> =
    match run args with
    | Ok () -> Ok "Finished exporting genres successfully!"
    | Error e -> Error (message e)
