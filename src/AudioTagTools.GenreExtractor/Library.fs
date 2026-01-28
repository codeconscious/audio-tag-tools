module GenreExtractor.Library

open ArgValidation
open Errors
open Exporting
open IO
open Shared.TagLibrary
open CCFSharpUtils.Library
open FsToolkit.ErrorHandling

let private run args : Result<unit, Error> =
    result {
        let! tagLibraryFile, genreFile = validate args

        let! oldGenres = readLines genreFile |. printOldSummary

        let! tags =
            tagLibraryFile
            |> readThenParseToJson
            |. printTagCount
            |! TagParseError

        // The separator character should be rare and highly unlikely to appear in files' tags.
        let newGenres = generateGenreData "＼" tags

        printChanges oldGenres newGenres

        let! _ =
            genreFile
            |> copyToBackupFile
            |. fun fileInfo -> printfn $"Created backup file \"{fileInfo}\"."

        return!
            newGenres
            |> writeLines genreFile.FullName
    }

let start args : Result<string, string> =
    match run args with
    | Ok () -> Ok "Finished exporting genres successfully!"
    | Error e -> Error (message e)
