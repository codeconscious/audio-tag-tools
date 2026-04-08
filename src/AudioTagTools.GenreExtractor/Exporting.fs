module GenreExtractor.Exporting

open Shared.TagLibrary
open CCFSharpUtils
open FSharpPlus.Data
open System

let private mainArtist (fileTags: LibraryTags) =
    match fileTags with
    | a when Array.isNotEmpty a.Artists -> a.Artists[0]
    | a when Array.isNotEmpty a.AlbumArtists -> a.AlbumArtists[0]
    | _ -> String.Empty

let printOldSummary (oldGenres: string list) : unit =
    match oldGenres with
    | [] ->
        printfn "No existing genre data found."
    | _  ->
        let count = oldGenres.Length
        printfn "%s %s in the old file."
            (String.formatInt count)
            (String.pluralize "entry" "entries" count)

let printTagCount (tags: LibraryTags NonEmptyList) =
    printfn $"Parsed tags for {String.formatInt tags.Length} files from the tag library."

let private allGenres (fileTags: LibraryTags NonEmptyList) : string list =
    fileTags
    |> NonEmptyList.toList
    |> List.collect (fun x -> x.Genres |> List.ofArray)

let private mostCommon (items: string list) : string =
    match items with
    | [] ->
        String.Empty
    | _ ->
        items
        |> List.groupBy id
        |> List.maxBy (snd >> List.length)
        |> fst

let private mostCommonGenre = allGenres >> mostCommon

let generateGenreData (separator: string) (allFileTags: LibraryTags NonEmptyList) =
    allFileTags
    |> NonEmptyList.groupBy mainArtist
    |> NonEmptyList.choose (fun (artist, tags) ->
        let genre = mostCommonGenre tags
        if String.allHaveText [artist; genre]
        then Some $"{artist}{separator}{genre}"
        else None)
    |> NonEmptyList.sort

let printChanges (oldGenres: string list) (newGenres: string NonEmptyList) =
    let newTotalCount = newGenres.Length
    let addedCount = newGenres |> NonEmptyList.except oldGenres |> _.Length
    let deletedCount = oldGenres |> List.except newGenres |> _.Length
    printfn "Prepared %s artist-genre entries total (%s new, %s deleted)."
        (String.formatInt newTotalCount)
        (String.formatInt addedCount)
        (String.formatInt deletedCount)
