module LibraryAnalysis.Analysis

open Shared.TagLibrary
open Shared.Types
open CCFSharpUtils
open CCFSharpUtils.Collections
open CCFSharpUtils.Text
open FSharpPlus.Data
open FSharpPlus.Operators
open System.IO

type TableRowData = string list nlist option
type RatioData = { Count: int; Total: int; DecimalPlaces: int }

let inline private mostPopulous count (grouper: 'a -> 'a) (items: 'a nlist) =
    items
    |> NonEmptyList.groupBy grouper
    |> NonEmptyList.map (fun (_, group) -> (group[0], group.Length))
    |> NonEmptyList.sortByDescending snd
    |> NonEmptyList.truncate count

let private asPercentage ratioData =
    float ratioData.Count / float ratioData.Total
    |> String.formatPercent ratioData.DecimalPlaces

let filteredArtists tags =
    tags
    |> NonEmptyList.map (fun tags' ->
        tags'
        |> allDistinctArtists
        |> List.map (fun (Artist artistName) -> artistName)
        |> List.except ignorableAlbumArtistNames)
    |> List.concat
    |> function [] -> None | artists -> Some (NonEmptyList.ofList artists)

let uniqueArtistCount tags =
    tags
    |> filteredArtists
    |> Option.map NonEmptyList.distinct
    |> Option.map _.Length
    |> Option.defaultValue 0

let averageFileSize tags =
    let sizeTotal = tags |> NonEmptyList.map _.FileSize |> NonEmptyList.sum
    let fileCount = tags.Length
    sizeTotal / (int64 fileCount)

let albumArtPercentage tags =
    tags
    |> NonEmptyList.choose (fun t -> Option.isPos t.ImageCount)
    |> NonEmptyList.length
    |> fun count -> asPercentage { Count = count; Total = tags.Length; DecimalPlaces = 2 }

let topArtists count tags : TableRowData =
    filteredArtists tags
    |> Option.map (fun artists ->
        let artistCount = artists.Length

        artists
        |> mostPopulous count id
        |> NonEmptyList.map (fun (artist, count) ->
                [ artist
                  String.formatInt count
                  asPercentage { Count = count; Total = artistCount; DecimalPlaces = 3 } ]) )

let topAlbums count tags : TableRowData =
    let albums = tags |> NonEmptyList.map _.Album
    let albumCount = albums.Length

    albums
    |> mostPopulous count id
    |> NonEmptyList.map (fun (album, count) ->
        [ album
          String.formatInt count
          asPercentage { Count = count; Total = albumCount; DecimalPlaces = 2 } ])
    |> Some

let topTitles count tags : TableRowData =
    tags
    |> NonEmptyList.map _.Title
    |> mostPopulous count String.toLower
    |> NonEmptyList.map (fun (title, count) -> [ title; String.formatInt count ])
    |> Some

let topGenres count tags : TableRowData =
    let genres = tags |> NonEmptyList.map (fun xs -> xs.Genres |> List.ofArray)
    let genreCount = genres.Length

    // TODO: Clean this up.
    genres
    |> NonEmptyList.toList
    |> List.collect id
    |> NonEmptyList.ofList
    |> mostPopulous count String.toLower
    |> NonEmptyList.map (fun (genre, count) ->
        [ genre
          String.formatInt count
          asPercentage { Count = count; Total = genreCount; DecimalPlaces = 2 } ])
    |> Some

let artistsWithMostGenres count tags : TableRowData =
    let genreCounts (genres: string list) : string =
        genres
        |> List.countBy id
        |> List.sortBy fst
        |> List.map (fun (g, count) -> $"{g} ({count})")
        |> String.concat "; "

    let extractArtistGenreInfo (a, tags) =
        let genres = tags |> NonEmptyList.map _.Genres |> Array.concat |> Array.map _.Trim() |> List.ofArray
        let uniqGenreCount genres = genres |> List.distinctIgnoreCase |> _.Length
        (a, uniqGenreCount genres, genres)

    tags
    |> NonEmptyList.tryFilter hasAnyArtist
    |> Option.map (NonEmptyList.groupBy firstDistinctArtist)
    |> Option.map (NonEmptyList.map extractArtistGenreInfo)
    |> Option.map (NonEmptyList.sortByDescending item2)
    |> Option.map (NonEmptyList.take count)
    |> Option.map (NonEmptyList.map (fun (Artist artist, uniqGenreCount, genres) ->
        [ artist
          String.formatInt uniqGenreCount
          genreCounts genres ]))

let largestFiles count tags : TableRowData =
    tags
    |> NonEmptyList.sortByDescending _.FileSize
    |> NonEmptyList.truncate count
    |> NonEmptyList.map (fun file ->
        let artist = String.concat ", " file.Artists
        [ $"{artist} / {file.Title}"
          String.formatBytes file.FileSize ])
    |> Some

let uppercaseFileExtension tagFile =
    ((Path.GetExtension tagFile.FileName)[1..]).ToUpperInvariant()

let topFormats count tags : TableRowData =
    tags
    |> NonEmptyList.map uppercaseFileExtension
    |> mostPopulous count id
    |> NonEmptyList.map (fun (ext, count) -> [ $"{ext}"; String.formatInt count ])
    |> Some

let topBitRates count tags : TableRowData =
    tags
    |> NonEmptyList.map _.BitRate
    |> mostPopulous count id
    |> NonEmptyList.map (fun (bitrate, count) ->
        [ $"{bitrate} kbps"
          String.formatInt count ])
    |> Some

let topSampleRates count tags : TableRowData =
    tags
    |> NonEmptyList.map _.SampleRate
    |> mostPopulous count id
    |> NonEmptyList.map (fun (sampleRate, count) ->
        [ $"{String.formatInt sampleRate}"
          String.formatInt count ])
    |> Some

let topQualityData count tags : TableRowData =
    tags
    |> NonEmptyList.map (fun t ->
        {| BitRate    = t.BitRate
           SampleRate = t.SampleRate
           Extension  = uppercaseFileExtension t |})
    |> mostPopulous count id
    |> NonEmptyList.map (fun (data, count) ->
        [ data.Extension
          $"{data.BitRate} kbps"
          String.formatInt data.SampleRate
          String.formatInt count ])
    |> Some

let longestFileNames count tags : TableRowData =
    tags
    |> NonEmptyList.map (fun t -> t.FileName.Length, t)
    |> NonEmptyList.sortByDescending fst
    |> NonEmptyList.take count
    |> NonEmptyList.map (fun (count, t) ->
        [ $"""{mainArtists "; " t}{String.nl}↪︎ {t.Title}"""
          t.FileName
          String.formatInt count ])
    |> Some
