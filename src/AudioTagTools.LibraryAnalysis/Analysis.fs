module LibraryAnalysis.Analysis

open System.IO
open Shared.TagLibrary
open CCFSharpUtils.Library

let inline private mostPopulous count (grouper: 'a -> 'a) (items: 'a array) =
    items
    |> Array.groupBy grouper
    |> Array.map (fun (_, group) -> (group[0], group.Length))
    |> Array.sortByDescending snd
    |> Array.truncate count

let private asLower (x: string) = x.ToLowerInvariant()

let private asPercentage count totalCount decimalPlaces =
    float count / float totalCount
    |> String.formatPercent decimalPlaces

let averageFileSize tags =
    let sizeTotal = tags |> Array.map _.FileSize |> Array.sum
    let fileCount = tags.Length
    sizeTotal / (int64 fileCount)

let albumArtPercentage tags =
    tags
    |> Array.choose (fun t -> if t.ImageCount > 0 then Some t.ImageCount else None)
    |> Array.length
    |> fun count -> asPercentage count tags.Length 2

let filteredArtists tags =
    tags
    |> Array.map (fun t -> t |> allDistinctArtists |> Array.except ignorableArtists)
    |> Array.concat

let uniqueArtistCount tags =
    tags |> filteredArtists |> Array.distinct |> _.Length

let topArtists count tags =
    let artists = filteredArtists tags
    let artistCount = artists.Length

    artists
    |> mostPopulous count id
    |> Array.map (fun (artist, count) ->
        [| artist
           String.formatInt count
           asPercentage count artistCount 3 |])

let topAlbums count tags =
    let albums = tags |> Array.map _.Album
    let albumCount = albums.Length

    albums
    |> mostPopulous count id
    |> Array.map (fun (album, count) ->
        [| album
           String.formatInt count
           asPercentage count albumCount 2 |])

let topTitles count tags =
    tags
    |> Array.map _.Title
    |> mostPopulous count asLower
    |> Array.map (fun (title, count) -> [| title; String.formatInt count |])

let topGenres count tags =
    let genres = tags |> Array.map _.Genres
    let genreCount = genres.Length

    genres
    |> Array.collect id
    |> mostPopulous count asLower
    |> Array.map (fun (genre, count) ->
        [| genre
           String.formatInt count
           asPercentage count genreCount 2 |])

let artistsWithMostGenres count tags =
    let genreCounts (genres: string array) : string =
        genres
        |> Array.countBy id
        |> Array.sortBy fst
        |> Array.map (fun (g, count) -> $"{g} ({count})")
        |> String.concat "; "

    let artistsWithTheirGenres (a, tags) : 'a * string array =
        (a, tags
            |> Array.map _.Genres
            |> Array.concat
            |> Array.map _.Trim())

    let uniqGenreCount (genres: string array) =
        genres |> Array.distinctBy _.ToLowerInvariant() |> _.Length

    tags
    |> Array.filter hasAnyArtist
    |> Array.groupBy firstDistinctArtist
    |> Array.map artistsWithTheirGenres
    |> Array.map (fun (a, gs) -> a, uniqGenreCount gs, gs)
    |> Array.sortByDescending (fun (_, uniqGenreCount, _) -> uniqGenreCount)
    |> Array.take count
    |> Array.map (fun (a, uniqGenreCount, gs) -> [| a; String.formatInt uniqGenreCount; genreCounts gs |])

let largestFiles count tags =
    tags
    |> Array.sortByDescending _.FileSize
    |> Array.truncate count
    |> Array.map (fun file ->
        let artist = String.concat ", " file.Artists
        [| $"{artist} / {file.Title}"; String.formatBytes file.FileSize |])

let topBitRates count tags =
    tags
    |> Array.map _.BitRate
    |> mostPopulous count id
    |> Array.map (fun (bitrate, count) -> [|$"{bitrate} kbps"; String.formatInt count|])

let topSampleRates count tags =
    tags
    |> Array.map _.SampleRate
    |> mostPopulous count id
    |> Array.map (fun (sampleRate, count) -> [|$"{String.formatInt sampleRate}"; String.formatInt count|])

let uppercaseFileExtension tagFile =
    ((Path.GetExtension tagFile.FileName)[1..]).ToUpperInvariant()

let topFormats count tags =
    tags
    |> Array.map uppercaseFileExtension
    |> mostPopulous count id
    |> Array.map (fun (ext, count) -> [| $"{ext}"; String.formatInt count |])

let topQualityData count tags =
    tags
    |> Array.map (fun t ->
        {| BitRate = t.BitRate
           SampleRate = t.SampleRate
           Extension = uppercaseFileExtension t |})
    |> mostPopulous count id
    |> Array.map (fun (data, count) ->
        [| data.Extension
           $"{data.BitRate} kbps"
           String.formatInt data.SampleRate
           String.formatInt count |])

