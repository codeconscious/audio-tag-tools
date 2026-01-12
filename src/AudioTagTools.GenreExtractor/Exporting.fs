module GenreExtractor.Exporting

open Shared.TagLibrary
open CCFSharpUtils.Library
open System

let private mainArtist (fileTags: LibraryTags) =
    match fileTags with
    | a when Array.isNotEmpty a.Artists -> a.Artists[0]
    | a when Array.isNotEmpty a.AlbumArtists -> a.AlbumArtists[0]
    | _ -> String.Empty

let private allGenres (fileTags: MultipleLibraryTags) : string array =
    fileTags
    |> Array.collect _.Genres

let private mostCommon (items: string array) : string =
    match items with
    | [||] -> String.Empty
    | _ ->
        items
        |> Array.groupBy id
        |> Array.maxBy (fun (_, groupItems) -> Array.length groupItems)
        |> fst

let private mostCommonGenre = allGenres >> mostCommon

let groupArtistsWithGenres (separator: string) (allFileTags: MultipleLibraryTags) =
    allFileTags
    |> Array.groupBy mainArtist
    |> Array.choose (fun (artist, tags) ->
        let genre = mostCommonGenre tags
        if String.allHaveText [artist; genre]
        then Some $"{artist}{separator}{genre}"
        else None)
    |> Array.sort
