module GenreExtractor.Exporting

open Shared.TagLibrary
open CCFSharpUtils.Library
open System

let private mainArtist (fileTags: LibraryTags) =
    match fileTags with
    | a when Array.isNotEmpty a.Artists -> a.Artists[0]
    | a when Array.isNotEmpty a.AlbumArtists -> a.AlbumArtists[0]
    | _ -> String.Empty

let printOldSummary (oldGenres: string array) : unit =
    match oldGenres with
    | [||] ->
        printfn "No existing genre data found."
    | _  ->
        let count = oldGenres.Length
        printfn "%s %s in the old file." (String.formatInt count) (String.pluralize "entry" "entries" count)

let printTagCount (tags: LibraryTags array) =
    printfn $"Parsed tags for {String.formatInt tags.Length} files from the tag library."

let private allGenres (fileTags: LibraryTags array) : string array =
    fileTags
    |> Array.collect _.Genres

let private mostCommon (items: string array) : string =
    match items with
    | [||] ->
        String.Empty
    | _ ->
        items
        |> Array.groupBy id
        |> Array.maxBy (snd >> Array.length)
        |> fst

let private mostCommonGenre = allGenres >> mostCommon

let generateGenreData (separator: string) (allFileTags: LibraryTags array) =
    allFileTags
    |> Array.groupBy mainArtist
    |> Array.choose (fun (artist, tags) ->
        let genre = mostCommonGenre tags
        if String.allHaveText [artist; genre]
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
