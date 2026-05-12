module LibraryAnalysis.Analysis

open System.IO
open Shared.TagLibrary
open Shared.Types
open FSharpPlus.Operators
open CCFSharpUtils
open CCFSharpUtils.Text

type RatioData = { Count: int; Total: int; DecimalPlaces: int }

let inline private mostPopulous count (grouper: 'a -> 'a) (items: 'a list) =
    items
    |> List.groupBy grouper
    |> List.map (fun (_, group) -> (group[0], group.Length))
    |> List.sortByDescending snd
    |> List.truncate count

let private asLower (x: string) = x.ToLowerInvariant()

let private asPercentage ratioData =
    float ratioData.Count / float ratioData.Total
    |> String.formatPercent ratioData.DecimalPlaces

let averageFileSize tags =
    let sizeTotal = tags |> List.map _.FileSize |> List.sum
    let fileCount = tags.Length
    sizeTotal / (int64 fileCount)

let albumArtPercentage tags =
    tags
    |> List.choose (fun t -> Option.isPos t.ImageCount)
    |> List.length
    |> fun count -> asPercentage { Count = count; Total = tags.Length; DecimalPlaces = 2 }

let filteredArtists tags =
    tags
    |> List.map (fun tags' ->
        tags'
        |> allDistinctArtists
        |> List.map (fun (Artist artistName) -> artistName)
        |> List.except ignorableAlbumArtistNames)
    |> List.concat

let uniqueArtistCount tags =
    tags |> filteredArtists |> List.distinct |> _.Length

let topArtists count tags =
    let artists = filteredArtists tags
    let artistCount = artists.Length

    artists
    |> mostPopulous count id
    |> List.map (fun (artist, count) ->
        [ artist
          String.formatInt count
          asPercentage { Count = count; Total = artistCount; DecimalPlaces = 3 } ])

let topAlbums count tags =
    let albums = tags |> List.map _.Album
    let albumCount = albums.Length

    albums
    |> mostPopulous count id
    |> List.map (fun (album, count) ->
        [ album
          String.formatInt count
          asPercentage { Count = count; Total = albumCount; DecimalPlaces = 2 } ])

let topTitles count tags =
    tags
    |> List.map _.Title
    |> mostPopulous count asLower
    |> List.map (fun (title, count) -> [ title; String.formatInt count ])

let topGenres count tags =
    let genres = tags |> List.map (fun xs -> xs.Genres |> List.ofArray)
    let genreCount = genres.Length

    genres
    |> List.collect id
    |> mostPopulous count asLower
    |> List.map (fun (genre, count) ->
        [ genre
          String.formatInt count
          asPercentage { Count = count; Total = genreCount; DecimalPlaces = 2 } ])

let artistsWithMostGenres count tags =
    let genreCounts (genres: string list) : string =
        genres
        |> List.countBy id
        |> List.sortBy fst
        |> List.map (fun (g, count) -> $"{g} ({count})")
        |> String.concat "; "

    let extractArtistGenreInfo (a, tags) =
        let genres: string list =
            tags
            |> List.map _.Genres
            |> Array.concat
            |> Array.map _.Trim()
            |> List.ofArray

        let uniqGenreCount (genres: string list) : int =
            genres
            |> List.distinctBy _.ToLowerInvariant()
            |> _.Length

        (a, uniqGenreCount genres, genres)

    tags
    |> List.filter hasAnyArtist
    |> List.groupBy firstDistinctArtist
    |> List.map extractArtistGenreInfo
    |> List.sortByDescending item2
    |> List.take count
    |> List.map (fun (Artist artist, uniqGenreCount, genres) ->
        [ artist
          String.formatInt uniqGenreCount
          genreCounts genres ])

let largestFiles count tags =
    tags
    |> List.sortByDescending _.FileSize
    |> List.truncate count
    |> List.map (fun file ->
        let artist = String.concat ", " file.Artists
        [ $"{artist} / {file.Title}"
          String.formatBytes file.FileSize ])

let topBitRates count tags =
    tags
    |> List.map _.BitRate
    |> mostPopulous count id
    |> List.map (fun (bitrate, count) ->
        [ $"{bitrate} kbps"
          String.formatInt count ])

let topSampleRates count tags =
    tags
    |> List.map _.SampleRate
    |> mostPopulous count id
    |> List.map (fun (sampleRate, count) ->
        [ $"{String.formatInt sampleRate}"
          String.formatInt count ])

let uppercaseFileExtension tagFile =
    ((Path.GetExtension tagFile.FileName)[1..]).ToUpperInvariant()

let topFormats count tags =
    tags
    |> List.map uppercaseFileExtension
    |> mostPopulous count id
    |> List.map (fun (ext, count) -> [ $"{ext}"; String.formatInt count ])

let topQualityData count tags =
    tags
    |> List.map (fun t ->
        {| BitRate = t.BitRate
           SampleRate = t.SampleRate
           Extension = uppercaseFileExtension t |})
    |> mostPopulous count id
    |> List.map (fun (data, count) ->
        [ data.Extension
          $"{data.BitRate} kbps"
          String.formatInt data.SampleRate
          String.formatInt count ])

let longestFileNames count tags =
    tags
    |> List.map (fun t -> t.FileName.Length, t)
    |> List.sortByDescending fst
    |> List.take count
    |> List.map (fun (count, t) ->
        [ $"""{mainArtists "; " t}{String.nl}↪︎ {t.Title}"""
          t.FileName
          String.formatInt count ])
