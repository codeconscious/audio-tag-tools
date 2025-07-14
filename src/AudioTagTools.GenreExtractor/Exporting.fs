module Exporting

open System
open TagLibrary

let private mainArtist (fileTags: LibraryTags) =
    match fileTags with
    | a when a.Artists.Length > 0 -> a.Artists[0]
    | a when a.AlbumArtists.Length > 0 -> a.AlbumArtists[0]
    | _ -> String.Empty

let private mostCommon (items: string seq) : string =
    items
    |> Seq.countBy id
    |> Seq.fold (fun acc (item, count) ->
        match acc with
        | None -> Some (item, count)
        | Some (_, currentCount) when count > currentCount -> Some (item, count)
        | Some _ -> acc) None
    |> Option.map fst
    |> Option.defaultValue String.Empty

let private allGenres (fileTags: LibraryTags array) : string array =
    fileTags
    |> Array.map _.Genres
    |> Array.filter (fun gs -> gs.Length > 0)
    |> Array.collect id

let private mostCommonGenres = allGenres >> mostCommon

let getArtistsWithGenres (allFileTags: LibraryTags array) =
    let separator = "＼" // Should be a character unlikely to appear in files' tags.

    allFileTags
    |> Array.groupBy mainArtist
    |> Array.filter (fun (a, _) -> a <> String.Empty) // Maybe need tag check too.
    |> Array.map (fun (a, ts) -> a, mostCommonGenres ts)
    |> Array.filter (fun (_, g) -> g <> String.Empty)
    |> Array.sortBy fst
    |> Array.map (fun (a, g) -> $"{a}{separator}{g}")
