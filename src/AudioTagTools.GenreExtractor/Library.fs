module GenreExtractor.Library

open ArgValidation
open Errors
open Exporting
open IO
open Shared
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.Result

let private run args : Result<unit, Error> =
    result {
        let! tagLibraryFile, genreFile = validate args

        let! oldGenres = readLines genreFile
        printfn "%s entries in the old file." (String.formatInt oldGenres.Length)

        let! newGenres =
            tagLibraryFile
            |> readFile
            |> Result.bind parseJsonToTags
            |> Result.tee (fun tags -> printfn $"Parsed tags for {String.formatInt tags.Length} files from the tag library.")
            |> Result.map (groupArtistsWithGenres "＼") // Separator should be text highly unlikely to appear in files' tags.

        let newTotalCount = newGenres.Length
        let addedCount = newGenres |> Array.except oldGenres |> _.Length
        let deletedCount = oldGenres |> Array.except newGenres |> _.Length
        printfn "Prepared %s artist-genre entries total (%s new, %s deleted)."
            (String.formatInt newTotalCount)
            (String.formatInt addedCount)
            (String.formatInt deletedCount)

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
