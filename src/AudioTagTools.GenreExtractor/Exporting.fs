module GenreExtractor.Exporting

open Shared
open Shared.TagLibrary
open System

let private mainArtist (fileTags: LibraryTags) =
    match fileTags with
    | a when a.Artists.Length > 0 -> a.Artists[0]
    | a when a.AlbumArtists.Length > 0 -> a.AlbumArtists[0]
    | _ -> String.Empty

let printOldSummary (oldGenres: string array) =
    printfn "%s entries in the old file." (String.formatInt oldGenres.Length)

let printTagSummary (tags: MultipleLibraryTags) =
    printfn $"Parsed tags for {String.formatInt tags.Length} files from the tag library."

let private allGenres (fileTags: MultipleLibraryTags) : string array =
    fileTags
    |> Array.collect _.Genres

let private mostCommon (items: string array) : string =
    match items with
    | [||] ->
        String.Empty
    | _ ->
        items
        |> Array.groupBy id
        |> Array.maxBy (fun (_, groupItems) -> Array.length groupItems)
        |> fst

let private mostCommonGenre = allGenres >> mostCommon

let generateNewGenreData (separator: string) (allFileTags: MultipleLibraryTags) =
    allFileTags
    |> Array.groupBy mainArtist
    |> Array.choose (fun (artist, tags) ->
        let genre = mostCommonGenre tags
        if String.hasText artist && String.hasText genre
        then Some $"{artist}{separator}{genre}"
        else None)
    |> Array.sort

let printChanges (oldGenres: string array) (newGenres: string array) =
    let newTotalCount = newGenres.Length
    let addedCount = newGenres |> Array.except oldGenres |> _.Length
    let deletedCount = oldGenres |> Array.except newGenres |> _.Length
    printfn "Prepared %s artist-genre entries total (%s new, %s deleted)."
        (String.formatInt newTotalCount)
        (String.formatInt addedCount)
        (String.formatInt deletedCount)
