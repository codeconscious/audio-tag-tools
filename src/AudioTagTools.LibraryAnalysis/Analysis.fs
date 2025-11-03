module LibraryAnalysis.Analysis

open System
open System.IO
open Shared.TagLibrary
open Shared.Utilities

let inline private mostPopulous count (grouper: 'a -> 'a) (items: 'a array) =
    items
    |> Array.groupBy grouper
    |> Array.map (fun (_, group) -> (group[0], group.Length))
    |> Array.sortByDescending snd
    |> Array.truncate count

let private asLower (x: string) = x.ToLowerInvariant()

let averageFileSize tags =
    let sizeTotal = tags |> Array.map _.FileSize |> Array.sum
    let fileCount = tags.Length
    sizeTotal / (int64 fileCount)

let albumArtPercentage (tags: LibraryTags array) =
    tags
    |> Array.choose (fun t -> if t.ImageCount > 0 then Some t.ImageCount else None)
    |> Array.length
    |> fun count -> float count / float tags.Length * 100.

let filteredArtists (tags: LibraryTags array) =
    tags
    |> Array.map (fun t ->
        Array.concat [| t.Artists; t.AlbumArtists |]
        |> Array.distinct
        |> Array.except [| String.Empty; " "; "Various Artists"; "<unknown>" |])
    |> Array.concat

let uniqueArtistCount tags =
    tags
    |> filteredArtists
    |> Array.distinct
    |> _.Length

let topArtists count (tags: LibraryTags array) =
    tags
    |> filteredArtists
    |> mostPopulous count id
    |> Array.map (fun (artist, count) -> [| artist; formatInt count |])

let topAlbums count (tags: LibraryTags array) =
    tags
    |> Array.map _.Album
    |> mostPopulous count id
    |> Array.map (fun (album, count) -> [| album; formatInt count |])

let topTitles count (tags: LibraryTags array) =
    tags
    |> Array.map _.Title
    |> mostPopulous count asLower
    |> Array.map (fun (title, count) -> [| title; formatInt count |])

let topGenres count (tags: LibraryTags array) =
    tags
    |> Array.map _.Genres
    |> Array.collect id
    |> mostPopulous count asLower
    |> Array.map (fun (genre, count) -> [| genre; formatInt count |])

let largestFiles count (tags: LibraryTags array) =
    tags
    |> Array.sortByDescending _.FileSize
    |> Array.truncate count
    |> Array.map (fun file ->
        let artist = String.concat ", " file.Artists
        [| $"{artist} / {file.Title}"; formatBytes file.FileSize |])

let topBitRates count tags =
    tags
    |> Array.map _.BitRate
    |> mostPopulous count id
    |> Array.map (fun (bitrate, count) -> [|$"{bitrate} kbps"; formatInt count|])

let topSampleRates count tags =
    tags
    |> Array.map _.SampleRate
    |> mostPopulous count id
    |> Array.map (fun (sampleRate, count) -> [|$"{formatInt sampleRate}"; formatInt count|])

let topFormats count tags =
    tags
    |> Array.map (fun t -> (Path.GetExtension t.FileName)[1..] |> _.ToUpperInvariant())
    |> mostPopulous count id
    |> Array.map (fun (ext, count) -> [|$"{ext}"; formatInt count|])

let topQualityData count tags =
    tags
    |> Array.map (fun t -> {| BitRate = t.BitRate
                              SampleRate = t.SampleRate
                              Extension = (Path.GetExtension t.FileName)[1..] |> _.ToUpperInvariant() |} )
    |> mostPopulous count id
    |> Array.map (fun (data, count) ->
        [|
           data.Extension
           $"{data.BitRate} kbps"
           formatInt data.SampleRate
           formatInt count
        |])

