module AudioTagTools.GenreExtractor

open Operators
open Errors
open Exporting
open ArgValidation
open Utilities
open Shared.IO
open FsToolkit.ErrorHandling

let private run args : Result<unit, Error> =
    result {
        let! tagLibraryFile, genreFile = validate args

        let! oldGenres = IO.readLines tagLibraryFile
        let oldCount = oldGenres.Length
        printfn $"{oldCount} entries in the old file."

        let! newGenres =
            tagLibraryFile
            |> IO.readFile
            >>= IO.parseJsonToTags
            <.> fun tags -> printfn $"Parsed tags for {formatInt tags.Length} files from the tag library."
            <!> groupArtistsWithGenres "＼" // Separator should be text very unlikely to appear in files' tags.

        let newTotalCount = newGenres.Length
        let newCount = newGenres |> Array.except oldGenres |> _.Length
        let deletedCount = oldGenres |> Array.except newGenres |> _.Length
        printfn $"Prepared {newTotalCount} artist-genre pairs."
        printfn $"{newCount} are new entries."
        printfn $"{deletedCount} removed entries."

        do!
            genreFile
            |> copyToBackupFile
            |> Result.map ignore
            |> Result.mapError IoWriteError

        return! IO.writeLines genreFile.FullName newGenres
    }

let start args : Result<string, string> =
    match run args with
    | Ok () -> Ok "Finished exporting genres successfully!"
    | Error e -> Error (message e)
